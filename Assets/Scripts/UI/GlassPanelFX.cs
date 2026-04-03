using UnityEngine;
using UnityEngine.UI;

namespace FWTCG.UI
{
    /// <summary>
    /// Applies a simulated frosted-glass material (FWTCG/GlassPanel) to the Image on
    /// this GameObject.  Attach to any panel background Image for the glass look.
    /// Falls back silently if the shader is not found.
    /// DEV-25.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class GlassPanelFX : MonoBehaviour
    {
        [SerializeField] private Color _tintColor   = new Color(0.04f, 0.08f, 0.18f, 0.82f);
        [SerializeField] private Color _borderColor = new Color(0.04f, 0.78f, 0.73f, 0.85f);
        [SerializeField] private float _borderWidth = 0.018f;
        [SerializeField] private float _noiseScale  = 80f;
        [SerializeField] private float _noiseStr    = 0.04f;

        private Material _mat;

        private void Awake() => Apply();

        private void Apply()
        {
            var shader = Shader.Find("FWTCG/GlassPanel");
            if (shader == null)
            {
                // Shader stripped in build? Add FWTCG/GlassPanel to Always Included Shaders in Project Settings.
                Debug.LogWarning("[GlassPanelFX] Shader 'FWTCG/GlassPanel' not found — glass effect disabled.");
                return;
            }

            _mat = new Material(shader) { hideFlags = HideFlags.DontSave };
            _mat.SetColor("_TintColor",   _tintColor);
            _mat.SetColor("_BorderColor", _borderColor);
            _mat.SetFloat("_BorderWidth", _borderWidth);
            _mat.SetFloat("_NoiseScale",  _noiseScale);
            _mat.SetFloat("_NoiseStr",    _noiseStr);

            GetComponent<Image>().material = _mat;
        }

        private void OnDestroy()
        {
            if (_mat != null) Destroy(_mat);
        }

        // ── Runtime setters ────────────────────────────────────────────────────

        /// <summary>Change border color at runtime (e.g. cyan for buff panel, red for debuff).</summary>
        public void SetBorderColor(Color c)
        {
            _borderColor = c;
            if (_mat != null) _mat.SetColor("_BorderColor", c);
        }

        /// <summary>Change tint alpha at runtime for fade-in / fade-out effects.</summary>
        public void SetTintAlpha(float a)
        {
            _tintColor.a = a;
            if (_mat != null) _mat.SetColor("_TintColor", _tintColor);
        }
    }
}
