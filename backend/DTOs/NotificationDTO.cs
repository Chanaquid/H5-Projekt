namespace backend.DTOs
{
    public class NotificationDTO
    {
        //--------------REQUESTS-----------------
        public class NotificationResponseDTO
        {
            public int Id { get; set; }
            public string Type { get; set; } = string.Empty;  //e.g. "LoanApproved", "FineIssued"
            public string Message { get; set; } = string.Empty; //e.g. "Your loan for 'Guitar' has been approved."
            public int? ReferenceId { get; set; }
            public string? ReferenceType { get; set; } //"Loan", "Item", "Dispute", "Appeal", "Fine"
            public bool IsRead { get; set; }
            public DateTime CreatedAt { get; set; }
        }


        //Returned for the bell icon — unread count + last 10
        public class NotificationSummaryDTO
        {
            public int UnreadCount { get; set; }
            public List<NotificationResponseDTO> Recent { get; set; } = new();
        }
    }
}
