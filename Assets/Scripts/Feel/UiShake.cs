using System.Collections;
using UnityEngine;

namespace SlotGame.Feel
{
    /// <summary>
    /// Shakes a RectTransform around its resting position. Used for reel stop "thunks"
    /// (tiny) and the jackpot (big). Calls stack by taking the stronger shake instead of fighting.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UiShake : MonoBehaviour
    {
        [Tooltip("Shake frequency in Hz. Higher feels more 'rattly'.")]
        [SerializeField] private float frequency = 28f;

        private RectTransform _rect;
        private Vector2 _restPosition;
        private float _strength;
        private float _duration;
        private float _elapsed;
        private Coroutine _routine;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _restPosition = _rect.anchoredPosition;
        }

        /// <param name="strength">Max offset in canvas pixels.</param>
        /// <param name="duration">Seconds until it fully settles.</param>
        public void Shake(float strength, float duration)
        {
            // If a bigger shake is already running, let it finish instead of shrinking it.
            if (_routine != null)
            {
                float remaining = _strength * (1f - Mathf.Clamp01(_elapsed / _duration));
                if (remaining > strength)
                    return;
            }

            _strength = strength;
            _duration = Mathf.Max(0.01f, duration);
            _elapsed = 0f;

            if (_routine == null)
                _routine = StartCoroutine(ShakeRoutine());
        }

        private IEnumerator ShakeRoutine()
        {
            // random seed per shake so two shakes don't look identical
            float seed = Random.value * 100f;

            while (_elapsed < _duration)
            {
                _elapsed += Time.unscaledDeltaTime;
                float falloff = 1f - Mathf.Clamp01(_elapsed / _duration);
                falloff *= falloff;

                // Perlin noise instead of Random so the motion is smooth, not teleporting each frame
                float time = _elapsed * frequency;
                float x = (Mathf.PerlinNoise(seed, time) - 0.5f) * 2f;
                float y = (Mathf.PerlinNoise(seed + 50f, time) - 0.5f) * 2f;
                _rect.anchoredPosition = _restPosition + new Vector2(x, y) * (_strength * falloff);
                yield return null;
            }

            _rect.anchoredPosition = _restPosition;
            _routine = null;
        }
    }
}
