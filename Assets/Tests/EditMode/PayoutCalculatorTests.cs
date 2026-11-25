using NUnit.Framework;
using SlotGame.Core;

namespace SlotGame.Tests
{
    public class PayoutCalculatorTests
    {
        private PayoutCalculator _calculator;

        [SetUp]
        public void SetUp()
        {
            _calculator = new PayoutCalculator(new Paytable(SlotRules.CreateDefault().Payouts));
        }

        [TestCase(SymbolType.Cherry, 4)]
        [TestCase(SymbolType.Bell, 5)]
        [TestCase(SymbolType.Bar, 25)]
        [TestCase(SymbolType.Seven, 50)]
        public void ThreeOfAKind_PaysBetTimesMultiplier(SymbolType symbol, int multiplier)
        {
            var symbols = new[] { symbol, symbol, symbol };
            Assert.AreEqual(10 * multiplier, _calculator.CalculatePayout(symbols, 10));
        }

        [Test]
        public void TwoMatching_PaysNothing()
        {
            // brief says all slots have to match, so 2 of a kind is a loss
            var symbols = new[] { SymbolType.Seven, SymbolType.Seven, SymbolType.Bar };
            Assert.AreEqual(0, _calculator.CalculatePayout(symbols, 100));
        }

        [Test]
        public void AllDifferent_PaysNothing()
        {
            var symbols = new[] { SymbolType.Cherry, SymbolType.Bell, SymbolType.Bar };
            Assert.AreEqual(0, _calculator.CalculatePayout(symbols, 100));
        }

        [Test]
        public void AllSymbolsMatch_ReportsMatchedSymbol()
        {
            var symbols = new[] { SymbolType.Bell, SymbolType.Bell, SymbolType.Bell };
            Assert.IsTrue(PayoutCalculator.AllSymbolsMatch(symbols, out var matched));
            Assert.AreEqual(SymbolType.Bell, matched);
        }
    }
}
