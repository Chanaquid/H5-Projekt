namespace backend.Models
{
    public class Category
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty; //Books, Games, Power Tools, etc

        public string? Icon { get; set; } //Optional emoji or icon identifier for UI display

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        //Navigation
        public ICollection<Item> Items { get; set; } = new List<Item>();




    }
}
