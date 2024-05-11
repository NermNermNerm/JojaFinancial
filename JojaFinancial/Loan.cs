using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace StardewValleyMods.JojaFinancial
{
    public class Loan : ISimpleLog
    {
        public ModEntry Mod { get; private set; } = null!;

        private const string LoanBalanceModKey = "JojaFinancial.LoanBalance";
        private const string PaidThisSeasonModKey = "JojaFinancial.PaidThisSeason";
        private const string IsOnAutoPayModKey = "JojaFinancial.Autopay";
        private const string OriginationSeasonModKey = "JojaFinancial.OriginationSeason";
        private const string SeasonLedgerModKey = "JojaFinancial.SeasonLedger";

        private const int LastDayOfSeason = 28;
        public const int PaymentDueDayOfSeason = 21;
        public const int PrepareStatementDayOfSeason = 13;
        public const string PaymentDueDayString = "21st";
        public const int AutoPayDayOfSeason = 16;

        private const int LateFee = 1000;

        public void Entry(ModEntry mod)
        {
            this.Mod = mod;
            mod.Helper.Events.GameLoop.DayEnding += this.GameLoop_DayEnding;
        }

        private void GameLoop_DayEnding(object? sender, DayEndingEventArgs e)
        {
            if (!this.IsLoanUnderway)
            {
                return;
            }

            int paymentDue = this.MinimumPayment - (this.GetPaidThisSeason() ?? 0);
            switch (this.Mod.Game1.Date.DayOfMonth)
            {
                case PrepareStatementDayOfSeason:
                    this.SendStatementMail();
                    break;
                case AutoPayDayOfSeason:
                    if (this.IsOnAutoPay)
                    {
                        this.Autopay(paymentDue);
                    }
                    break;
                case PaymentDueDayOfSeason:
                    if (paymentDue > 0)
                    {
                        this.AssessLateFee();
                    }
                    break;
                case LastDayOfSeason:
                    this.ClearPaidThisSeason();
                    this.ChangeLoanBalance((int)(this.RemainingBalance * this.Schedule.GetInterestRate(this.GetSeasonsSinceOrigination())), "Interest");
                    break;
            }
        }

        private void Autopay(int paymentDue)
        {
            if (paymentDue <= 0)
            {
                // already paid.
                this.LogTrace("Auto-pay did nothing as the player is already paid up");
            }
            else if (this.TryMakePayment(paymentDue))
            {
                this.SendMailAutoPaySucceeded(paymentDue);
            }
            else
            {
                this.SendMailAutoPayFailed(paymentDue);
            }
        }

        private int GetSeasonsSinceOrigination()
            => this.SeasonsSinceGameStart- this.GetPlayerModDataValueInt(OriginationSeasonModKey)!.Value;

        public int MinimumPayment
            => this.Schedule.GetMinimumPayment(this.GetSeasonsSinceOrigination(), this.RemainingBalance);

        public int RemainingBalance => this.GetBalance() ?? 0;

        public bool TryMakePayment(int amount)
        {
            if (this.Mod.Game1.PlayerMoney >= amount)
            {
                this.Mod.Game1.PlayerMoney -= amount;
                this.ChangeLoanBalance(-amount, "Payment");
                this.ChangePaidThisSeason(amount);
                if (this.GetBalance() == 0)
                {
                    this.SendLoanPaidOffMail();

                }
                return true;
            }
            else
            {
                return false;
            }
        }
            
        public bool IsLoanUnderway => this.GetBalance() > 0;
        public bool IsPaidOff => this.GetBalance() == 0;

        public bool IsOnAutoPay
        {
            get => this.Mod.Game1.GetPlayerModData(IsOnAutoPayModKey) is not null;
            set => this.Mod.Game1.SetPlayerModData(IsOnAutoPayModKey, value ? true.ToString(CultureInfo.InvariantCulture) : null);
        }

        private void AssessLateFee()
        {
            this.ChangeLoanBalance(LateFee, "Late Fee");
            this.SendMailMissedPayment();
            // TODO: Carry over minimum payment to next Season?
        }

        private void ChangeLoanBalance(int amount, string ledgerEntry)
        {
            // One Time Fees:
            //   xyz: 1000g
            //   abcdef: 700g
            // Interest: 123456g
            // Payment: -2345g
            this.SetPlayerModDataValue(LoanBalanceModKey, (this.GetBalance() ?? 0) + amount);
            this.AddLedgerLine($"{ledgerEntry}: {amount}");
        }

        private void AddLedgerLine(string s)
        {
            string? oldLedger = this.Mod.Game1.GetPlayerModData(SeasonLedgerModKey);
            string newEntry = (oldLedger is null ? "" : (oldLedger + Environment.NewLine)) + s;
            this.Mod.Game1.SetPlayerModData(SeasonLedgerModKey, newEntry);
        }

        private int? GetBalance() => this.GetPlayerModDataValueInt(LoanBalanceModKey);

        private int? GetPaidThisSeason() => this.GetPlayerModDataValueInt(PaidThisSeasonModKey);

        private void ChangePaidThisSeason(int amount)
            => this.SetPlayerModDataValue(PaidThisSeasonModKey, (this.GetPaidThisSeason() ?? 0) + amount);

        private void ClearPaidThisSeason()
            => this.SetPlayerModDataValue(PaidThisSeasonModKey, null);

        private int? GetPlayerModDataValueInt(string modDataKey)
        {
            string? strValue = this.Mod.Game1.GetPlayerModData(modDataKey);
            if (strValue is not null)
            {
                if (int.TryParse(strValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int balanceAsInt))
                {
                    return balanceAsInt;
                }
                else
                {
                    this.LogError($"{Game1.player.Name}'s ModData.{LoanBalanceModKey} is corrupt: '{strValue}'");
                    // Err.  Allow a new loan I guess.
                }
            }
            return null;
        }

        private void SetPlayerModDataValue(string modDataKey, int? value)
        {
            this.Mod.Game1.SetPlayerModData(modDataKey, value?.ToString(CultureInfo.InvariantCulture));
        }

        public void WriteToLog(string message, LogLevel level, bool isOnceOnly)
            => this.Mod.WriteToLog(message, level, isOnceOnly);

        private void SendStatementMail()
        {
            string ledger = this.Mod.Game1.GetPlayerModData(SeasonLedgerModKey) ?? "";
            int paymentDue = Math.Max(0, this.MinimumPayment - (this.GetPaidThisSeason() ?? 0));
            StringBuilder content = new StringBuilder();
            content.AppendLine($"Here is your complimentary JojaFinancial Furniture loan statement for {this.Mod.Game1.Date.Season.ToString()} of year {this.Mod.Game1.Date.Year}.");
            content.AppendLine();
            if (paymentDue > 0)
            {
                content.AppendLine($"Your minimum payment, due before the {PaymentDueDayString} of this season is: {paymentDue}g.");
            }
            else
            {
                content.AppendLine($"No payment is necessary this season.");
            }
            content.AppendLine();
            content.AppendLine($"Loan Balance: {this.GetBalance() ?? 0}");
            content.AppendLine();
            content.AppendLine("Activity:");
            content.AppendLine(ledger);

            this.Mod.Game1.SetPlayerModData(SeasonLedgerModKey, null);
            this.SendMail("statement", $"{this.Mod.Game1.Date.Season.ToString()} year {this.Mod.Game1.Date.Year} statement", content.ToString());
        }

        private void SendLoanPaidOffMail()
        {
            string ledger = this.Mod.Game1.GetPlayerModData(SeasonLedgerModKey) ?? "";
            StringBuilder content = new StringBuilder();
            content.AppendLine("Your JojaFinancial Furniture Loan is paid in full!  We know you have choices, and we're super-happy that you chose us.  Almost as happy as we are to have all that money!");
            content.AppendLine();
            content.AppendLine("Activity:");
            content.AppendLine(ledger);

            this.Mod.Game1.SetPlayerModData(SeasonLedgerModKey, null);
            this.SendMail("statement", $"{this.Mod.Game1.Date.Season.ToString()} year {this.Mod.Game1.Date.Year} statement", content.ToString());
        }

        private void SendMailAutoPaySucceeded(int amountPaid)
        {
            this.SendMail("autopay", "Autopay Succeeded", $@"Thank you for participating in JojaFinancial's AutoPay system!
Your payment of {amountPaid} was processed on {this.Mod.Game1.Date.Localize()}.");
        }

        private void SendMailAutoPayFailed(int amountOwed)
        {
            this.SendMail("autopay", "Auto-pay Failed!", $@"ALERT!  Your JojaFinancial Loan Automatic Payment of {amountOwed}g did not go through!
In order to avoid a penalty fee and possible interest rate increases, pay this amount by calling JojaFinancial's Super-Helpful Phone Assistant(tm) on or before the {PaymentDueDayString} of this month.");
        }

        private void SendMailMissedPayment()
        {
            this.SendMail("payment", "Missed Payment!", $@"CREDIT DISASTER IMPENDING!  You missed your payment for this season, and a fee of {LateFee}g has been imposed and added to your outstanding balance.");
        }

        public void SendMailLoanTerms()
        {
            ILoanSchedule schedule = new LoanSchedulePaidInFullWinter2();
            int loanAmount = 220000; // TODO: Figure out the list price of the furniture and wallpaper catalogs from game data.

            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine(
@"The JojaFinancial furniture loan is the ideal way to enjoy the fruits of your inevitable later success right now!
Call the Joja Super-Helpful Phone Assistant today to get your life of comfort in the mail tomorrow with no up-front fees,
no down-payment, no payments at all for the first two seasons, and no interest for the first season!

The JojaFinancial furniture loan features a payment system structured to your rise to financial security.
Sure, if your rosy view of the future doesn't end up coming to pass, it'll load you up with soul-crushing debt.
But that's your fault for going into a field where you're actually trying to make something of value instead
of finance where you sell dreams!  Our loan comes with a low 2%/season interest rate and while there are
some fees we have to charge in order to bring this wonderful opportunity to you, they're rolled into the loan
to allow us to give you two seasons with ZERO PAYMENTS!

There's a whole bunch of fine print below that you should definitely read.  If you do, please tell us and
we'll work harder in the future to make it even longer and finer so you won't next time.  What you really
want to do now is call up the Super-Helpful Phone Assistant and kick off this loan to bring yourself the
comforts of tomorrow today!".Replace("\r", "").Replace("\n\n", "||").Replace("\n", " ").Replace("||", "\n\n"));
            messageBuilder.AppendLine();

            string seasonAndYear(WorldDate date) => $"{date.Season} of year {date.Year}";

            messageBuilder.AppendLine(seasonAndYear(this.Mod.Game1.Date));
            messageBuilder.AppendLine($" Loan Amount: {loanAmount}g");
            messageBuilder.AppendLine($" Fees");
            int balance = loanAmount;
            foreach (var pair in this.GetFees(schedule, loanAmount))
            {
                messageBuilder.AppendLine($"  {pair.amount}g {pair.name}");
                balance += pair.amount;
            }
            messageBuilder.AppendLine($" Opening Balance: {balance}g");

            // The *Season variables here are really season+year*4.
            int startingSeason = this.Mod.Game1.Date.TotalWeeks / 4;
            for (int currentSeason = this.Mod.Game1.Date.TotalWeeks/4; balance > 0; ++currentSeason)
            {
                WorldDate paymentDate = new WorldDate(1 + (currentSeason / 4), (Season)(currentSeason % 4), Loan.PaymentDueDayOfSeason);
                if (paymentDate > this.Mod.Game1.Date)
                {
                    messageBuilder.AppendLine();
                    messageBuilder.AppendLine(seasonAndYear(paymentDate));

                    int payment = schedule.GetMinimumPayment(currentSeason - startingSeason, balance);
                    messageBuilder.AppendLine($" Payment: -{payment}g");
                    balance -= payment;

                    int interest = (int)(balance * schedule.GetInterestRate(currentSeason - startingSeason));
                    balance += interest;
                    messageBuilder.AppendLine($" Interest: {interest}g");

                    messageBuilder.AppendLine($" Remaining balance: {balance}g");
                }
            }

            this.SendMail("terms", "Furniture loan terms", messageBuilder.ToString());
        }

        private IEnumerable<(string name, int amount)> GetFees(ILoanSchedule schedule, int loanAmount)
        {
            yield return ("Loan origination fee", schedule.GetLoanOriginationFeeAmount(loanAmount));
            yield return ("SuperHelpful phone service fee", 1700);
            yield return ("Personal visit fee", 1300);
            yield return ("Statement preparation fee", 1000);
            yield return ("Complimentary phone fee", 1000);
        }

        public void InitiateLoan()
        {
            int loanAmount = 220000; // TODO: Figure out the list price of the furniture and wallpaper catalogs from game data.
            this.ChangeLoanBalance(loanAmount, "Principal");

            // Consider changing things up if the loan is initiated after the 21st so it doesn't go around assessing fees.
            // That doesn't matter now since we're hard-coding a loan that has no minimum payment for the first two seasons.
            this.SetPlayerModDataValue(OriginationSeasonModKey, this.SeasonsSinceGameStart);
            this.SetPlayerModDataValue(LoanBalanceModKey, loanAmount + this.GetFees(this.Schedule, loanAmount).Sum(f => f.amount));
            this.SetPlayerModDataValue(PaidThisSeasonModKey, null);
            this.SendWelcomeMail();
        }

        private void SendWelcomeMail()
        {
            this.SendMail(
                "welcome",
                "Your Furniture Catalog",
                @"JojaFinancial is so pleased that you have taken your first steps towards comfortable living and a solid credit rating!  Watch your mail in the coming months for reminders about making your EZ Payments later on this year.

                - the JojaFinancial Team",
                "(F)1226",
                "(F)1308");
        }

        // Possibly allow a refinance to different terms at some point.
        private ILoanSchedule Schedule => new LoanSchedulePaidInFullWinter2();

        private void SendMail(string idPrefix, string synopsis, string message, params string[] attachedItemQiids)
        {
            this.Mod.GeneratedMail.SendMail(idPrefix, synopsis, message, attachedItemQiids);
        }
        private int SeasonsSinceGameStart => this.Mod.Game1.Date.TotalWeeks / 4; // 4 weeks per season...

    }
}
