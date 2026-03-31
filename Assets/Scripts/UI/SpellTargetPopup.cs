using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using FWTCG.Core;
using FWTCG.Data;

namespace FWTCG.UI
{
    /// <summary>
    /// Modal popup for selecting a spell target.
    /// Shows valid targets split into enemy/player sections.
    /// ShowAsync resolves with the chosen UnitInstance, or null if cancelled.
    /// </summary>
    public class SpellTargetPopup : MonoBehaviour
    {
        public static SpellTargetPopup Instance { get; private set; }

        [SerializeField] private CanvasGroup   _canvasGroup;
        [SerializeField] private Transform     _enemyContainer;   // parent for enemy unit buttons
        [SerializeField] private Transform     _playerContainer;  // parent for player unit buttons
        [SerializeField] private Button        _cancelBtn;

        private TaskCompletionSource<UnitInstance> _tcs;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (_cancelBtn != null) _cancelBtn.onClick.AddListener(CancelSelection);
            HidePanel();
        }

        private void HidePanel()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha          = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable   = false;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Show the target selection popup.
        /// Returns the chosen unit, or null if cancelled / no valid targets.
        /// </summary>
        public Task<UnitInstance> ShowAsync(SpellTargetType targetType, GameState gs)
        {
            _tcs = new TaskCompletionSource<UnitInstance>();

            // Collect valid targets
            var enemyUnits  = new List<UnitInstance>();
            var playerUnits = new List<UnitInstance>();

            if (targetType == SpellTargetType.EnemyUnit || targetType == SpellTargetType.AnyUnit)
            {
                enemyUnits.AddRange(gs.EBase);
                foreach (var bf in gs.BF) enemyUnits.AddRange(bf.EnemyUnits);
            }
            if (targetType == SpellTargetType.FriendlyUnit || targetType == SpellTargetType.AnyUnit)
            {
                playerUnits.AddRange(gs.PBase);
                foreach (var bf in gs.BF) playerUnits.AddRange(bf.PlayerUnits);
            }

            if (enemyUnits.Count == 0 && playerUnits.Count == 0)
            {
                _tcs.TrySetResult(null);
                return _tcs.Task;
            }

            BuildSection(_enemyContainer,  enemyUnits,  new Color(0.85f, 0.28f, 0.28f));
            BuildSection(_playerContainer, playerUnits, new Color(0.22f, 0.72f, 0.38f));

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha          = 1f;
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.interactable   = true;
            }

            return _tcs.Task;
        }

        public void CancelSelection()
        {
            Close();
            _tcs?.TrySetResult(null);
        }

        // ── Internal ───────────────────────────────────────────────────────────

        private void BuildSection(Transform container, List<UnitInstance> units, Color btnColor)
        {
            if (container == null) return;

            // Clear previous buttons
            for (int i = container.childCount - 1; i >= 0; i--)
                Destroy(container.GetChild(i).gameObject);

            foreach (var unit in units)
            {
                var u   = unit; // closure capture
                var go  = new GameObject(u.UnitName);
                go.transform.SetParent(container, false);

                var le  = go.AddComponent<LayoutElement>();
                le.preferredHeight = 38f;
                le.flexibleWidth   = 1f;

                var img = go.AddComponent<Image>();
                img.color = btnColor;

                var btn = go.AddComponent<Button>();
                var cb  = btn.colors;
                cb.highlightedColor = Color.white;
                cb.normalColor      = btnColor;
                btn.colors = cb;
                btn.onClick.AddListener(() => SelectTarget(u));

                var lblGO = new GameObject("Label");
                lblGO.transform.SetParent(go.transform, false);
                var lblRT = lblGO.AddComponent<RectTransform>();
                lblRT.anchorMin  = Vector2.zero;
                lblRT.anchorMax  = Vector2.one;
                lblRT.offsetMin  = new Vector2(4f, 0f);
                lblRT.offsetMax  = new Vector2(-4f, 0f);
                var lbl = lblGO.AddComponent<Text>();
                lbl.text              = $"{u.UnitName}  [{u.CurrentAtk}/{u.CurrentHp}]";
                lbl.color             = Color.white;
                lbl.fontSize          = 14;
                lbl.alignment         = TextAnchor.MiddleCenter;
                lbl.horizontalOverflow = HorizontalWrapMode.Overflow;
                lbl.verticalOverflow  = VerticalWrapMode.Overflow;
            }
        }

        private void SelectTarget(UnitInstance unit)
        {
            Close();
            _tcs?.TrySetResult(unit);
        }

        private void Close()
        {
            HidePanel();
        }
    }
}
