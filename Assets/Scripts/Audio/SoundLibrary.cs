using UnityEngine;

namespace SlotGame.Audio
{
    /// <summary>All the game's clips in one asset, so the AudioManager doesn't need a dozen inspector slots.</summary>
    [CreateAssetMenu(fileName = "SoundLibrary", menuName = "Slot Game/Sound Library")]
    public class SoundLibrary : ScriptableObject
    {
        [Header("UI")]
        public AudioClip uiClick;

        [Header("Machine")]
        public AudioClip leverPull;
        public AudioClip reelSpinLoop;
        public AudioClip reelStop;
        [Tooltip("Soft click each time a symbol passes the payline while spinning.")]
        public AudioClip reelTick;
        [Tooltip("Chime when a reel lands on the same symbol as the reels before it.")]
        public AudioClip reelMatch;
        public AudioClip anticipation;

        [Header("Results")]
        public AudioClip winSmall;
        public AudioClip winBig;
        public AudioClip winJackpot;
        public AudioClip freeSpins;
        public AudioClip noWin;
        public AudioClip coinTick;
    }
}
