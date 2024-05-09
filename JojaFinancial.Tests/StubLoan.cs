using System.Text.RegularExpressions;
using JojaFinancial.Tests;
using StardewValley;

namespace StardewValleyMods.JojaFinancial.Tests
{
    internal class StubLoan
        : Loan
    {
        public StubModHelper StubHelper => (StubModHelper)this.Mod.Helper;


        public record MailItem(string IdPrefix, string Synopsis, string Message, WorldDate SentDate);

        public List<MailItem> SentMail = new();

        private static readonly Regex PaymentEntryRegex = new Regex(@"(?<season>(spring|summer|fall|winter)) of year (?<year>\d+)(.|\n)*?Payment: \-(?<payment>\d+)g", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public List<(Season season, int year, int payment)> EnsureTermsHaveBeenDelivered()
        {
            var mailItem = this.SentMail.Find(m => m.IdPrefix == "terms");
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

            this.SentMail.Remove(mailItem);
            mailItem = this.SentMail.Find(m => m.IdPrefix == "terms");
            Assert.IsNull(mailItem, "More than one loan terms mail was sent");
            return result;
        }

        public void EnsureCatalogsHaveBeenDelivered()
        {
            var mailItem = this.SentMail.Find(m => m.IdPrefix == "welcome");
            Assert.IsNotNull(mailItem, "Welcome mail was not delivered");
            this.SentMail.Remove(mailItem);
            Assert.AreEqual(0, this.SentMail.Count);
        }

        private static readonly Regex StatementPaymentRegex = new Regex($@"minimum payment.*{Loan.PaymentDueDayOfSeason}.*season is: (?<payment>\d+)g", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex NoPaymentDueRegex = new Regex($@"No payment is necessary", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public int EnsureSeasonalStatementDelivered()
        {
            var mailItem = this.SentMail.Find(m => m.IdPrefix == "statement");
            Assert.IsNotNull(mailItem, "Statement mail was not delivered");
            this.SentMail.Remove(mailItem);
            Assert.IsNull(this.SentMail.Find(m => m.IdPrefix == "statement"), "More than one statement sent");

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

        protected override string? GetPlayerModDataValueRaw(string modDataKey)
            => this.StubHelper.GetPlayerModDataValue(modDataKey);

        protected override void SetPlayerModDataValueRaw(string modDataKey, string? value)
            => this.StubHelper.SetPlayerModDataValue(modDataKey, value);

        protected override WorldDate Game1Date => this.StubHelper.Game1Date;

        protected override int Game1PlayerMoney {
            get => this.StubHelper.Game1PlayerMoney;
            set => this.StubHelper.Game1PlayerMoney = value;
        }

        protected override void SendMail(string idPrefix, string synopsis, string message, params string[] attachedItemQiids)
        {
            this.SentMail.Add(new MailItem(idPrefix, synopsis, message, this.Game1Date));
        }
    }
}
