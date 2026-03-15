export namespace VerificationDTO {
  //Requests
  export interface CreateVerificationRequestDTO {
    documentUrl: string;
    documentType: string;//"Passport" | "NationalId" | "DrivingLicense"
  }
 
  export interface AdminVerificationDecisionDTO {
    isApproved: boolean;
    adminNote?: string;
  }
 
  //Responses
  export interface VerificationRequestResponseDTO {
    id: number;
    userId: string;
    userName: string;
    userEmail: string;
    documentUrl: string;
    documentType: string;
    status: string; //"Pending" | "Approved" | "Rejected"
    adminNote?: string;
    reviewedByAdminName?: string;
    submittedAt: string;
    reviewedAt?: string;
  }
}