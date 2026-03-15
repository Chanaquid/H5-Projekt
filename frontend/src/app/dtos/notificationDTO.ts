export namespace NotificationDTO {
  export interface NotificationResponseDTO {
    id: number;
    type: string; // e.g."LoanApproved" | "FineIssued"
    message: string;
    referenceId?: number;
    referenceType?: string; //"Loan" | "Item" | "Dispute" | "Appeal" | "Fine"
    isRead: boolean;
    createdAt: string;
  }
 
  export interface NotificationSummaryDTO {
    unreadCount: number;
    recent: NotificationResponseDTO[];
  }
}