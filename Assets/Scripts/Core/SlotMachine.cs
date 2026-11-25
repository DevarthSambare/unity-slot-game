using System;
using System.Collections.Generic;

namespace SlotGame.Core
{
    /// <summary>
    /// The actual game model. Owns the wallet, the reels, the bet and the bonus features, and
    /// turns a "spin" request into a <see cref="SpinResult"/>. Knows nothing about sprites,
    /// animation or sound, the presentation layer listens to it and plays things back.
    /// </summary>
    public sealed class SlotMachine
    {
        private readonly SlotRules _rules;
        private readonly IRandomNumberGenerator _rng;
        private readonly WeightedReel[] _reels;
        private readonly PayoutCalculator _payoutCalculator;

        private int _betIndex;
        private int _freeSpinBet;

        public CreditWallet Wallet { get; }
        public ProgressiveJackpot Jackpot { get; }
        public SlotRules Rules => _rules;
        public IReadOnlyList<WeightedReel> Reels => _reels;

        public int BetIndex => _betIndex;

        /// <summary>During free spins the bet is locked to whatever triggered them.</summary>
        public int CurrentBet => IsInFreeSpins ? _freeSpinBet : _rules.BetOptions[_betIndex];

        public int FreeSpinsRemaining { get; private set; }
        public bool IsInFreeSpins => FreeSpinsRemaining > 0;

        public event Action<int> BetChanged;
        public event Action<int> FreeSpinsChanged;

        public SlotMachine(SlotRules rules, IRandomNumberGenerator rng,
            int? startingCredits = null, int? startingJackpot = null)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));

            _reels = new WeightedReel[_rules.ReelCount];
            for (int i = 0; i < _reels.Length; i++)
                _reels[i] = new WeightedReel(_rules.ReelWeights);

            _payoutCalculator = new PayoutCalculator(new Paytable(_rules.Payouts));
            Wallet = new CreditWallet(startingCredits ?? _rules.StartingCredits);
            Jackpot = new ProgressiveJackpot(_rules.JackpotSeed, _rules.JackpotContributionRate, startingJackpot);
        }

        public bool CanSpin => IsInFreeSpins || Wallet.CanAfford(CurrentBet);

        public bool CanChangeBet => !IsInFreeSpins;

        public void ChangeBet(int direction)
        {
            if (!CanChangeBet)
                return;

            int next = Math.Max(0, Math.Min(_rules.BetOptions.Length - 1, _betIndex + direction));
            if (next == _betIndex)
                return;

            _betIndex = next;
            BetChanged?.Invoke(CurrentBet);
        }

        /// <summary>
        /// Takes the bet (unless it's a free spin) and rolls every reel. The win is NOT added to
        /// the wallet here, call <see cref="CollectWinnings"/> once the reels have finished
        /// animating so the credit counter doesn't jump before the player sees the symbols.
        /// </summary>
        public bool TrySpin(out SpinResult result)
        {
            result = null;
            bool isFreeSpin = IsInFreeSpins;
            int bet = CurrentBet;

            if (isFreeSpin)
            {
                FreeSpinsRemaining--;
                FreeSpinsChanged?.Invoke(FreeSpinsRemaining);
            }
            else
            {
                if (!Wallet.TrySpend(bet))
                    return false;

                // only real money feeds the jackpot, free spins don't
                Jackpot.Contribute(bet);
            }

            var symbols = new SymbolType[_reels.Length];
            for (int i = 0; i < _reels.Length; i++)
                symbols[i] = _reels[i].Pick(_rng);

            result = Evaluate(symbols, bet, isFreeSpin);
            return true;
        }

        private SpinResult Evaluate(SymbolType[] symbols, int bet, bool isFreeSpin)
        {
            int multiplier = isFreeSpin ? _rules.FreeSpinWinMultiplier : 1;
            int payout = _payoutCalculator.CalculatePayout(symbols, bet) * multiplier;

            if (payout == 0)
                return new SpinResult(symbols, bet, 0, WinType.None, wasFreeSpin: isFreeSpin, multiplier: multiplier);

            PayoutCalculator.AllSymbolsMatch(symbols, out SymbolType matched);
            var winType = WinType.Line;
            int jackpotAmount = 0;
            int freeSpins = 0;

            if (matched == _rules.JackpotSymbol)
            {
                // Line pay for the sevens plus the bet's share of the progressive pool.
                jackpotAmount = Jackpot.Award((float)bet / _rules.MaxBet);
                payout += jackpotAmount;
                winType = WinType.Jackpot;
            }
            else if (matched == _rules.FreeSpinsSymbol)
            {
                // Retriggers just stack more spins on top, keeps the bet from the first trigger.
                if (!IsInFreeSpins)
                    _freeSpinBet = bet;

                freeSpins = _rules.FreeSpinsAwarded;
                FreeSpinsRemaining += freeSpins;
                FreeSpinsChanged?.Invoke(FreeSpinsRemaining);
                winType = WinType.FreeSpins;
            }

            return new SpinResult(symbols, bet, payout, winType, jackpotAmount, freeSpins, isFreeSpin, multiplier);
        }

        public void CollectWinnings(SpinResult result)
        {
            if (result != null && result.Payout > 0)
                Wallet.Add(result.Payout);
        }
    }
}
