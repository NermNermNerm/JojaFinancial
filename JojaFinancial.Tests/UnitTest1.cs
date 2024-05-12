using JojaFinancial.Tests;
using StardewModdingAPI;
using StardewValley;

namespace StardewValleyMods.JojaFinancial.Tests
{
    [TestClass]
    public class UnitTest1
    {
        private ModEntry mod = new ModEntry();
        private StubLoan stubLoan = new();
        private StubJojaPhoneHandler stubPhoneHandler = new();
        private StubModHelper stubHelper = new();
        private StubGame1 stubGame1;
        private StubGeneratedMail stubGeneratedMail = new StubGeneratedMail();

        public UnitTest1()
        {
            this.stubGame1 = new StubGame1(this.stubHelper);
            this.stubLoan = new();
            this.stubPhoneHandler = new();
            this.mod = new ModEntry(this.stubGame1, this.stubLoan, this.stubPhoneHandler, this.stubGeneratedMail);
        }

        [TestInitialize]
        public void TestInitialize()
        {
            var helperProp = typeof(Mod).GetProperty("Helper", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            helperProp!.SetValue(this.mod, this.stubHelper);
            this.mod.Entry(this.stubHelper);
        }

        [TestMethod]
        public void BasicTwoYear()
        {
            // Wait for Morris visit
            this.stubGame1.AdvanceDay(new WorldDate(1, Season.Spring, 5));

            // Player asks for the terms
            this.stubPhoneHandler.GivenPlayerPhonesIn("terms");

            this.stubGame1.AdvanceDay();

            // Expect the terms mail to be delivered
            var paymentDates = this.stubLoan.EnsureTermsHaveBeenDelivered();
            this.stubGame1.AdvanceDay();

            // Player gets the loan
            this.stubPhoneHandler.GivenPlayerPhonesIn("Start");
            this.stubGame1.AdvanceDay();
            this.stubLoan.EnsureCatalogsHaveBeenDelivered();

            foreach (var payment in paymentDates)
            {
                this.stubGame1.AdvanceDay(new WorldDate(payment.year, payment.season, Loan.PrepareStatementDayOfSeason + 1));
                int minimumPaymentPerStatement = this.stubLoan.EnsureSeasonalStatementDelivered();
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
            this.stubLoan.AssertGotPaidInFullMail();

            // Advance a long time and validate that no mail happens.
            this.stubGame1.AdvanceDay(new WorldDate(4, Season.Spring, 1));
            this.stubGeneratedMail.AssertNoMoreMail();
        }


        [TestMethod]
        public void LateStarts()
        {
            // Start a couple seasons late, and late in the season too
            this.stubGame1.AdvanceDay(new WorldDate(1, Season.Fall, 27));

            this.stubPhoneHandler.GivenPlayerPhonesIn("terms");
            this.stubGame1.AdvanceDay();
            var paymentDates = this.stubLoan.EnsureTermsHaveBeenDelivered();

            // Player starts the loan on the 28th - that still counts as in the Fall, so fall and winter are payment free.
            this.stubPhoneHandler.GivenPlayerPhonesIn("Start");
            this.stubGame1.AdvanceDay();
            this.stubLoan.EnsureCatalogsHaveBeenDelivered();

            this.stubPhoneHandler.AssertPaymentAndBalance(0, 230000);

            foreach (var payment in paymentDates)
            {
                this.stubGame1.AdvanceDay(new WorldDate(payment.year, payment.season, Loan.PrepareStatementDayOfSeason + 1));
                int minimumPaymentPerStatement = this.stubLoan.EnsureSeasonalStatementDelivered();
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
            this.stubLoan.AssertGotPaidInFullMail();
            // y3 summer, 22
            Assert.AreEqual(new WorldDate(3, Season.Summer, Loan.PaymentDueDayOfSeason + 1), this.stubGame1.Date);

            // Advance a long time and validate that no mail happens.
            this.stubGame1.AdvanceDay(new WorldDate(4, Season.Spring, 1));
            this.stubGeneratedMail.AssertNoMoreMail();
        }


        [TestMethod]
        public void AutoPay()
        {
            this.stubLoan.InitiateLoan(); // Simulate agreeing with Morris.
            this.stubPhoneHandler.AssertPaymentAndBalance(0, 230000); // Should work right away.

            this.stubPhoneHandler.GivenPlayerSetsUpAutopay();

            this.stubGame1.AdvanceDay();
            this.stubLoan.EnsureCatalogsHaveBeenDelivered();

            for (int seasonCounter = 0; seasonCounter < 8; ++seasonCounter)
            {
                this.stubGame1.AdvanceDay(new WorldDate(1+(seasonCounter/4), (Season)(seasonCounter % 4), Loan.PrepareStatementDayOfSeason + 1));
                int minimumPaymentPerStatement = this.stubLoan.EnsureSeasonalStatementDelivered();

                switch (seasonCounter)
                {
                    case 0:
                    case 1:
                        Assert.AreEqual(0, this.stubLoan.MinimumPayment);
                        this.stubGeneratedMail.AssertNoMoreMail();
                        break;
                    case 4:
                        this.stubGame1.PlayerMoney = 5; // given insufficient funds
                        this.stubGame1.AdvanceDay(new WorldDate(1 + (seasonCounter / 4), (Season)(seasonCounter % 4), Loan.AutoPayDayOfSeason + 1));
                        this.stubLoan.AssertGotAutoPayFailedMail();
                        Assert.AreEqual(5, this.stubGame1.PlayerMoney);

                        // Phones it in the next day
                        this.stubGame1.AdvanceDay();
                        this.stubGame1.PlayerMoney = minimumPaymentPerStatement + 5;
                        this.stubPhoneHandler.GivenPlayerPhonesIn("Make a payment", "minimum");
                        Assert.AreEqual(5, this.stubGame1.PlayerMoney);

                        // Set up a no-autopay thing for next month.
                        this.stubPhoneHandler.GivenPlayerTurnsOffAutopay();
                        break;
                    case 5:
                        this.stubGeneratedMail.AssertNoMoreMail(); // Not on autopay this month
                        this.stubGame1.PlayerMoney = minimumPaymentPerStatement + 5;
                        this.stubPhoneHandler.GivenPlayerPhonesIn("Make a payment", "minimum");
                        Assert.AreEqual(5, this.stubGame1.PlayerMoney);
                        this.stubPhoneHandler.GivenPlayerSetsUpAutopay();
                        break;
                    default:
                        this.stubGame1.PlayerMoney = minimumPaymentPerStatement + 5;
                        this.stubGame1.AdvanceDay(new WorldDate(1 + (seasonCounter / 4), (Season)(seasonCounter % 4), Loan.AutoPayDayOfSeason + 1));
                        this.stubLoan.AssertGotAutoPaySuccessMail();
                        Assert.AreEqual(5, this.stubGame1.PlayerMoney);
                        break;
                }
            }

            this.stubLoan.AssertGotPaidInFullMail();

            // Advance a long time and validate that no mail happens.
            this.stubGame1.AdvanceDay(new WorldDate(4, Season.Spring, 1));
            this.stubGeneratedMail.AssertNoMoreMail();
            Assert.AreEqual(5, this.stubGame1.PlayerMoney);
        }


        [TestMethod]
        public void MissedPaymentsAndEarlyRepay()
        {
            // Miss a payment without autopay
            // Wait for Morris visit
            this.stubGame1.AdvanceDay(new WorldDate(1, Season.Spring, 5));

            // Player asks for the terms
            this.stubPhoneHandler.GivenPlayerPhonesIn("terms");

            this.stubGame1.AdvanceDay();

            // Expect the terms mail to be delivered
            var paymentDates = this.stubLoan.EnsureTermsHaveBeenDelivered();
            this.stubGame1.AdvanceDay();

            // Player gets the loan
            this.stubPhoneHandler.GivenPlayerPhonesIn("Start");
            this.stubGame1.AdvanceDay();
            this.stubLoan.EnsureCatalogsHaveBeenDelivered();

            foreach (var payment in paymentDates)
            {
                this.stubGame1.AdvanceDay(new WorldDate(payment.year, payment.season, Loan.PrepareStatementDayOfSeason + 1));
                int minimumPaymentPerStatement = this.stubLoan.EnsureSeasonalStatementDelivered();
                if (this.stubGame1.Date.Year == 2)
                {
                    this.stubGame1.PlayerMoney = 1 + this.stubLoan.RemainingBalance;
                    // Early payback
                    this.stubPhoneHandler.GivenPlayerPhonesIn("Make a payment", "balance");
                    Assert.AreEqual(1, this.stubGame1.PlayerMoney);
                    break;
                }
                else if (minimumPaymentPerStatement > 0)
                {
                    int balanceBeforeMissingPayment = this.stubLoan.RemainingBalance;
                    this.stubGame1.AdvanceDay(new WorldDate(payment.year, payment.season, Loan.PaymentDueDayOfSeason + 1));
                    this.stubLoan.AssertGotMissedPaymentMail();
                    Assert.AreEqual(balanceBeforeMissingPayment + Loan.LateFee, this.stubLoan.RemainingBalance); // Late fee is assessed
                }
            }

            // Next day the player should get a closure statement.
            this.stubGame1.AdvanceDay();
            this.stubLoan.AssertGotPaidInFullMail();

            // Advance a long time and validate that no mail happens.
            this.stubGame1.AdvanceDay(new WorldDate(4, Season.Spring, 1));
            this.stubGeneratedMail.AssertNoMoreMail();
        }


        // To Test:
        //  Rigmarole tests

        // Other:
        //  Debug function to pump out all the mails to validate the text.
    }
}
