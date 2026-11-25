using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SlotGame.UI
{
    /// <summary>
    /// The pull lever on the side of the cabinet. Clicking it spins, and it also animates
    /// when the spin comes from the button or keyboard so the machine always "reacts".
    /// The up/down swap itself is the LeverPull animation clip on the Animator.
    /// </summary>
    public class LeverView : MonoBehaviour, IPointerClickHandler
    {
        private static readonly int PullTrigger = Animator.StringToHash("Pull");

        [SerializeField] private Animator animator;

        public event Action Clicked;

        public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke();

        public void PlayPull() => animator.SetTrigger(PullTrigger);
    }
}
