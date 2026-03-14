namespace backend.Interfaces
{
    public interface IOnlineTracker
    {
        bool IsUserInLoanGroup(string userId, int loanId);
    }
}
