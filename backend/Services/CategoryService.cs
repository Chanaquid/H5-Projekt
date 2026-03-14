using backend.DTOs;
using backend.Interfaces;
using backend.Models;

namespace backend.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryService(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        //Get all category (if admin -> see all | if user -> see only active categories)
        public async Task<List<CategoryDTO.CategoryResponseDTO>> GetAllAsync(bool isAdmin = false)
        {
            var categories = await _categoryRepository.GetAllAsync(isAdmin);
            return categories.Select(MapToCategoryDTO).ToList();
        }

        //Get a category by id
        public async Task<CategoryDTO.CategoryResponseDTO> GetByIdAsync(int id, bool isAdmin = false)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
                throw new KeyNotFoundException($"Category {id} not found.");

            if (!isAdmin && !category.IsActive)
                throw new KeyNotFoundException($"Category {id} not found.");

            return MapToCategoryDTO(category);
        }

        //Create a category
        public async Task<CategoryDTO.CategoryResponseDTO> CreateAsync(CategoryDTO.CreateCategoryDTO dto)
        {
            //Name must be unique
            var existing = await _categoryRepository.GetByNameAsync(dto.Name.Trim());
            if (existing != null)
                throw new ArgumentException($"A category named '{dto.Name}' already exists.");

            var category = new Category
            {
                Name = dto.Name.Trim(),
                Icon = dto.Icon?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _categoryRepository.AddAsync(category);
            await _categoryRepository.SaveChangesAsync();

            return MapToCategoryDTO(category);
        }

        //Update category
        public async Task<CategoryDTO.CategoryResponseDTO> UpdateAsync(int id, CategoryDTO.UpdateCategoryDTO dto)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
                throw new KeyNotFoundException($"Category {id} not found.");

            //Check name uniqueness — only if the name is actually changing
            if (!string.Equals(category.Name, dto.Name.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                var nameConflict = await _categoryRepository.GetByNameAsync(dto.Name.Trim());
                if (nameConflict != null)
                    throw new ArgumentException($"A category named '{dto.Name}' already exists.");
            }

            category.Name = dto.Name.Trim();
            category.Icon = dto.Icon?.Trim();
            category.IsActive = dto.IsActive;

            _categoryRepository.Update(category);
            await _categoryRepository.SaveChangesAsync();

            return MapToCategoryDTO(category);
        }

        //Delete
        public async Task DeleteAsync(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
                throw new KeyNotFoundException($"Category {id} not found.");

            _categoryRepository.Delete(category);
            await _categoryRepository.SaveChangesAsync();
        }

        //maptoDTO
        private static CategoryDTO.CategoryResponseDTO MapToCategoryDTO(Category c)
        {
            return new CategoryDTO.CategoryResponseDTO
            {
                Id = c.Id,
                Name = c.Name,
                Icon = c.Icon,
                IsActive = c.IsActive,
                ItemCount = c.Items?.Count(i => i.Status == ItemStatus.Approved && i.IsActive) ?? 0
            };
        }

    }
}
