using System;

namespace SlotGame.Core
{
    /// <summary>
    /// Holds the player's credits. Kept separate from the slot machine so balance rules
    /// (no going negative etc) live in one place.
    /// </summary>
    public sealed class CreditWallet
    {
        public int Balance { get; private set; }

        /// <summary>Raised with the new balance whenever it changes.</summary>
        public event Action<int> BalanceChanged;

        public CreditWallet(int startingBalance)
        {
            if (startingBalance < 0)
                throw new ArgumentOutOfRangeException(nameof(startingBalance));
            Balance = startingBalance;
        }

        public bool CanAfford(int amount) => amount >= 0 && Balance >= amount;

        public bool TrySpend(int amount)
        {
            if (!CanAfford(amount))
                return false;

            Balance -= amount;
            BalanceChanged?.Invoke(Balance);
            return true;
        }

        public void Add(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "Use TrySpend to remove credits.");
            if (amount == 0)
                return;

            Balance += amount;
            BalanceChanged?.Invoke(Balance);
        }

        /// <summary>Used by the "refill credits" popup when the player runs dry.</summary>
        public void Reset(int newBalance)
        {
            Balance = Math.Max(0, newBalance);
            BalanceChanged?.Invoke(Balance);
        }
    }
}
