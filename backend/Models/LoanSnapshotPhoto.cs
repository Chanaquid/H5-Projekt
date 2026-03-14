namespace backend.Models
{
    public class LoanSnapshotPhoto
    {
        public int Id { get; set; }

        //Loan info
        public int LoanId { get; set; }
        public Loan Loan { get; set; } = null!;

        //Item info
        public string PhotoUrl { get; set; } = string.Empty;
        public int DisplayOrder { get; set; } = 0; //Preserved display order from the original ItemPhoto

        public DateTime SnapshotTakenAt { get; set; } = DateTime.UtcNow;

    }
}
