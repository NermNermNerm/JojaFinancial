using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValleyMods.JojaFinancial;

namespace JojaFinancial.Tests
{
    public class StubJojaPhoneHandler
        : JojaPhoneHandler
    {
        public Queue<string> PromptToTake { get; } = new Queue<string>();

        public void GivenPlayerPhonesIn(params string[] prompts)
        {
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
            doAfter();
        }
    }
}
