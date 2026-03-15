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
    verdict: string; // "OwnerFavored" | "BorrowerFavored" | "PartialDamage" | "Inconclusive"
    customFineAmount?: number;
    adminNote: string;
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
    filedByName: string;
    filedAs: string;
    description: string;
    responseDescription?: string;
    responseDeadline: string;
    status: string;
    adminVerdict?: string;
    customFineAmount?: number;
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
    filedByName: string;
    filedAs: string;
    status: string;
    responseDeadline: string;
    createdAt: string;
  }
 
  export interface DisputePhotoDTO {
    id: number;
    photoUrl: string;
    submittedByName: string;
    caption?: string;
    uploadedAt: string;
  }
}
 