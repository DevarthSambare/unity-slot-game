using NUnit.Framework;
using SlotGame.Core;

namespace SlotGame.Tests
{
    public class BonusFeatureTests
    {
        [Test]
        public void Jackpot_GrowsWithEachPaidBet()
        {
            var jackpot = new ProgressiveJackpot(1000, 0.05f);
            for (int i = 0; i < 10; i++)
                jackpot.Contribute(100);

            Assert.AreEqual(1050, jackpot.Pool);
        }

        [Test]
        public void Jackpot_SmallBetsStillAddUp()
        {
            // 5% of 10 is 0.5, make sure the fractions aren't thrown away
            var jackpot = new ProgressiveJackpot(1000, 0.05f);
            for (int i = 0; i < 4; i++)
                jackpot.Contribute(10);

            Assert.AreEqual(1002, jackpot.Pool);
        }

        [Test]
        public void Jackpot_AwardScalesWithBetAndResets()
        {
            var jackpot = new ProgressiveJackpot(1000, 0.05f, startingPool: 2000);

            Assert.AreEqual(1000, jackpot.Award(0.5f));
            Assert.AreEqual(1000, jackpot.Pool, "pool should be back at the seed value");
        }

        [Test]
        public void FreeSpins_DontCostCredits()
        {
            var machine = SpinUntil(WinType.FreeSpins, out var trigger);
            Assert.AreEqual(5, trigger.FreeSpinsAwarded);
            Assert.IsTrue(machine.IsInFreeSpins);

            machine.CollectWinnings(trigger);
            int balance = machine.Wallet.Balance;

            machine.TrySpin(out var freeSpin);
            Assert.IsTrue(freeSpin.WasFreeSpin);
            Assert.AreEqual(2, freeSpin.Multiplier);
            Assert.AreEqual(balance, machine.Wallet.Balance, "free spin should not take a bet");
        }

        [Test]
        public void FreeSpins_LockTheBet()
        {
            var machine = SpinUntil(WinType.FreeSpins, out var trigger);
            machine.ChangeBet(+1);
            Assert.AreEqual(trigger.Bet, machine.CurrentBet);
        }

        [Test]
        public void TripleSeven_PaysLineWinPlusJackpot()
        {
            var machine = SpinUntil(WinType.Jackpot, out var result);
            Assert.Greater(result.JackpotAmount, 0);
            Assert.AreEqual(result.Bet * 50 * result.Multiplier + result.JackpotAmount, result.Payout);
            Assert.AreEqual(machine.Rules.JackpotSeed, machine.Jackpot.Pool);
        }

        [Test]
        public void Simulator_RtpIsInASaneRange()
        {
            // Not pinning an exact number (that would break every time I tweak weights),
            // just making sure nobody accidentally makes the machine pay 300%.
            var report = RtpSimulator.Run(SlotRules.CreateDefault(), 200000, seed: 99);
            Assert.That(report.Rtp, Is.InRange(0.80, 1.05), report.ToString());
        }

        private static SlotMachine SpinUntil(WinType type, out SpinResult hit)
        {
            var machine = new SlotMachine(SlotRules.CreateDefault(), new SeededRandomNumberGenerator(3), 1000000);
            for (int i = 0; i < 100000; i++)
            {
                machine.TrySpin(out var result);
                if (result.WinType == type)
                {
                    hit = result;
                    return machine;
                }
                machine.CollectWinnings(result);
            }

            Assert.Fail($"Never hit {type}");
            hit = null;
            return machine;
        }
    }
}
