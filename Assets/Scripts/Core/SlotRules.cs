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
            { SymbolType.Bell, 10 },
            { SymbolType.Bar, 25 },
            { SymbolType.Seven, 50 }
        };

        public static SlotRules CreateDefault() => new SlotRules();
    }
}
