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

        public UnitTest1()
        {
            this.loan = new();
            this.stubPhoneHandler = new();
            this.mod = new ModEntry(this.loan, this.stubPhoneHandler);
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
            this.stubHelper.AdvanceDay(new WorldDate(1, Season.Spring, 5));

            // Player asks for the terms
            this.stubPhoneHandler.GivenPlayerPhonesIn("prospectus");

            this.stubHelper.AdvanceDay();

            // Expect the prospectus mail to be delivered
            var paymentDates = this.loan.EnsureTermsHaveBeenDelivered();
            this.stubHelper.AdvanceDay();

            // Player gets the loan
            this.stubPhoneHandler.GivenPlayerPhonesIn("Start");
            this.stubHelper.AdvanceDay();
            this.loan.EnsureCatalogsHaveBeenDelivered();

            foreach (var payment in paymentDates)
            {
                this.stubHelper.AdvanceDay(new WorldDate(payment.year, payment.season, Loan.PrepareStatementDayOfSeason + 1));
                int minimumPaymentPerStatement = this.loan.EnsureSeasonalStatementDelivered();
                Assert.AreEqual(minimumPaymentPerStatement, payment.payment);
                if (minimumPaymentPerStatement > 0)
                {
                    this.stubHelper.AdvanceDay(new WorldDate(payment.year, payment.season, Loan.PaymentDueDayOfSeason));
                    this.stubHelper.Game1PlayerMoney = minimumPaymentPerStatement;
                    this.stubPhoneHandler.GivenPlayerPhonesIn("Make a payment", "minimum");
                    Assert.AreEqual(0, this.stubHelper.Game1PlayerMoney);
                }
            }
        }
    }
}
