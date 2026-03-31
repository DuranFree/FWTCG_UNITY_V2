using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FWTCG.UI
{
    /// <summary>
    /// Screen-center small event banner for game event feedback (DEV-18b).
    ///
    /// Receives events from GameEventBus.OnEventBanner.
    /// Queues banners so they never overlap.
    /// Each banner slides in from above (0.2s), stays, then fades out (0.25s).
    ///
    /// Assign in SceneBuilder as a child of Canvas root.
    /// The panel is always Active; visibility is controlled by CanvasGroup alpha.
    /// </summary>
    public class EventBanner : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [SerializeField] private Text _bannerText;
        [SerializeField] private Image _bannerBg;
        [SerializeField] private RectTransform _bannerRT;

        // ── State ─────────────────────────────────────────────────────────────
        private readonly Queue<(string text, float duration, bool large)> _queue
            = new Queue<(string, float, bool)>();
        private Coroutine _showRoutine;
        private CanvasGroup _cg;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            _cg = GetComponent<CanvasGroup>();
            if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
            _cg.alpha = 0f;
            _cg.blocksRaycasts = false;

            if (_bannerRT == null) _bannerRT = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            GameEventBus.OnEventBanner += EnqueueBanner;
        }

        private void OnDisable()
        {
            GameEventBus.OnEventBanner -= EnqueueBanner;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Called by GameEventBus subscription to queue a banner.</summary>
        public void EnqueueBanner(string text, float duration, bool large)
        {
            _queue.Enqueue((text, duration, large));
            if (_showRoutine == null)
                _showRoutine = StartCoroutine(DrainQueue());
        }

        // ── Animation ─────────────────────────────────────────────────────────

        private IEnumerator DrainQueue()
        {
            while (_queue.Count > 0)
            {
                var (text, duration, large) = _queue.Dequeue();
                yield return StartCoroutine(ShowOne(text, duration, large));
            }
            _showRoutine = null;
        }

        private IEnumerator ShowOne(string text, float duration, bool large)
        {
            // Set content
            if (_bannerText != null)
            {
                _bannerText.text = text;
                _bannerText.fontSize = large ? 26 : 20;
                _bannerText.color = large ? GameColors.BannerYellow : GameColors.GoldLight;
            }

            if (_bannerBg != null)
            {
                _bannerBg.color = large
                    ? new Color(0.05f, 0.02f, 0f, 0.92f)
                    : new Color(0.02f, 0.05f, 0.1f, 0.88f);
            }

            // Slide in from above (+30px offset → 0) while fading in
            Vector2 origin = _bannerRT != null ? _bannerRT.anchoredPosition : Vector2.zero;
            Vector2 above = origin + new Vector2(0f, 30f);

            yield return StartCoroutine(AnimateIn(above, origin));

            // Stay
            _cg.alpha = 1f;
            yield return new WaitForSeconds(duration);

            // Fade out
            yield return StartCoroutine(AnimateOut());
        }

        private IEnumerator AnimateIn(Vector2 fromPos, Vector2 toPos)
        {
            const float dur = 0.2f;
            float elapsed = 0f;
            if (_bannerRT != null) _bannerRT.anchoredPosition = fromPos;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                _cg.alpha = t;
                if (_bannerRT != null)
                    _bannerRT.anchoredPosition = Vector2.Lerp(fromPos, toPos, t * t * (3f - 2f * t));
                yield return null;
            }
            if (_bannerRT != null) _bannerRT.anchoredPosition = toPos;
            _cg.alpha = 1f;
        }

        private IEnumerator AnimateOut()
        {
            const float dur = 0.25f;
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                _cg.alpha = Mathf.Clamp01(1f - elapsed / dur);
                yield return null;
            }
            _cg.alpha = 0f;
        }
    }
}
