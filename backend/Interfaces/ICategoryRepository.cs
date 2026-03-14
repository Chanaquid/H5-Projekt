using backend.Models;

namespace backend.Interfaces
{
    public interface ICategoryRepository
    {
        Task<List<Category>> GetAllAsync(bool isAdmin = false);
        Task<Category?> GetByIdAsync(int id);
        Task<Category?> GetByNameAsync(string name);
        Task AddAsync(Category category);
        void Update(Category category);
        void Delete(Category category);
        Task SaveChangesAsync();
    }
}
