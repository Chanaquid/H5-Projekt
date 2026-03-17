namespace backend.Interfaces
{
    public interface IOnlineTracker
    {
        void Add(string connectionId, string userId, int loanId);
        void Remove(string connectionId);
        bool IsUserInLoanGroup(string userId, int loanId);
    }
}