import { FineStatus } from "./enums";

export namespace FineDTO {
  // Requests
  export interface PayFineDTO {
    fineId: number;
    paymentProofImageUrl: string;
    paymentDescription: string;
  }
 
  export interface AdminIssueFineDTO {
    userId: string;
    loanId?: number;
    amount: number;
    reason: string;
  }
 
  export interface AdminUpdateFineDTO {
    amount?: number;
    reason?: string;
    status?: FineStatus;
  }
 
  export interface AdminFineVerificationDTO {
    fineId: number;
    isApproved: boolean;
    rejectionReason?: string;
  }
 
  // Responses
  export interface FineResponseDTO {
    id: number;
    loanId?: number;
    itemTitle: string;
    type: string; // "Late" | "Damaged" | "Lost"
    status: string;
    amount: number;
    itemValueAtTimeOfFine: number;
    paymentProofImageUrl?: string;
    paymentDescription?: string;
    rejectionReason?: string;
    paidAt?: string;
    verifiedAt?: string;
    disputeId?: number;
    createdAt: string;
  }
}