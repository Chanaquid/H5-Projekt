
namespace backend.Models
{
    public class Loan
    {
        public int Id { get; set; }

        //Item info - it has owner info in it 
        public int ItemId { get; set; }
        public Item Item { get; set; } = null!;

        //Loaner Info
        public string BorrowerId { get; set; } = string.Empty;
        public ApplicationUser Borrower { get; set; } = null!;

        //DAtes
        public DateTime StartDate {  get; set; }
        public DateTime EndDate { get; set; } //Capped at Item.AvailableUntil

        public DateTime? ActualReturnDate { get; set; }


        //Extension Date
        public DateTime? RequestedExtensionDate { get; set; } //New end date requested by borrower
        public ExtensionStatus? ExtensionRequestStatus { get; set; } //Pending/Approved/Rejected

        //Status
        public LoanStatus Status { get; set; } = LoanStatus.Pending;
        public ItemCondition SnapshotCondition { get; set; }


        //Admin
        public string? DecisionNote { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt {  get; set; }


        //Navigation
        public ICollection<LoanSnapshotPhoto> SnapshotPhotos { get; set; } = new List<LoanSnapshotPhoto>();
        public ICollection<Fine> Fines { get; set; } = new List<Fine>(); //One loan can have multiple fines like late + damaged/lost
        public ICollection<LoanMessage> Messages { get; set; } = new List<LoanMessage>(); //All messages are scoped to a loan
        public ICollection<Dispute> Disputes { get; set; } = new List<Dispute>();




    }
}
