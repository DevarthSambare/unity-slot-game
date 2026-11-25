using System.Collections.Generic;

namespace SlotGame.Core
{
    /// <summary>
    /// Maps "three of a symbol" to a multiplier of the bet.
    /// Win rule from the brief: the player only wins when ALL reels show the same symbol.
    /// </summary>
    public sealed class Paytable
    {
        private readonly Dictionary<SymbolType, int> _multipliers;

        public Paytable(IReadOnlyDictionary<SymbolType, int> multipliers)
        {
            _multipliers = new Dictionary<SymbolType, int>();
            foreach (var pair in multipliers)
                _multipliers[pair.Key] = pair.Value;
        }

        public int GetMultiplier(SymbolType symbol) =>
            _multipliers.TryGetValue(symbol, out int multiplier) ? multiplier : 0;

        public IReadOnlyDictionary<SymbolType, int> Entries => _multipliers;
    }
}
