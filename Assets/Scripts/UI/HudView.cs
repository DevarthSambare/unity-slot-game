using System;
using System.Collections;
using SlotGame.Feel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.UI
{
    /// <summary>
    /// Everything around the machine: credits, bet selector, last win, jackpot meter,
    /// free spins counter, spin button and the little status message.
    /// Only displays things and forwards button presses, the controller decides what they do.
    /// </summary>
    public class HudView : MonoBehaviour
    {
        [Header("Values")]
        [SerializeField] private CountUpText creditsText;
        [SerializeField] private CountUpText winText;
        [SerializeField] private CountUpText jackpotText;
        [SerializeField] private TMP_Text betText;
        [SerializeField] private TMP_Text messageText;

        [Header("Free spins")]
        [SerializeField] private CanvasGroup freeSpinsGroup;
        [SerializeField] private TMP_Text freeSpinsText;

        [Header("Buttons")]
        [SerializeField] private Button spinButton;
        [SerializeField] private TMP_Text spinButtonLabel;
        [SerializeField] private Button betDownButton;
        [SerializeField] private Button betUpButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button muteButton;
        [SerializeField] private TMP_Text muteButtonLabel;

        [Header("Idle feel")]
        [SerializeField] private float spinButtonBreathScale = 0.035f;

        private Coroutine _messageRoutine;
        private bool _spinReady = true;

        public event Action SpinClicked;
        public event Action<int> BetChangeClicked;
        public event Action ResetClicked;
        public event Action MuteClicked;

        public CountUpText Credits => creditsText;
        public CountUpText Win => winText;
        public CountUpText Jackpot => jackpotText;
        public RectTransform BetLabelRect => (RectTransform)betText.transform;

        private void Awake()
        {
            spinButton.onClick.AddListener(() => SpinClicked?.Invoke());
            betDownButton.onClick.AddListener(() => BetChangeClicked?.Invoke(-1));
            betUpButton.onClick.AddListener(() => BetChangeClicked?.Invoke(+1));
            resetButton.onClick.AddListener(() => ResetClicked?.Invoke());
            muteButton.onClick.AddListener(() => MuteClicked?.Invoke());
            SetFreeSpins(0, 1, instant: true);
        }

        private void Update()
        {
            // Slow "breathing" on the spin button while it's waiting for the player.
            // Small thing but it tells you where to click without a tutorial.
            float breath = _spinReady ? Mathf.Sin(Time.unscaledTime * 2.6f) * spinButtonBreathScale : 0f;
            spinButton.transform.localScale = Vector3.one * (1f + breath);
        }

        public void SetBet(int bet, bool animate)
        {
            betText.text = bet.ToString("N0");
            if (animate)
                StartCoroutine(Ease.PunchScale(betText.transform, 1.25f, 0.2f));
        }

        /// <summary>Controls which buttons can be pressed in the current game state.</summary>
        public void SetState(bool spinning, bool canChangeBet, bool inFreeSpins)
        {
            _spinReady = !spinning;
            betDownButton.interactable = canChangeBet && !spinning;
            betUpButton.interactable = canChangeBet && !spinning;

            // spin button stays clickable while spinning, a second press = slam stop
            spinButtonLabel.text = spinning ? "STOP" : inFreeSpins ? "FREE SPIN" : "SPIN";
        }

        public void SetFreeSpins(int remaining, int multiplier, bool instant = false)
        {
            bool show = remaining > 0;
            freeSpinsText.text = show ? $"{remaining} LEFT  <color=#FFD54A>x{multiplier}</color>" : string.Empty;

            if (instant)
            {
                freeSpinsGroup.alpha = show ? 1f : 0f;
                return;
            }

            float from = freeSpinsGroup.alpha;
            float to = show ? 1f : 0f;
            StartCoroutine(Ease.Tween(0.25f, t => freeSpinsGroup.alpha = Mathf.Lerp(from, to, t)));
            if (show)
                StartCoroutine(Ease.PunchScale(freeSpinsGroup.transform, 1.12f, 0.25f));
        }

        public void SetMuted(bool muted) => muteButtonLabel.text = muted ? "SOUND: OFF" : "SOUND: ON";

        /// <summary>Status line under the credits. Pops in so changes get noticed.</summary>
        public void ShowMessage(string message, Color color)
        {
            messageText.text = message;
            messageText.color = color;
            if (_messageRoutine != null)
                StopCoroutine(_messageRoutine);
            _messageRoutine = StartCoroutine(Ease.PunchScale(messageText.transform, 1.12f, 0.22f));
        }
    }
}
