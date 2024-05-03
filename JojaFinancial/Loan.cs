using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace StardewValleyMods.JojaFinancial
{
    public class Loan : ISimpleLog
    {
        private ModEntry mod = null!;

        private const string LoanBalanceModKey = "JojaFinancial.LoanBalance";
        private const string PaidThisSeasonModKey = "JojaFinancial.PaidThisMonth";
        private const string IsOnAutoPayModKey = "JojaFinancial.Autopay";
        private const string OriginationSeasonModKey = "JojaFinancial.OriginationSeason";

        private const int LastDayOfSeason = 28;
        public const int PaymentDueDayOfSeason = 21;
        public const string PaymentDueDayString = "21st";
        private const int AutoPayDayOfSeason = 16;

        private const int LateFee = 1000;

        public void Entry(ModEntry mod)
        {
            this.mod = mod;
            mod.Helper.Events.GameLoop.DayEnding += this.GameLoop_DayEnding;
        }

        private void GameLoop_DayEnding(object? sender, DayEndingEventArgs e)
        {
            this.OnDayEnding(Game1.Date);
        }

        protected void OnDayEnding(WorldDate date)
        {
            if (!this.IsLoanUnderway)
            {
                return;
            }

            int paymentDue = this.MinimumPayment - (this.GetPaidThisMonth() ?? 0);
            switch (Game1.Date.DayOfMonth)
            {
                case AutoPayDayOfSeason:
                    this.Autopay(paymentDue);
                    break;
                case PaymentDueDayOfSeason:
                    if (paymentDue > 0)
                    {
                        this.AssessLateFee();
                    }
                    break;
                case LastDayOfSeason:
                    this.ClearPaidThisMonth();
                    this.ChangeLoanBalance((int)(this.RemainingBalance * this.Schedule.GetInterestRate(this.GetSeasonsSinceOrigination())));
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
        {
            int currentMonth = Game1.Date.TotalDays / LastDayOfSeason;
            return this.GetPlayerModDataValue(OriginationSeasonModKey)!.Value - currentMonth;
        }

        public int MinimumPayment => this.Schedule.GetMinimumPayment(this.GetSeasonsSinceOrigination(), this.RemainingBalance);

        public int RemainingBalance => this.GetBalance() ?? 0;

        public bool TryMakePayment(int amount)
        {
            if (Game1.player.Money > amount)
            {
                Game1.player.Money -= amount;
                this.ChangeLoanBalance(amount);
                this.ChangePaidThisMonth(amount);
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
            get => Game1.player.modData.ContainsKey(IsOnAutoPayModKey);
            set => Game1.player.modData[IsOnAutoPayModKey] = true.ToString(CultureInfo.InvariantCulture);
        }

        private void AssessLateFee()
        {
            this.ChangeLoanBalance(LateFee);
            this.SendMailMissedPayment();
            // TODO: Carry over minimum payment to next month?
        }

        private void ChangeLoanBalance(int amount)
        {
            Game1.player.modData[LoanBalanceModKey] = ((this.GetBalance() ?? 0) + amount).ToString(CultureInfo.InvariantCulture);
        }

        private int? GetBalance() => this.GetPlayerModDataValue(LoanBalanceModKey);

        private int? GetPaidThisMonth() => this.GetPlayerModDataValue(PaidThisSeasonModKey);
        private void ChangePaidThisMonth(int amount)
            => this.SetPlayerModDataValue(PaidThisSeasonModKey, (this.GetPaidThisMonth() ?? 0) + amount);
        private void ClearPaidThisMonth()
            => this.SetPlayerModDataValue(PaidThisSeasonModKey, null);

        protected virtual int? GetPlayerModDataValue(string modDataKey)
        {
            if (Game1.player.modData.TryGetValue(modDataKey, out string balance))
            {
                if (int.TryParse(balance, NumberStyles.Integer, CultureInfo.InvariantCulture, out int balanceAsInt))
                {
                    return balanceAsInt;
                }
                else
                {
                    this.LogError($"{Game1.player.Name}'s ModData.{LoanBalanceModKey} is corrupt: '{balance}'");
                    // Err.  Allow a new loan I guess.
                }
            }
            return null;
        }

        protected virtual void SetPlayerModDataValue(string modDataKey, int? value)
        {
            if (value.HasValue)
            {
                Game1.player.modData[modDataKey] = value.Value.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                Game1.player.modData.Remove(modDataKey);
            }
        }

        public void WriteToLog(string message, LogLevel level, bool isOnceOnly)
            => this.mod.WriteToLog(message, level, isOnceOnly);

        private void SendMailAutoPaySucceeded(int amountPaid)
        {
            this.SendMail("autopay", $@"Thank you for participating in JojaFinancial's AutoPay system!
Your payment of {amountPaid} was processed on {Game1.Date.Localize()}.");
        }

        private void SendMailAutoPayFailed(int amountOwed)
        {
            var dueDate = new WorldDate(Game1.year, Game1.season, PaymentDueDayOfSeason);
            this.SendMail("autopay", $@"ALERT!  Your JojaFinancial Loan Automatic Payment of {amountOwed}g did not go through!
In order to avoid a penalty fee and possible interest rate increases, pay this amount by calling the SuperHelpful JojaFinancial Phone System(tm) on or before {dueDate.Localize()}.");
        }

        private void SendMailMissedPayment()
        {
            this.SendMail("autopay", $@"CREDIT DISASTER IMPENDING!  You missed your payment for this month, and a fee of {LateFee}g has been imposed and added to your outstanding balance.");
        }

        public void SendMailLoanTerms()
        {
            ILoanSchedule schedule = new LoanSchedulePaidInFullWinter2();
            int loanAmount = 220000; // TODO: Figure out the list price of the furniture and wallpaper catalogs from game data.

            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine(@"The JojaFinancial furniture loan is the ideal way to enjoy the fruits of your inevitable later success right now!
It features a payment system structured to your rise to financial security.  Sure, if your rosy view of the future doesn't end up coming
to pass, it'll load you up with soul-crushing debt.  But that's your fault for going into a field where you're actually trying to make something of
value instead of finance where we sell dreams!  Our loan comes with a low 2%/season interest rate and while there are some
fees we have to charge in order to bring this wonderful opportunity to you, they're rolled into the loan to allow us to give you two seasons with
ZERO PAYMENTS!");
            messageBuilder.AppendLine();
            messageBuilder.AppendLine(@"There's a whole bunch of fine print below that you should definitely read.  If you do, we'll work harder
in the future to make it even longer and finer so you won't next time.  What you really want to do now is call up the Super-Helpful phone assistant
and kick off this loan to bring yourself the comforts of tomorrow today!");
            messageBuilder.AppendLine();

            messageBuilder.AppendLine($"{Game1.Date.Localize()} - Loan Amount: {loanAmount}g");
            messageBuilder.AppendLine($"Fees");
            int balance = loanAmount;
            foreach (var pair in this.GetFees(schedule, loanAmount))
            {
                messageBuilder.AppendLine($"  {pair.amount}g {pair.name}");
                balance += pair.amount;
            }
            messageBuilder.AppendLine($"Total Owed: {loanAmount}g");
            for (int i = 0; balance > 0; ++i)
            {
                WorldDate paymentDate = new WorldDate(i / 2, (Season)(i % 4), Loan.PaymentDueDayOfSeason);
                int payment = schedule.GetMinimumPayment(i, balance);
                messageBuilder.AppendLine($"{paymentDate.Localize()} payment: ({payment}g)");
                balance -= payment;

                const int LastDayOfMonth = 28;
                WorldDate endOfMonthDate = new WorldDate(i / 2, (Season)(i % 4), LastDayOfMonth);
                int interest = (int)(balance * schedule.GetInterestRate(i));
                balance += interest;
                messageBuilder.AppendLine($"{endOfMonthDate.Localize()} interest: {payment}g");
                messageBuilder.AppendLine($"Remaining balance: {balance}g");
            }

            this.SendMail("terms", messageBuilder.ToString());
        }


        private IEnumerable<(string name, int amount)> GetFees(ILoanSchedule schedule, int loanAmount)
        {
            yield return ("Loan origination fee", schedule.GetLoanOriginationFeeAmount(loanAmount));
            yield return ("SuperHelpful phone service fee", 1700);
            yield return ("Personal visit fee", 1300);
            yield return ("Do we even need a reason fee", 1000);
        }

        protected virtual void SendMail(string idPrefix, string message)
        {
            this.LogInfo($"SendMail({idPrefix}):\r\n{message}");
        }

        public void InitiateLoan()
        {
            int loanAmount = 220000; // TODO: Figure out the list price of the furniture and wallpaper catalogs from game data.

            // Consider changing things up if the loan is initiated after the 21st so it doesn't go around assessing fees.
            // That doesn't matter now since we're hard-coding a loan that has no minimum payment for the first two months.
            this.SetPlayerModDataValue(OriginationSeasonModKey, Game1.Date.TotalDays / LastDayOfSeason);
            this.SetPlayerModDataValue(LoanBalanceModKey, loanAmount + this.GetFees(this.Schedule, loanAmount).Sum(f => f.amount));
            this.SetPlayerModDataValue(PaidThisSeasonModKey, null);
        }


        // Possibly allow a refinance to different terms at some point.
        private ILoanSchedule Schedule => new LoanSchedulePaidInFullWinter2();
    }
}
