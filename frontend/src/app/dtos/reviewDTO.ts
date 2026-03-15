export namespace ReviewDTO {
  // Requests
  export interface CreateItemReviewDTO {
    loanId?: number;
    itemId: number;
    rating: number;
    comment?: string;
  }
 
  export interface CreateUserReviewDTO {
    loanId?: number;
    reviewedUserId: string;
    rating: number;
    comment?: string;
  }
 
  export interface EditReviewDTO {
    rating: number;
    comment?: string;
  }
 
  // Responses
  export interface ItemReviewResponseDTO {
    id: number;
    loanId?: number;
    rating: number;
    comment?: string;
    reviewerName: string;
    reviewerAvatarUrl?: string;
    createdAt: string;
    isAdminReview: boolean;
    isEdited: boolean;
    editedAt?: string;
  }
 
  export interface UserReviewResponseDTO {
    id: number;
    loanId?: number;
    itemTitle: string;
    rating: number;
    comment?: string;
    reviewerName: string;
    reviewerAvatarUrl?: string;
    createdAt: string;
    isAdminReview: boolean;
    isEdited: boolean;
    editedAt?: string;
  }
}