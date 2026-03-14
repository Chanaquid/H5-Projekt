using backend.Controllers;
using backend.DTOs;
using backend.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace backend.Tests.Controllers
{
    public class CategoryControllerTests
    {
        private readonly Mock<ICategoryService> _categoryServiceMock;
        private readonly CategoryController _controller;

        public CategoryControllerTests()
        {
            _categoryServiceMock = new Mock<ICategoryService>();
            _controller = new CategoryController(_categoryServiceMock.Object);
            SetUser("user-1", "User");
        }

        private void SetUser(string userId, string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        private static CategoryDTO.CategoryResponseDTO MakeCategoryResponse(
            int id = 1,
            string name = "Tools",
            bool isActive = true) => new()
            {
                Id = id,
                Name = name,
                Icon = "🔧",
                IsActive = isActive,
                ItemCount = 0
            };

  
        [Fact]
        public async Task GetAll_AsUser_ReturnsOk_WithActiveCategories()
        {
            SetUser("user-1", "User");
            var categories = new List<CategoryDTO.CategoryResponseDTO>
            {
                MakeCategoryResponse(1, "Tools"),
                MakeCategoryResponse(2, "Books")
            };
            _categoryServiceMock
                .Setup(s => s.GetAllAsync(false))
                .ReturnsAsync(categories);

            var result = await _controller.GetAll();

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<CategoryDTO.CategoryResponseDTO>>(ok.Value);
            Assert.Equal(2, returned.Count);
        }

        [Fact]
        public async Task GetAll_AsAdmin_PassesIsAdminTrue()
        {
            SetUser("admin-1", "Admin");
            _categoryServiceMock
                .Setup(s => s.GetAllAsync(true))
                .ReturnsAsync(new List<CategoryDTO.CategoryResponseDTO>());

            await _controller.GetAll();

            _categoryServiceMock.Verify(s => s.GetAllAsync(true), Times.Once);
        }

        [Fact]
        public async Task GetAll_AsUser_PassesIsAdminFalse()
        {
            SetUser("user-1", "User");
            _categoryServiceMock
                .Setup(s => s.GetAllAsync(false))
                .ReturnsAsync(new List<CategoryDTO.CategoryResponseDTO>());

            await _controller.GetAll();

            _categoryServiceMock.Verify(s => s.GetAllAsync(false), Times.Once);
        }

        [Fact]
        public async Task GetAll_ReturnsOk_WithEmptyList()
        {
            _categoryServiceMock
                .Setup(s => s.GetAllAsync(false))
                .ReturnsAsync(new List<CategoryDTO.CategoryResponseDTO>());

            var result = await _controller.GetAll();

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsAssignableFrom<List<CategoryDTO.CategoryResponseDTO>>(ok.Value);
            Assert.Empty(returned);
        }

        [Fact]
        public async Task GetById_AsUser_ReturnsOk_WithCategory()
        {
            SetUser("user-1", "User");
            var category = MakeCategoryResponse(1, "Tools");
            _categoryServiceMock
                .Setup(s => s.GetByIdAsync(1, false))
                .ReturnsAsync(category);

            var result = await _controller.GetById(1);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<CategoryDTO.CategoryResponseDTO>(ok.Value);
            Assert.Equal(1, returned.Id);
            Assert.Equal("Tools", returned.Name);
        }

        [Fact]
        public async Task GetById_AsAdmin_PassesIsAdminTrue()
        {
            SetUser("admin-1", "Admin");
            _categoryServiceMock
                .Setup(s => s.GetByIdAsync(1, true))
                .ReturnsAsync(MakeCategoryResponse());

            await _controller.GetById(1);

            _categoryServiceMock.Verify(s => s.GetByIdAsync(1, true), Times.Once);
        }

        [Fact]
        public async Task GetById_AsUser_PassesIsAdminFalse()
        {
            SetUser("user-1", "User");
            _categoryServiceMock
                .Setup(s => s.GetByIdAsync(1, false))
                .ReturnsAsync(MakeCategoryResponse());

            await _controller.GetById(1);

            _categoryServiceMock.Verify(s => s.GetByIdAsync(1, false), Times.Once);
        }

        [Fact]
        public async Task GetById_ServiceThrows_ExceptionPropagates()
        {
            _categoryServiceMock
                .Setup(s => s.GetByIdAsync(999, false))
                .ThrowsAsync(new KeyNotFoundException("Category 999 not found."));

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _controller.GetById(999));
        }

        [Fact]
        public async Task Create_ReturnsCreatedAtAction_WithCategory()
        {
            SetUser("admin-1", "Admin");
            var dto = new CategoryDTO.CreateCategoryDTO { Name = "Gaming", Icon = "🎮" };
            var response = new CategoryDTO.CategoryResponseDTO
            {
                Id = 5,
                Name = "Gaming",
                Icon = "🎮",
                IsActive = true
            };
            _categoryServiceMock
                .Setup(s => s.CreateAsync(dto))
                .ReturnsAsync(response);

            var result = await _controller.Create(dto);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(_controller.GetById), created.ActionName);
            Assert.Equal(5, ((CategoryDTO.CategoryResponseDTO)created.Value!).Id);
        }

        [Fact]
        public async Task Create_CallsServiceWithCorrectDto()
        {
            SetUser("admin-1", "Admin");
            var dto = new CategoryDTO.CreateCategoryDTO { Name = "Sports", Icon = "⚽" };
            _categoryServiceMock
                .Setup(s => s.CreateAsync(dto))
                .ReturnsAsync(MakeCategoryResponse());

            await _controller.Create(dto);

            _categoryServiceMock.Verify(s => s.CreateAsync(dto), Times.Once);
        }

        [Fact]
        public async Task Create_ServiceThrows_ExceptionPropagates()
        {
            SetUser("admin-1", "Admin");
            var dto = new CategoryDTO.CreateCategoryDTO { Name = "Tools" };
            _categoryServiceMock
                .Setup(s => s.CreateAsync(dto))
                .ThrowsAsync(new InvalidOperationException("Category 'Tools' already exists."));

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _controller.Create(dto));
        }


        [Fact]
        public async Task Update_ReturnsOk_WithUpdatedCategory()
        {
            SetUser("admin-1", "Admin");
            var dto = new CategoryDTO.UpdateCategoryDTO
            {
                Name = "Updated Tools",
                Icon = "🔨",
                IsActive = true
            };
            var response = new CategoryDTO.CategoryResponseDTO
            {
                Id = 1,
                Name = "Updated Tools",
                Icon = "🔨",
                IsActive = true
            };
            _categoryServiceMock
                .Setup(s => s.UpdateAsync(1, dto))
                .ReturnsAsync(response);

            var result = await _controller.Update(1, dto);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<CategoryDTO.CategoryResponseDTO>(ok.Value);
            Assert.Equal("Updated Tools", returned.Name);
        }

        [Fact]
        public async Task Update_CallsServiceWithCorrectArguments()
        {
            SetUser("admin-1", "Admin");
            var dto = new CategoryDTO.UpdateCategoryDTO { Name = "Books", IsActive = false };
            _categoryServiceMock
                .Setup(s => s.UpdateAsync(3, dto))
                .ReturnsAsync(MakeCategoryResponse());

            await _controller.Update(3, dto);

            _categoryServiceMock.Verify(s => s.UpdateAsync(3, dto), Times.Once);
        }

        [Fact]
        public async Task Update_ServiceThrows_ExceptionPropagates()
        {
            SetUser("admin-1", "Admin");
            var dto = new CategoryDTO.UpdateCategoryDTO { Name = "Tools", IsActive = true };
            _categoryServiceMock
                .Setup(s => s.UpdateAsync(999, dto))
                .ThrowsAsync(new KeyNotFoundException("Category 999 not found."));

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _controller.Update(999, dto));
        }

        [Fact]
        public async Task Delete_ReturnsNoContent()
        {
            SetUser("admin-1", "Admin");
            _categoryServiceMock
                .Setup(s => s.DeleteAsync(1))
                .Returns(Task.CompletedTask);

            var result = await _controller.Delete(1);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Delete_CallsServiceWithCorrectId()
        {
            SetUser("admin-1", "Admin");
            _categoryServiceMock
                .Setup(s => s.DeleteAsync(2))
                .Returns(Task.CompletedTask);

            await _controller.Delete(2);

            _categoryServiceMock.Verify(s => s.DeleteAsync(2), Times.Once);
        }

        [Fact]
        public async Task Delete_ServiceThrows_ExceptionPropagates()
        {
            SetUser("admin-1", "Admin");
            _categoryServiceMock
                .Setup(s => s.DeleteAsync(999))
                .ThrowsAsync(new KeyNotFoundException("Category 999 not found."));

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _controller.Delete(999));
        }
    }
}