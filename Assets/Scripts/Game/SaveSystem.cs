using UnityEngine;

namespace SlotGame.Game
{
    /// <summary>
    /// Remembers credits and the jackpot pool between sessions (PlayerPrefs, which on WebGL
    /// ends up in the browser's IndexedDB). Two ints, so no need for anything fancier.
    /// </summary>
    public static class SaveSystem
    {
        private const string CreditsKey = "slot.credits";
        private const string JackpotKey = "slot.jackpot";

        public static int? LoadCredits() => PlayerPrefs.HasKey(CreditsKey) ? PlayerPrefs.GetInt(CreditsKey) : (int?)null;

        public static int? LoadJackpot() => PlayerPrefs.HasKey(JackpotKey) ? PlayerPrefs.GetInt(JackpotKey) : (int?)null;

        public static void Save(int credits, int jackpot)
        {
            PlayerPrefs.SetInt(CreditsKey, credits);
            PlayerPrefs.SetInt(JackpotKey, jackpot);
            PlayerPrefs.Save();
        }
    }
}
