using System;

namespace SlotGame.Core
{
    /// <summary>
    /// Deterministic RNG for tests and the RTP simulator. Same seed = same sequence of spins,
    /// which makes failures reproducible. Not used for real play.
    /// </summary>
    public sealed class SeededRandomNumberGenerator : IRandomNumberGenerator
    {
        private readonly Random _random;

        public SeededRandomNumberGenerator(int seed)
        {
            _random = new Random(seed);
        }

        public int Next(int maxExclusive) => _random.Next(maxExclusive);
    }
}
