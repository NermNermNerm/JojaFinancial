using JojaFinancial.Tests;
using StardewModdingAPI;
using StardewValley;

namespace StardewValleyMods.JojaFinancial.Tests
{
    [TestClass]
    public class UnitTest1
    {
        private ModEntry mod = new ModEntry();
        private StubLoan loan = new();
        private StubJojaPhoneHandler stubPhoneHandler = new();
        private StubModHelper stubHelper = new();
        private StubGame1 stubGame1;
        private StubGeneratedMail stubGeneratedMail = new StubGeneratedMail();

        public UnitTest1()
        {
            this.stubGame1 = new StubGame1(this.stubHelper);
            this.loan = new();
            this.stubPhoneHandler = new();
            this.mod = new ModEntry(this.stubGame1, this.loan, this.stubPhoneHandler, this.stubGeneratedMail);
        }

        [TestInitialize]
        public void TestInitialize()
        {
            var helperProp = typeof(Mod).GetProperty("Helper", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            helperProp!.SetValue(this.mod, this.stubHelper);
            this.mod.Entry(this.stubHelper);
        }

        [TestMethod]
        public void TestMethod1()
        {
            // Wait for Morris visit
            this.stubGame1.AdvanceDay(new WorldDate(1, Season.Spring, 5));

            // Player asks for the terms
            this.stubPhoneHandler.GivenPlayerPhonesIn("terms");

            this.stubGame1.AdvanceDay();

            // Expect the terms mail to be delivered
            var paymentDates = this.loan.EnsureTermsHaveBeenDelivered();
            this.stubGame1.AdvanceDay();

            // Player gets the loan
            this.stubPhoneHandler.GivenPlayerPhonesIn("Start");
            this.stubGame1.AdvanceDay();
            this.loan.EnsureCatalogsHaveBeenDelivered();

            foreach (var payment in paymentDates)
            {
                this.stubGame1.AdvanceDay(new WorldDate(payment.year, payment.season, Loan.PrepareStatementDayOfSeason + 1));
                int minimumPaymentPerStatement = this.loan.EnsureSeasonalStatementDelivered();
                Assert.AreEqual(minimumPaymentPerStatement, payment.payment);
                if (minimumPaymentPerStatement > 0)
                {
                    this.stubGame1.AdvanceDay(new WorldDate(payment.year, payment.season, Loan.PaymentDueDayOfSeason));
                    this.stubGame1.PlayerMoney = minimumPaymentPerStatement;
                    this.stubPhoneHandler.GivenPlayerPhonesIn("Make a payment", "minimum");
                    Assert.AreEqual(0, this.stubGame1.PlayerMoney);
                }
            }
            // Next day the player should get a closure statement.
            this.stubGame1.AdvanceDay();
            _ = this.stubGeneratedMail.EnsureSingleMatchingItemWasDelivered(m => m.Message.Contains("paid in full"), "Closing Message");

            // Advance a long time and validate that no mail happens.
            this.stubGame1.AdvanceDay(new WorldDate(4, Season.Spring, 1));
            Assert.AreEqual(0, this.stubGeneratedMail.SentMail.Count);
        }

        // To Test:
        //  Player misses payments
        //  Autopay
        //  Pays back early
        //  
    }
}
