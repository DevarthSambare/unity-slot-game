using System;
using System.Collections;
using SlotGame.Feel;
using TMPro;
using UnityEngine;

namespace SlotGame.UI
{
    /// <summary>
    /// Number label that rolls up/down to its new value instead of snapping.
    /// Numbers that snap feel broken, numbers that roll feel like money.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class CountUpText : MonoBehaviour
    {
        [SerializeField] private string format = "{0:N0}";
        [SerializeField] private float minDuration = 0.25f;
        [SerializeField] private float maxDuration = 1.6f;
        [Tooltip("How many units per second we're happy to count, bigger changes get more time up to maxDuration.")]
        [SerializeField] private float unitsPerSecond = 600f;

        private TMP_Text _label;
        private Coroutine _routine;
        private int _shownValue;
        private int _targetValue;

        /// <summary>Called whenever the displayed number changes, handy for coin tick sounds.</summary>
        public event Action<int> Ticked;

        public int Value => _targetValue;
        public bool IsCounting => _routine != null;

        private TMP_Text Label => _label ? _label : _label = GetComponent<TMP_Text>();

        public void SetImmediate(int value)
        {
            StopCounting();
            _shownValue = _targetValue = value;
            Label.text = string.Format(format, value);
        }

        public void CountTo(int value)
        {
            if (value == _targetValue && !IsCounting)
                return;

            StopCounting();
            _targetValue = value;

            int from = _shownValue;
            float duration = Mathf.Clamp(Mathf.Abs(value - from) / unitsPerSecond, minDuration, maxDuration);
            _routine = StartCoroutine(Count(from, value, duration));
        }

        /// <summary>Jump straight to the end of the current count (player skipped the celebration).</summary>
        public void Complete()
        {
            if (IsCounting)
                SetImmediate(_targetValue);
        }

        private IEnumerator Count(int from, int to, float duration)
        {
            int lastShown = from;
            yield return Ease.Tween(duration, t =>
            {
                _shownValue = Mathf.RoundToInt(Mathf.Lerp(from, to, t));
                Label.text = string.Format(format, _shownValue);
                if (_shownValue != lastShown)
                {
                    lastShown = _shownValue;
                    Ticked?.Invoke(_shownValue);
                }
            }, Ease.OutCubic);

            _routine = null;
        }

        private void StopCounting()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
        }
    }
}
