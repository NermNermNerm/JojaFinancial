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
        protected ModEntry Mod { get; private set; } = null!;

        private const string JojaFinancialOutgoingPhone = "JojaFinancial.CallCenter";

        public void Entry(ModEntry mod)
        {
            this.Mod = mod;

            Phone.PhoneHandlers.Add(this);
        }

        public string? CheckForIncomingCall(Random random)
        {
            return null;
        }

        public IEnumerable<KeyValuePair<string, string>> GetOutgoingNumbers()
        {
            yield return new KeyValuePair<string, string>(JojaFinancialOutgoingPhone, I18n.Phone_JojaFinancial());
        }

        public bool TryHandleIncomingCall(string callId, out Action? showDialogue)
        {
            showDialogue = null;
            return false;
        }

        [ExcludeFromCodeCoverage]
        public bool TryHandleOutgoingCall(string callId)
        {
            if (callId != JojaFinancialOutgoingPhone) return false;

            Game1.currentLocation.playShopPhoneNumberSounds("JojaFinancial"); // <- string is only used to generate a consistent, random DTMF tone sequence.
            Game1.player.freezePause = 4950;
            DelayedAction.functionAfterDelay(delegate
            {
                Game1.playSound("bigSelect");
                if (this.ShouldGivePlayerTheRunAround)
                {
                    this.PhoneDialog(I18n.Phone_Welcome(), () => this.RigmaroleMenu());
                }
                else
                {
                    this.PhoneDialog(I18n.Phone_Welcome(), () => this.MainMenu(I18n.Phone_HowCanWeHelp()));
                }
            }, 4950);

            return true;
        }

        // Consider making this configurable
        public bool ShouldGivePlayerTheRunAround => this.Mod.Loan.IsLoanUnderway || this.Mod.Loan.IsPaidOff;

        public record PhoneMenuItem(string Response, Action Action);

        private static string[] Names => [
            I18n.Phone_PopName1(),
            I18n.Phone_PopName2(),
            I18n.Phone_PopName3(),
            I18n.Phone_PopName4(),
            I18n.Phone_PopName5(),
            I18n.Phone_PopName6(),
            I18n.Phone_PopName7(),
            I18n.Phone_PopName8(),
            I18n.Phone_PopName9(),
            I18n.Phone_PopName10(),
            I18n.Phone_PopName11(),
            I18n.Phone_PopName12(),
            I18n.Phone_PopName13(),
            I18n.Phone_PopName14()
        ];

        public void RigmaroleMenu()
        {
            var actions = Names
                .OrderBy(x => Game1.random.Next())
                .Take(3)
                .Append(I18n.Phone_Farmer(this.Mod.Game1.PlayerName))
                .Select(x => new PhoneMenuItem(x, () => this.HaveIGotADealForYou(x)))
                .ToArray();
            this.PhoneDialog(I18n.Phone_GiveName(), actions);
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
                thingToSell = this.Mod.Game1.CreateObject(item, Game1.random.Next(18)+2);
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
            string message = I18n.Phone_SpecialOffer(chosenName, thingToSell.Stack, thingToSell.Name, salesPrice);
            this.PhoneDialog(message, [
                new PhoneMenuItem(I18n.Phone_Buy1(), () => this.HandleOneBornEveryMinute(chosenName, thingToSell, salesPrice)),
                new PhoneMenuItem(I18n.Phone_NoSale1(), () => this.HandleHardSell(chosenName, thingToSell, salesPrice)),
            ]);
        }

        public void HandleHardSell(string chosenName, StardewValley.Object item, int salesPrice)
        {
            // Maybe randomize the messages?
            //   "Are you sure?  Next-day shipping is included at no extra charge!"
            this.PhoneDialog(
                I18n.Phone_HardSell(),
                new PhoneMenuItem(I18n.Phone_Buy2(), () => this.HandleOneBornEveryMinute(chosenName, item, salesPrice)),
                new PhoneMenuItem(I18n.Phone_NoSale2(), () => this.MainMenu(I18n.Phone_HowElseCanWeHelp())));
        }

        public void HandleOneBornEveryMinute(string chosenName, StardewValley.Object item, int salesPrice)
        {
            if (this.Mod.Game1.PlayerMoney >= salesPrice)
            {
                // When we run in tests, the DisplayName property blows up.  Can't think of a good way to handle that, but here's a bad one.
                string displayName;
                try
                {
                    displayName = item.DisplayName;
                }
                catch
                {
                    displayName = item.Name;
                }

                this.Mod.Game1.PlayerMoney -= salesPrice;
                this.Mod.GeneratedMail.SendMail("jojaSale", I18n.Phone_JunkTitle(displayName), I18n.Phone_JunkContent(),
                    (item.QualifiedItemId, item.Stack));
                this.MainMenu(I18n.Phone_JunkBoughtResponse(chosenName, displayName));
            }
            else
            {
                this.MainMenu(I18n.Phone_JunkNoMoney(chosenName));
            }
        }

        public void MainMenu(string message)
        {
            if (this.Mod.Loan.IsLoanUnderway)
            {
                this.PhoneDialog(message, [
                    new PhoneMenuItem(I18n.Phone_GetBalanceAndPayment(), this.HandleGetBalance),
                    new PhoneMenuItem(I18n.Phone_MakePayment(), this.HandleMakePayment),
                    this.Mod.Loan.IsOnAutoPay
                        ? new PhoneMenuItem(I18n.Phone_TurnOffAutopay(), this.HandleAutoPay)
                        : new PhoneMenuItem(I18n.Phone_SetUpAutopay(), this.HandleAutoPay),
                ]);
            }
            else if (this.Mod.Loan.IsPaidOff)
            {
                this.PhoneDialog(I18n.Phone_NoMoreLoans(), () => { });
            }
            else
            {
                this.PhoneDialog(message, [
                    new PhoneMenuItem(I18n.Phone_GetTerms2(), () => this.HandleGetLoanTerms(new LoanScheduleTwoYear())),
                    new PhoneMenuItem(I18n.Phone_GetTerms3(), () => this.HandleGetLoanTerms(new LoanScheduleThreeYear())),
                    new PhoneMenuItem(I18n.Phone_Start2(), () => this.HandleStartTheLoan(new LoanScheduleTwoYear())),
                    new PhoneMenuItem(I18n.Phone_Start3(), () => this.HandleStartTheLoan(new LoanScheduleThreeYear())),
                ]);
            }
        }

        protected virtual void PhoneDialog(string message, Action doAfter)
        {
            var dialog = new Dialogue(null, null, message);
            dialog.overridePortrait = this.Mod.Helper.GameContent.Load<Texture2D>("Portraits\\AnsweringMachine");
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
                Game1.activeClickableMenu = new DialogueAndAction(message, responsesPlusHangUp, actionsPlusHangUp, this.Mod.Helper.Input);
            }
        }

        private void HandleGetLoanTerms(ILoanSchedule schedule)
        {
            this.Mod.Loan.SendMailLoanTerms(schedule);
            this.PhoneDialog(I18n.Phone_TermsSent(),
                () => this.MainMenu(I18n.Phone_AnythingElse()));
        }

        private void HandleStartTheLoan(ILoanSchedule schedule)
        {
            this.Mod.Loan.InitiateLoan(schedule);
            this.PhoneDialog(I18n.Phone_LoanStarted(),
                () => this.MainMenu(I18n.Phone_AnythingElse()));
        }

        public void HandleGetBalance()
        {
            string message = this.Mod.Game1.Date.DayOfMonth > Loan.PaymentDueDayOfSeason || this.Mod.Loan.MinimumPayment == 0
                ? I18n.Phone_GetBalanceAfterDueDate(this.Mod.Loan.RemainingBalance)
                : I18n.Phone_GetBalanceNotPaid(this.Mod.Loan.RemainingBalance, this.Mod.Loan.MinimumPayment);
            this.PhoneDialog(message,
                () => this.MainMenu(I18n.Phone_AnythingElse()));
        }

        public void HandleMakePayment()
        {
            this.PhoneDialog(I18n.Phone_HowMuch(),
                new PhoneMenuItem(I18n.Phone_PayMinimum(this.Mod.Loan.MinimumPayment), () => this.HandleMakePayment(this.Mod.Loan.MinimumPayment)),
                new PhoneMenuItem(I18n.Phone_PayInFull(this.Mod.Loan.RemainingBalance), () => this.HandleMakePayment(this.Mod.Loan.RemainingBalance)));
        }

        public void HandleMakePayment(int amount)
        {
            if (amount == 0)
            {
                this.MainMenu(I18n.Phone_YouOweNothing());
            }
            else
            {
                this.PhoneDialog(I18n.Phone_Processing(), () =>
                {
                    string message = this.Mod.Loan.TryMakePayment(amount)
                        ? (this.Mod.Loan.IsPaidOff
                            ? I18n.Phone_PaidOff()
                            : I18n.Phone_MadePayment())
                        : I18n.Phone_PaymentFailed();
                    this.MainMenu(message);
                });
            }
        }

        public void HandleAutoPay()
        {
            this.Mod.Loan.IsOnAutoPay = !this.Mod.Loan.IsOnAutoPay;
            this.MainMenu(this.Mod.Loan.IsOnAutoPay ? I18n.Phone_AutopayEnabled() : I18n.Phone_AutopayDisabled());
        }

        public void WriteToLog(string message, LogLevel level, bool isOnceOnly)
        {
            ((ISimpleLog)this.Mod).WriteToLog(message, level, isOnceOnly);
        }
    }
}
