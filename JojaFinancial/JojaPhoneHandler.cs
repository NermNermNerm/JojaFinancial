using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.GameData.Objects;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.Objects;

namespace StardewValleyMods.JojaFinancial
{
    public class JojaPhoneHandler
        : IPhoneHandler, ISimpleLog
    {
        private ModEntry mod = null!;

        private const string JojaFinancialOutgoingPhone = "JojaFinancial.CallCenter";

        // TODO: Add much more spam to the opening.

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

        // TODO: make this configurable
        public bool GivePlayerTheRunAround => true;

        [ExcludeFromCodeCoverage]
        public bool TryHandleOutgoingCall(string callId)
        {
            if (callId != JojaFinancialOutgoingPhone) return false;

            Game1.currentLocation.playShopPhoneNumberSounds("JojaFinancial"); // <- string is only used to generate a consistent, random DTMF tone sequence.
            Game1.player.freezePause = 4950;
            DelayedAction.functionAfterDelay(delegate
            {
                Game1.playSound("bigSelect");
                if (this.GivePlayerTheRunAround)
                {
                    this.PhoneDialog("Welcome to JojaFinancial's Super-Helpful(tm) automated phone system!#$b#This call may be monitored for training and quality purposes.", () => this.RigmaroleMenu());
                }
                else
                {
                    this.PhoneDialog("Welcome to JojaFinancial's Super-Helpful(tm) automated phone system!", () => this.MainMenu("How can we help you today?"));
                }
            }, 4950);

            return true;
        }



        public record PhoneMenuItem(string Response, Action Action);

        private static string[] Names = [
            "Elvis Aaron Presley",
            "King Leopold II",
            "George Foreman",
            "Judith Sheindlin",
            "Nipsy Russell",
            "Janet Reno",
            "The Right Honourable Viscount Nelson",
            "Wilma Flintstone",
            "James T. Kirk",
            "Tina Turner",
            "Beverly Crusher",
            "Patsy Stone",
            "Bruce Lee",
            "Detective Lennie Briscoe"
        ];

        public void RigmaroleMenu()
        {
            var actions = Names
                .OrderBy(x => Game1.random.Next())
                .Take(3)
                .Append($"Farmer {this.mod.Game1.PlayerName}")
                .Select(x => new PhoneMenuItem(x, () => this.HaveIGotADealForYou(x)))
                .ToArray();
            this.PhoneDialog("In order to server you better, please give us your name:", actions);
        }

        private static readonly string[] RandomSaleItems = [
            "208", // glazed yams
            "16",  // Wild horseradish
            "24",  // parsnip
            "78",  // Cave Carrot
            "88",  // Coconut
            "92",  // Sap
            "136", // Largemouth Bass
            "142", // Carp
            "153", // Green Algae
            "167", // Joja Cola
            "231", // Eggplant Parmesan
            "271", // Unmilled rice
            "306", // Mayonnaise
            "456", // Algae Soup
            "731", // Maple Bar
            "874", // Bug Steak
        ];

        public void HaveIGotADealForYou(string chosenName)
        {
            StardewValley.Object? thingToSell = null;
            do
            {
                string item = Game1.random.Choose(RandomSaleItems);
                thingToSell = ItemRegistry.Create<StardewValley.Object>(item, Game1.random.Next(20));
                if (thingToSell is null)
                {
                    this.LogError($"Bad random item {item} - not able to create it.");
                }
                else if (thingToSell.Price <= 0)
                {
                    this.LogError($"Bad random item {item}'s price is {thingToSell.Price}.");
                }
            } while (thingToSell is null || thingToSell.Price <= 0);
            int salesPrice = (int)(thingToSell.Stack * thingToSell.Price * 2.11); // randomize the number a bit
            string message =
                "Hey, just a quick second here to let you know that our buyers scour the earth for great deals that "
                + $"we can share with you. {chosenName}, our AI-backed sales team has picked a special deal, just for you. "
                + $"We are prepared to offer you {thingToSell.Stack} {thingToSell.Name} at the extra special, discounted "
                + $"price of {salesPrice}g.";
            this.PhoneDialog(message, [
                new PhoneMenuItem("I'll take it!", () => this.HandleOneBornEveryMinute(chosenName, thingToSell, salesPrice)),
                new PhoneMenuItem("No Thanks!", () => this.HandleHardSell(chosenName, thingToSell, salesPrice)),
            ]);
        }

        public void HandleOneBornEveryMinute(string chosenName, StardewValley.Object item, int salesPrice)
        {
            if (this.mod.Game1.PlayerMoney >= salesPrice)
            {
                Game1.player.Money -= salesPrice;
                this.mod.GeneratedMail.SendMail("jojaSale", $"Your {item.Name} from JojaFinancial",
                    "Here's your special purchase from JojaFinancial's Super-Helpful Automated Phone System!  We're so glad our AI predicted your needs so well!",
                    (item.QualifiedItemId, item.Stack));
                this.MainMenu($"Processing...#$b#{chosenName}, your {item.Name} is on its way!  JojaCorp appreciates your business!#$b#Is there anything else I can help you with?");
            }
            else
            {
                this.MainMenu($"Processing...#$b#{chosenName}, I'm sorry to tell you that your bank declined the transaction citing insufficient funds!  Please come back when your credit situation improves!#$b#Is there anything else I can help you with?");
            }
        }

        public void HandleHardSell(string chosenName, StardewValley.Object item, int salesPrice)
        {
            // Maybe randomize the messages?
            //   "Are you sure?  Next-day shipping is included at no extra charge!"
            this.PhoneDialog(
                "Are you sure?  You know that buying stuff you don't need at inflated prices is a great way to boost your credit score!",
                new PhoneMenuItem("Well, okay, if it'll boost my credit score...", () => this.HandleOneBornEveryMinute(chosenName, item, salesPrice)),
                new PhoneMenuItem("Yes, quit asking!", () => this.MainMenu("Okay, but if you change your mind, call us back!  Now, how else can we help you today?")));
        }

        public void MainMenu(string message)
        {
            if (this.mod.Loan.IsLoanUnderway)
            {
                this.PhoneDialog(message, [
                    new PhoneMenuItem("Get your balance and minimum payment amount", this.HandleGetBalance),
                    new PhoneMenuItem("Make a payment", this.HandleMakePayment),
                    this.mod.Loan.IsOnAutoPay
                        ? new PhoneMenuItem("Turn off autopay", this.HandleAutoPay)
                        : new PhoneMenuItem("Set up autopay", this.HandleAutoPay),
                ]);
            }
            else if (this.mod.Loan.IsPaidOff)
            {
                this.PhoneDialog("I'm sorry, but we have no more loan opportunities to offer you at this time.  But keep calling back for our special offers!", () => { });
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
            // The PhoneDialog without the menu uses the 'Dialogue' class, which supports multipart messages, while
            //  this one does not.  So we'll do some shenanigans to give it consistent behavior.  Maybe.
            int multipartIndex = message.LastIndexOf("#$b#");
            if (multipartIndex > 0)
            {
                string introPart = message.Substring(0, multipartIndex);
                string menuPart = message.Substring(multipartIndex + "#$b#".Length);
                this.PhoneDialog(introPart, () => this.PhoneDialog(menuPart, menuItems));
            }
            else
            {
                // The "JojaFinanceResponse" string doesn't seem to be important at all.
                var responsesPlusHangUp = menuItems
                    .Select(i => new Response("JojaFinanceResponse", i.Response))
                    .Append(new Response("HangUp", Game1.content.LoadString("Strings\\Characters:Phone_HangUp")))
                    .ToArray();
                var actionsPlusHangUp = menuItems.Select(i => i.Action).Append(() => {}).ToArray();
                Game1.activeClickableMenu = new DialogueAndAction(message, responsesPlusHangUp, actionsPlusHangUp, this.mod.Helper.Input);
            }
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
            this.PhoneDialog($"Your current balance is {this.mod.Loan.RemainingBalance}g.  Your minimum payment is {this.mod.Loan.MinimumPayment}g and is due on the {Loan.PaymentDueDayString}.",
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
            if (amount == 0)
            {
                this.MainMenu("There's no need to pay at this point - you owe nothing right now.");
            }
            else
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
        }

        public void HandleAutoPay()
        {
            this.mod.Loan.IsOnAutoPay = !this.mod.Loan.IsOnAutoPay;
            string message = this.mod.Loan.IsOnAutoPay
                ? $"Thank you for taking advantage of AutoPay - remember to have sufficient funds in your account by day {Loan.AutoPayDayOfSeason} of each season to cover the minimum payment."
                : $"Auto-Pay has been turned off for your account.  Remember to call the Super-Helpful(tm) automated phone system by day {Loan.PaymentDueDayOfSeason} of each season to make your seasonal payment.";
            message += " Is there anything else I can help you with?";
            this.MainMenu(message);
        }

        public void WriteToLog(string message, LogLevel level, bool isOnceOnly)
        {
            ((ISimpleLog)this.mod).WriteToLog(message, level, isOnceOnly);
        }
    }
}
