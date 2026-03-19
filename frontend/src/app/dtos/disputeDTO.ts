import { LoanDTO } from "./loanDTO";

export namespace DisputeDTO {
  // Requests
  export interface CreateDisputeDTO {
    loanId: number;
    filedAs: string; // "AsOwner" | "AsBorrower"
    description: string;
  }
 
  export interface DisputeResponseDTO {
    responseDescription: string;
  }
 
  export interface AdminVerdictDTO {
    verdict: string; // "OwnerFavored" | "BorrowerFavored" | "Neutral"
    adminNote?: string;
    fineIssuedToUserId?: string;
    fineAmount?: number;     
    scoreAdjustment?: number;
  }
 
  export interface AddDisputePhotoDTO {
    photoUrl: string;
    caption?: string;
  }
 
  // Responses
  export interface DisputeDetailDTO {
    id: number;
    loanId: number;
    itemTitle: string;
    filedById: string;
    filedByName: string;
    filedAs: string;
    description: string;
    responseDescription?: string;
    responseDeadline: string;
    status: string;
    adminVerdict?: string;
    adminNote?: string;
    resolvedAt?: string;
    filedByPhotos: DisputePhotoDTO[];
    responsePhotos: DisputePhotoDTO[];
    snapshotCondition: string;
    snapshotPhotos: LoanDTO.LoanSnapshotPhotoDTO[];
    createdAt: string;
  }
 
  export interface DisputeSummaryDTO {
    id: number;
    loanId: number;
    itemTitle: string;
    filedById: string;
    filedByName: string;
    filedAs: string;
    status: string;
    responseDeadline: string;
    createdAt: string;
  }
 
  export interface DisputePhotoDTO {
    id: number;
    photoUrl: string;
    submittedById: string;
    submittedByName: string;
    caption?: string;
    uploadedAt: string;
  }
}
 