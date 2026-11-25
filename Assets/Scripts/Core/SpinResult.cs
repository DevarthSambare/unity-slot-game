using System.Collections.Generic;

namespace SlotGame.Core
{
    /// <summary>What kind of win a spin produced. Used by the UI to pick how loud the celebration is.</summary>
    public enum WinType
    {
        None,
        Line
    }

    /// <summary>
    /// Immutable outcome of a single spin. The outcome is decided the moment the player spins,
    /// the reel animation just "plays back" this result afterwards.
    /// </summary>
    public sealed class SpinResult
    {
        public IReadOnlyList<SymbolType> Symbols { get; }
        public int Bet { get; }
        public int Payout { get; }
        public WinType WinType { get; }

        public bool IsWin => Payout > 0;

        public SpinResult(IReadOnlyList<SymbolType> symbols, int bet, int payout, WinType winType)
        {
            Symbols = symbols;
            Bet = bet;
            Payout = payout;
            WinType = winType;
        }

        public override string ToString() =>
            $"[{string.Join(", ", Symbols)}] bet {Bet} -> {WinType} pays {Payout}";
    }
}
