namespace SlotGame.Core
{
    /// <summary>
    /// Small abstraction over the RNG so the game logic never calls UnityEngine.Random directly.
    /// Lets me swap in a seeded generator for unit tests and keep the real game on a crypto RNG.
    /// </summary>
    public interface IRandomNumberGenerator
    {
        /// <summary>Returns a uniformly distributed integer in [0, maxExclusive).</summary>
        int Next(int maxExclusive);
    }
}
