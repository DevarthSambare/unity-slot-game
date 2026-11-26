using System;
using System.Collections;
using SlotGame.Core;
using SlotGame.Feel;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.Reels
{
    /// <summary>
    /// Drives all the reels for one spin: staggered start, staggered stop (left to right),
    /// anticipation on the last reel when the first ones already match, and slam stop.
    /// </summary>
    public class ReelSetController : MonoBehaviour
    {
        [SerializeField] private ReelView[] reels;

        [Tooltip("Glow overlays, one per reel window. Pulsed during anticipation and on wins.")]
        [SerializeField] private Image[] reelGlows;

        [Header("Timing")]
        [SerializeField] private float startStagger = 0.07f;
        [SerializeField] private float firstStopDelay = 0.85f;
        [SerializeField] private float stopInterval = 0.32f;
        [SerializeField] private float anticipationExtraTime = 1.3f;
        [SerializeField] private float anticipationSpeedBoost = 1.35f;
        [SerializeField] private float slamStopInterval = 0.06f;

        private bool _slamRequested;
        private Coroutine _glowRoutine;

        public int ReelCount => reels.Length;
        public bool IsSpinning { get; private set; }

        /// <summary>Fires with the reel index each time a reel lands.</summary>
        public event Action<int> ReelStopped;

        /// <summary>Fires with the reel index whenever a symbol scrolls past the payline.</summary>
        public event Action<int> SymbolPassed;

        /// <summary>Fires when the last reel starts its "will it, won't it" extra spin.</summary>
        public event Action AnticipationStarted;

        private void Awake()
        {
            for (int i = 0; i < reels.Length; i++)
            {
                int index = i; // capture for the lambda
                reels[i].Stopped += _ => ReelStopped?.Invoke(index);
                reels[i].SymbolPassed += _ => SymbolPassed?.Invoke(index);
            }
            SetGlows(0f, -1);
        }

        public ReelView GetReel(int index) => reels[index];

        /// <summary>Player hit spin again while reels are running, stop them all quickly.</summary>
        public void SlamStop() => _slamRequested = true;

        /// <summary>Snap reels to a set of symbols without animating, used on startup.</summary>
        public void ShowImmediately(System.Collections.Generic.IReadOnlyList<SymbolType> symbols)
        {
            for (int i = 0; i < reels.Length && i < symbols.Count; i++)
                reels[i].SnapTo(symbols[i]);
        }

        /// <summary>Spins and lands every reel on the result's symbols. Yield on this.</summary>
        public IEnumerator Spin(SpinResult result)
        {
            IsSpinning = true;
            _slamRequested = false;
            SetGlows(0f, -1);

            for (int i = 0; i < reels.Length; i++)
            {
                reels[i].StartSpin();
                yield return WaitOrSlam(startStagger);
            }

            yield return WaitOrSlam(firstStopDelay);

            for (int i = 0; i < reels.Length; i++)
            {
                bool isLastReel = i == reels.Length - 1;

                // Anticipation: if every reel so far shows the same symbol, the last one could
                // complete the win. Hold it a bit longer, speed it up and glow it. Pure theatre,
                // the outcome was decided before the reels even started.
                if (isLastReel && !_slamRequested && reels.Length > 1 && PreviousReelsMatch(result, i))
                {
                    AnticipationStarted?.Invoke();
                    reels[i].SetSpeedMultiplier(anticipationSpeedBoost);
                    _glowRoutine = StartCoroutine(PulseGlow(i));
                    yield return WaitOrSlam(anticipationExtraTime);
                    StopGlowPulse();
                }

                reels[i].RequestStop(result.Symbols[i]);
                yield return WaitOrSlam(stopInterval);
            }

            // wait for the last one to finish its bounce
            foreach (var reel in reels)
            {
                while (reel.IsSpinning)
                    yield return null;
            }

            IsSpinning = false;
        }

        /// <summary>Lights up all reel windows (used by the win presentation).</summary>
        public void SetWinGlow(bool on)
        {
            StopGlowPulse();
            if (on)
                _glowRoutine = StartCoroutine(PulseGlow(-1));
            else
                SetGlows(0f, -1);
        }

        private static bool PreviousReelsMatch(SpinResult result, int upToExclusive)
        {
            for (int i = 1; i < upToExclusive; i++)
            {
                if (result.Symbols[i] != result.Symbols[0])
                    return false;
            }
            return true;
        }

        private IEnumerator WaitOrSlam(float seconds)
        {
            float limit = _slamRequested ? slamStopInterval : seconds;
            for (float t = 0f; t < limit; t += Time.deltaTime)
            {
                // if a slam comes in mid wait, cut the wait short
                if (_slamRequested && t >= slamStopInterval)
                    yield break;
                yield return null;
            }
        }

        private IEnumerator PulseGlow(int reelIndex)
        {
            float time = 0f;
            while (true)
            {
                time += Time.deltaTime;
                float alpha = 0.35f + 0.35f * Mathf.Sin(time * 12f);
                SetGlows(alpha, reelIndex);
                yield return null;
            }
        }

        private void StopGlowPulse()
        {
            if (_glowRoutine != null)
            {
                StopCoroutine(_glowRoutine);
                _glowRoutine = null;
            }
            SetGlows(0f, -1);
        }

        /// <param name="reelIndex">-1 = all reels.</param>
        private void SetGlows(float alpha, int reelIndex)
        {
            if (reelGlows == null) return;

            for (int i = 0; i < reelGlows.Length; i++)
            {
                if (reelGlows[i] == null) continue;
                var c = reelGlows[i].color;
                c.a = reelIndex < 0 || reelIndex == i ? alpha : 0f;
                reelGlows[i].color = c;
            }
        }
    }
}
