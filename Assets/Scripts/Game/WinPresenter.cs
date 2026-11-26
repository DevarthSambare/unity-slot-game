using System.Collections;
using SlotGame.Audio;
using SlotGame.Core;
using SlotGame.Feel;
using SlotGame.Reels;
using SlotGame.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.Game
{
    /// <summary>
    /// Plays the celebration for a finished spin. Kept out of the controller so all the
    /// "juice" tuning lives in one place.
    ///
    /// Win tiers (based on payout / bet):
    ///   small  (under 10x)  -> symbols pulse, payline blinks, jingle, a few coins
    ///   big    (10x+)       -> + banner, flash, more coins
    ///   jackpot / free spins -> + shake, big flash, banner with its own title
    /// </summary>
    public class WinPresenter : MonoBehaviour
    {
        [SerializeField] private ReelSetController reels;
        [SerializeField] private Image payline;
        [SerializeField] private WinBanner banner;
        [SerializeField] private CoinBurst coinBurst;
        [SerializeField] private RectTransform coinOrigin;
        [SerializeField] private ScreenFlash screenFlash;
        [SerializeField] private UiShake machineShake;
        [SerializeField] private AudioManager audioManager;

        [Header("Tuning")]
        [SerializeField] private int bigWinMultiplier = 10;
        [SerializeField] private Color paylineIdle = new Color(1f, 0.25f, 0.3f, 0.45f);
        [SerializeField] private Color paylineWin = new Color(1f, 0.85f, 0.25f, 1f);
        [SerializeField] private Color jackpotColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color freeSpinsColor = new Color(0.55f, 0.9f, 1f);
        [SerializeField] private Color bigWinColor = new Color(1f, 0.55f, 0.2f);

        private Coroutine _paylineBlink;

        public bool IsBannerShowing => banner.IsShowing;

        private void Awake()
        {
            payline.color = paylineIdle;
        }

        /// <summary>Skip button for impatient players, jumps the banner to the end.</summary>
        public void Skip() => banner.Skip();

        /// <summary>Called on every reel landing.</summary>
        public void OnReelStopped(int reelIndex)
        {
            // little thunk + bump, last reel lands a touch harder
            bool last = reelIndex == reels.ReelCount - 1;
            audioManager.Play(audioManager.Sounds.reelStop, last ? 1f : 0.85f, 1f, 0.06f);
            machineShake.Shake(last ? 4f : 2.5f, 0.12f);
        }

        public IEnumerator PresentResult(SpinResult result)
        {
            if (!result.IsWin)
            {
                audioManager.Play(audioManager.Sounds.noWin, 0.45f);
                yield break;
            }

            // 1. Instant feedback, same frame the last reel lands
            for (int i = 0; i < reels.ReelCount; i++)
                reels.GetReel(i).GetCenterView().StartWinPulse();

            reels.SetWinGlow(true);
            _paylineBlink = StartCoroutine(BlinkPayline());

            float winRatio = (float)result.Payout / result.Bet;
            bool isJackpot = result.WinType == WinType.Jackpot;
            bool isFreeSpins = result.WinType == WinType.FreeSpins;
            bool isBig = winRatio >= bigWinMultiplier;

            // 2. Sound + coins + flash, scaled by how big the win is
            if (isJackpot)
            {
                audioManager.Play(audioManager.Sounds.winJackpot);
                screenFlash.Flash(jackpotColor, 0.7f, 0.25f);
                machineShake.Shake(18f, 0.6f);
            }
            else if (isFreeSpins)
            {
                audioManager.Play(audioManager.Sounds.freeSpins);
                audioManager.Play(audioManager.Sounds.winSmall, 0.6f);
                screenFlash.Flash(freeSpinsColor, 0.45f, 0.2f);
            }
            else
            {
                audioManager.Play(isBig ? audioManager.Sounds.winBig : audioManager.Sounds.winSmall, isBig ? 1f : 0.85f);
                if (isBig)
                    screenFlash.Flash(bigWinColor, 0.35f, 0.15f);
            }

            int coins = Mathf.Clamp(Mathf.RoundToInt(6 + winRatio * 1.5f), 8, 70);
            coinBurst.Burst(LocalPointIn(coinBurst.transform as RectTransform, coinOrigin), coins);

            // 3. Banner for the moments that deserve it
            if (isJackpot)
                yield return banner.Show("JACKPOT!", jackpotColor, result.Payout, "THREE SEVENS");
            else if (isFreeSpins)
                yield return banner.Show($"{result.FreeSpinsAwarded} FREE SPINS", freeSpinsColor, result.Payout,
                    "ALL WINS x2");
            else if (isBig)
                yield return banner.Show("BIG WIN", bigWinColor, result.Payout, $"{winRatio:0}x YOUR BET");
            else
                yield return new WaitForSeconds(0.5f);
        }

        /// <summary>Clears the win visuals, called right before the next spin.</summary>
        public void ClearWin()
        {
            if (_paylineBlink != null)
            {
                StopCoroutine(_paylineBlink);
                _paylineBlink = null;
            }
            payline.color = paylineIdle;
            reels.SetWinGlow(false);

            for (int i = 0; i < reels.ReelCount; i++)
            {
                // all five views, a pulsing one may have scrolled away from the centre slot by now
                foreach (var view in reels.GetReel(i).GetComponentsInChildren<SymbolView>())
                    view.StopWinPulse();
            }
        }

        private IEnumerator BlinkPayline()
        {
            float time = 0f;
            while (true)
            {
                time += Time.unscaledDeltaTime;
                bool on = Mathf.Repeat(time, 0.3f) < 0.18f;
                payline.color = on ? paylineWin : paylineIdle;
                yield return null;
            }
        }

        private static Vector2 LocalPointIn(RectTransform space, RectTransform target)
        {
            Vector3 world = target.TransformPoint(target.rect.center);
            return space.InverseTransformPoint(world);
        }
    }
}
