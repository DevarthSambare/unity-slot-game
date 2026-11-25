using NUnit.Framework;
using SlotGame.Core;

namespace SlotGame.Tests
{
    public class SlotMachineTests
    {
        private static SlotMachine CreateMachine(int credits = 1000, int seed = 42) =>
            new SlotMachine(SlotRules.CreateDefault(), new SeededRandomNumberGenerator(seed), credits);

        [Test]
        public void Spin_TakesTheBet()
        {
            var machine = CreateMachine();
            int bet = machine.CurrentBet;

            Assert.IsTrue(machine.TrySpin(out var result));
            Assert.AreEqual(1000 - bet, machine.Wallet.Balance);
            Assert.AreEqual(3, result.Symbols.Count);
        }

        [Test]
        public void Spin_FailsWhenBroke()
        {
            var machine = CreateMachine(credits: 5);
            Assert.IsFalse(machine.CanSpin);
            Assert.IsFalse(machine.TrySpin(out _));
            Assert.AreEqual(5, machine.Wallet.Balance);
        }

        [Test]
        public void CollectWinnings_AddsPayout()
        {
            var machine = CreateMachine();

            // spin until we hit something, seeded so it's deterministic
            SpinResult win = null;
            for (int i = 0; i < 500 && win == null; i++)
            {
                machine.TrySpin(out var result);
                if (result.IsWin)
                    win = result;
            }

            Assert.IsNotNull(win, "Expected at least one win in 500 seeded spins");
            int before = machine.Wallet.Balance;
            machine.CollectWinnings(win);
            Assert.AreEqual(before + win.Payout, machine.Wallet.Balance);
        }

        [Test]
        public void ChangeBet_ClampsToOptions()
        {
            var machine = CreateMachine();
            machine.ChangeBet(-1);
            Assert.AreEqual(10, machine.CurrentBet);

            machine.ChangeBet(+1);
            machine.ChangeBet(+1);
            machine.ChangeBet(+1);
            Assert.AreEqual(100, machine.CurrentBet);
        }

        [Test]
        public void SameSeed_SameOutcomes()
        {
            var a = CreateMachine(seed: 7);
            var b = CreateMachine(seed: 7);
            for (int i = 0; i < 20; i++)
            {
                a.TrySpin(out var ra);
                b.TrySpin(out var rb);
                CollectionAssert.AreEqual(ra.Symbols, rb.Symbols);
            }
        }
    }
}
