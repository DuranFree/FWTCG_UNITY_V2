using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FWTCG.UI
{
    /// <summary>
    /// Floating damage number. Spawned at a card's canvas position, floats up and fades out.
    /// Self-destructs after animation. DEV-17.
    /// </summary>
    public class DamagePopup : MonoBehaviour
    {
        private Text _text;
        private RectTransform _rt;

        /// <summary>
        /// Spawns a floating damage number at the given canvas-local position.
        /// Parent should be the root Canvas transform so it renders above everything.
        /// </summary>
        public static DamagePopup Create(int damage, Vector2 canvasLocalPos, Transform canvasRoot)
        {
            var go = new GameObject("DmgPopup");
            go.transform.SetParent(canvasRoot, false);

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(90f, 44f);
            rt.anchoredPosition = canvasLocalPos;
            rt.localScale = Vector3.one;

            var text = go.AddComponent<Text>();
            text.text = $"-{damage}";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 30;
            text.fontStyle = FontStyle.Bold;
            text.color = new Color(1f, 0.18f, 0.18f, 1f);
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;

            // Outline for readability
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            var popup = go.AddComponent<DamagePopup>();
            popup._text = text;
            popup._rt = rt;
            return popup;
        }

        private void Start()
        {
            StartCoroutine(AnimateRoutine());
        }

        private IEnumerator AnimateRoutine()
        {
            Vector2 startPos = _rt.anchoredPosition;
            Vector2 endPos = startPos + new Vector2(0f, 75f);
            const float duration = 0.85f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Float upward with ease-out
                float easedT = 1f - (1f - t) * (1f - t);
                _rt.anchoredPosition = Vector2.Lerp(startPos, endPos, easedT);

                // Stay solid first 40%, then fade out
                float alpha = t < 0.4f ? 1f : 1f - (t - 0.4f) / 0.6f;
                var c = _text.color;
                c.a = Mathf.Clamp01(alpha);
                _text.color = c;

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
