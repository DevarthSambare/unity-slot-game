using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.Feel
{
    /// <summary>Fullscreen colour flash for big moments (jackpot, free spins trigger).</summary>
    [RequireComponent(typeof(Image))]
    public class ScreenFlash : MonoBehaviour
    {
        private Image _image;
        private Coroutine _routine;

        private void Awake()
        {
            _image = GetComponent<Image>();
            _image.raycastTarget = false;
            SetAlpha(0f);
        }

        public void Flash(Color color, float peakAlpha = 0.6f, float duration = 0.15f)
        {
            if (_routine != null)
                StopCoroutine(_routine);

            _image.color = new Color(color.r, color.g, color.b, peakAlpha);
            _routine = StartCoroutine(FadeOut(peakAlpha, duration));
        }

        private IEnumerator FadeOut(float from, float duration)
        {
            yield return Ease.Tween(duration, t => SetAlpha(Mathf.Lerp(from, 0f, t)), Ease.OutCubic);
            _routine = null;
        }

        private void SetAlpha(float alpha)
        {
            var c = _image.color;
            c.a = alpha;
            _image.color = c;
        }
    }
}
