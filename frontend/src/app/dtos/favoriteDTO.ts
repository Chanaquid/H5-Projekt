import { ItemDTO } from "./itemDTO";

export namespace FavoriteDTO {
  export interface FavoriteResponseDTO {
    item: ItemDTO.ItemSummaryDTO;
    notifyWhenAvailable: boolean;
    savedAt: string;
  }
 
  export interface ToggleNotifyDTO {
    notifyWhenAvailable: boolean;
  }
}