using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Buildings;

namespace StardewValleyMods.JojaFinancial
{
    public class Loan : ISimpleLog
    {
        public ModEntry Mod { get; private set; } = null!;

        private const string LoanBalanceModKey = "JojaFinancial.LoanBalance";
        private const string PaidThisSeasonModKey = "JojaFinancial.PaidThisSeason";
        private const string IsOnAutoPayModKey = "JojaFinancial.Autopay";
        private const string OriginationSeasonModKey = "JojaFinancial.OriginationSeason";
        private const string YearsToPayModKey = "JojaFinancial.LoanSchedule";
        private const string SeasonLedgerModKey = "JojaFinancial.SeasonLedger";

        private const int LastDayOfSeason = 28;
        public const int PaymentDueDayOfSeason = 21; // Has to line up with default.json (and all the translations)
        public const int PrepareStatementDayOfSeason = 13;
        public const int AutoPayDayOfSeason = 16; // Has to line up with default.json (and all the translations)

        public const int MissedPaymentFee = 5000;

        public void Entry(ModEntry mod)
        {
            this.Mod = mod;
            mod.Helper.Events.GameLoop.DayEnding += this.GameLoop_DayEnding;
            mod.Helper.Events.GameLoop.SaveLoaded += this.GameLoop_SaveLoaded;
            mod.Helper.Events.Content.AssetRequested += this.Content_AssetRequested;
        }

        private static string? oldGoldClockBuilder;

        private void Content_AssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Buildings"))
            {
                e.Edit(editor =>
                {
                    var buildingData = editor.AsDictionary<string, BuildingData>().Data;
                    if (Game1.player is null)
                    {
                        this.LogTrace("Skipping modifying the gold clock - the player doesn't exist yet.");
                        return;
                    }

                    if (!buildingData.TryGetValue("Gold Clock", out BuildingData? goldClock))
                    {
                        this.LogWarning($"The gold clock purchase can't be disabled - it doesn't exist.");
                        return;
                    }

                    if (this.IsLoanUnderway)
                    {
                        if (oldGoldClockBuilder is null && goldClock.Builder is not null)
                        {
                            oldGoldClockBuilder = goldClock.Builder;
                            this.LogTrace($"Storing '{oldGoldClockBuilder}' as the builder for the gold clock.");
                        }

                        this.LogTrace("Disabling the gold clock as the loan is underway");
                        goldClock.Builder = null;
                    }
                    else
                    {
                        if (goldClock.Builder is null)
                        {
                            goldClock.Builder = oldGoldClockBuilder ?? "Wizard";
                            this.LogTrace($"Re-enabling the gold clock by setting it to {goldClock.Builder} - stashed value was {oldGoldClockBuilder}");
                        }
                    }
                });
            }
        }

        private void InvalidateBuildingData()
        {
            this.Mod.Helper.GameContent.InvalidateCache("Data/Buildings");
        }

        private void GameLoop_SaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            this.InvalidateBuildingData();
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
                // this.LogTrace("Auto-pay did nothing as the player is already paid up");
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
            => this.SeasonsSinceGameStart - this.GetPlayerModDataValueInt(OriginationSeasonModKey)!.Value;

        public int MinimumPayment
            => this.Schedule.GetMinimumPayment(this.GetSeasonsSinceOrigination(), this.RemainingBalance);

        public int RemainingBalance => this.GetBalance() ?? 0;

        public bool TryMakePayment(int amount)
        {
            if (this.Mod.Game1.PlayerMoney >= amount)
            {
                this.Mod.Game1.PlayerMoney -= amount;
                this.ChangeLoanBalance(-amount, I18n.Loan_Payment());
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
            this.ChangeLoanBalance(MissedPaymentFee, I18n.Loan_LateFee());
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

        // Shouldn't need this - these strings should be in the game somewhere, but I struggled to find it.
        static string Localize(Season season) =>
            season switch
            {
                Season.Spring => I18n.Loan_SeasonSpring(),
                Season.Summer => I18n.Loan_SeasonSummer(),
                Season.Fall => I18n.Loan_SeasonFall(),
                _ => I18n.Loan_SeasonWinter()
            };
        string LocalizedSeason => Localize(this.Mod.Game1.Date.Season);

        private void SendStatementMail()
        {
            string ledger = this.Mod.Game1.GetPlayerModData(SeasonLedgerModKey) ?? "";
            int paymentDue = Math.Max(0, this.MinimumPayment - (this.GetPaidThisSeason() ?? 0));
            StringBuilder content = new StringBuilder();
            content.AppendLine(I18n.Loan_StatementHeader(season: this.LocalizedSeason, year: this.Mod.Game1.Date.Year));
            content.AppendLine();
            if (paymentDue > 0)
            {
                content.Append(I18n.Loan_StatementPayment(paymentDue));
                if (this.IsOnAutoPay)
                {
                    content.Append(I18n.Loan_StatementAutopayAddendum());
                }
                content.AppendLine();
            }
            else
            {
                content.AppendLine(I18n.Loan_StatementNoPay());
            }
            content.AppendLine();
            content.AppendLine(I18n.Loan_StatementBalance(this.GetBalance() ?? 0));
            content.AppendLine();
            content.AppendLine(I18n.Loan_StatementActivity());
            content.AppendLine(ledger);

            this.Mod.Game1.SetPlayerModData(SeasonLedgerModKey, null);
            this.SendMail(
                "statement"
                , I18n.Loan_StatementTitle(this.LocalizedSeason, this.Mod.Game1.Date.Year)
                , content.ToString());
        }

        private void SendLoanPaidOffMail()
        {
            string ledger = this.Mod.Game1.GetPlayerModData(SeasonLedgerModKey) ?? "";
            StringBuilder content = new StringBuilder();
            content.AppendLine(I18n.Loan_PaidInFull());
            content.AppendLine();
            content.AppendLine(I18n.Loan_StatementActivity());
            content.AppendLine(ledger);

            this.Mod.Game1.SetPlayerModData(SeasonLedgerModKey, null);
            this.SendMail("statement", I18n.Loan_StatementTitle(this.LocalizedSeason, this.Mod.Game1.Date.Year), content.ToString());
            this.InvalidateBuildingData();
        }

        private void SendMailAutoPaySucceeded(int amountPaid)
        {
            this.SendMail(
                "autopay"
                , I18n.Loan_AutopaySucceededTitle(this.LocalizedSeason, this.Mod.Game1.Date.Year)
                , I18n.Loan_AutopaySucceededBody(amountPaid));
        }

        private void SendMailAutoPayFailed(int amountOwed)
        {
            this.SendMail("autopay",
                I18n.Loan_AutopayFailedTitle(this.LocalizedSeason, this.Mod.Game1.Date.Year),
                I18n.Loan_AutopayFailedBody(amountOwed));
        }

        private void SendMailMissedPayment()
        {
            this.SendMail("payment",
                I18n.Loan_MissedPaymentTitle(this.LocalizedSeason, this.Mod.Game1.Date.Year),
                I18n.Loan_MissedPaymentBody(MissedPaymentFee));
        }

        private int GetTotalCostOfCatalogs()
            => this.Mod.GetConfiguredCatalogs().Sum(i => i.Price);

        public void SendMailLoanTerms(ILoanSchedule schedule)
        {
            int loanAmount = this.GetTotalCostOfCatalogs();
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine(I18n.Loan_TermsPara1());
            messageBuilder.AppendLine();
            messageBuilder.AppendLine(I18n.Loan_TermsPara2());
            messageBuilder.AppendLine();
            messageBuilder.AppendLine(I18n.Loan_TermsPara3());
            messageBuilder.AppendLine();

            string seasonAndYear(WorldDate date) => I18n.Loan_SeasonAndYear(Localize(date.Season), date.Year);

            messageBuilder.AppendLine(seasonAndYear(this.Mod.Game1.Date));
            messageBuilder.AppendLine(I18n.Loan_TermsLoanAmount(loanAmount));
            messageBuilder.AppendLine(I18n.Loan_TermsFees());
            int balance = loanAmount;
            int totalFeesAndInterest = 0;
            foreach (var pair in this.GetFees(schedule, loanAmount))
            {
                messageBuilder.AppendLine($"  {pair.amount}g {pair.name}");
                balance += pair.amount;
                totalFeesAndInterest += pair.amount;
            }
            messageBuilder.AppendLine(I18n.Loan_TermsOpeningBalance(balance));

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
                    messageBuilder.AppendLine(I18n.Loan_TermsPayment(payment));
                    balance -= payment;

                    int interest = (int)(balance * schedule.GetInterestRate(currentSeason - startingSeason));
                    balance += interest;
                    totalFeesAndInterest += interest;
                    messageBuilder.AppendLine(I18n.Loan_TermsInterest(interest));
                    messageBuilder.AppendLine(I18n.Loan_TermsRemainingBalance(balance));
                }
            }

            messageBuilder.AppendLine();
            messageBuilder.AppendLine(I18n.Loan_TermsTotalCost(totalFeesAndInterest));
            messageBuilder.AppendLine(I18n.Loan_TermsMissedFee(MissedPaymentFee));

            this.SendMail("terms." + schedule.GetType().Name, I18n.Loan_TermsTitle(), messageBuilder.ToString());
        }

        private IEnumerable<(string name, int amount)> GetFees(ILoanSchedule schedule, int loanAmount)
        {
            yield return (I18n.Loan_FeesOrigination(), schedule.GetLoanOriginationFeeAmount(loanAmount));
            yield return (I18n.Loan_FeesAnswerer(), 1700);
            yield return (I18n.Loan_FeesVisit(), 1300);
            yield return (I18n.Loan_FeesStatement(), 1000);
            yield return (I18n.Loan_FeesPhone(), 1000);
        }

        public void InitiateLoan(ILoanSchedule schedule)
        {
            int loanAmount = this.GetTotalCostOfCatalogs();
            this.ChangeLoanBalance(loanAmount, I18n.Loan_LedgerPrincipal());

            // Consider changing things up if the loan is initiated after the 21st so it doesn't go around assessing fees.
            // That doesn't matter now since we're hard-coding a loan that has no minimum payment for the first two seasons.
            this.SetPlayerModDataValue(OriginationSeasonModKey, this.SeasonsSinceGameStart);
            this.SetPlayerModDataValue(YearsToPayModKey, schedule is LoanScheduleTwoYear ? 2 : 3);
            this.SetPlayerModDataValue(LoanBalanceModKey, loanAmount + this.GetFees(this.Schedule, loanAmount).Sum(f => f.amount));
            this.SetPlayerModDataValue(PaidThisSeasonModKey, null);
            this.SendWelcomeMail();
            this.InvalidateBuildingData();
        }

        private void SendWelcomeMail()
        {
            this.SendMail(
                "welcome",
                I18n.Loan_WelcomeTitle(),
                I18n.Loan_WelcomeContent(),
                this.Mod.GetConfiguredCatalogs().Select(i => i.QualifiedItemId).ToArray());
        }

        // Possibly allow a refinance to different terms at some point.
        private ILoanSchedule Schedule => this.GetPlayerModDataValueInt(YearsToPayModKey) == 3
            ? new LoanScheduleThreeYear()
            : new LoanScheduleTwoYear();

        private void SendMail(string idPrefix, string synopsis, string message, params string[] attachedItemQiids)
        {
            this.Mod.GeneratedMail.SendMail(idPrefix, synopsis, message, attachedItemQiids);
        }
        private int SeasonsSinceGameStart => this.Mod.Game1.Date.TotalWeeks / 4; // 4 weeks per season...

    }
}
