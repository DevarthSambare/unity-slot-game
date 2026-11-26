using System.Collections;
using SlotGame.Feel;
using TMPro;
using UnityEngine;

namespace SlotGame.UI
{
    /// <summary>
    /// The big "BIG WIN / JACKPOT / FREE SPINS" banner over the reels.
    /// Shape of the moment: scale in with overshoot -> amount rolls up -> hold -> scale out.
    /// Can be skipped (player presses spin) which jumps straight to the end.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class WinBanner : MonoBehaviour
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private CountUpText amountText;
        [SerializeField] private TMP_Text subtitleText;

        [Header("Timing")]
        [SerializeField] private float inTime = 0.35f;
        [SerializeField] private float holdTime = 1.3f;
        [SerializeField] private float outTime = 0.2f;

        private CanvasGroup _group;
        private bool _skip;

        public bool IsShowing { get; private set; }

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            content.localScale = Vector3.zero;
        }

        public void Skip() => _skip = true;

        public IEnumerator Show(string title, Color titleColor, int amount, string subtitle)
        {
            IsShowing = true;
            _skip = false;

            titleText.text = title;

            // light top -> full colour bottom, reads like the classic shiny arcade lettering
            Color top = Color.Lerp(titleColor, Color.white, 0.65f);
            titleText.color = Color.white;
            titleText.enableVertexGradient = true;
            titleText.colorGradient = new VertexGradient(top, top, titleColor, titleColor);
            subtitleText.text = subtitle;
            amountText.gameObject.SetActive(amount > 0);
            amountText.SetImmediate(0);

            _group.alpha = 1f;
            yield return Ease.Tween(inTime, t => content.localScale = Vector3.one * Ease.OutBack(t, 2f));

            if (amount > 0)
                amountText.CountTo(amount);

            // hold until the count finishes + a beat, or until the player skips
            float timer = 0f;
            while (!_skip && (amountText.IsCounting || timer < holdTime))
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }
            amountText.Complete();

            yield return Ease.Tween(_skip ? 0.08f : outTime, t =>
            {
                content.localScale = Vector3.one * Mathf.Lerp(1f, 0f, Ease.InQuad(t));
                _group.alpha = 1f - t;
            });

            IsShowing = false;
        }
    }
}
