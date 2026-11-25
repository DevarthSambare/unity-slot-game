using System.Collections.Generic;
using NUnit.Framework;
using SlotGame.Core;

namespace SlotGame.Tests
{
    public class RandomAndReelTests
    {
        [Test]
        public void CryptoRng_StaysInRange()
        {
            using (var rng = new CryptoRandomNumberGenerator())
            {
                for (int i = 0; i < 10000; i++)
                {
                    int value = rng.Next(7);
                    Assert.That(value, Is.InRange(0, 6));
                }
            }
        }

        [Test]
        public void CryptoRng_IsRoughlyUniform()
        {
            // Loose sanity check, not a real statistical test. 60k rolls of a d6 should put
            // every face within a few % of 10k. If this ever fails something is very wrong.
            const int rolls = 60000;
            var counts = new int[6];
            using (var rng = new CryptoRandomNumberGenerator())
            {
                for (int i = 0; i < rolls; i++)
                    counts[rng.Next(6)]++;
            }

            foreach (int count in counts)
                Assert.That(count, Is.InRange(9000, 11000));
        }

        [Test]
        public void WeightedReel_HitRatesFollowWeights()
        {
            var weights = new Dictionary<SymbolType, int>
            {
                { SymbolType.Cherry, 3 },
                { SymbolType.Seven, 1 }
            };
            var reel = new WeightedReel(weights);
            var rng = new SeededRandomNumberGenerator(1234);

            int sevens = 0;
            const int spins = 40000;
            for (int i = 0; i < spins; i++)
            {
                if (reel.Pick(rng) == SymbolType.Seven)
                    sevens++;
            }

            // expected 25%
            Assert.AreEqual(0.25, (double)sevens / spins, 0.01);
            Assert.AreEqual(0.25, reel.ProbabilityOf(SymbolType.Seven), 1e-9);
        }

        [Test]
        public void WeightedReel_RejectsZeroWeight()
        {
            var weights = new Dictionary<SymbolType, int> { { SymbolType.Bar, 0 } };
            Assert.Throws<System.ArgumentException>(() => new WeightedReel(weights));
        }
    }
}
