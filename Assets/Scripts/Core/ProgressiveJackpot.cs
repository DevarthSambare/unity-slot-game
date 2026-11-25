using System;

namespace SlotGame.Core
{
    /// <summary>
    /// Bonus feature: classic progressive jackpot.
    /// A small cut of every paid bet goes into a shared pool. Landing three Sevens pays the pool
    /// out (scaled by bet size, so you only get the full pot on max bet like real cabinets do),
    /// then the pool resets back to its seed value.
    /// </summary>
    public sealed class ProgressiveJackpot
    {
        private readonly int _seedAmount;
        private readonly float _contributionRate;

        // Kept as a double internally so 5% of a 10 credit bet (0.5) isn't lost to rounding,
        // the UI just shows the whole number part.
        private double _pool;

        public int Pool => (int)Math.Floor(_pool);

        public event Action<int> PoolChanged;

        /// <param name="seedAmount">What the pool resets to after a win.</param>
        /// <param name="contributionRate">Fraction of each bet added to the pool, e.g. 0.05 = 5%.</param>
        /// <param name="startingPool">Restored value from a save, or null to start at the seed.</param>
        public ProgressiveJackpot(int seedAmount, float contributionRate, int? startingPool = null)
        {
            _seedAmount = seedAmount;
            _contributionRate = contributionRate;
            _pool = Math.Max(seedAmount, startingPool ?? seedAmount);
        }

        public void Contribute(int bet)
        {
            if (bet <= 0)
                return;

            int before = Pool;
            _pool += bet * _contributionRate;
            if (Pool != before)
                PoolChanged?.Invoke(Pool);
        }

        /// <summary>
        /// Pays out the share of the pool this bet is entitled to and resets the pool.
        /// </summary>
        /// <param name="betShare">bet / maxBet, clamped to 0..1.</param>
        public int Award(float betShare)
        {
            betShare = Math.Max(0f, Math.Min(1f, betShare));
            int won = (int)Math.Floor(_pool * betShare);

            _pool = _seedAmount;
            PoolChanged?.Invoke(Pool);
            return won;
        }
    }
}
