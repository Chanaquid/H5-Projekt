import { CategoryDTO } from "./categoryDTO";
import { ItemCondition } from "./enums";
import { UserDTO } from "./userDTO";

export namespace ItemDTO {
  // Requests
  export interface CreateItemDTO {
    categoryId: number;
    title: string;
    description: string;
    currentValue: number;
    condition: ItemCondition;
    minLoanDays?: number;
    requiresVerification: boolean;
    pickupAddress?: string;
    pickupLatitude?: number;
    pickupLongitude?: number;
    availableFrom: string;
    availableUntil: string;
  }

 
  export interface UpdateItemDTO {
    categoryId?: number;
    title: string;
    description: string;
    currentValue: number;
    condition: ItemCondition;
    minLoanDays?: number;
    requiresVerification: boolean;
    pickupAddress: string;
    pickupLatitude: number;
    pickupLongitude: number;
    availableFrom?: string;
    availableUntil?: string;
    isActive: boolean;
  }
 
  export interface AdminItemDecisionDTO {
    isApproved: boolean;
    adminNote?: string;
  }
 
  export interface AdminItemStatusDTO {
    status: string; // "Pending" | "Approved" | "Rejected"
    adminNote?: string;
  }
 
  // Responses
  export interface ItemDetailDTO {
    id: number;
    title: string;
    description: string;
    currentValue: number;
    condition: string;
    minLoanDays?: number;
    requiresVerification: boolean;
    pickupAddress: string;
    pickupLatitude: number;
    pickupLongitude: number;
    availableFrom: string;
    availableUntil: string;
    status: string;
    isActive: boolean;
    adminNote?: string;
    createdAt: string;
    updatedAt?: string;
    owner: UserDTO.UserSummaryDTO;
    category: CategoryDTO.CategoryResponseDTO;
    photos: ItemPhotoDTO[];
    averageRating: number;
    reviewCount: number;
    isCurrentlyOnLoan: boolean;
  }
 
  export interface ItemSummaryDTO {
    id: number;
    title: string;
    condition: string;
    status: string;
    pickupAddress: string;
    pickupLatitude: number;
    pickupLongitude: number;
    availableFrom: string;
    availableUntil: string;
    primaryPhotoUrl?: string;
    categoryName: string;
    categoryIcon?: string;
    ownerId: string;
    ownerName: string;
    averageRating: number;
    reviewCount: number;
    isActive: boolean;
    isCurrentlyOnLoan: boolean;
  }
 
  export interface ItemPhotoDTO {
    id: number;
    photoUrl: string;
    isPrimary: boolean;
    displayOrder: number;
  }
 
  export interface ItemQrCodeDTO {
    itemId: number;
    qrCode: string;
  }
 
  export interface AdminPendingItemDTO {
    id: number;
    title: string;
    description: string;
    currentValue: number;
    condition: string;
    ownerName: string;
    ownerEmail: string;
    ownerScore: number;
    categoryName: string;
    photos: ItemPhotoDTO[];
    createdAt: string;
  }
}