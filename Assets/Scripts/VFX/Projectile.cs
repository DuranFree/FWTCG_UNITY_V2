using System;
using UnityEngine;

namespace FWTCG.VFX
{
    /// <summary>
    /// VFX-8: Moves a GameObject along a quadratic Bezier arc from start to end,
    /// then invokes an onArrived callback and self-destructs.
    /// Attach via FXTool.DoProjectileFX — do not add manually.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        // ── Constants ────────────────────────────────────────────────────────
        public const float DEFAULT_DURATION   = 0.4f;
        public const float ARC_HEIGHT_RATIO   = 0.3f;
        public const float MIN_DURATION       = 0.05f;

        // ── State ────────────────────────────────────────────────────────────
        private Vector3 _start;
        private Vector3 _end;
        private Vector3 _control;  // Bezier control point (midpoint + perpendicular offset)
        private float   _duration;
        private float   _elapsed;
        private bool    _running;
        private Action  _onArrived;

        // ── Public read-only accessors (for tests) ──────────────────────────
        public Vector3 StartPos  => _start;
        public Vector3 EndPos    => _end;
        public Vector3 ControlPt => _control;
        public float   Duration  => _duration;
        public bool    IsRunning => _running;

        /// <summary>
        /// Initialise the projectile flight.
        /// Called by FXTool.DoProjectileFX immediately after AddComponent.
        /// </summary>
        public void Init(Vector3 start, Vector3 end, float duration, Action onArrived = null)
        {
            _start     = start;
            _end       = end;
            _duration  = Mathf.Max(MIN_DURATION, duration);
            _onArrived = onArrived;
            _elapsed   = 0f;
            _running   = true;

            // Control point: midpoint offset perpendicular to the travel direction
            Vector3 mid    = (start + end) * 0.5f;
            float   dist   = Vector3.Distance(start, end);
            float   height = dist * ARC_HEIGHT_RATIO;

            // For a 2D UI canvas, "up" means +Y (screen up)
            // Perpendicular to travel direction in 2D: rotate 90°
            Vector2 dir2D = ((Vector2)(end - start)).normalized;
            Vector2 perp  = new Vector2(-dir2D.y, dir2D.x); // left-hand perpendicular
            // Always arc upward (positive Y preferred)
            if (perp.y < 0f) perp = -perp;
            _control = mid + (Vector3)(perp * height);

            transform.position = start;
        }

        private void Update()
        {
            if (!_running) return;

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);

            // Quadratic Bezier: B(t) = (1-t)^2 * P0 + 2(1-t)t * P1 + t^2 * P2
            float u  = 1f - t;
            Vector3 pos = u * u * _start + 2f * u * t * _control + t * t * _end;
            transform.position = pos;

            // Rotate Z to face movement direction (tangent)
            // Tangent: B'(t) = 2(1-t)(P1-P0) + 2t(P2-P1)
            Vector3 tangent = 2f * u * (_control - _start) + 2f * t * (_end - _control);
            if (tangent.sqrMagnitude > 0.001f)
            {
                float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            if (t >= 1f)
            {
                _running = false;
                _onArrived?.Invoke();
                Destroy(gameObject);
            }
        }
    }
}
