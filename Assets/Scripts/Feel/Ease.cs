using System;
using System.Collections;
using UnityEngine;

namespace SlotGame.Feel
{
    /// <summary>
    /// Tiny easing + tween helper so I don't need DOTween/LeanTween for a handful of UI animations.
    /// Curves are the standard ones from easings.net.
    /// Tweens use unscaled time by default so UI keeps animating even if timeScale changes.
    /// </summary>
    public static class Ease
    {
        public static float Linear(float t) => t;

        public static float InQuad(float t) => t * t;

        public static float OutQuad(float t) => 1f - (1f - t) * (1f - t);

        public static float OutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

        public static float InOutSine(float t) => -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;

        /// <summary>Overshoots past 1 then settles. overshoot 1.70158 is the "classic" amount.</summary>
        public static float OutBack(float t, float overshoot = 1.70158f)
        {
            float c3 = overshoot + 1f;
            float x = t - 1f;
            return 1f + c3 * x * x * x + overshoot * x * x;
        }

        /// <summary>
        /// Slope of OutBack at t = 0. The reel uses this to pick a stop duration that matches its
        /// current speed, so there's no visible "hitch" when it switches from spinning to stopping.
        /// </summary>
        public static float OutBackStartSlope(float overshoot = 1.70158f) => overshoot + 3f;

        public static float OutElastic(float t)
        {
            const float c4 = 2f * Mathf.PI / 3f;
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }

        /// <summary>
        /// Runs apply(eased t) every frame for duration seconds, always finishing on apply(1).
        /// </summary>
        public static IEnumerator Tween(float duration, Action<float> apply, Func<float, float> ease = null, bool unscaled = true)
        {
            ease ??= Linear;
            if (duration <= 0f)
            {
                apply(ease(1f));
                yield break;
            }

            for (float t = 0f; t < 1f; t += (unscaled ? Time.unscaledDeltaTime : Time.deltaTime) / duration)
            {
                apply(ease(t));
                yield return null;
            }

            apply(ease(1f));
        }

        /// <summary>Quick "pop": scales up to peak and back to the original size.</summary>
        public static IEnumerator PunchScale(Transform target, float peak = 1.2f, float duration = 0.25f)
        {
            if (target == null) yield break;

            Vector3 baseScale = target.localScale;
            yield return Tween(duration, t =>
            {
                // 0 -> 1 -> 0 hump, sin gives a nice smooth one
                float hump = Mathf.Sin(t * Mathf.PI);
                target.localScale = baseScale * Mathf.Lerp(1f, peak, hump);
            });
            target.localScale = baseScale;
        }
    }
}
