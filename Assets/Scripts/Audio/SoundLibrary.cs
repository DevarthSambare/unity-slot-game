using UnityEngine;

namespace SlotGame.Audio
{
    /// <summary>All the game's clips in one asset, so the AudioManager doesn't need 11 inspector slots.</summary>
    [CreateAssetMenu(fileName = "SoundLibrary", menuName = "Slot Game/Sound Library")]
    public class SoundLibrary : ScriptableObject
    {
        [Header("UI")]
        public AudioClip uiClick;

        [Header("Machine")]
        public AudioClip leverPull;
        public AudioClip reelSpinLoop;
        public AudioClip reelStop;
        public AudioClip anticipation;

        [Header("Results")]
        public AudioClip winSmall;
        public AudioClip winJackpot;
        public AudioClip freeSpins;
        public AudioClip noWin;
        public AudioClip coinTick;

        [Header("Ambience")]
        public AudioClip ambience;
    }
}
