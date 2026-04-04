using UnityEngine;

namespace FWTCG.Audio
{
    /// <summary>
    /// Backward-compatible audio facade. Delegates to AudioTool channels internally.
    /// All 9 original SFX methods are preserved.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("BGM")]
        [SerializeField] private AudioClip _bgmClip;

        [Header("SFX Clips")]
        [SerializeField] private AudioClip _cardPlaySfx;
        [SerializeField] private AudioClip _spellCastSfx;
        [SerializeField] private AudioClip _combatHitSfx;
        [SerializeField] private AudioClip _unitDeathSfx;
        [SerializeField] private AudioClip _turnEndSfx;
        [SerializeField] private AudioClip _gameOverWinSfx;
        [SerializeField] private AudioClip _gameOverLoseSfx;
        [SerializeField] private AudioClip _uiClickSfx;
        [SerializeField] private AudioClip _scoreSfx;

        [Header("Settings")]
        [Range(0f, 1f)] [SerializeField] private float _bgmVolume = 0.4f;
        [Range(0f, 1f)] [SerializeField] private float _sfxVolume = 0.7f;

        // Fallback sources when AudioTool is not available
        private AudioSource _fallbackBgm;
        private AudioSource _fallbackSfx;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            // Apply volume settings to AudioTool if available
            if (AudioTool.Instance != null)
            {
                AudioTool.Instance.SetChannelVolume(AudioTool.CH_BGM, _bgmVolume);
            }
            PlayBGM();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── BGM ──────────────────────────────────────────────────────────────

        public void PlayBGM()
        {
            if (_bgmClip == null) return;
            if (AudioTool.Instance != null)
            {
                AudioTool.Instance.SetChannelVolume(AudioTool.CH_BGM, _bgmVolume);
                AudioTool.Instance.Play(AudioTool.CH_BGM, _bgmClip);
                return;
            }
            // Fallback
            EnsureFallbackSources();
            _fallbackBgm.clip = _bgmClip;
            _fallbackBgm.volume = _bgmVolume;
            _fallbackBgm.Play();
        }

        public void StopBGM()
        {
            if (AudioTool.Instance != null)
            {
                AudioTool.Instance.StopChannel(AudioTool.CH_BGM);
                return;
            }
            if (_fallbackBgm != null) _fallbackBgm.Stop();
        }

        public void FadeBGMIn(float duration)
        {
            if (AudioTool.Instance != null)
                AudioTool.Instance.FadeIn(AudioTool.CH_BGM, duration);
        }

        public void FadeBGMOut(float duration)
        {
            if (AudioTool.Instance != null)
                AudioTool.Instance.FadeOut(AudioTool.CH_BGM, duration);
        }

        // ── SFX (9 original methods) ────────────────────────────────────────

        public void PlayCardPlay()     => PlaySfx(AudioTool.CH_CARD_SPAWN, _cardPlaySfx);
        public void PlaySpellCast()    => PlaySfx(AudioTool.CH_SPELL,      _spellCastSfx);
        public void PlayCombatHit()    => PlaySfx(AudioTool.CH_ATTACK,     _combatHitSfx);
        public void PlayUnitDeath()    => PlaySfx(AudioTool.CH_DEATH,      _unitDeathSfx);
        public void PlayTurnEnd()      => PlaySfx(AudioTool.CH_SYSTEM,     _turnEndSfx);
        public void PlayGameOverWin()  => PlaySfx(AudioTool.CH_SYSTEM,     _gameOverWinSfx);
        public void PlayGameOverLose() => PlaySfx(AudioTool.CH_SYSTEM,     _gameOverLoseSfx);
        public void PlayUIClick()      => PlaySfx(AudioTool.CH_UI,         _uiClickSfx);
        public void PlayScore()        => PlaySfx(AudioTool.CH_SCORE,      _scoreSfx);

        private void PlaySfx(string channel, AudioClip clip)
        {
            if (clip == null) return;
            if (AudioTool.Instance != null)
            {
                AudioTool.Instance.PlayOneShot(channel, clip);
                return;
            }
            // Fallback
            EnsureFallbackSources();
            _fallbackSfx.PlayOneShot(clip, _sfxVolume);
        }

        // ── Volume control ───────────────────────────────────────────────────

        public void SetBGMVolume(float vol)
        {
            _bgmVolume = Mathf.Clamp01(vol);
            if (AudioTool.Instance != null)
                AudioTool.Instance.SetChannelVolume(AudioTool.CH_BGM, _bgmVolume);
            else if (_fallbackBgm != null)
                _fallbackBgm.volume = _bgmVolume;
        }

        public void SetSFXVolume(float vol)
        {
            _sfxVolume = Mathf.Clamp01(vol);
        }

        // ── Fallback ─────────────────────────────────────────────────────────

        private void EnsureFallbackSources()
        {
            if (_fallbackBgm == null)
            {
                _fallbackBgm = gameObject.AddComponent<AudioSource>();
                _fallbackBgm.loop = true;
                _fallbackBgm.playOnAwake = false;
            }
            if (_fallbackSfx == null)
            {
                _fallbackSfx = gameObject.AddComponent<AudioSource>();
                _fallbackSfx.playOnAwake = false;
            }
        }
    }
}
