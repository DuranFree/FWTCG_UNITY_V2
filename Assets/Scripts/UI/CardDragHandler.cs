using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using FWTCG;
using FWTCG.Core;

namespace FWTCG.UI
{
    /// <summary>
    /// DEV-22: Drag-to-play card handler.
    ///
    /// Attach to the same GameObject as CardView.
    /// Supports:
    ///   - Hand unit card: drag to base zone → play from hand
    ///   - Hand spell card: drag out of hand zone → trigger spell (shows target popup)
    ///   - Base unit: drag to BF zone → move to battlefield
    ///   - Multi-select: when base units are selected, dragging any one clusters all selected
    ///     cards toward the mouse, then drops them all on the target BF zone.
    ///
    /// Static zone RTs are set once by GameUI on startup.
    /// Callbacks route back to GameManager via GameUI.
    /// </summary>
    public class CardDragHandler : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        // ── Static zone refs (set by GameUI.SetupDragZones) ──────────────────
        public static RectTransform HandZoneRT;
        public static RectTransform BaseZoneRT;
        public static RectTransform Bf1ZoneRT;
        public static RectTransform Bf2ZoneRT;
        public static Canvas        RootCanvas;

        // ── Drag callbacks (set by GameUI per refresh cycle) ─────────────────
        // GameUI assigns these when building each CardView so GameManager logic
        // is invoked from the right context.
        public System.Action<UnitInstance>           OnDragToBase;     // hand unit → base
        public System.Action<UnitInstance>           OnSpellDragOut;   // hand spell → released outside hand
        public System.Action<List<UnitInstance>, int> OnDragToBF;      // base units → BF (bfIdx)

        // ── Internal state ───────────────────────────────────────────────────
        private CardView _cardView;
        private RectTransform _rt;
        private bool _isDragging;

        // Ghost image that follows the mouse
        private GameObject _ghost;

        // Cluster: other selected CardDragHandlers follow this drag
        private readonly List<RectTransform> _clusterRTs    = new List<RectTransform>();
        private readonly List<Vector2>       _clusterOrigin = new List<Vector2>();
        private Coroutine _clusterMoveCoroutine;

        // Portal VFX (optional)
        private PortalVFX _portalVFX;

        // Drag source location (populated on BeginDrag)
        private enum DragSource { Hand, Base, Other }
        private DragSource _dragSource;

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            _cardView  = GetComponent<CardView>();
            _rt        = GetComponent<RectTransform>();
            _portalVFX = GetComponent<PortalVFX>();
        }

        private void OnDestroy()
        {
            _isDragging = false;
            RestoreCluster();
            if (_ghost != null) { Destroy(_ghost); _ghost = null; }
            if (_portalVFX != null) _portalVFX.Hide();
        }

        // ── IBeginDragHandler ────────────────────────────────────────────────

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!CanStartDrag()) return;

            _isDragging = true;
            _dragSource = DetectDragSource();

            // Create semi-transparent ghost on top of all UI
            CreateGhost(eventData.position);

            // For multi-select base drags: cluster the other selected cards
            if (_dragSource == DragSource.Base)
                GatherCluster();

            // Show vortex VFX at drag origin
            if (_portalVFX != null)
            {
                Vector2 canvasPos = ScreenToCanvas(eventData.position);
                _portalVFX.Show(canvasPos);
            }
        }

        // ── IDragHandler ─────────────────────────────────────────────────────

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;

            Vector2 canvasPos = ScreenToCanvas(eventData.position);

            // Move ghost
            if (_ghost != null)
                _ghost.GetComponent<RectTransform>().anchoredPosition = canvasPos;

            // Move portal VFX
            if (_portalVFX != null)
                _portalVFX.MoveTo(canvasPos);
        }

        // ── IEndDragHandler ──────────────────────────────────────────────────

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            _isDragging = false;

            // Stop cluster animation & restore original positions
            RestoreCluster();

            // Destroy ghost
            if (_ghost != null) { Destroy(_ghost); _ghost = null; }

            // Hide portal VFX
            if (_portalVFX != null) _portalVFX.Hide();

            // Resolve drop
            HandleDrop(eventData.position);
        }

        // ── Drop resolution ──────────────────────────────────────────────────

        private void HandleDrop(Vector2 screenPos)
        {
            var unit = _cardView != null ? _cardView.Unit : null;
            if (unit == null) return;

            Camera cam = RootCanvas != null ? RootCanvas.worldCamera : null;

            bool overBf1  = Bf1ZoneRT  != null && RectTransformUtility.RectangleContainsScreenPoint(Bf1ZoneRT,  screenPos, cam);
            bool overBf2  = Bf2ZoneRT  != null && RectTransformUtility.RectangleContainsScreenPoint(Bf2ZoneRT,  screenPos, cam);
            bool overBase = BaseZoneRT != null && RectTransformUtility.RectangleContainsScreenPoint(BaseZoneRT, screenPos, cam);
            bool overHand = HandZoneRT != null && RectTransformUtility.RectangleContainsScreenPoint(HandZoneRT, screenPos, cam);

            switch (_dragSource)
            {
                case DragSource.Hand:
                    if (unit.CardData.IsSpell)
                    {
                        // Spell: trigger as long as released outside hand zone
                        if (!overHand)
                            OnSpellDragOut?.Invoke(unit);
                    }
                    else
                    {
                        // Unit: must be dropped on base zone
                        if (overBase)
                            OnDragToBase?.Invoke(unit);
                    }
                    break;

                case DragSource.Base:
                    // Base unit(s): drop on BF0 or BF1
                    int bfIdx = overBf1 ? 0 : overBf2 ? 1 : -1;
                    if (bfIdx >= 0 && OnDragToBF != null)
                    {
                        // Build the full unit list (this card + cluster)
                        var units = BuildDragGroup(unit);
                        OnDragToBF.Invoke(units, bfIdx);
                    }
                    break;
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private bool CanStartDrag()
        {
            if (_cardView == null || _cardView.Unit == null) return false;
            // Only allow drag for player cards in action phase
            var gm = GameManager.Instance;
            if (gm == null) return false;
            return gm.IsPlayerActionPhase();
        }

        private DragSource DetectDragSource()
        {
            var gm = GameManager.Instance;
            if (gm == null) return DragSource.Other;
            var unit = _cardView.Unit;
            if (gm.IsUnitInHand(unit))  return DragSource.Hand;
            if (gm.IsUnitInBase(unit))  return DragSource.Base;
            return DragSource.Other;
        }

        private void CreateGhost(Vector2 screenPos)
        {
            if (RootCanvas == null) return;

            // Clone card GameObject as ghost
            _ghost = Instantiate(gameObject, RootCanvas.transform);
            _ghost.name = "DragGhost";

            // Remove interactive components from ghost
            var dh = _ghost.GetComponent<CardDragHandler>();
            if (dh != null) Destroy(dh);
            var btn = _ghost.GetComponent<Button>();
            if (btn != null) Destroy(btn);
            // Strip PortalVFX so ghost destroy doesn't corrupt the original's VFX children
            var pvfx = _ghost.GetComponent<PortalVFX>();
            if (pvfx != null) Destroy(pvfx);

            // Semi-transparent, non-raycast blocking
            var cg = _ghost.GetComponent<CanvasGroup>() ?? _ghost.AddComponent<CanvasGroup>();
            cg.alpha = 0.72f;
            cg.blocksRaycasts = false;
            cg.interactable   = false;

            // Position ghost at mouse, slightly smaller
            var ghostRT = _ghost.GetComponent<RectTransform>();
            ghostRT.localScale = Vector3.one * 0.88f;
            ghostRT.anchoredPosition = ScreenToCanvas(screenPos);
        }

        // Gather the other selected base unit CardViews into cluster list
        private void GatherCluster()
        {
            _clusterRTs.Clear();
            _clusterOrigin.Clear();

            var gm = GameManager.Instance;
            if (gm == null) return;

            var selected = gm.GetSelectedBaseUnits();
            if (selected == null || selected.Count <= 1) return;

            var thisUnit = _cardView.Unit;

            foreach (var unit in selected)
            {
                if (unit == thisUnit) continue;
                var cv = FindCardViewInScene(unit);
                if (cv == null) continue;
                var rt = cv.GetComponent<RectTransform>();
                if (rt == null) continue;
                _clusterRTs.Add(rt);
                _clusterOrigin.Add(rt.anchoredPosition);
            }

            if (_clusterRTs.Count > 0)
                _clusterMoveCoroutine = StartCoroutine(ClusterFollowRoutine());
        }

        // Animate cluster cards toward the ghost position
        private IEnumerator ClusterFollowRoutine()
        {
            while (_isDragging && _ghost != null)
            {
                // Re-check after null guard to avoid NRE if OnEndDrag fires same frame
                var ghostRT = _ghost != null ? _ghost.GetComponent<RectTransform>() : null;
                if (ghostRT == null) yield break;
                var targetPos = ghostRT.anchoredPosition;

                for (int i = 0; i < _clusterRTs.Count; i++)
                {
                    if (_clusterRTs[i] == null) continue;
                    // Fan slightly offset so cards don't stack perfectly
                    Vector2 offset = new Vector2((i + 1) * 12f, -(i + 1) * 6f);
                    Vector2 dest   = targetPos + offset;
                    _clusterRTs[i].anchoredPosition = Vector2.Lerp(
                        _clusterRTs[i].anchoredPosition, dest, Time.deltaTime * 12f);
                }
                yield return null;
            }
        }

        private void RestoreCluster()
        {
            if (_clusterMoveCoroutine != null)
            {
                StopCoroutine(_clusterMoveCoroutine);
                _clusterMoveCoroutine = null;
            }
            // Snap cluster cards back to their original positions
            for (int i = 0; i < _clusterRTs.Count && i < _clusterOrigin.Count; i++)
            {
                if (_clusterRTs[i] != null)
                    _clusterRTs[i].anchoredPosition = _clusterOrigin[i];
            }
            _clusterRTs.Clear();
            _clusterOrigin.Clear();
        }

        private List<UnitInstance> BuildDragGroup(UnitInstance dragged)
        {
            var gm = GameManager.Instance;
            var selected = gm != null ? gm.GetSelectedBaseUnits() : null;
            if (selected != null && selected.Contains(dragged))
                return new List<UnitInstance>(selected);
            return new List<UnitInstance> { dragged };
        }

        /// <summary>
        /// Searches all card containers in the scene for a CardView matching <paramref name="unit"/>.
        /// Called once per selected unit during cluster gathering — acceptable cost.
        /// </summary>
        private static CardView FindCardViewInScene(UnitInstance unit)
        {
            foreach (var cv in FindObjectsOfType<CardView>())
            {
                if (cv.Unit == unit) return cv;
            }
            return null;
        }

        private Vector2 ScreenToCanvas(Vector2 screenPos)
        {
            if (RootCanvas == null) return Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                RootCanvas.GetComponent<RectTransform>(),
                screenPos,
                RootCanvas.worldCamera,
                out Vector2 local);
            return local;
        }
    }
}
