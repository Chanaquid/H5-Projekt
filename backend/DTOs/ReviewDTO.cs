namespace backend.DTOs
{
    public class ReviewDTO
    {
        //-------------------REQUESTS-----------------------

        //Borrower leaves a review for the item after loan is completed
        public class CreateItemReviewDTO
        {
            public int? LoanId { get; set; }  //Optional for admin
            public int ItemId { get; set; }   //Admin specifies item directly
            public int Rating { get; set; }
            public string? Comment { get; set; }
        }

        //Either party leaves a review for the other user after loan is completed
        public class CreateUserReviewDTO
        {
            public int? LoanId { get; set; } //Optionalf for admin
            public string ReviewedUserId { get; set; } = string.Empty;
            public int Rating { get; set; }  //1–5 stars
            public string? Comment { get; set; }
        }


        //-------------------RESPONSES-----------------------
        //Item review — shown on the item detail page
        public class ItemReviewResponseDTO
        {
            public int Id { get; set; }
            public int? LoanId { get; set; }
            public int Rating { get; set; }
            public string? Comment { get; set; }
            public string ReviewerName { get; set; } = string.Empty;
            public string? ReviewerAvatarUrl { get; set; }
            public DateTime CreatedAt { get; set; }

            //ADMIN
            public bool IsAdminReview { get; set; }
            public bool IsEdited { get; set; }
            public DateTime? EditedAt { get; set; }
        }

        //User review — shown on a user's profile page
        public class UserReviewResponseDTO
        {
            public int Id { get; set; }
            public int? LoanId { get; set; }
            public string ItemTitle { get; set; } = string.Empty;  //Context for what loan this review is from
            public int Rating { get; set; }
            public string? Comment { get; set; }
            public string ReviewerName { get; set; } = string.Empty;
            public string? ReviewerAvatarUrl { get; set; }
            public DateTime CreatedAt { get; set; }

            //ADMIN REVIEW
            public bool IsAdminReview { get; set; }
            public bool IsEdited { get; set; }
            public DateTime? EditedAt { get; set; }
        }

        //Admin edits their review
        public class EditReviewDTO
        {
            public int Rating { get; set; }
            public string? Comment { get; set; }
        }

    }
}
