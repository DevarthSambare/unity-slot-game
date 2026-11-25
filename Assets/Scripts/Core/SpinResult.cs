using System.Collections.Generic;

namespace SlotGame.Core
{
    /// <summary>What kind of win a spin produced. Used by the UI to pick how loud the celebration is.</summary>
    public enum WinType
    {
        None,
        Line,
        FreeSpins,
        Jackpot
    }

    /// <summary>
    /// Immutable outcome of a single spin. The outcome is decided the moment the player spins,
    /// the reel animation just "plays back" this result afterwards.
    /// </summary>
    public sealed class SpinResult
    {
        public IReadOnlyList<SymbolType> Symbols { get; }
        public int Bet { get; }

        /// <summary>Total credits won (line win + jackpot share), multipliers already applied.</summary>
        public int Payout { get; }
        public WinType WinType { get; }

        /// <summary>Jackpot part of <see cref="Payout"/>, 0 unless WinType is Jackpot.</summary>
        public int JackpotAmount { get; }

        /// <summary>Free spins this spin awarded (0 most of the time).</summary>
        public int FreeSpinsAwarded { get; }

        /// <summary>True if this spin was itself a free spin (no bet was taken).</summary>
        public bool WasFreeSpin { get; }
        public int Multiplier { get; }

        public bool IsWin => Payout > 0;

        public SpinResult(IReadOnlyList<SymbolType> symbols, int bet, int payout, WinType winType,
            int jackpotAmount = 0, int freeSpinsAwarded = 0, bool wasFreeSpin = false, int multiplier = 1)
        {
            Symbols = symbols;
            Bet = bet;
            Payout = payout;
            WinType = winType;
            JackpotAmount = jackpotAmount;
            FreeSpinsAwarded = freeSpinsAwarded;
            WasFreeSpin = wasFreeSpin;
            Multiplier = multiplier;
        }

        public override string ToString() =>
            $"[{string.Join(", ", Symbols)}] bet {Bet} -> {WinType} pays {Payout}" +
            (FreeSpinsAwarded > 0 ? $" (+{FreeSpinsAwarded} free spins)" : string.Empty) +
            (WasFreeSpin ? $" [free spin x{Multiplier}]" : string.Empty);
    }
}
