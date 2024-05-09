using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

namespace StardewValleyMods.JojaFinancial
{
    public class JojaPhoneHandler
        : IPhoneHandler
    {
        private ModEntry mod = null!;

        private const string JojaFinancialOutgoingPhone = "JojaFinancial.CallCenter";

        // TODO: Add much more spam to the opening.
        // TODO: Add a monthly "special offer" of some random crap item.
        //    When you tell it no, it says "Are you sure?  Buying stuff you don't really need is a great way to boost your credit score!"

        public void Entry(ModEntry mod)
        {
            this.mod = mod;

            Phone.PhoneHandlers.Add(this);
        }

        public string? CheckForIncomingCall(Random random)
        {
            return null;
        }

        public IEnumerable<KeyValuePair<string, string>> GetOutgoingNumbers()
        {
            yield return new KeyValuePair<string, string>(JojaFinancialOutgoingPhone, "Joja Finance");
        }

        public bool TryHandleIncomingCall(string callId, out Action? showDialogue)
        {
            showDialogue = null;
            return false;
        }

        public bool TryHandleOutgoingCall(string callId)
        {
            if (callId != JojaFinancialOutgoingPhone) return false;

            Game1.currentLocation.playShopPhoneNumberSounds("JojaFinancial"); // <- string is only used to generate a consistent, random DTMF tone sequence.
            Game1.player.freezePause = 4950;
            DelayedAction.functionAfterDelay(delegate
            {
                Game1.playSound("bigSelect");
                this.PhoneDialog("Welcome to JojaFinancial's Super-Helpful(tm) automated phone system!", () => this.MainMenu("How can we help you today?"));
            }, 4950);

            return true;
        }

        public record PhoneMenuItem(string Response, Action Action);

        public void MainMenu(string message) // public for test access.
        {
            if (this.mod.Loan.IsLoanUnderway)
            {
                this.PhoneDialog(message, [
                    new PhoneMenuItem("Get your balance and minimum payment amount", this.HandleGetBalance),
                    new PhoneMenuItem("Make a payment", this.HandleMakePayment),
                    new PhoneMenuItem("Set up autopay", this.HandleSetupAutoPay),
                ]);
            }
            else if (this.mod.Loan.IsPaidOff)
            {
                this.PhoneDialog(message, [
                    // TODO: Maybe add review special offers item?
                ]);
            }
            else
            {
                this.PhoneDialog(message, [
                    new PhoneMenuItem("Get the loan terms", this.HandleGetLoanTerms),
                    new PhoneMenuItem("Start the loan", this.HandleStartTheLoan),
                ]);
            }
        }

        protected virtual void PhoneDialog(string message, Action doAfter)
        {
            var dialog = new Dialogue(null, null, message);
            dialog.overridePortrait = this.mod.Helper.GameContent.Load<Texture2D>("Portraits\\AnsweringMachine");
            Game1.DrawDialogue(dialog);
            Game1.afterDialogues = (Game1.afterFadeFunction)Delegate.Combine(Game1.afterDialogues, (Game1.afterFadeFunction)delegate { doAfter(); });
        }

        protected virtual void PhoneDialog(string message, params PhoneMenuItem[] menuItems)
        {
            // The "JojaFinanceResponse" string doesn't seem to be important at all.
            var responsesPlusHangUp = menuItems.Select(i => new Response("JojaFinanceResponse", i.Response)).Union(new Response[] {
                new Response("HangUp", Game1.content.LoadString("Strings\\Characters:Phone_HangUp"))
            }).ToArray();
            var actionsPlusHangUp = menuItems.Select(i => i.Action).Union(new Action[] { () => { } }).ToArray();

            var dialog = new DialogueAndAction(message, responsesPlusHangUp, actionsPlusHangUp, this.mod.Helper.Input);
            // dialog.overridePortrait = mod.Helper.GameContent.Load<Texture2D>("Portraits\\AnsweringMachine");
            Game1.activeClickableMenu = new DialogueAndAction("How can we help you today?", responsesPlusHangUp, actionsPlusHangUp, this.mod.Helper.Input);
        }

        private void HandleGetLoanTerms()
        {
            this.mod.Loan.SendMailLoanTerms();
            this.PhoneDialog($"Great!  I just mailed to you the loan terms, you should have them tomorrow morning!  Call us back before the end of the month to lock in these low rates!",
                () => this.MainMenu("Is there anything else we can do for you?"));
        }

        private void HandleStartTheLoan()
        {
            this.mod.Loan.InitiateLoan();
            this.PhoneDialog($"Great!  I just mailed to you the catalogs and started your loan!  Remember to make your payments by the {Loan.PaymentDueDayString} of every month or you can set up auto-pay !",
                () => this.MainMenu("Is there anything else we can do for you?"));
        }

        public void HandleGetBalance()
        {
            this.PhoneDialog($"Your current balance is {this.mod.Loan.RemainingBalance}.  Your minimum payment is {this.mod.Loan.MinimumPayment} and is due on the {Loan.PaymentDueDayString}.",
                () => this.MainMenu("Is there anything else we can do for you?"));
        }

        public void HandleMakePayment()
        {
            this.PhoneDialog("How much would you like to pay?",
                new PhoneMenuItem($"The minimum ({this.mod.Loan.MinimumPayment}g)", () => this.HandleMakePayment(this.mod.Loan.MinimumPayment)),
                new PhoneMenuItem($"The full remaining balance ({this.mod.Loan.RemainingBalance}g)", () => this.HandleMakePayment(this.mod.Loan.RemainingBalance)));
        }

        public void HandleMakePayment(int amount)
        {
            this.PhoneDialog("Processing...", () =>
            {
                string message = this.mod.Loan.TryMakePayment(amount)
                    ? (this.mod.Loan.IsPaidOff
                        ? "Thank you!  Your loan is fully repaid!  You and JojaCorp thrive together!  Is there anything else we can do for you today?"
                        : "Thank you for making your payment!  Is there anything else we can do for you today?")
                    : "I'm sorry, but your bank declined the request citing insufficient funds.  Is there anything else we can do for you today?";
                this.MainMenu(message);
            });
        }

        public void HandleSetupAutoPay()
        {
            this.mod.Loan.IsOnAutoPay = !this.mod.Loan.IsOnAutoPay;
            string message = this.mod.Loan.IsOnAutoPay
                ? $"Thank you for taking advantage of AutoPay - remember to have sufficient funds in your account by day {Loan.AutoPayDayOfSeason} of each season to cover the minimum payment."
                : $"Auto-Pay has been turned off for your account.  Remember to call the Super-Helpful(tm) automated phone system by day {Loan.PaymentDueDayOfSeason} of each season to make your seasonal payment.";
            this.MainMenu(message);
        }
    }
}
