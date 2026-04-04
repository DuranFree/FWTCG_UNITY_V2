using UnityEngine;
using UnityEngine.UI;

namespace FWTCG.Audio
{
    /// <summary>
    /// Auto-attaches a click sound to any Button component.
    /// Add to a GameObject with a Button to get UI click SFX for free.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ButtonAudio : MonoBehaviour
    {
        [Tooltip("Optional override clip. Leave null to use AudioManager default UI click.")]
        [SerializeField] private AudioClip _overrideClip;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button != null)
                _button.onClick.AddListener(OnClick);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
            if (_overrideClip != null)
            {
                if (AudioTool.Instance != null)
                    AudioTool.Instance.PlayOneShot(AudioTool.CH_UI, _overrideClip);
            }
            else
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayUIClick();
            }
        }
    }
}
