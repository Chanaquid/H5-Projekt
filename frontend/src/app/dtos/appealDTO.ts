import { FineAppealResolution } from "./enums";

export namespace AppealDTO {
  //Requests
  export interface CreateScoreAppealDTO {
    message: string;
  }
 
  export interface CreateFineAppealDTO {
    fineId: number;
    message: string;
  }
 
  export interface AdminScoreAppealDecisionDTO {
    isApproved: boolean;
    adminNote?: string;
    newScore?: number;
  }
 
  export interface AdminFineAppealDecisionDTO {
    isApproved: boolean;
    adminNote?: string;
    resolution?: FineAppealResolution;
    customFineAmount?: number;
  }
 
  //Responses
  export interface AppealResponseDTO {
    id: number;
    userId: string;
    userName: string;
    userScore: number;
    appealType: string;
    fineId?: number;
    fineAmount?: number;
    message: string;
    status: string;
    adminNote?: string;
    restoredScore?: number;
    fineResolution?: string;
    customFineAmount?: number;
    resolvedByAdminName?: string;
    createdAt: string;
    resolvedAt?: string;
  }
}