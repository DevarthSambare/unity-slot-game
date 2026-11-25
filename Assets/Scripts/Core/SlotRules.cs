using System.Collections.Generic;

namespace SlotGame.Core
{
    /// <summary>
    /// All the tunable numbers for the machine in one place (reel odds, paytable, bets).
    /// The Unity side fills this in from a ScriptableObject so designers can tweak it in the
    /// inspector, and tests just build one in code.
    /// </summary>
    public sealed class SlotRules
    {
        public int ReelCount = 3;
        public int StartingCredits = 1000;
        public int[] BetOptions = { 10, 50, 100 };

        /// <summary>Per symbol weight on each reel. Same strip for every reel to keep it simple.</summary>
        public Dictionary<SymbolType, int> ReelWeights = new Dictionary<SymbolType, int>
        {
            { SymbolType.Cherry, 12 },
            { SymbolType.Bell, 9 },
            { SymbolType.Bar, 7 },
            { SymbolType.Seven, 4 }
        };

        /// <summary>Bet multiplier for three of a kind.</summary>
        public Dictionary<SymbolType, int> Payouts = new Dictionary<SymbolType, int>
        {
            { SymbolType.Cherry, 4 },
            // Bell pays low on purpose, it also triggers free spins which is where its real value is
            { SymbolType.Bell, 5 },
            { SymbolType.Bar, 25 },
            { SymbolType.Seven, 50 }
        };

        // ---- Bonus: progressive jackpot (triple Seven) ----
        public SymbolType JackpotSymbol = SymbolType.Seven;
        public int JackpotSeed = 1000;
        public float JackpotContributionRate = 0.05f;

        // ---- Bonus: free spins (triple Bell) ----
        public SymbolType FreeSpinsSymbol = SymbolType.Bell;
        public int FreeSpinsAwarded = 5;
        public int FreeSpinWinMultiplier = 2;

        public int MaxBet
        {
            get
            {
                int max = 0;
                foreach (int bet in BetOptions)
                    if (bet > max) max = bet;
                return max;
            }
        }

        public static SlotRules CreateDefault() => new SlotRules();
    }
}
