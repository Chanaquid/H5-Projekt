import { ItemDTO } from "./itemDTO";

export namespace RecentlyViewedDTO {
  export interface RecentlyViewedResponseDTO {
    item: ItemDTO.ItemSummaryDTO;
    viewedAt: string;
  }
}