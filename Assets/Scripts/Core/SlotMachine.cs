using System;
using System.Collections.Generic;

namespace SlotGame.Core
{
    /// <summary>
    /// The actual game model. Owns the wallet, the reels and the bet, and turns a "spin" request
    /// into a <see cref="SpinResult"/>. Knows nothing about sprites, animation or sound,
    /// the presentation layer listens to it and plays things back.
    /// </summary>
    public sealed class SlotMachine
    {
        private readonly SlotRules _rules;
        private readonly IRandomNumberGenerator _rng;
        private readonly WeightedReel[] _reels;
        private readonly PayoutCalculator _payoutCalculator;

        private int _betIndex;

        public CreditWallet Wallet { get; }
        public SlotRules Rules => _rules;
        public int CurrentBet => _rules.BetOptions[_betIndex];
        public int BetIndex => _betIndex;

        public event Action<int> BetChanged;

        public SlotMachine(SlotRules rules, IRandomNumberGenerator rng, int? startingCredits = null)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));

            _reels = new WeightedReel[_rules.ReelCount];
            for (int i = 0; i < _reels.Length; i++)
                _reels[i] = new WeightedReel(_rules.ReelWeights);

            _payoutCalculator = new PayoutCalculator(new Paytable(_rules.Payouts));
            Wallet = new CreditWallet(startingCredits ?? _rules.StartingCredits);
        }

        public bool CanSpin => Wallet.CanAfford(CurrentBet);

        public void ChangeBet(int direction)
        {
            int next = Math.Max(0, Math.Min(_rules.BetOptions.Length - 1, _betIndex + direction));
            if (next == _betIndex)
                return;

            _betIndex = next;
            BetChanged?.Invoke(CurrentBet);
        }

        /// <summary>
        /// Takes the bet and rolls every reel. The win is NOT added to the wallet here,
        /// call <see cref="CollectWinnings"/> once the reels have finished animating so the
        /// credit counter doesn't jump before the player sees the symbols.
        /// </summary>
        public bool TrySpin(out SpinResult result)
        {
            result = null;
            int bet = CurrentBet;

            if (!Wallet.TrySpend(bet))
                return false;

            var symbols = new SymbolType[_reels.Length];
            for (int i = 0; i < _reels.Length; i++)
                symbols[i] = _reels[i].Pick(_rng);

            int payout = _payoutCalculator.CalculatePayout(symbols, bet);
            result = new SpinResult(symbols, bet, payout, payout > 0 ? WinType.Line : WinType.None);
            return true;
        }

        public void CollectWinnings(SpinResult result)
        {
            if (result != null && result.Payout > 0)
                Wallet.Add(result.Payout);
        }

        public IReadOnlyList<WeightedReel> Reels => _reels;
    }
}
