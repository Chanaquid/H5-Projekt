using backend.Data;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace backend.Tests.Repositories
{
    public class CategoryRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly CategoryRepository _repo;

        public CategoryRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _repo = new CategoryRepository(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
        }


        [Fact]
        public async Task GetAllAsync_AsAdmin_ReturnsAllCategories()
        {
            await SeedCategoryAsync("Tools", isActive: true);
            await SeedCategoryAsync("Sports", isActive: false);

            var result = await _repo.GetAllAsync(isAdmin: true);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetAllAsync_AsUser_ReturnsActiveOnly()
        {
            await SeedCategoryAsync("Tools", isActive: true);
            await SeedCategoryAsync("Sports", isActive: false);

            var result = await _repo.GetAllAsync(isAdmin: false);

            Assert.Single(result);
            Assert.All(result, c => Assert.True(c.IsActive));
        }

        [Fact]
        public async Task GetAllAsync_OrderedByNameAscending()
        {
            await SeedCategoryAsync("Tools");
            await SeedCategoryAsync("Books");
            await SeedCategoryAsync("Electronics");

            var result = await _repo.GetAllAsync(isAdmin: true);

            var names = result.Select(c => c.Name).ToList();
            Assert.Equal(names.OrderBy(n => n).ToList(), names);
        }

        [Fact]
        public async Task GetAllAsync_NoCategories_ReturnsEmptyList()
        {
            var result = await _repo.GetAllAsync();

            Assert.Empty(result);
        }


        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsCategory()
        {
            var seeded = await SeedCategoryAsync("Tools");

            var result = await _repo.GetByIdAsync(seeded.Id);

            Assert.NotNull(result);
            Assert.Equal(seeded.Id, result!.Id);
            Assert.Equal("Tools", result.Name);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repo.GetByIdAsync(999);

            Assert.Null(result);
        }


        [Fact]
        public async Task GetByNameAsync_ExactMatch_ReturnsCategory()
        {
            await SeedCategoryAsync("Tools");

            var result = await _repo.GetByNameAsync("Tools");

            Assert.NotNull(result);
            Assert.Equal("Tools", result!.Name);
        }

        [Fact]
        public async Task GetByNameAsync_CaseInsensitive_ReturnsCategory()
        {
            await SeedCategoryAsync("Tools");

            var result = await _repo.GetByNameAsync("tOoLs");

            Assert.NotNull(result);
            Assert.Equal("Tools", result!.Name);
        }

        [Fact]
        public async Task GetByNameAsync_NonExistingName_ReturnsNull()
        {
            var result = await _repo.GetByNameAsync("NonExistent");

            Assert.Null(result);
        }


        [Fact]
        public async Task AddAsync_SaveChangesAsync_PersistsCategory()
        {
            var category = new Category
            {
                Name = "Gaming",
                Icon = "🎮",
                IsActive = true
            };

            await _repo.AddAsync(category);
            await _repo.SaveChangesAsync();

            var saved = await _context.Categories.FirstOrDefaultAsync(c => c.Name == "Gaming");
            Assert.NotNull(saved);
            Assert.Equal("🎮", saved!.Icon);
        }

        [Fact]
        public async Task Update_SaveChangesAsync_PersistsChanges()
        {
            var seeded = await SeedCategoryAsync("Tools", isActive: true);

            seeded.Name = "Updated Tools";
            seeded.IsActive = false;
            _repo.Update(seeded);
            await _repo.SaveChangesAsync();

            var updated = await _context.Categories.FindAsync(seeded.Id);
            Assert.Equal("Updated Tools", updated!.Name);
            Assert.False(updated.IsActive);
        }

        [Fact]
        public async Task Delete_SaveChangesAsync_RemovesCategory()
        {
            var seeded = await SeedCategoryAsync("Tools");

            _repo.Delete(seeded);
            await _repo.SaveChangesAsync();

            var deleted = await _context.Categories.FindAsync(seeded.Id);
            Assert.Null(deleted);
        }





        private async Task<Category> SeedCategoryAsync(string name, bool isActive = true)
        {
            var category = new Category
            {
                Name = name,
                Icon = "🔧",
                IsActive = isActive
            };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }





    }
}
