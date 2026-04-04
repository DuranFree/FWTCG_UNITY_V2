using UnityEngine;
using UnityEngine.UI;

namespace FWTCG.UI
{
    /// <summary>
    /// VFX-7b: Discrete icon bar — N icon Images, filled (bright) or empty (dim).
    /// Used for mana/sch display replacing pure text.
    /// </summary>
    public class IconBar : MonoBehaviour
    {
        [SerializeField] private Sprite _fullSprite;
        [SerializeField] private Sprite _emptySprite;
        [SerializeField] private int _maxIcons = 10;

        private Image[] _icons;
        private int _currentValue;
        private int _currentMax;

        public const float ICON_SIZE = 16f;
        public const float ICON_SPACING = 2f;
        public const float FULL_ALPHA = 1f;
        public const float EMPTY_ALPHA = 0.3f;

        private void Awake()
        {
            EnsureIcons();
        }

        private void EnsureIcons()
        {
            if (_icons != null) return;
            _icons = new Image[_maxIcons];
            for (int i = 0; i < _maxIcons; i++)
            {
                var go = new GameObject($"Icon_{i}");
                go.transform.SetParent(transform, false);
                var rt = go.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(ICON_SIZE, ICON_SIZE);
                rt.anchoredPosition = new Vector2(i * (ICON_SIZE + ICON_SPACING), 0f);
                var img = go.AddComponent<Image>();
                img.raycastTarget = false;
                img.preserveAspect = true;
                img.sprite = _emptySprite;
                img.color = new Color(1f, 1f, 1f, EMPTY_ALPHA);
                go.SetActive(false);
                _icons[i] = img;
            }
        }

        /// <summary>Set the displayed value. Icons 0..value-1 are full, value..max-1 are empty.</summary>
        public void SetValue(int value, int max)
        {
            EnsureIcons();
            _currentValue = Mathf.Clamp(value, 0, _maxIcons);
            _currentMax = Mathf.Clamp(max, 0, _maxIcons);

            for (int i = 0; i < _maxIcons; i++)
            {
                bool visible = i < _currentMax;
                _icons[i].gameObject.SetActive(visible);
                if (!visible) continue;

                bool filled = i < _currentValue;
                _icons[i].sprite = filled ? _fullSprite : _emptySprite;
                _icons[i].color = new Color(1f, 1f, 1f, filled ? FULL_ALPHA : EMPTY_ALPHA);
            }
        }

        public int CurrentValue => _currentValue;
        public int CurrentMax => _currentMax;
        public int MaxIcons => _maxIcons;
    }
}
