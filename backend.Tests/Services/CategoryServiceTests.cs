using backend.DTOs;
using backend.Interfaces;
using backend.Models;
using backend.Repositories;
using backend.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace backend.Tests.Services
{
    public class CategoryServiceTests
    {
        private readonly Mock<ICategoryRepository> _mockCategoryRepository;
        private readonly CategoryService _categoryService;

        public CategoryServiceTests()
        {
            _mockCategoryRepository = new Mock<ICategoryRepository>();
            _categoryService = new CategoryService(_mockCategoryRepository.Object);
        }


        [Fact]
        public async Task GetAllAsync_WhenAdmin_PassesTrueToRepository()
        {
            var categories = new List<Category>
            {
                new Category { Id = 1, Name = "Tools", IsActive = true, Items = new List<Item>() },
                new Category { Id = 2, Name = "Books", IsActive = false, Items = new List<Item>() }
            };
            _mockCategoryRepository.Setup(r => r.GetAllAsync(It.IsAny<bool>())).ReturnsAsync(categories);

            var result = await _categoryService.GetAllAsync(isAdmin: true);

            Assert.Equal(1, result[0].Id);
            Assert.Equal("Tools", result[0].Name);

            Assert.Equal(2, result[1].Id);
            Assert.Equal(2, result.Count);
            _mockCategoryRepository.Verify(r => r.GetAllAsync(true), Times.Once);

        }

        [Fact]
        public async Task GetAllAsync_WhenNotAdmin_PassesFalseToRepository()
        {
            var categories = new List<Category>();

            _mockCategoryRepository
                .Setup(r => r.GetAllAsync(false))
                .ReturnsAsync(categories);

            await _categoryService.GetAllAsync(false);

            _mockCategoryRepository.Verify(r => r.GetAllAsync(false), Times.Once);
        }


        [Fact]
        public async Task GetByIdAsync_WhenCategoryIsInactiveAndUserNotAdmin_ThrowsKeyNotFound()
        {
            var inactiveCategory = new Category { Id = 1, Name = "Secret", IsActive = false };
            _mockCategoryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(inactiveCategory);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _categoryService.GetByIdAsync(1, isAdmin: false));
        }


        [Fact]
        public async Task GetByIdAsync_WhenCategoryIsInactiveAndUserIsAdmin_ReturnsDTO()
        {
            var inactiveCategory = new Category { Id = 1, Name = "Secret", IsActive = false };
            _mockCategoryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(inactiveCategory);

            var result = await _categoryService.GetByIdAsync(1, isAdmin: true);

            Assert.NotNull(result);
            Assert.Equal("Secret", result.Name);
        }

        [Fact]
        public async Task GetByIdAsync_WhenCategoryIsActiveAndUserIsAdmin_ReturnsDTO()
        {
            var inactiveCategory = new Category { Id = 1, Name = "Secret", IsActive = true };
            _mockCategoryRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(inactiveCategory);

            var result = await _categoryService.GetByIdAsync(1, isAdmin: true);

            Assert.NotNull(result);
            Assert.Equal("Secret", result.Name);
        }

        [Fact]
        public async Task CreateAsync_WhenNameExists_ThrowsArgumentException()
        {
            var dto = new CategoryDTO.CreateCategoryDTO { Name = "Existing" };
            _mockCategoryRepository.Setup(r => r.GetByNameAsync("Existing")).ReturnsAsync(new Category());

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => _categoryService.CreateAsync(dto));
            Assert.Contains("already exists", ex.Message);

            _mockCategoryRepository.Verify(r => r.GetByNameAsync("Existing"), Times.Once);
            _mockCategoryRepository.Verify(r => r.AddAsync(It.IsAny<Category>()), Times.Never);
            _mockCategoryRepository.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ValidData_CallsAddAndSave()
        {
            var dto = new CategoryDTO.CreateCategoryDTO { Name = "New Cat", Icon = "fa-home" };
            _mockCategoryRepository.Setup(r => r.GetByNameAsync(It.IsAny<string>())).ReturnsAsync((Category)null);

            var result = await _categoryService.CreateAsync(dto);

            _mockCategoryRepository.Verify(r => r.AddAsync(It.Is<Category>(c => c.Name == "New Cat")), Times.Once);
            _mockCategoryRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
            Assert.Equal("New Cat", result.Name);
        }

        [Fact]
        public async Task UpdateAsync_WhenCategoryNotFound_ThrowsKeyNotFoundException()
        {
            var dto = new CategoryDTO.UpdateCategoryDTO { Name = "Tools", IsActive = true };

            _mockCategoryRepository
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync((Category)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _categoryService.UpdateAsync(1, dto));

            _mockCategoryRepository.Verify(r => r.GetByIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenNameAlreadyExists_ThrowsArgumentException()
        {
            var existingCategory = new Category { Id = 1, Name = "Tools" };

            var dto = new CategoryDTO.UpdateCategoryDTO
            {
                Name = "Books",
                IsActive = true
            };

            _mockCategoryRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(existingCategory);

            _mockCategoryRepository.Setup(r => r.GetByNameAsync("Books"))
                .ReturnsAsync(new Category());

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _categoryService.UpdateAsync(1, dto));

            _mockCategoryRepository.Verify(r => r.GetByNameAsync("Books"), Times.Once);
            _mockCategoryRepository.Verify(r => r.Update(It.IsAny<Category>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenNameUnchanged_UpdatesWithoutCheckingUniqueness()
        {
            var category = new Category
            {
                Id = 1,
                Name = "Tools",
                Icon = "old",
                IsActive = true
            };

            var dto = new CategoryDTO.UpdateCategoryDTO
            {
                Name = "Tools",
                Icon = "new",
                IsActive = false
            };

            _mockCategoryRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(category);

            var result = await _categoryService.UpdateAsync(1, dto);

            Assert.Equal("Tools", result.Name);
            Assert.False(result.IsActive);

            _mockCategoryRepository.Verify(r => r.GetByNameAsync(It.IsAny<string>()), Times.Never);
            _mockCategoryRepository.Verify(r => r.Update(It.IsAny<Category>()), Times.Once);
            _mockCategoryRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenValid_UpdatesCategoryAndReturnsDTO()
        {
            var category = new Category
            {
                Id = 1,
                Name = "Tools",
                Icon = "old",
                IsActive = true
            };

            var dto = new CategoryDTO.UpdateCategoryDTO
            {
                Name = "Hardware",
                Icon = "hammer",
                IsActive = false
            };

            _mockCategoryRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(category);

            _mockCategoryRepository.Setup(r => r.GetByNameAsync("Hardware"))
                .ReturnsAsync((Category)null);

            var result = await _categoryService.UpdateAsync(1, dto);

            Assert.Equal("Hardware", result.Name);
            Assert.False(result.IsActive);

            _mockCategoryRepository.Verify(r => r.Update(It.Is<Category>(c =>
                c.Name == "Hardware" &&
                c.Icon == "hammer" &&
                c.IsActive == false)), Times.Once);

            _mockCategoryRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenInvalidCategoryId_ThrowsKeyNotFoundException()
        {
            var id = 1;

            _mockCategoryRepository.Setup(x => x.GetByIdAsync(id))
                .ReturnsAsync((Category)null);


            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _categoryService.DeleteAsync(id));

            Assert.Equal($"Category {id} not found.", ex.Message);
            _mockCategoryRepository.Verify(r => r.GetByIdAsync(id), Times.Once);
            _mockCategoryRepository.Verify(r => r.Delete(It.IsAny<Category>()), Times.Never);
            _mockCategoryRepository.Verify(r => r.SaveChangesAsync(), Times.Never);

        }

        [Fact]
        public async Task DeleteAsync_WhenValidCategoryId_DeleteCategory()
        {
            var id = 1;

            _mockCategoryRepository.Setup(x => x.GetByIdAsync(id))
                .ReturnsAsync(new Category { });


            await _categoryService.DeleteAsync(id);

            _mockCategoryRepository.Verify(r => r.GetByIdAsync(id), Times.Once);
            _mockCategoryRepository.Verify(r => r.Delete(It.IsAny<Category>()), Times.Once);
            _mockCategoryRepository.Verify(r => r.SaveChangesAsync(), Times.Once);

        }



    }
}
