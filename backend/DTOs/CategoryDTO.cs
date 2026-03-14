namespace backend.DTOs
{
    //-----------REQUESTS----------------
    public class CategoryDTO
    {
        //Admin creating a category
        public class CreateCategoryDTO
        {
            public string Name { get; set; } = string.Empty;
            public string? Icon { get; set; }
        }

        //Admin updates an existing category
        public class UpdateCategoryDTO
        {
            public string Name { get; set; } = string.Empty;
            public string? Icon { get; set; }
            public bool IsActive { get; set; }
        }

        //-----------RESPONSES----------------
        public class CategoryResponseDTO
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? Icon { get; set; }
            public bool IsActive { get; set; }
            public int ItemCount { get; set; } //How many approved + active items are in this category
        }

    }
}
