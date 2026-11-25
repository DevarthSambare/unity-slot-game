using System;
using System.Collections.Generic;

namespace SlotGame.Core
{
    /// <summary>
    /// A "virtual reel": each symbol gets a weight, and a stop is picked with probability
    /// weight / totalWeight. This is how real slot machines tune their odds without changing
    /// what the physical reel looks like.
    ///
    /// Example: Cherry 12, Bell 9, Bar 7, Seven 4 (total 32) means a Seven lands 4/32 = 12.5% of the time.
    /// </summary>
    public sealed class WeightedReel
    {
        private readonly SymbolType[] _symbols;
        private readonly int[] _cumulativeWeights;

        public int TotalWeight { get; }

        public WeightedReel(IReadOnlyDictionary<SymbolType, int> weights)
        {
            if (weights == null || weights.Count == 0)
                throw new ArgumentException("A reel needs at least one weighted symbol.", nameof(weights));

            _symbols = new SymbolType[weights.Count];
            _cumulativeWeights = new int[weights.Count];

            int running = 0;
            int i = 0;
            foreach (var pair in weights)
            {
                if (pair.Value <= 0)
                    throw new ArgumentException($"Weight for {pair.Key} must be positive.", nameof(weights));

                running += pair.Value;
                _symbols[i] = pair.Key;
                _cumulativeWeights[i] = running;
                i++;
            }

            TotalWeight = running;
        }

        /// <summary>Picks one symbol using the given RNG.</summary>
        public SymbolType Pick(IRandomNumberGenerator rng)
        {
            int roll = rng.Next(TotalWeight);

            // Walk the cumulative table, first bucket whose upper bound is above the roll wins.
            // Only 4 symbols so a linear scan is plenty, no need for binary search here.
            for (int i = 0; i < _cumulativeWeights.Length; i++)
            {
                if (roll < _cumulativeWeights[i])
                    return _symbols[i];
            }

            // Can't actually get here since roll < TotalWeight, but the compiler doesn't know that.
            return _symbols[_symbols.Length - 1];
        }

        /// <summary>Probability (0..1) of this reel landing on the given symbol.</summary>
        public double ProbabilityOf(SymbolType symbol)
        {
            int previous = 0;
            for (int i = 0; i < _symbols.Length; i++)
            {
                if (_symbols[i] == symbol)
                    return (double)(_cumulativeWeights[i] - previous) / TotalWeight;
                previous = _cumulativeWeights[i];
            }
            return 0d;
        }
    }
}
