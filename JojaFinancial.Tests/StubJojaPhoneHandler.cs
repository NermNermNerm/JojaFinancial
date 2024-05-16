using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using StardewValley;
using StardewValleyMods.JojaFinancial;

namespace JojaFinancial.Tests
{
    public class StubJojaPhoneHandler
        : JojaPhoneHandler
    {
        public Queue<string> PromptToTake { get; } = new Queue<string>();
        private List<string> Messages = new List<string>();

        public void GivenPlayerMakesMainMenuChoices(params string[] prompts)
        {

            this.Messages.Clear();
            Assert.IsTrue(!this.PromptToTake.Any(), "There are some untaken phone choices left.  Perhaps a bad test or something went wrong earlier?");
            foreach (string prompt in prompts)
            {
                this.PromptToTake.Enqueue(prompt);
            }
            this.PromptToTake.Enqueue("");
            this.MainMenu("test");

            if (this.PromptToTake.Count == 1 && this.PromptToTake.Peek() == "")
            {
                // On the last day of the season, the system hangs up on the player...  We'll allow that in general I guess.
                this.PromptToTake.Dequeue();
            }
            Assert.IsTrue(!this.PromptToTake.Any(), "There are some untaken phone choices left after phone call completed.");
        }

        public void GivenPlayerGetsRigmarole(params string[] prompts)
        {
            this.Messages.Clear();
            Assert.IsTrue(!this.PromptToTake.Any(), "There are some untaken phone choices left.  Perhaps a bad test or something went wrong earlier?");
            foreach (string prompt in prompts)
            {
                this.PromptToTake.Enqueue(prompt);
            }
            this.PromptToTake.Enqueue("");
            this.RigmaroleMenu();

            Assert.IsTrue(!this.PromptToTake.Any(), "There are some untaken phone choices left after phone call completed.");
        }

        protected override void PhoneDialog(string message, params PhoneMenuItem[] menuItems)
        {
            if (!this.PromptToTake.Any())
            {
                Assert.Fail("Test failed to set PromptToTake");
            }

            this.Messages.Add(message);
            string prompt = this.PromptToTake.Dequeue();
            if (prompt == "")
            {
                // Hang-up
                return;
            }

            var options = menuItems.Where(mi => mi.Response.Contains(prompt, StringComparison.OrdinalIgnoreCase)).ToList();
            Assert.IsTrue(options.Count > 0, $"None of the actual prompts matched '{prompt}'.  Options include: {string.Join(", ", menuItems.Select(i => i.Response))}");
            Assert.IsTrue(options.Count == 1, $"'{this.PromptToTake}' needs to be made more specific, options include: {string.Join(", ", menuItems.Select(i => i.Response))}");

            options[0].Action();
        }

        protected override void PhoneDialog(string message, Action doAfter)
        {
            this.Messages.Add(message);
            doAfter();
        }

        public void AssertPaymentAndBalance(int expectedPayment, int expectedBalance)
        {
            this.GivenPlayerGetsPaymentAndBalance(out int actualPayment, out int actualBalance);
            Assert.AreEqual(expectedPayment, actualPayment);
            Assert.AreEqual(expectedBalance, actualBalance);
        }

        public void GivenPlayerGetsPaymentAndBalance(out int payment, out int balance)
        {
            this.GivenPlayerMakesMainMenuChoices("balance and minimum payment");
            string response = this.Messages[1];
            var match = new Regex(@"balance is (?<balance>\d+)g.", RegexOptions.IgnoreCase).Match(response);
            Assert.IsTrue(match.Success, $"The balance and minimum payment response is unreadable: {response}");
            balance = int.Parse(match.Groups["balance"].Value);

            match = new Regex(@"No.*payments can be made", RegexOptions.IgnoreCase).Match(response);
            if (match.Success)
            {
                payment = 0;
            }
            else
            {
                match = new Regex(@"minimum payment is (?<payment>\d+)g", RegexOptions.IgnoreCase).Match(response);
                Assert.IsTrue(match.Success, $"The balance and minimum payment response is unreadable: {response}");
                payment = int.Parse(match.Groups["payment"].Value);
            }
        }

        public void GivenPlayerSetsUpAutopay()
        {
            this.GivenPlayerMakesMainMenuChoices("Set up autopay");
            string response = this.Messages[1];
            Assert.IsTrue(response.StartsWith("Thank you for taking advantage of AutoPay"), $"Unexpected response: {response}");
            Assert.IsTrue(response.Contains(Loan.AutoPayDayOfSeason.ToString()), $"AutoPay response should have mentioned the automatic payment date: {response}");
        }

        public void GivenPlayerTurnsOffAutopay()
        {
            this.GivenPlayerMakesMainMenuChoices("Turn off autopay");
            string response = this.Messages[1];
            Assert.IsTrue(response.StartsWith("Auto-Pay has been turned off"), $"Unexpected response: {response}");
            Assert.IsTrue(response.Contains(Loan.PaymentDueDayOfSeason.ToString()), $"AutoPay response should have mentioned the payment date: {response}");
        }

        internal void EnsureRandoSaleObjectDeliveredAndPaidFor(int priorPlayerMoney)
        {
            var stubMailer = (StubGeneratedMail)this.Mod.GeneratedMail;
            var mailItem = stubMailer.EnsureSingleMatchingItemWasDelivered(m => m.IdPrefix == "jojaSale", "Joja Rando-Sale");
            Assert.AreEqual($"Your {StubGame1.RandoSaleObjectName} from JojaFinancial", mailItem.Synopsis);
            Assert.AreEqual(1, mailItem.attachedItems.Length);
            Assert.AreEqual("(O)" + StubGame1.RandoSaleObjectId, mailItem.attachedItems[0].qiid);
            var m = new Regex(@" (?<price>\d+)g").Match(this.Messages[1]);
            Assert.IsTrue(m.Success);
            int listPrice = int.Parse(m.Groups["price"].Value);
            Assert.IsTrue(listPrice >= 2 * mailItem.attachedItems[0].count * StubGame1.RandoSalePrice);
            Assert.IsTrue(listPrice <= 3 * mailItem.attachedItems[0].count * StubGame1.RandoSalePrice);
            Assert.AreEqual(priorPlayerMoney - listPrice, this.Mod.Game1.PlayerMoney);
        }
    }
}
