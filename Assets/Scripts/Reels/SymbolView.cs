using System.Collections;
using SlotGame.Core;
using SlotGame.Feel;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.Reels
{
    /// <summary>One symbol cell on a reel. The reel moves these around and recycles them.</summary>
    [RequireComponent(typeof(RectTransform))]
    public class SymbolView : MonoBehaviour
    {
        [SerializeField] private Image image;

        private RectTransform _rect;
        private Coroutine _pulseRoutine;

        public SymbolType Symbol { get; private set; }
        public RectTransform Rect => _rect ? _rect : _rect = (RectTransform)transform;

        public void SetSymbol(SymbolType symbol, Sprite sprite)
        {
            // only touch the Image when it actually changes, sprite swaps dirty the canvas
            if (Symbol == symbol && image.sprite == sprite)
                return;

            Symbol = symbol;
            image.sprite = sprite;
        }

        /// <summary>Vertical stretch while the reel is moving fast, a cheap stand in for motion blur.</summary>
        public void SetMotionStretch(float stretch, float alpha)
        {
            image.rectTransform.localScale = new Vector3(1f / Mathf.Sqrt(stretch), stretch, 1f);
            var c = image.color;
            c.a = alpha;
            image.color = c;
        }

        /// <summary>Looping "this symbol won" pulse. Call StopWinPulse to end it.</summary>
        public void StartWinPulse()
        {
            StopWinPulse();
            _pulseRoutine = StartCoroutine(WinPulse());
        }

        public void StopWinPulse()
        {
            if (_pulseRoutine != null)
            {
                StopCoroutine(_pulseRoutine);
                _pulseRoutine = null;
            }
            image.rectTransform.localScale = Vector3.one;
            image.rectTransform.localRotation = Quaternion.identity;
        }

        private IEnumerator WinPulse()
        {
            Transform t = image.rectTransform;

            // first beat overshoots so the win registers instantly
            yield return Ease.Tween(0.3f, k => t.localScale = Vector3.one * Mathf.LerpUnclamped(1f, 1.22f, Ease.OutBack(k, 2.5f)));

            while (true)
            {
                yield return Ease.Tween(0.45f, k =>
                {
                    float wave = Mathf.Sin(k * Mathf.PI * 2f);
                    t.localScale = Vector3.one * (1.15f + wave * 0.07f);
                    t.localRotation = Quaternion.Euler(0f, 0f, wave * 4f);
                });
            }
        }
    }
}
