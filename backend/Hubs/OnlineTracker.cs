using backend.Interfaces;

namespace backend.Hubs
{
    public class OnlineTracker : IOnlineTracker
    {
        private readonly Dictionary<string, (string UserId, int LoanId)> _connections = new();
        private readonly object _lock = new();

        public void Add(string connectionId, string userId, int loanId)
        {
            lock (_lock)
            {
                _connections[connectionId] = (userId, loanId);
            }
        }

        public void Remove(string connectionId)
        {
            lock (_lock)
            {
                _connections.Remove(connectionId);
            }
        }

        public bool IsUserInLoanGroup(string userId, int loanId)
        {
            lock (_lock)
            {
                return _connections.Values.Any(v => v.UserId == userId && v.LoanId == loanId);
            }
        }


    }
}
