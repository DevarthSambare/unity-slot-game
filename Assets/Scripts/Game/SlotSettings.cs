using System;
using System.Collections.Generic;
using SlotGame.Core;
using UnityEngine;

namespace SlotGame.Game
{
    /// <summary>
    /// Inspector friendly version of <see cref="SlotRules"/>. Designers tune odds and payouts
    /// here, the game converts it to a plain SlotRules object at startup.
    /// </summary>
    [CreateAssetMenu(fileName = "SlotSettings", menuName = "Slot Game/Slot Settings")]
    public class SlotSettings : ScriptableObject
    {
        [Serializable]
        public struct SymbolOdds
        {
            public SymbolType symbol;
            [Tooltip("Relative chance of landing on each reel.")]
            [Min(1)] public int reelWeight;
            [Tooltip("Bet multiplier for three of a kind.")]
            [Min(0)] public int payoutMultiplier;
        }

        [Header("Credits & bets")]
        [Min(0)] public int startingCredits = 1000;
        public int[] betOptions = { 10, 50, 100 };

        [Header("Symbols")]
        public SymbolOdds[] symbols =
        {
            new SymbolOdds { symbol = SymbolType.Cherry, reelWeight = 12, payoutMultiplier = 4 },
            new SymbolOdds { symbol = SymbolType.Bell, reelWeight = 9, payoutMultiplier = 5 },
            new SymbolOdds { symbol = SymbolType.Bar, reelWeight = 7, payoutMultiplier = 25 },
            new SymbolOdds { symbol = SymbolType.Seven, reelWeight = 4, payoutMultiplier = 50 },
        };

        [Header("Progressive jackpot")]
        public SymbolType jackpotSymbol = SymbolType.Seven;
        [Min(0)] public int jackpotSeed = 1000;
        [Range(0f, 0.25f)] public float jackpotContribution = 0.05f;

        [Header("Free spins")]
        public SymbolType freeSpinsSymbol = SymbolType.Bell;
        [Min(0)] public int freeSpinsAwarded = 5;
        [Min(1)] public int freeSpinMultiplier = 2;

        public SlotRules ToRules()
        {
            var weights = new Dictionary<SymbolType, int>();
            var payouts = new Dictionary<SymbolType, int>();
            foreach (var odds in symbols)
            {
                weights[odds.symbol] = odds.reelWeight;
                payouts[odds.symbol] = odds.payoutMultiplier;
            }

            return new SlotRules
            {
                StartingCredits = startingCredits,
                BetOptions = (int[])betOptions.Clone(),
                ReelWeights = weights,
                Payouts = payouts,
                JackpotSymbol = jackpotSymbol,
                JackpotSeed = jackpotSeed,
                JackpotContributionRate = jackpotContribution,
                FreeSpinsSymbol = freeSpinsSymbol,
                FreeSpinsAwarded = freeSpinsAwarded,
                FreeSpinWinMultiplier = freeSpinMultiplier
            };
        }
    }
}
