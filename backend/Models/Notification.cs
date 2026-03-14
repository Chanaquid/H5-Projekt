
namespace backend.Models
{
    public class Notification
    {

        public int Id { get; set; }

        //Notification receiver
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;
        public NotificationType Type { get; set; }
        public string Message { get; set; } = string.Empty; //f.x. Your loan request for 'Guitar' has been approved.


        //ID of the related entity - Loan, Dispute, Appeal, Fine,etc
        public int? ReferenceId { get; set; }
        public NotificationReferenceType? ReferenceType { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;



    }
}
