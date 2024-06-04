using JojaFinancial.Tests;
using StardewModdingAPI;
using StardewValley;

namespace StardewValleyMods.JojaFinancial.Tests
{
    [TestClass]
    public class LoanTests
    {
        private ModEntry mod;
        private StubLoan stubLoan = new();
        private StubJojaPhoneHandler stubPhoneHandler = new();
        private StubModHelper stubHelper = new();
        private StubGame1 stubGame1;
        private StubGeneratedMail stubGeneratedMail = new();
        private StubMonitor stubMonitor = new();

        public LoanTests()
        {
            this.stubLoan = new();
            this.stubPhoneHandler = new();
            this.stubGame1 = new StubGame1(this.stubHelper);
            this.mod = new ModEntry(this.stubGame1, this.stubLoan, this.stubPhoneHandler, this.stubGeneratedMail);
        }

        [TestInitialize]
        public void TestInitialize()
        {
            StubMonitor.PrepMod(this.mod, this.stubMonitor, this.stubHelper);
        }

        [TestMethod]
        public void BasicTwoYear()
        {
            // Wait for Morris visit
            this.stubGame1.AdvanceDay(new WorldDate(1, Season.Spring, 5));

            // Player asks for the terms
            this.stubPhoneHandler.GivenPlayerMakesMainMenuChoices("2-year loan terms");

            this.stubGame1.AdvanceDay();

            // Expect the terms mail to be delivered
            var paymentDates = this.stubLoan.EnsureTermsHaveBeenDelivered();
            this.stubGame1.AdvanceDay();

            // Player gets the loan
            this.stubPhoneHandler.GivenPlayerMakesMainMenuChoices("Start a 2");
            this.stubGame1.AdvanceDay();
            this.stubLoan.EnsureCatalogsHaveBeenDelivered();
            this.stubGeneratedMail.AssertNoMoreMail();

            foreach (var payment in paymentDates)
            {
                this.stubGame1.AdvanceDay(new WorldDate(payment.year, payment.season, Loan.PrepareStatementDayOfSeason + 1));
                int minimumPaymentPerStatement = this.stubLoan.EnsureSeasonalStatementDelivered();
                Assert.AreEqual(minimumPaymentPerStatement, payment.payment);
                if (minimumPaymentPerStatement > 0)
                {
                    this.stubGame1.AdvanceDay(new WorldDate(payment.year, payment.season, Loan.PaymentDueDayOfSeason));
                    this.stubGame1.PlayerMoney = minimumPaymentPerStatement;
                    this.stubPhoneHandler.GivenPlayerMakesMainMenuChoices("Make a payment", "minimum");
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
        public void BasicThreeYear()
        {
            // Wait for Morris visit
            this.stubGame1.AdvanceDay(new WorldDate(1, Season.Spring, 5));

            // Player asks for the terms
            this.stubPhoneHandler.GivenPlayerMakesMainMenuChoices("3-year loan terms");

            this.stubGame1.AdvanceDay();

            // Expect the terms mail to be delivered
            var paymentDates = this.stubLoan.EnsureTermsHaveBeenDelivered();
            this.stubGame1.AdvanceDay();

            // Player gets the loan
            this.stubPhoneHandler.GivenPlayerMakesMainMenuChoices("Start a 3");
            this.stubGame1.AdvanceDay();
            this.stubLoan.EnsureCatalogsHaveBeenDelivered();
            this.stubGeneratedMail.AssertNoMoreMail();

            foreach (var payment in paymentDates)
            {
                this.stubGame1.AdvanceDay(new WorldDate(payment.year, payment.season, Loan.PrepareStatementDayOfSeason + 1));
                int minimumPaymentPerStatement = this.stubLoan.EnsureSeasonalStatementDelivered();
                Assert.AreEqual(minimumPaymentPerStatement, payment.payment);
                if (minimumPaymentPerStatement > 0)
                {
                    this.stubGame1.AdvanceDay(new WorldDate(payment.year, payment.season, Loan.PaymentDueDayOfSeason));
                    this.stubGame1.PlayerMoney = minimumPaymentPerStatement;
                    this.stubPhoneHandler.GivenPlayerMakesMainMenuChoices("Make a payment", "minimum");
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

            this.stubPhoneHandler.GivenPlayerMakesMainMenuChoices("2-year loan terms");
            this.stubGame1.AdvanceDay();
            var paymentDates = this.stubLoan.EnsureTermsHaveBeenDelivered();

            // Player starts the loan on the 28th - that still counts as in the Fall, so fall and winter are payment free.
            this.stubPhoneHandler.GivenPlayerMakesMainMenuChoices("Start a 2-year");
            this.stubGame1.AdvanceDay();
            this.stubLoan.EnsureCatalogsHaveBeenDelivered();
            this.stubGeneratedMail.AssertNoMoreMail();

            this.stubPhoneHandler.AssertPaymentAndBalance(0, 240000);

            foreach (var payment in paymentDates)
            {
                this.stubGame1.AdvanceDay(new WorldDate(payment.year, payment.season, Loan.PrepareStatementDayOfSeason + 1));
                int minimumPaymentPerStatement = this.stubLoan.EnsureSeasonalStatementDelivered();
                Assert.AreEqual(minimumPaymentPerStatement, payment.payment);
                if (minimumPaymentPerStatement > 0)
                {
                    this.stubGame1.AdvanceDay(new WorldDate(payment.year, payment.season, Loan.PaymentDueDayOfSeason));
                    this.stubGame1.PlayerMoney = minimumPaymentPerStatement;
                    this.stubPhoneHandler.GivenPlayerMakesMainMenuChoices("Make a payment", "minimum");
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
            this.stubLoan.InitiateLoan(new LoanScheduleTwoYear()); // Simulate agreeing with Morris.
            this.stubPhoneHandler.AssertPaymentAndBalance(0, 240000); // Should work right away.

            this.stubPhoneHandler.GivenPlayerSetsUpAutopay();

            this.stubGame1.AdvanceDay();
            this.stubLoan.EnsureCatalogsHaveBeenDelivered();
            this.stubGeneratedMail.AssertNoMoreMail();

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
                        this.stubPhoneHandler.GivenPlayerMakesMainMenuChoices("Make a payment", "minimum");
                        Assert.AreEqual(5, this.stubGame1.PlayerMoney);

                        // Set up a no-autopay thing for next month.
                        this.stubPhoneHandler.GivenPlayerTurnsOffAutopay();
                        break;
                    case 5:
                        this.stubGeneratedMail.AssertNoMoreMail(); // Not on autopay this month
                        this.stubGame1.PlayerMoney = minimumPaymentPerStatement + 5;
                        this.stubPhoneHandler.GivenPlayerMakesMainMenuChoices("Make a payment", "minimum");
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
            this.stubPhoneHandler.GivenPlayerMakesMainMenuChoices("2-year loan terms");

            this.stubGame1.AdvanceDay();

            // Expect the terms mail to be delivered
            var paymentDates = this.stubLoan.EnsureTermsHaveBeenDelivered();
            this.stubGame1.AdvanceDay();

            // Player gets the loan
            this.stubPhoneHandler.GivenPlayerMakesMainMenuChoices("Start a 2-year");
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
                    this.stubPhoneHandler.GivenPlayerMakesMainMenuChoices("Make a payment", "balance");
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

        [TestMethod]
        public void RigmaroleTestNoSale()
        {
            this.stubGame1.PlayerMoney = 1000;
            this.stubPhoneHandler.GivenPlayerGetsRigmarole(this.stubGame1.PlayerName, "No thanks", "Yes", "Start a 2-year");
            Assert.IsTrue(this.stubLoan.IsLoanUnderway);
            this.stubLoan.EnsureCatalogsHaveBeenDelivered();
            this.stubGeneratedMail.AssertNoMoreMail();
            Assert.AreEqual(1000, this.stubGame1.PlayerMoney);
        }

        [TestMethod]
        public void RigmaroleBitesOnFirst()
        {
            const int priorPlayerMoney = 100000;
            this.stubGame1.PlayerMoney = priorPlayerMoney;
            this.stubPhoneHandler.GivenPlayerGetsRigmarole(this.stubGame1.PlayerName, "take it", "Start a 2-year");
            Assert.IsTrue(this.stubLoan.IsLoanUnderway);
            this.stubLoan.EnsureCatalogsHaveBeenDelivered();
            this.stubPhoneHandler.EnsureRandoSaleObjectDeliveredAndPaidFor(priorPlayerMoney);
            this.stubGeneratedMail.AssertNoMoreMail();
        }

        [TestMethod]
        public void RigmaroleTooPoor()
        {
            const int priorPlayerMoney = 1;
            this.stubGame1.PlayerMoney = priorPlayerMoney;
            this.stubPhoneHandler.GivenPlayerGetsRigmarole(this.stubGame1.PlayerName, "take it", "Start a 2-year");
            Assert.IsTrue(this.stubLoan.IsLoanUnderway);
            this.stubLoan.EnsureCatalogsHaveBeenDelivered();
            this.stubGeneratedMail.AssertNoMoreMail();
            Assert.AreEqual(priorPlayerMoney, this.stubGame1.PlayerMoney);
        }

        [TestMethod]
        public void RigmaroleBitesOnSecond()
        {
            const int priorPlayerMoney = 100000;
            this.stubGame1.PlayerMoney = priorPlayerMoney;
            this.stubPhoneHandler.GivenPlayerGetsRigmarole(this.stubGame1.PlayerName, "No thanks", "okay", "Start a 2-year");
            Assert.IsTrue(this.stubLoan.IsLoanUnderway);
            this.stubLoan.EnsureCatalogsHaveBeenDelivered();
            this.stubPhoneHandler.EnsureRandoSaleObjectDeliveredAndPaidFor(priorPlayerMoney);
            this.stubGeneratedMail.AssertNoMoreMail();
        }
    }
}
