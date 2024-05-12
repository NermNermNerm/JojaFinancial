using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using StardewValleyMods.JojaFinancial;

namespace JojaFinancial.Tests
{
    public class StubJojaPhoneHandler
        : JojaPhoneHandler
    {
        public Queue<string> PromptToTake { get; } = new Queue<string>();
        private List<string> Messages = new List<string>();

        public void GivenPlayerPhonesIn(params string[] prompts)
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
            Assert.IsTrue(options.Count > 0, $"None of the actual prompts matched '{this.PromptToTake}'.  Options include: {string.Join(", ", options.Select(i => i.Response))}");
            Assert.IsTrue(options.Count == 1, $"'{this.PromptToTake}' needs to be made more specific, options include: {string.Join(", ", options.Select(i => i.Response))}");

            options[0].Action();
        }

        protected override void PhoneDialog(string message, Action doAfter)
        {
            this.Messages.Add(message);
            doAfter();
        }

        public void AssertPaymentAndBalance(int payment, int balance)
        {
            this.GivenPlayerPhonesIn("balance and minimum payment");
            var match = new Regex(@"balance is (?<balance>\d+)g.*minimum payment is (?<payment>\d+)g", RegexOptions.IgnoreCase).Match(this.Messages[1]);
            Assert.IsTrue(match.Success, $"The balance and minimum payment result is unreadable: {this.Messages[1]}");
            Assert.AreEqual(payment, int.Parse(match.Groups["payment"].Value));
            Assert.AreEqual(balance, int.Parse(match.Groups["balance"].Value));
        }

        public void GivenPlayerSetsUpAutopay()
        {
            this.GivenPlayerPhonesIn("Set up autopay");
            string response = this.Messages[1];
            Assert.IsTrue(response.StartsWith("Thank you for taking advantage of AutoPay"), $"Unexpected response: {response}");
            Assert.IsTrue(response.Contains(Loan.AutoPayDayOfSeason.ToString()), $"AutoPay response should have mentioned the automatic payment date: {response}");
        }

        public void GivenPlayerTurnsOffAutopay()
        {
            this.GivenPlayerPhonesIn("Turn off autopay");
            string response = this.Messages[1];
            Assert.IsTrue(response.StartsWith("Auto-Pay has been turned off"), $"Unexpected response: {response}");
            Assert.IsTrue(response.Contains(Loan.PaymentDueDayOfSeason.ToString()), $"AutoPay response should have mentioned the payment date: {response}");
        }
    }
}
