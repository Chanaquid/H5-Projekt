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
    pendingVerifications: number;
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
    loans: LoanHistoryEntryDTO[];
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