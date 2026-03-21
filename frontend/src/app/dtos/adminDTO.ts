import { DisputeDTO } from "./disputeDTO";
import { FineDTO } from "./fineDTO";
import { LoanDTO } from "./loanDTO";

export namespace AdminDTO {
  export interface AdminDashboardDTO {
    //Action queues
    pendingItemApprovals: number;
    pendingLoanApprovals: number;
    openDisputes: number;
    pendingAppeals: number;
    pendingUserVerifications: number;
    pendingPaymentVerifications: number;
    //Platform stats
    totalUsers: number;
    totalActiveItems: number;
    totalActiveLoans: number;
    totalUnpaidFines: number;
    totalUnpaidFinesAmount: number;
  }
 
   export interface ItemHistoryDTO {
    itemId: number;
    itemTitle: string;
    ownerName: string;
    averageRating: number;
    reviewCount: number;
    reviews: ItemReviewEntryDTO[];
    loans: LoanHistoryEntryDTO[];
  }

  export interface ItemReviewEntryDTO {
    id: number;
    reviewerId: string;
    reviewerName: string;
      reviewerAvatarUrl?: string;
    rating: number;
    comment?: string;
    isAdminReview: boolean;
    createdAt: string;
  }
 
  export interface LoanHistoryEntryDTO {
    loanId: number;
    borrowerName: string;
    startDate: string;
    endDate: string;
    actualReturnDate?: string;
    status: string;
    snapshotCondition: string;
    snapshotPhotos: LoanDTO.LoanSnapshotPhotoDTO[];
    fines: FineDTO.FineResponseDTO[];
    disputes: DisputeDTO.DisputeSummaryDTO[];
  }
}