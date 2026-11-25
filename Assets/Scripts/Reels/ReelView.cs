using System;
using System.Collections;
using System.Collections.Generic;
using SlotGame.Core;
using SlotGame.Feel;
using UnityEngine;

namespace SlotGame.Reels
{
    /// <summary>
    /// One spinning reel.
    ///
    /// How it works: the reel is an endless virtual strip of symbols indexed by integers.
    /// <see cref="_position"/> is a float "how far the strip has scrolled" measured in cells.
    /// Every frame I place a handful of recycled <see cref="SymbolView"/>s around that position,
    /// so only 5 Images ever exist no matter how long the reel spins.
    ///
    /// Spin phases: small wind up (kicks back the wrong way like a real reel) ->
    /// accelerate -> constant speed -> eased stop that lands exactly on the target symbol
    /// with a little overshoot bounce.
    /// </summary>
    public class ReelView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SymbolSpriteLibrary spriteLibrary;
        [Tooltip("Recycled symbol cells. 5 covers the visible window plus one buffer on each side.")]
        [SerializeField] private SymbolView[] symbolViews;

        [Header("Layout")]
        [Tooltip("Distance between symbol centres, in the reel's local units.")]
        [SerializeField] private float cellHeight = 105f;

        [Header("Spin feel")]
        [SerializeField] private float maxSpeed = 16f;          // cells per second
        [SerializeField] private float accelerationTime = 0.28f;
        [SerializeField] private float windUpDistance = 0.22f;   // cells
        [SerializeField] private float windUpTime = 0.12f;
        [SerializeField] private float targetStopTime = 0.5f;
        [Tooltip("OutBack overshoot amount, how much the reel bounces past the symbol before settling.")]
        [SerializeField] private float stopOvershoot = 1.1f;
        [SerializeField] private float maxMotionStretch = 1.18f;

        // Endless strip contents, generated lazily. Old entries get trimmed so it doesn't grow forever.
        private readonly Dictionary<int, SymbolType> _strip = new Dictionary<int, SymbolType>();
        private System.Random _cosmeticRandom;

        private float _position;
        private float _speed;          // current cells/sec, only used for motion stretch
        private float _speedMultiplier = 1f;
        private bool _stopRequested;
        private SymbolType _targetSymbol;
        private Coroutine _spinRoutine;

        public bool IsSpinning { get; private set; }

        /// <summary>The symbol sitting on the payline once the reel has stopped.</summary>
        public SymbolType CenterSymbol => GetStripSymbol(Mathf.RoundToInt(_position));

        public event Action<ReelView> Stopped;

        private void Awake()
        {
            // cosmetic only, this has nothing to do with the outcome
            _cosmeticRandom = new System.Random(Guid.NewGuid().GetHashCode());
            Layout();
        }

        /// <summary>Starts spinning. Outcome isn't known yet, that comes in with RequestStop.</summary>
        public void StartSpin()
        {
            if (IsSpinning)
                return;

            _stopRequested = false;
            _speedMultiplier = 1f;
            IsSpinning = true;
            _spinRoutine = StartCoroutine(SpinRoutine());
        }

        /// <summary>Tells the reel which symbol to land on. It finishes its current motion and eases in.</summary>
        public void RequestStop(SymbolType target)
        {
            _targetSymbol = target;
            _stopRequested = true;
        }

        /// <summary>Used for anticipation, the last reel spins a bit faster while the tension builds.</summary>
        public void SetSpeedMultiplier(float multiplier) => _speedMultiplier = multiplier;

        /// <summary>Puts a symbol on the payline without animating (used on startup).</summary>
        public void SnapTo(SymbolType symbol)
        {
            if (_spinRoutine != null)
                StopCoroutine(_spinRoutine);

            IsSpinning = false;
            _position = Mathf.Round(_position);
            _strip[Mathf.RoundToInt(_position)] = symbol;
            Layout();
        }

        public SymbolView GetCenterView()
        {
            // the view whose index matches the payline index
            int center = Mathf.RoundToInt(_position);
            int first = Mathf.FloorToInt(_position) - symbolViews.Length / 2;
            int slot = center - first;
            return slot >= 0 && slot < symbolViews.Length ? symbolViews[slot] : symbolViews[symbolViews.Length / 2];
        }

        private IEnumerator SpinRoutine()
        {
            // 1. Wind up: nudge the strip backwards a little, like the reel catching before it goes.
            float start = _position;
            yield return Ease.Tween(windUpTime, t =>
            {
                _position = start - windUpDistance * t;
                Layout();
            }, Ease.OutQuad, unscaled: false);

            // 2. Accelerate up to full speed, then 3. hold until a stop is requested.
            float elapsed = 0f;
            while (!_stopRequested || elapsed < accelerationTime)
            {
                elapsed += Time.deltaTime;
                float ramp = Mathf.Clamp01(elapsed / accelerationTime);
                _speed = maxSpeed * _speedMultiplier * Ease.InQuad(ramp);
                _position += _speed * Time.deltaTime;
                Layout();
                yield return null;
            }

            // 4. Stop. Work out how far we need to travel so the OutBack curve starts at the same
            //    speed we're currently going (otherwise you see a jerk when the stop kicks in).
            //    distance = speed * duration / slope_at_0, then round up to the next whole cell so
            //    a symbol lands exactly on the payline.
            float currentSpeed = Mathf.Max(_speed, 1f);
            float slope = Ease.OutBackStartSlope(stopOvershoot);
            float idealDistance = currentSpeed * targetStopTime / slope;

            // Need at least ~1.6 cells of travel so the target symbol gets swapped in while it's
            // still above the window and the player never sees it pop in.
            int targetIndex = Mathf.CeilToInt(_position + Mathf.Max(idealDistance, 1.6f));
            _strip[targetIndex] = _targetSymbol;

            float from = _position;
            float distance = targetIndex - from;
            float duration = distance * slope / currentSpeed;

            yield return Ease.Tween(duration, t =>
            {
                float previous = _position;
                _position = Mathf.LerpUnclamped(from, targetIndex, Ease.OutBack(t, stopOvershoot));
                _speed = Mathf.Abs(_position - previous) / Mathf.Max(Time.deltaTime, 0.0001f);
                Layout();
            }, Ease.Linear, unscaled: false);

            _position = targetIndex;
            _speed = 0f;
            IsSpinning = false;
            Layout();
            TrimStrip();

            _spinRoutine = null;
            Stopped?.Invoke(this);
        }

        /// <summary>Positions the recycled symbol views around the current strip position.</summary>
        private void Layout()
        {
            int half = symbolViews.Length / 2;
            int firstIndex = Mathf.FloorToInt(_position) - half;

            // stretch symbols vertically while moving fast, settles back to 1 as the reel slows
            float speed01 = Mathf.Clamp01(_speed / maxSpeed);
            float stretch = Mathf.Lerp(1f, maxMotionStretch, speed01);
            float alpha = Mathf.Lerp(1f, 0.85f, speed01);

            for (int i = 0; i < symbolViews.Length; i++)
            {
                int stripIndex = firstIndex + i;
                SymbolView view = symbolViews[i];

                // Higher strip index = higher up on screen. As _position grows, everything slides down.
                float y = (stripIndex - _position) * cellHeight;
                view.Rect.anchoredPosition = new Vector2(0f, y);

                SymbolType symbol = GetStripSymbol(stripIndex);
                view.SetSymbol(symbol, spriteLibrary.GetSprite(symbol));

                if (IsSpinning)
                    view.SetMotionStretch(stretch, alpha);
            }

            if (!IsSpinning)
            {
                foreach (var view in symbolViews)
                    view.SetMotionStretch(1f, 1f);
            }
        }

        private SymbolType GetStripSymbol(int index)
        {
            if (!_strip.TryGetValue(index, out SymbolType symbol))
            {
                symbol = spriteLibrary.RandomFillerSymbol(_cosmeticRandom ??= new System.Random());
                _strip[index] = symbol;
            }
            return symbol;
        }

        private void TrimStrip()
        {
            // keep a few cells around the current position, drop everything that already scrolled past
            int keepFrom = Mathf.FloorToInt(_position) - symbolViews.Length;
            var toRemove = new List<int>();
            foreach (int key in _strip.Keys)
            {
                if (key < keepFrom)
                    toRemove.Add(key);
            }
            foreach (int key in toRemove)
                _strip.Remove(key);
        }
    }
}
