using System;
using System.Security.Cryptography;

namespace SlotGame.Core
{
    /// <summary>
    /// RNG used for the actual spins. Backed by the OS crypto random source, so outcomes can't be
    /// predicted from previous spins (System.Random / UnityEngine.Random are fine for visuals but
    /// they're seeded PRNGs, not something you'd want deciding a payout).
    /// </summary>
    public sealed class CryptoRandomNumberGenerator : IRandomNumberGenerator, IDisposable
    {
        private readonly RandomNumberGenerator _source = RandomNumberGenerator.Create();
        private readonly byte[] _buffer = new byte[4];

        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Must be greater than 0.");

            // Rejection sampling to avoid modulo bias.
            // A plain "random % max" slightly favours the low numbers whenever 2^32 isn't a multiple
            // of max. We throw away the few values from the incomplete last bucket and draw again.
            uint range = (uint)maxExclusive;
            uint limit = uint.MaxValue - (uint.MaxValue % range);

            uint value;
            do
            {
                _source.GetBytes(_buffer);
                value = BitConverter.ToUInt32(_buffer, 0);
            } while (value >= limit);

            return (int)(value % range);
        }

        public void Dispose() => _source.Dispose();
    }
}
