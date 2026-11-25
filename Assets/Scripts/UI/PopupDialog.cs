using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.UI
{
    /// <summary>
    /// Yes/No dialog using the popup + YES/NO art from the asset pack.
    /// Open/close motion is an Animator (PopupOpen / PopupClose clips in Assets/Animations),
    /// this script only handles content and callbacks.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class PopupDialog : MonoBehaviour
    {
        private static readonly int VisibleParam = Animator.StringToHash("Visible");

        [SerializeField] private Animator animator;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;

        private CanvasGroup _group;
        private Action _onYes;
        private Action _onNo;

        public bool IsOpen { get; private set; }

        /// <summary>Fires on either button, used to play the click sound.</summary>
        public event Action ButtonPressed;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            yesButton.onClick.AddListener(() => Close(_onYes));
            noButton.onClick.AddListener(() => Close(_onNo));
            SetBlocking(false);
        }

        public void Show(string title, string message, Action onYes, Action onNo)
        {
            titleText.text = title;
            messageText.text = message;
            _onYes = onYes;
            _onNo = onNo;

            IsOpen = true;
            SetBlocking(true);
            animator.SetBool(VisibleParam, true);
        }

        private void Close(Action callback)
        {
            if (!IsOpen)
                return;

            IsOpen = false;
            SetBlocking(false);
            animator.SetBool(VisibleParam, false);
            ButtonPressed?.Invoke();

            // clear before invoking in case the callback opens another popup
            _onYes = _onNo = null;
            callback?.Invoke();
        }

        private void SetBlocking(bool blocking)
        {
            // Alpha is owned by the animation, but input blocking is set right away so the
            // player can't double click YES during the close animation.
            _group.blocksRaycasts = blocking;
            _group.interactable = blocking;
        }
    }
}
