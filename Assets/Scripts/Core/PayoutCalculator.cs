using System;
using System.Collections.Generic;

namespace SlotGame.Core
{
    /// <summary>
    /// Pure function style class: symbols + bet in, payout out. No state, no Unity,
    /// which keeps the money maths easy to unit test.
    /// </summary>
    public sealed class PayoutCalculator
    {
        private readonly Paytable _paytable;

        public PayoutCalculator(Paytable paytable)
        {
            _paytable = paytable ?? throw new ArgumentNullException(nameof(paytable));
        }

        /// <summary>
        /// Returns true if every reel landed on the same symbol (the only winning combination).
        /// </summary>
        public static bool AllSymbolsMatch(IReadOnlyList<SymbolType> symbols, out SymbolType matched)
        {
            matched = default;
            if (symbols == null || symbols.Count == 0)
                return false;

            matched = symbols[0];
            for (int i = 1; i < symbols.Count; i++)
            {
                if (symbols[i] != matched)
                    return false;
            }
            return true;
        }

        public int CalculatePayout(IReadOnlyList<SymbolType> symbols, int bet)
        {
            if (!AllSymbolsMatch(symbols, out SymbolType symbol))
                return 0;

            return bet * _paytable.GetMultiplier(symbol);
        }
    }
}
