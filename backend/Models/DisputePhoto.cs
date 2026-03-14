namespace backend.Models
{
    public class DisputePhoto
    {
        public int Id { get; set; }

        //Dispute info
        public int DisputeId { get; set; }
        public Dispute Dispute { get; set; } = null!;


        //Who submitted the pics
        public string SubmittedById { get; set; } = string.Empty;
        public ApplicationUser SubmittedBy { get; set; } = null!;
        public string PhotoUrl { get; set; } = string.Empty;

        public string? Caption { get; set; } //f.x. scratches on the front, dent on the side, etc etc

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;



    }
}
