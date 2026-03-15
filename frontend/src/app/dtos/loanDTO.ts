import { FineDTO } from "./fineDTO";
import { ItemDTO } from "./itemDTO";
import { UserDTO } from "./userDTO";

export namespace LoanDTO {
  //Requests
  export interface CreateLoanDTO {
    itemId: number;
    startDate: string;
    endDate: string;
  }
 
  export interface LoanDecisionDTO {
    isApproved: boolean;
    decisionNote?: string;
  }
 
  export interface CancelLoanDTO {
    reason?: string;
  }
 
  export interface RequestExtensionDTO {
    requestedExtensionDate: string;
  }
 
  export interface ExtensionDecisionDTO {
    isApproved: boolean;
  }
 
  //Responses
  export interface LoanDetailDTO {
    id: number;
    startDate: string;
    endDate: string;
    actualReturnDate?: string;
    status: string;
    snapshotCondition: string;
    decisionNote?: string;
    requestedExtensionDate?: string;
    extensionRequestStatus?: string; // "Pending" | "Approved" | "Rejected"
    createdAt: string;
    updatedAt?: string;
    item: ItemDTO.ItemSummaryDTO;
    owner: UserDTO.UserSummaryDTO;
    borrower: UserDTO.UserSummaryDTO;
    snapshotPhotos: LoanSnapshotPhotoDTO[];
    fines: FineDTO.FineResponseDTO[];
    hasOpenDispute: boolean;
    daysOverdue?: number;
    hasUnreadMessages: boolean;
  }
 
  export interface LoanSummaryDTO {
    id: number;
    itemTitle: string;
    itemPrimaryPhoto?: string;
    otherPartyName: string;
    startDate: string;
    endDate: string;
    status: string;
    hasUnreadMessages: boolean;
    daysOverdue?: number;
  }
 
  export interface LoanSnapshotPhotoDTO {
    id: number;
    photoUrl: string;
    displayOrder: number;
  }
 
  export interface AdminPendingLoanDTO {
    id: number;
    itemTitle: string;
    ownerName: string;
    borrowerName: string;
    borrowerEmail: string;
    borrowerScore: number;
    borrowerUnpaidFines: number;
    startDate: string;
    endDate: string;
    createdAt: string;
  }
}