using UnityEngine;
using UnityEngine.UI;

namespace FWTCG.UI
{
    /// <summary>
    /// VFX-7q: Crosshair/target ring at mouse position during target-requiring card drag.
    /// Uses target.png sprite with pulsating alpha.
    /// </summary>
    public class AimTargetFX : MonoBehaviour
    {
        public const float PULSE_FREQ = 2f; // Hz
        public const float ALPHA_MIN = 0.4f;
        public const float ALPHA_MAX = 0.8f;
        public const float TARGET_SIZE = 48f;

        private GameObject _targetGO;
        private Image _targetImg;
        private RectTransform _targetRT;
        private RectTransform _canvasRT;
        private Canvas _rootCanvas;
        private bool _active;

        private void Awake()
        {
            _rootCanvas = GetComponentInParent<Canvas>();
            if (_rootCanvas != null) _rootCanvas = _rootCanvas.rootCanvas;
            _canvasRT = _rootCanvas?.GetComponent<RectTransform>();
            CreateTarget();
        }

        private void CreateTarget()
        {
            _targetGO = new GameObject("AimTarget");
            _targetGO.transform.SetParent(transform, false);
            _targetRT = _targetGO.AddComponent<RectTransform>();
            _targetRT.sizeDelta = new Vector2(TARGET_SIZE, TARGET_SIZE);
            _targetRT.anchorMin = _targetRT.anchorMax = new Vector2(0.5f, 0.5f);
            _targetImg = _targetGO.AddComponent<Image>();
            var spr = Resources.Load<Sprite>("FX/target"); // from Assets/Sprites/FX/target.png if in Resources
            if (spr == null) spr = Resources.Load<Sprite>("Prefabs/FX/target");
            _targetImg.sprite = spr;
            _targetImg.color = new Color(1f, 1f, 1f, 0f);
            _targetImg.raycastTarget = false;
            _targetImg.preserveAspect = true;
            _targetGO.SetActive(false);
        }

        public void Activate()
        {
            _active = true;
            _targetGO.SetActive(true);
        }

        public void Deactivate()
        {
            _active = false;
            if (_targetGO != null) _targetGO.SetActive(false);
        }

        private void Update()
        {
            if (!_active || _canvasRT == null || _targetRT == null) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRT, Input.mousePosition, _rootCanvas?.worldCamera, out Vector2 mouseLocal);
            _targetRT.anchoredPosition = mouseLocal;

            float pulse = (Mathf.Sin(Time.time * PULSE_FREQ * Mathf.PI * 2f) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(ALPHA_MIN, ALPHA_MAX, pulse);
            _targetImg.color = new Color(1f, 1f, 1f, alpha);
        }

        public bool IsActive => _active;

        private void OnDestroy()
        {
            if (_targetGO != null) Destroy(_targetGO);
        }
    }
}
