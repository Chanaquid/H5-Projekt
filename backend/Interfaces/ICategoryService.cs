using backend.DTOs;

namespace backend.Interfaces
{
    public interface ICategoryService
    {
        Task<List<CategoryDTO.CategoryResponseDTO>> GetAllAsync(bool isAdmin = false);
        Task<CategoryDTO.CategoryResponseDTO> GetByIdAsync(int id, bool isAdmin = false);
        Task<CategoryDTO.CategoryResponseDTO> CreateAsync(CategoryDTO.CreateCategoryDTO dto);
        Task<CategoryDTO.CategoryResponseDTO> UpdateAsync(int id, CategoryDTO.UpdateCategoryDTO dto);
        Task DeleteAsync(int id);
    }
}
