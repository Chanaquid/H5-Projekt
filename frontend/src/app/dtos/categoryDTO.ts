export namespace CategoryDTO {
  //Requests
  export interface CreateCategoryDTO {
    name: string;
    icon?: string;
  }
 
  export interface UpdateCategoryDTO {
    name: string;
    icon?: string;
    isActive: boolean;
  }
 
  //Responses
  export interface CategoryResponseDTO {
    id: number;
    name: string;
    icon?: string;
    isActive: boolean;
    itemCount: number;
  }
}