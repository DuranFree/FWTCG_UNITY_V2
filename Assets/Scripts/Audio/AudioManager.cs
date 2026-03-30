using UnityEngine;

namespace FWTCG.Audio
{
    /// <summary>
    /// Centralized audio manager for FWTCG.
    /// Placeholder implementation — plays audio clips via AudioSource.
    /// All SFX methods are safe to call even without assigned clips (they just skip).
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource _bgmSource;
        [SerializeField] private AudioSource _sfxSource;

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

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (_bgmSource == null)
            {
                _bgmSource = gameObject.AddComponent<AudioSource>();
                _bgmSource.loop = true;
                _bgmSource.playOnAwake = false;
            }
            if (_sfxSource == null)
            {
                _sfxSource = gameObject.AddComponent<AudioSource>();
                _sfxSource.playOnAwake = false;
            }
        }

        private void Start()
        {
            PlayBGM();
        }

        // ── BGM ──────────────────────────────────────────────────────────────

        public void PlayBGM()
        {
            if (_bgmSource == null || _bgmClip == null) return;
            _bgmSource.clip = _bgmClip;
            _bgmSource.volume = _bgmVolume;
            _bgmSource.Play();
        }

        public void StopBGM()
        {
            if (_bgmSource != null) _bgmSource.Stop();
        }

        // ── SFX ──────────────────────────────────────────────────────────────

        public void PlayCardPlay()    => PlaySfx(_cardPlaySfx);
        public void PlaySpellCast()   => PlaySfx(_spellCastSfx);
        public void PlayCombatHit()   => PlaySfx(_combatHitSfx);
        public void PlayUnitDeath()   => PlaySfx(_unitDeathSfx);
        public void PlayTurnEnd()     => PlaySfx(_turnEndSfx);
        public void PlayGameOverWin() => PlaySfx(_gameOverWinSfx);
        public void PlayGameOverLose()=> PlaySfx(_gameOverLoseSfx);
        public void PlayUIClick()     => PlaySfx(_uiClickSfx);
        public void PlayScore()       => PlaySfx(_scoreSfx);

        private void PlaySfx(AudioClip clip)
        {
            if (_sfxSource == null || clip == null) return;
            _sfxSource.PlayOneShot(clip, _sfxVolume);
        }

        // ── Volume control ───────────────────────────────────────────────────

        public void SetBGMVolume(float vol)
        {
            _bgmVolume = Mathf.Clamp01(vol);
            if (_bgmSource != null) _bgmSource.volume = _bgmVolume;
        }

        public void SetSFXVolume(float vol)
        {
            _sfxVolume = Mathf.Clamp01(vol);
        }
    }
}
