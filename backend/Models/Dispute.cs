
namespace backend.Models
{
    public class Dispute
    {
        public int Id { get; set; }

        //Loan info
        public int LoanId { get; set; }
        public Loan Loan { get; set; } = null!;

        //The user who initiated the dispute
        public string FiledById { get; set; } = string.Empty;
        public ApplicationUser FiledBy { get; set; } = null!;

        public DisputeFiledAs FiledAs { get; set; } //As owner or as borrower
        public string Description { get; set; } = string.Empty; //Why the dispute was filed


        //From the other party
        public string? ResponseDescription { get; set; } //Response from the other party (null until they submit their side within the 72h window )
        public DateTime ResponseDeadline { get; set; } = DateTime.UtcNow.AddHours(72); //Deadline for the other party to submit their response (72 hrs)
        public DisputeStatus Status { get; set; } = DisputeStatus.Open;


        //Admin final verdict
        public DisputeVerdict? AdminVerdict { get; set; }

        public decimal? CustomFineAmount { get; set; } //when AdminVerdict = PartialDamage

        public string? AdminNote { get; set; }

        //Admin info who resolved this dispute
        public string? ResolvedByAdminId { get; set; }
        public ApplicationUser? ResolvedByAdmin { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }


        //Evidence
        public ICollection<DisputePhoto> Photos { get; set; } = new List<DisputePhoto>();
        public ICollection<Fine> Fines { get; set; } = new List<Fine>();



    }
}
