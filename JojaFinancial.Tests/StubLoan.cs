using System.Text.RegularExpressions;
using JojaFinancial.Tests;
using StardewValley;

namespace StardewValleyMods.JojaFinancial.Tests
{
    internal class StubLoan
        : Loan
    {
        public StubModHelper StubHelper => (StubModHelper)this.Mod.Helper;
        public StubGeneratedMail StubMailer => (StubGeneratedMail)this.Mod.GeneratedMail;

        private static readonly Regex PaymentEntryRegex = new Regex(@"(?<season>(spring|summer|fall|winter)) of year (?<year>\d+)(.|\n)*?Payment: \-(?<payment>\d+)g", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public List<(Season season, int year, int payment)> EnsureTermsHaveBeenDelivered()
        {
            var mailItem = this.StubMailer.SentMail.Find(m => m.IdPrefix == "terms");
            Assert.IsNotNull(mailItem, "Terms mail was not delivered");

            List<(Season season, int year, int payment)> result = new();
            foreach (Match match in PaymentEntryRegex.Matches(mailItem.Message))
            {
                Season season = Enum.Parse<Season>(match.Groups["season"].Value);
                int year = int.Parse(match.Groups["year"].Value);
                int payment = int.Parse(match.Groups["payment"].Value);
                result.Add(new(season, year, payment));
            }

            Assert.AreEqual(8, result.Count, $"Expected 8 payments, but found {result.Count}");

            this.StubMailer.SentMail.Remove(mailItem);
            mailItem = this.StubMailer.SentMail.Find(m => m.IdPrefix == "terms");
            Assert.IsNull(mailItem, "More than one loan terms mail was sent");
            return result;
        }

        public void EnsureCatalogsHaveBeenDelivered()
        {
            var mailItem = this.StubMailer.EnsureSingleMatchingItemWasDelivered(m => m.IdPrefix == "welcome", "Loan Welcome");
            this.StubMailer.AssertNoMoreMail();
        }

        private static readonly Regex StatementPaymentRegex = new Regex($@"minimum payment.*{Loan.PaymentDueDayOfSeason}.*season is: (?<payment>\d+)g", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex NoPaymentDueRegex = new Regex($@"No payment is necessary", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public int EnsureSeasonalStatementDelivered()
        {
            var mailItem = this.StubMailer.EnsureSingleMatchingItemWasDelivered(m => m.IdPrefix == "statement", "Seasonal Statement");
            this.StubMailer.AssertNoMoreMail();

            var match = StatementPaymentRegex.Match(mailItem.Message);
            if (match.Success)
            {
                return int.Parse(match.Groups["payment"].Value);
            }
            else if (NoPaymentDueRegex.IsMatch(mailItem.Message))
            {
                return 0;
            }
            else
            {
                Assert.Fail($"Could not find the payment amount in the statement:\r\n{mailItem.Message}");
                return 0;
            }
        }
    }
}
