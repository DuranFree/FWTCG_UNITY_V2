using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FWTCG.UI
{
    /// <summary>
    /// Screen-center small event banner for game event feedback (DEV-18b).
    ///
    /// Dedup + fast response:
    ///   - Same text currently showing → resets stay timer (no re-queue)
    ///   - Same text already in queue → dropped
    ///   - Queue cap MAX_QUEUE: extras dropped
    ///   - ClearAll: subscribed to GameEventBus.OnClearBanners (fires on turn start)
    ///   - Animation speed: slide-in 0.1s / slide-out 0.12s (halved from original 0.2/0.25s)
    /// </summary>
    public class EventBanner : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [SerializeField] private Text          _bannerText;
        [SerializeField] private Image         _bannerBg;
        [SerializeField] private RectTransform _bannerRT;

        // ── State ─────────────────────────────────────────────────────────────
        private readonly Queue<(string text, float duration, bool large)> _queue
            = new Queue<(string, float, bool)>();
        private Coroutine   _showRoutine;
        private CanvasGroup _cg;
        private string      _currentText;
        private bool        _extendCurrent;

        private const int   MAX_QUEUE = 4;
        private const float ANIM_IN   = 0.1f;   // was 0.2s
        private const float ANIM_OUT  = 0.12f;  // was 0.25s

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
            GameEventBus.OnEventBanner  += EnqueueBanner;
            GameEventBus.OnClearBanners += ClearAll;
        }

        private void OnDisable()
        {
            GameEventBus.OnEventBanner  -= EnqueueBanner;
            GameEventBus.OnClearBanners -= ClearAll;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Called by GameEventBus subscription to queue a banner.</summary>
        public void EnqueueBanner(string text, float duration, bool large)
        {
            // Same text currently showing → reset its stay timer
            if (_showRoutine != null && text == _currentText)
            {
                _extendCurrent = true;
                return;
            }

            // Already in queue → drop duplicate
            foreach (var item in _queue)
                if (item.text == text) return;

            // Queue full → drop
            if (_queue.Count >= MAX_QUEUE) return;

            _queue.Enqueue((text, duration, large));
            if (_showRoutine == null)
                _showRoutine = StartCoroutine(DrainQueue());
        }

        /// <summary>Immediately clears all queued banners and hides (e.g. on turn change).</summary>
        public void ClearAll()
        {
            if (_showRoutine != null)
            {
                StopCoroutine(_showRoutine);
                _showRoutine = null;
            }
            _queue.Clear();
            _currentText   = null;
            _extendCurrent = false;
            if (_cg != null) _cg.alpha = 0f;
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
            _currentText = null;
        }

        private IEnumerator ShowOne(string text, float duration, bool large)
        {
            _currentText   = text;
            _extendCurrent = false;

            // Set content
            if (_bannerText != null)
            {
                _bannerText.text     = text;
                _bannerText.fontSize = large ? 26 : 20;
                _bannerText.color    = large ? GameColors.BannerYellow : GameColors.GoldLight;
            }

            if (_bannerBg != null)
            {
                _bannerBg.color = large
                    ? new Color(0.05f, 0.02f, 0f, 0.92f)
                    : new Color(0.02f, 0.05f, 0.1f, 0.88f);
            }

            // Slide in from above (+30px → 0) while fading in
            Vector2 origin = _bannerRT != null ? _bannerRT.anchoredPosition : Vector2.zero;
            Vector2 above  = origin + new Vector2(0f, 30f);
            yield return StartCoroutine(AnimateIn(above, origin));

            // Stay — resets whenever same text fires again
            _cg.alpha = 1f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (_extendCurrent)
                {
                    elapsed        = 0f;
                    _extendCurrent = false;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Fade out
            yield return StartCoroutine(AnimateOut());
        }

        private IEnumerator AnimateIn(Vector2 fromPos, Vector2 toPos)
        {
            float elapsed = 0f;
            if (_bannerRT != null) _bannerRT.anchoredPosition = fromPos;
            while (elapsed < ANIM_IN)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / ANIM_IN);
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
            float elapsed = 0f;
            while (elapsed < ANIM_OUT)
            {
                elapsed   += Time.deltaTime;
                _cg.alpha  = Mathf.Clamp01(1f - elapsed / ANIM_OUT);
                yield return null;
            }
            _cg.alpha = 0f;
        }
    }
}
