using Microsoft.EntityFrameworkCore;
using WuanTech.API.Data;
using WuanTech.API.DTOs;
using WuanTech.API.Services.Interfaces;
using WuanTech.Models;

namespace WuanTech.API.Services.Implementations
{
    public class CategoryService : ICategoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(ApplicationDbContext context, ILogger<CategoryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
        {
            try
            {
                var categories = await _context.Categories
                    .Include(c => c.SubCategories)
                    .Include(c => c.Products.Where(p => p.IsActive))
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.Name)
                    .ToListAsync();

                return categories.Select(MapCategoryToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all categories");
                throw;
            }
        }

        public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.SubCategories.Where(sc => sc.IsActive))
                    .Include(c => c.Products.Where(p => p.IsActive))
                    .Include(c => c.ParentCategory)
                    .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

                if (category == null)
                    return null;

                return MapCategoryToDto(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category by id: {CategoryId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<CategoryMenuDto>> GetCategoryMenuAsync()
        {
            try
            {
                var categories = await _context.Categories
                    .Include(c => c.SubCategories.Where(sc => sc.IsActive))
                    .Where(c => c.ParentCategoryId == null && c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.Name)
                    .ToListAsync();

                return categories.Select(MapCategoryToMenuDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category menu");
                throw;
            }
        }

        public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto categoryDto)
        {
            try
            {
                // Validate parent category if specified
                if (categoryDto.ParentCategoryId.HasValue)
                {
                    var parentExists = await _context.Categories
                        .AnyAsync(c => c.Id == categoryDto.ParentCategoryId.Value && c.IsActive);

                    if (!parentExists)
                        throw new InvalidOperationException("Parent category not found");
                }

                var category = new Category
                {
                    Name = categoryDto.Name,
                    Icon = categoryDto.Icon,
                    Description = categoryDto.Description,
                    ParentCategoryId = categoryDto.ParentCategoryId,
                    IsActive = categoryDto.IsActive,
                    DisplayOrder = categoryDto.DisplayOrder,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                return MapCategoryToDto(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                throw;
            }
        }

        public async Task<CategoryDto?> UpdateCategoryAsync(int id, UpdateCategoryDto categoryDto)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                    return null;

                // Validate parent category if specified
                if (categoryDto.ParentCategoryId.HasValue)
                {
                    // Check if parent category exists and is not the category itself
                    if (categoryDto.ParentCategoryId.Value == id)
                        throw new InvalidOperationException("Category cannot be its own parent");

                    var parentExists = await _context.Categories
                        .AnyAsync(c => c.Id == categoryDto.ParentCategoryId.Value && c.IsActive);

                    if (!parentExists)
                        throw new InvalidOperationException("Parent category not found");

                    // Check for circular reference
                    if (await HasCircularReference(id, categoryDto.ParentCategoryId.Value))
                        throw new InvalidOperationException("Circular reference detected in category hierarchy");
                }

                category.Name = categoryDto.Name;
                category.Icon = categoryDto.Icon;
                category.Description = categoryDto.Description;
                category.ParentCategoryId = categoryDto.ParentCategoryId;
                category.IsActive = categoryDto.IsActive;
                category.DisplayOrder = categoryDto.DisplayOrder;
                category.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Reload with related data
                await _context.Entry(category)
                    .Collection(c => c.SubCategories)
                    .LoadAsync();
                await _context.Entry(category)
                    .Collection(c => c.Products)
                    .LoadAsync();

                return MapCategoryToDto(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category: {CategoryId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.SubCategories)
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                    return false;

                // Check if category has active products
                var hasActiveProducts = category.Products.Any(p => p.IsActive);
                if (hasActiveProducts)
                    throw new InvalidOperationException("Cannot delete category with active products");

                // Check if category has active subcategories
                var hasActiveSubcategories = category.SubCategories.Any(sc => sc.IsActive);
                if (hasActiveSubcategories)
                    throw new InvalidOperationException("Cannot delete category with active subcategories");

                // Soft delete
                category.IsActive = false;
                category.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category: {CategoryId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<CategoryDto>> GetCategoriesWithProductCountAsync()
        {
            try
            {
                var categories = await _context.Categories
                    .Include(c => c.Products.Where(p => p.IsActive))
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.Name)
                    .ToListAsync();

                return categories.Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Icon = c.Icon,
                    Description = c.Description,
                    ParentCategoryId = c.ParentCategoryId,
                    IsActive = c.IsActive,
                    DisplayOrder = c.DisplayOrder,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    ProductCount = c.Products.Count,
                    SubCategories = new List<CategoryDto>() // Avoid deep loading for this endpoint
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories with product count");
                throw;
            }
        }

        #region Private Methods

        private async Task<bool> HasCircularReference(int categoryId, int parentId)
        {
            var currentParentId = parentId;
            var visited = new HashSet<int> { categoryId };

            while (currentParentId != null)
            {
                if (visited.Contains(currentParentId))
                    return true;

                visited.Add(currentParentId);

                var parentCategory = await _context.Categories.FindAsync(currentParentId);
                currentParentId = parentCategory?.ParentCategoryId ?? 0;

                if (currentParentId == 0)
                    break;
            }

            return false;
        }

        private CategoryDto MapCategoryToDto(Category category)
        {
            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Icon = category.Icon,
                Description = category.Description,
                ParentCategoryId = category.ParentCategoryId,
                IsActive = category.IsActive,
                DisplayOrder = category.DisplayOrder,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt,
                ProductCount = category.Products?.Count(p => p.IsActive) ?? 0,
                SubCategories = category.SubCategories?.Where(sc => sc.IsActive)
                    .Select(MapCategoryToDto).ToList() ?? new List<CategoryDto>()
            };
        }

        private CategoryMenuDto MapCategoryToMenuDto(Category category)
        {
            return new CategoryMenuDto
            {
                Id = category.Id,
                Name = category.Name,
                Icon = category.Icon,
                SubCategories = category.SubCategories?.Where(sc => sc.IsActive)
                    .OrderBy(sc => sc.DisplayOrder)
                    .ThenBy(sc => sc.Name)
                    .Select(MapCategoryToMenuDto).ToList() ?? new List<CategoryMenuDto>()
            };
        }

        #endregion
    }
}