using WuanTech.API.Data;
using WuanTech.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using WuanTech.API.DTOs;
using WuanTech.Models;

namespace WuanTech.API.Services.Implementations
{
    public class SearchService : ISearchService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SearchService> _logger;

        public SearchService(ApplicationDbContext context, ILogger<SearchService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SearchResultDto> SearchAsync(string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return new SearchResultDto
                    {
                        Products = new List<ProductDto>(),
                        Categories = new List<CategoryDto>(),
                        TotalResults = 0,
                        Query = query ?? string.Empty
                    };
                }

                query = query.ToLower().Trim();

                // Log search query
                await LogSearchQueryAsync(query);

                // Search products - SỬA: Include Brand và sử dụng Brand.Name
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)      // ← INCLUDE Brand object
                    .Include(p => p.Inventories)
                    .Where(p => p.IsActive && (
                        p.Name.ToLower().Contains(query) ||
                        (p.Description != null && p.Description.ToLower().Contains(query)) ||
                        (p.Brand != null && p.Brand.Name.ToLower().Contains(query)) ||  // ← Brand.Name
                        p.Category.Name.ToLower().Contains(query) ||
                        p.SKU.ToLower().Contains(query)))
                    .OrderByDescending(p => p.Name.ToLower().StartsWith(query) ? 1 : 0)
                    .ThenByDescending(p => p.Rating)
                    .Take(20)
                    .Select(p => new ProductDto
                    {
                        Id = p.Id,
                        SKU = p.SKU,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        DiscountPrice = p.DiscountPrice,
                        ImageUrl = p.ImageUrl,
                        Rating = (double)p.Rating,  // ← Cast double to decimal
                        ReviewCount = p.ReviewCount,
                        Stock = p.Inventories != null ? p.Inventories.Sum(i => i.Quantity) : 0,
                        CategoryId = p.CategoryId,
                        CategoryName = p.Category.Name,
                        Brand = p.Brand != null ? p.Brand.Name : null,  // ← Brand.Name
                        IsFeatured = p.IsFeatured,
                        IsNew = p.IsNew,
                        IsActive = p.IsActive,
                        ViewCount = p.ViewCount,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt
                    })
                    .ToListAsync();

                // Search categories
                var categories = await _context.Categories
                    .Where(c => c.IsActive && c.Name.ToLower().Contains(query))
                    .Take(10)
                    .Select(c => new CategoryDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Icon = c.Icon,
                        Description = c.Description,
                        ParentCategoryId = c.ParentCategoryId,
                        IsActive = c.IsActive,
                        DisplayOrder = c.DisplayOrder,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt
                    })
                    .ToListAsync();

                return new SearchResultDto
                {
                    Products = products,
                    Categories = categories,
                    TotalResults = products.Count + categories.Count,
                    Query = query
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for: {Query}", query);
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetSearchSuggestionsAsync(string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                    return new List<string>();

                query = query.ToLower().Trim();

                var suggestions = new List<string>();

                // Product names
                var productNames = await _context.Products
                    .Where(p => p.IsActive && p.Name.ToLower().Contains(query))
                    .Select(p => p.Name)
                    .Take(5)
                    .ToListAsync();
                suggestions.AddRange(productNames);

                // Category names
                var categoryNames = await _context.Categories
                    .Where(c => c.IsActive && c.Name.ToLower().Contains(query))
                    .Select(c => c.Name)
                    .Take(3)
                    .ToListAsync();
                suggestions.AddRange(categoryNames);

                // Brand names - SỬA: Từ bảng Brands
                var brandNames = await _context.Set<Brand>()
                    .Where(b => b.IsActive && b.Name.ToLower().Contains(query))
                    .Select(b => b.Name)
                    .Distinct()
                    .Take(2)
                    .ToListAsync();
                suggestions.AddRange(brandNames);

                return suggestions.Distinct().Take(10);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting search suggestions for: {Query}", query);
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetPopularSearchesAsync()
        {
            try
            {
                // Thử lấy từ SearchQuery table trước
                try
                {
                    var popularSearches = await _context.Set<SearchQuery>()
                        .Where(sq => sq.SearchDate >= DateTime.UtcNow.AddDays(-30))
                        .GroupBy(sq => sq.Query.ToLower())
                        .OrderByDescending(g => g.Count())
                        .Select(g => g.Key)
                        .Take(10)
                        .ToListAsync();

                    if (popularSearches.Any())
                        return popularSearches;
                }
                catch
                {
                    // Fallback nếu SearchQuery table chưa có
                }

                // Return default popular searches
                return new List<string>
                {
                    "iPhone 15",
                    "MacBook Pro",
                    "Samsung Galaxy",
                    "AirPods",
                    "Gaming Laptop",
                    "Wireless Headphones",
                    "Smart Watch",
                    "Camera",
                    "Tablet",
                    "Accessories"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting popular searches");
                return new List<string>();
            }
        }

        public async Task<PagedResult<ProductDto>> AdvancedSearchAsync(ProductSearchFilterDto filters, int page = 1, int pageSize = 12)
        {
            try
            {
                var query = _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)      // ← INCLUDE Brand object
                    .Include(p => p.Inventories)
                    .Where(p => p.IsActive)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(filters.Query))
                {
                    var searchTerm = filters.Query.ToLower();
                    query = query.Where(p =>
                        p.Name.ToLower().Contains(searchTerm) ||
                        (p.Description != null && p.Description.ToLower().Contains(searchTerm)) ||
                        p.SKU.ToLower().Contains(searchTerm) ||
                        (p.Brand != null && p.Brand.Name.ToLower().Contains(searchTerm)));  // ← Brand.Name
                }

                if (filters.CategoryId.HasValue)
                    query = query.Where(p => p.CategoryId == filters.CategoryId.Value);

                // SỬA: BrandId filter - Sử dụng BrandId từ Product
                if (filters.BrandId.HasValue)
                    query = query.Where(p => p.BrandId == filters.BrandId.Value);

                if (filters.MinPrice.HasValue)
                    query = query.Where(p => p.Price >= filters.MinPrice.Value);

                if (filters.MaxPrice.HasValue)
                    query = query.Where(p => p.Price <= filters.MaxPrice.Value);

                if (filters.MinRating.HasValue)
                    query = query.Where(p => p.Rating >= filters.MinRating.Value);  // Rating là double

                if (filters.InStock == true)
                    query = query.Where(p => p.Inventories != null && p.Inventories.Sum(i => i.Quantity) > 0);

                if (filters.OnSale == true)
                    query = query.Where(p => p.DiscountPrice != null && p.DiscountPrice < p.Price);

                // Apply sorting
                query = filters.SortBy?.ToLower() switch
                {
                    "price" => filters.SortDescending ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
                    "rating" => filters.SortDescending ? query.OrderByDescending(p => p.Rating) : query.OrderBy(p => p.Rating),
                    "newest" => query.OrderByDescending(p => p.CreatedAt),
                    "popular" => query.OrderByDescending(p => p.ViewCount),
                    "sales" => query.OrderByDescending(p => p.ReviewCount),
                    _ => filters.SortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name)
                };

                var totalCount = await query.CountAsync();

                var products = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new ProductDto
                    {
                        Id = p.Id,
                        SKU = p.SKU,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        DiscountPrice = p.DiscountPrice,
                        ImageUrl = p.ImageUrl,
                        Rating = (double)p.Rating,  
                        ReviewCount = p.ReviewCount,
                        Stock = p.Inventories != null ? p.Inventories.Sum(i => i.Quantity) : 0,
                        CategoryId = p.CategoryId,
                        CategoryName = p.Category.Name,
                        Brand = p.Brand != null ? p.Brand.Name : null,  // ← Brand.Name
                        IsFeatured = p.IsFeatured,
                        IsNew = p.IsNew,
                        IsActive = p.IsActive,
                        ViewCount = p.ViewCount,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt
                    })
                    .ToListAsync();

                return new PagedResult<ProductDto>(products, totalCount, page, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing advanced search");
                throw;
            }
        }

        public async Task LogSearchQueryAsync(string query, int? userId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                    return;

                // SỬA: Thêm try-catch cho SearchQuery table
                try
                {
                    var searchQuery = new SearchQuery
                    {
                        Query = query.Trim(),
                        UserId = userId,
                        SearchDate = DateTime.UtcNow,
                        ResultCount = 0
                    };

                    _context.Set<SearchQuery>().Add(searchQuery);
                    await _context.SaveChangesAsync();
                }
                catch
                {
                    // Fallback: chỉ log ra console nếu table chưa có
                    _logger.LogInformation("Search query: {Query} by user: {UserId}", query, userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging search query: {Query}", query);
                // Don't throw here as it's not critical
            }
        }
    }
}