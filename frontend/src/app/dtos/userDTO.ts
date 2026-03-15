import { AppealDTO } from "./appealDTO";
import { ChatDTO } from "./chatDTO";
import { FineDTO } from "./fineDTO";
import { ItemDTO } from "./itemDTO";
import { LoanDTO } from "./loanDTO";
import { VerificationDTO } from "./verificationDTO";

export namespace UserDTO {
  //Requests
  export interface UpdateProfileDTO {
    fullName: string;
    userName: string;
    address?: string;
    latitude?: number;
    longitude?: number;
    gender?: string;
    avatarUrl?: string;
  }
 
  export interface DeleteAccountDTO {
    password: string;
  }
 
  export interface AdminScoreAdjustDTO {
    pointsChanged: number;
    note: string;
  }
 
  export interface AdminEditUserDTO {
    fullName?: string;
    username?: string;
    email?: string;
    newPassword?: string;
    address?: string;
    gender?: string;
    avatarUrl?: string;
    latitude?: number;
    longitude?: number;
    isVerified?: boolean;
    role?: string;
    score?: number;
    scoreNote?: string;
    unpaidFinesTotal?: number;
  }
 
  //Responses
  export interface UserProfileDTO {
    id: string;
    fullName: string;
    username: string;
    email: string;
    address: string;
    latitude?: number;
    longitude?: number;
    gender?: string;
    dateOfBirth: string;
    age: number;
    avatarUrl?: string;
    score: number;
    unpaidFinesTotal: number;
    isVerified: boolean;
    borrowingStatus: string; //"Free" | "AdminApproval" | "Blocked"
    membershipDate: string;
    createdAt: string;
  }
 
  export interface UserSummaryDTO {
    id: string;
    username: string;
    fullName: string;
    avatarUrl?: string;
    score: number;
    isVerified: boolean;
  }
 
  export interface AdminUserDTO {
    id: string;
    fullName: string;
    username: string;
    email: string;
    address: string;
    latitude?: number;
    longitude?: number;
    gender?: string;
    age: number;
    avatarUrl?: string;
    score: number;
    unpaidFinesTotal: number;
    isVerified: boolean;
    role: string;
    isDeleted: boolean;
    deletedAt?: string;
    membershipDate: string;
    createdAt: string;
  }
 
  export interface AdminUserDetailDTO extends AdminUserDTO {
    fines: FineDTO.FineResponseDTO[];
    scoreHistory: ScoreHistoryDTO[];
    loansAsBorrower: LoanDTO.LoanSummaryDTO[];
    items: ItemDTO.ItemSummaryDTO[];
    appeals: AppealDTO.AppealResponseDTO[];
    verificationRequests: VerificationDTO.VerificationRequestResponseDTO[];
    blockedUsers: ChatDTO.UserBlockDTO.BlockResponseDTO[];
    loanMessages: ChatDTO.LoanMessageDTO.LoanMessageResponseDTO[];
    directConversations: ChatDTO.DirectMessageDTO.DirectConversationSummaryDTO[];
  }
 
  export interface AdminDeleteResultDTO {
    success: boolean;
    warnings: string[];
  }
 
  export interface ScoreHistoryDTO {
    id: number;
    pointsChanged: number;
    scoreAfterChange: number;
    reason: string; //"OnTimeReturn" | "LateReturn" | "AdminAdjustment"
    note?: string;
    loanId?: number;
    createdAt: string;
  }
}