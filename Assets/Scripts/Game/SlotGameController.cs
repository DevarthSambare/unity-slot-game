using System.Collections;
using SlotGame.Audio;
using SlotGame.Core;
using SlotGame.Reels;
using SlotGame.UI;
using UnityEngine;

namespace SlotGame.Game
{
    /// <summary>
    /// Top level game flow. Owns the <see cref="SlotMachine"/> model and tells the views what
    /// to do: Idle -> Spinning -> Presenting -> Idle.
    ///
    /// Input: SPIN button, clicking the lever or Space/Enter. Left/Right change bet, M mutes.
    /// Pressing spin while reels are running slam stops them, pressing it during a win
    /// celebration skips straight to the next spin.
    /// </summary>
    public class SlotGameController : MonoBehaviour
    {
        private enum GameState
        {
            Idle,
            Spinning,
            Presenting
        }

        [Header("Config")]
        [SerializeField] private SlotSettings settings;
        [Tooltip("Clear saved credits on start, handy while testing in the editor.")]
        [SerializeField] private bool ignoreSaveData;

        [Header("Scene references")]
        [SerializeField] private ReelSetController reels;
        [SerializeField] private WinPresenter winPresenter;
        [SerializeField] private HudView hud;
        [SerializeField] private LeverView lever;
        [SerializeField] private PopupDialog popup;
        [SerializeField] private AudioManager audioManager;

        [Header("Feel")]
        [Tooltip("Gap between the lever pull and the reels starting, lets the pull read first.")]
        [SerializeField] private float leverToReelsDelay = 0.12f;
        [SerializeField] private float freeSpinAutoDelay = 0.7f;

        private SlotMachine _machine;
        private CryptoRandomNumberGenerator _rng;
        private GameState _state = GameState.Idle;
        private bool _skipRequested;
        private float _lastCoinTickTime;

        private static readonly Color MessageNeutral = new Color(0.85f, 0.88f, 1f);
        private static readonly Color MessageWin = new Color(1f, 0.85f, 0.3f);
        private static readonly Color MessageWarn = new Color(1f, 0.45f, 0.4f);

        private void Awake()
        {
            _rng = new CryptoRandomNumberGenerator();

            int? savedCredits = ignoreSaveData ? null : SaveSystem.LoadCredits();
            int? savedJackpot = ignoreSaveData ? null : SaveSystem.LoadJackpot();

            // Coming back with 0 credits would just show a "refill?" popup on load, start fresh instead
            if (savedCredits.HasValue && savedCredits.Value <= 0)
                savedCredits = null;

            _machine = new SlotMachine(settings.ToRules(), _rng, savedCredits, savedJackpot);
        }

        private void Start()
        {
            // model -> view
            _machine.Wallet.BalanceChanged += balance => hud.Credits.CountTo(balance);
            _machine.Jackpot.PoolChanged += pool => hud.Jackpot.CountTo(pool);
            _machine.BetChanged += bet => hud.SetBet(bet, animate: true);
            _machine.FreeSpinsChanged += remaining =>
                hud.SetFreeSpins(remaining, _machine.Rules.FreeSpinWinMultiplier);

            // view -> controller
            hud.SpinClicked += OnSpinPressed;
            hud.BetChangeClicked += OnBetChange;
            hud.ResetClicked += OnResetPressed;
            hud.MuteClicked += OnMutePressed;
            lever.Clicked += OnSpinPressed;
            popup.ButtonPressed += audioManager.PlayClick;

            reels.ReelStopped += winPresenter.OnReelStopped;
            reels.AnticipationStarted += OnAnticipation;

            // coin tick while the credits roll up, throttled by CountUpText only firing on change
            hud.Credits.Ticked += _ => { if (_state == GameState.Presenting) PlayCoinTick(); };

            // initial UI state
            hud.Credits.SetImmediate(_machine.Wallet.Balance);
            hud.Jackpot.SetImmediate(_machine.Jackpot.Pool);
            hud.Win.SetImmediate(0);
            hud.SetBet(_machine.CurrentBet, animate: false);
            hud.SetMuted(audioManager.IsMuted);
            hud.ShowMessage("PULL THE LEVER OR PRESS SPACE", MessageNeutral);
            RefreshHudState();

            // Start on a losing looking line instead of whatever the random filler gives
            reels.ShowImmediately(new[] { SymbolType.Seven, SymbolType.Bell, SymbolType.Cherry });
        }

        private void Update()
        {
            if (popup.IsOpen)
                return;

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                OnSpinPressed();
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.DownArrow))
                OnBetChange(-1);
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.UpArrow))
                OnBetChange(+1);
            else if (Input.GetKeyDown(KeyCode.M))
                OnMutePressed();
        }

        private void OnDestroy() => _rng?.Dispose();

        private void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        // ------------------------------------------------------------------ input

        private void OnSpinPressed()
        {
            if (popup.IsOpen)
                return;

            switch (_state)
            {
                case GameState.Spinning:
                    reels.SlamStop();
                    break;

                case GameState.Presenting:
                    // impatient player: finish the celebration now, the sequence starts the next spin
                    _skipRequested = true;
                    winPresenter.Skip();
                    break;

                default:
                    TryStartSpin();
                    break;
            }
        }

        private void OnBetChange(int direction)
        {
            if (_state != GameState.Idle || !_machine.CanChangeBet)
                return;

            int before = _machine.CurrentBet;
            _machine.ChangeBet(direction);
            if (_machine.CurrentBet != before)
                audioManager.PlayClick();
        }

        private void OnResetPressed()
        {
            if (_state != GameState.Idle)
                return;

            audioManager.PlayClick();
            popup.Show("START OVER?",
                $"Reset your credits back to {_machine.Rules.StartingCredits:N0}?",
                onYes: RefillCredits,
                onNo: null);
        }

        private void OnMutePressed()
        {
            audioManager.ToggleMute();
            hud.SetMuted(audioManager.IsMuted);
            audioManager.PlayClick();
        }

        // ------------------------------------------------------------------ flow

        private void TryStartSpin()
        {
            if (!_machine.CanSpin)
            {
                HandleCannotAfford();
                return;
            }

            winPresenter.ClearWin();

            if (!_machine.TrySpin(out SpinResult result))
                return;

            // useful when checking fairness in the console
            Debug.Log($"[Slot] {result}");

            StartCoroutine(SpinSequence(result));
        }

        private IEnumerator SpinSequence(SpinResult result)
        {
            _state = GameState.Spinning;
            _skipRequested = false;
            RefreshHudState();

            hud.Win.SetImmediate(0);
            hud.ShowMessage(result.WasFreeSpin ? "FREE SPIN!" : "GOOD LUCK!", MessageNeutral);

            lever.PlayPull();
            audioManager.Play(audioManager.Sounds.leverPull, 0.9f, 1f, 0.04f);
            yield return new WaitForSeconds(leverToReelsDelay);

            audioManager.StartReelLoop();
            yield return reels.Spin(result);
            audioManager.StopReelLoop();

            _state = GameState.Presenting;
            RefreshHudState();

            if (result.IsWin)
            {
                hud.ShowMessage(WinMessage(result), MessageWin);
                hud.Win.CountTo(result.Payout);

                // credit the wallet now so the credits counter rolls up alongside the celebration
                _machine.CollectWinnings(result);
            }
            else
            {
                hud.ShowMessage(_machine.IsInFreeSpins ? "NO LUCK, KEEP GOING" : "SO CLOSE! TRY AGAIN", MessageNeutral);
            }

            yield return winPresenter.PresentResult(result);

            if (_skipRequested)
            {
                hud.Credits.Complete();
                hud.Win.Complete();
            }

            Save();
            _state = GameState.Idle;
            RefreshHudState();

            if (_machine.IsInFreeSpins)
            {
                // free spins play themselves, classic slot behaviour
                if (!_skipRequested)
                    yield return new WaitForSeconds(freeSpinAutoDelay);
                if (_state == GameState.Idle)
                    TryStartSpin();
            }
            else if (_skipRequested)
            {
                // the skip press also counts as "spin again"
                TryStartSpin();
            }
            else if (!_machine.CanSpin)
            {
                HandleCannotAfford();
            }
        }

        private void OnAnticipation()
        {
            audioManager.Play(audioManager.Sounds.anticipation, 0.8f);
            audioManager.SetReelLoopPitch(1.15f);
            hud.ShowMessage("COME ON...", MessageWin);
        }

        private void HandleCannotAfford()
        {
            // still enough for a smaller bet? nudge them toward it instead of the refill popup
            if (_machine.Wallet.Balance >= _machine.Rules.BetOptions[0])
            {
                hud.ShowMessage("NOT ENOUGH CREDITS, LOWER YOUR BET", MessageWarn);
                StartCoroutine(Feel.Ease.PunchScale(hud.BetLabelRect, 1.35f, 0.3f));
                return;
            }

            popup.Show("OUT OF CREDITS",
                $"Take another {_machine.Rules.StartingCredits:N0} credits and keep playing?",
                onYes: RefillCredits,
                onNo: () => hud.ShowMessage("OUT OF CREDITS", MessageWarn));
        }

        private void RefillCredits()
        {
            _machine.Wallet.Reset(_machine.Rules.StartingCredits);
            hud.ShowMessage("CREDITS REFILLED, GOOD LUCK!", MessageWin);
            Save();
            RefreshHudState();
        }

        private void RefreshHudState()
        {
            hud.SetState(_state != GameState.Idle, _machine.CanChangeBet, _machine.IsInFreeSpins);
        }

        private void PlayCoinTick()
        {
            // the counter changes every frame, ~16 ticks a second is plenty
            if (Time.unscaledTime - _lastCoinTickTime < 0.06f)
                return;
            _lastCoinTickTime = Time.unscaledTime;

            // tick pitch climbs as the count goes up, gives the roll up some excitement
            float pitch = 1f + Mathf.PingPong(Time.unscaledTime * 0.6f, 0.35f);
            audioManager.Play(audioManager.Sounds.coinTick, 0.35f, pitch, 0.03f);
        }

        private static string WinMessage(SpinResult result)
        {
            switch (result.WinType)
            {
                case WinType.Jackpot: return $"JACKPOT! +{result.Payout:N0}";
                case WinType.FreeSpins: return $"{result.FreeSpinsAwarded} FREE SPINS! +{result.Payout:N0}";
                default:
                    return result.WasFreeSpin
                        ? $"WIN x{result.Multiplier}! +{result.Payout:N0}"
                        : $"WIN! +{result.Payout:N0}";
            }
        }

        private void Save() => SaveSystem.Save(_machine.Wallet.Balance, _machine.Jackpot.Pool);
    }
}
