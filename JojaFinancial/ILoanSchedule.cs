namespace StardewValleyMods.JojaFinancial
{
    public interface ILoanSchedule
    {
        int GetMinimumPayment(int seasonsSinceLoanOrigination, int remainingBalance);
        public double GetInterestRate(int seasonsSinceLoanOrigination);
        public int GetLoanOriginationFeeAmount(int loanAmount);
    }
}

