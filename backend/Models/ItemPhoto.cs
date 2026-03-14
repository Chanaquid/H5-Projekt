namespace backend.Models
{
    public class ItemPhoto
    {
        public int Id { get; set; }

        //Item info
        public int ItemId { get; set; }
        public Item Item { get; set; } = null!;
    
        //ImageUrl possibly from uploadthing (maybe)
        public string PhotoUrl { get; set; } = string.Empty;

        public bool IsPrimary { get; set; } = false; //Thumbnail

        public int DisplayOrder { get; set; } = 0;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;


    
    }
}
