using UnityEngine;
using UnityEngine.UI;

namespace FWTCG.UI
{
    /// <summary>
    /// VFX-7p: Dot-chain aim line from source card to mouse position.
    /// Activated during spell/target drag; color reflects validity.
    /// </summary>
    public class MouseLineFX : MonoBehaviour
    {
        public const int DOT_COUNT = 12;
        public const float DOT_SIZE = 6f;
        public const float DOT_MIN_ALPHA = 0.3f;
        public const float DOT_MAX_ALPHA = 0.85f;

        private GameObject[] _dots;
        private RectTransform _canvasRT;
        private Canvas _rootCanvas;
        private bool _active;
        private Vector2 _sourcePos; // canvas-local source position
        private Color _lineColor = Color.green;

        private void Awake()
        {
            _rootCanvas = GetComponentInParent<Canvas>();
            if (_rootCanvas != null) _rootCanvas = _rootCanvas.rootCanvas;
            _canvasRT = _rootCanvas?.GetComponent<RectTransform>();
            CreateDots();
            SetActive(false);
        }

        private void CreateDots()
        {
            _dots = new GameObject[DOT_COUNT];
            for (int i = 0; i < DOT_COUNT; i++)
            {
                var go = new GameObject($"AimDot_{i}");
                go.transform.SetParent(transform, false);
                var rt = go.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(DOT_SIZE, DOT_SIZE);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                var img = go.AddComponent<Image>();
                img.color = Color.white;
                img.raycastTarget = false;
                go.SetActive(false);
                _dots[i] = go;
            }
        }

        public void Activate(Vector2 sourceCanvasPos, bool isLegal)
        {
            _active = true;
            _sourcePos = sourceCanvasPos;
            _lineColor = isLegal ? GameColors.PlayerGreen : GameColors.EnemyRed;
            foreach (var d in _dots) d.SetActive(true);
        }

        public void Deactivate()
        {
            _active = false;
            foreach (var d in _dots) d.SetActive(false);
        }

        public void SetLegal(bool isLegal)
        {
            _lineColor = isLegal ? GameColors.PlayerGreen : GameColors.EnemyRed;
        }

        public void SetActive(bool active)
        {
            if (active) return; // use Activate() with params
            Deactivate();
        }

        private void Update()
        {
            if (!_active || _canvasRT == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRT, Input.mousePosition, _rootCanvas?.worldCamera, out Vector2 mouseLocal);

            for (int i = 0; i < DOT_COUNT; i++)
            {
                float t = (i + 1f) / (DOT_COUNT + 1f);
                Vector2 pos = Vector2.Lerp(_sourcePos, mouseLocal, t);
                var rt = _dots[i].GetComponent<RectTransform>();
                rt.anchoredPosition = pos;

                float alpha = Mathf.Lerp(DOT_MIN_ALPHA, DOT_MAX_ALPHA, t);
                var img = _dots[i].GetComponent<Image>();
                var c = _lineColor;
                c.a = alpha;
                img.color = c;
            }
        }

        public bool IsActive => _active;

        private void OnDestroy()
        {
            if (_dots != null)
                foreach (var d in _dots) if (d != null) Destroy(d);
        }
    }
}
