namespace SlotGame.Core
{
    /// <summary>
    /// Plays a lot of spins headlessly and reports the RTP (return to player) and hit rates.
    /// I used this to tune the reel weights and paytable so the game pays back a bit under
    /// 100% instead of guessing. Run it from Tools > Slot Game > Simulate RTP in the editor.
    /// </summary>
    public static class RtpSimulator
    {
        public struct Report
        {
            public int Spins;
            public long TotalBet;
            public long TotalWon;
            public int Wins;
            public int Jackpots;
            public int FreeSpinTriggers;

            public double Rtp => TotalBet == 0 ? 0 : (double)TotalWon / TotalBet;
            public double HitRate => Spins == 0 ? 0 : (double)Wins / Spins;

            public override string ToString() =>
                $"{Spins:N0} paid spins | RTP {Rtp:P2} | hit rate {HitRate:P2} (1 in {1 / System.Math.Max(HitRate, 1e-9):0.0}) | " +
                $"jackpots {Jackpots} | free spin triggers {FreeSpinTriggers}";
        }

        public static Report Run(SlotRules rules, int paidSpins, int seed = 1)
        {
            // Give the machine effectively infinite credits so it never runs dry mid sim.
            var machine = new SlotMachine(rules, new SeededRandomNumberGenerator(seed), int.MaxValue / 2);
            var report = new Report();

            // always play max bet, that's the case the jackpot maths is designed around
            machine.ChangeBet(rules.BetOptions.Length);

            while (report.Spins < paidSpins || machine.IsInFreeSpins)
            {
                bool paid = !machine.IsInFreeSpins;
                if (!machine.TrySpin(out SpinResult result))
                    break;

                if (paid)
                {
                    report.Spins++;
                    report.TotalBet += result.Bet;
                }

                report.TotalWon += result.Payout;
                if (result.IsWin) report.Wins++;
                if (result.WinType == WinType.Jackpot) report.Jackpots++;
                if (result.FreeSpinsAwarded > 0) report.FreeSpinTriggers++;

                machine.CollectWinnings(result);
            }

            return report;
        }
    }
}
