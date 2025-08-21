using Microsoft.EntityFrameworkCore;
using WuanTech.API.Data;
using WuanTech.API.DTOs;
using WuanTech.API.Services.Interfaces;
using WuanTech.Models;

namespace WuanTech.API.Services.Implementations
{
    public class FavoriteService : IFavoriteService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FavoriteService> _logger;

        public FavoriteService(ApplicationDbContext context, ILogger<FavoriteService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<ProductDto>> GetUserFavoritesAsync(int userId)
        {
            try
            {
                var favorites = await _context.Favorites
                    .Include(f => f.Product)
                        .ThenInclude(p => p.Category)
                    .Include(f => f.Product)
                        .ThenInclude(p => p.Brand)
                    .Include(f => f.Product)
                        .ThenInclude(p => p.Inventories)
                    .Where(f => f.UserId == userId)
                    .OrderByDescending(f => f.AddedAt)
                    .ToListAsync();

                return favorites.Select(f => MapProductToDto(f.Product)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user favorites: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> AddToFavoritesAsync(int userId, int productId)
        {
            try
            {
                // Check if already exists
                var existingFavorite = await _context.Favorites
                    .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);

                if (existingFavorite != null)
                    return true; // Already in favorites

                var favorite = new Favorite
                {
                    UserId = userId,
                    ProductId = productId,
                    AddedAt = DateTime.UtcNow
                };

                _context.Favorites.Add(favorite);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to favorites: UserId={UserId}, ProductId={ProductId}", userId, productId);
                throw;
            }
        }

        public async Task<bool> RemoveFromFavoritesAsync(int userId, int productId)
        {
            try
            {
                var favorite = await _context.Favorites
                    .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);

                if (favorite == null)
                    return false;

                _context.Favorites.Remove(favorite);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing from favorites: UserId={UserId}, ProductId={ProductId}", userId, productId);
                throw;
            }
        }

        public async Task<bool> IsProductFavoriteAsync(int userId, int productId)
        {
            try
            {
                return await _context.Favorites
                    .AnyAsync(f => f.UserId == userId && f.ProductId == productId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if product is favorite");
                throw;
            }
        }

        public async Task<int> GetFavoriteCountAsync(int userId)
        {
            try
            {
                return await _context.Favorites
                    .CountAsync(f => f.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting favorite count for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> ClearFavoritesAsync(int userId)
        {
            try
            {
                var favorites = await _context.Favorites
                    .Where(f => f.UserId == userId)
                    .ToListAsync();

                _context.Favorites.RemoveRange(favorites);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing favorites for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<PagedResult<ProductDto>> GetUserFavoritesPagedAsync(int userId, int page = 1, int pageSize = 12)
        {
            try
            {
                var query = _context.Favorites
                    .Include(f => f.Product)
                        .ThenInclude(p => p.Category)
                    .Include(f => f.Product)
                        .ThenInclude(p => p.Brand)
                    .Include(f => f.Product)
                        .ThenInclude(p => p.Inventories)
                    .Where(f => f.UserId == userId)
                    .OrderByDescending(f => f.AddedAt);

                var totalCount = await query.CountAsync();

                var favorites = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var products = favorites.Select(f => MapProductToDto(f.Product)).ToList();

                return new PagedResult<ProductDto>(products, totalCount, page, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paged favorites for user: {UserId}", userId);
                throw;
            }
        }

        private ProductDto MapProductToDto(Product product)
        {
            return new ProductDto
            {
                Id = product.Id,
                SKU = product.SKU,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                DiscountPrice = product.DiscountPrice,
                ImageUrl = product.ImageUrl,
                Weight = product.Weight,
                Dimensions = product.Dimensions,
                Rating = product.Rating,
                ReviewCount = product.ReviewCount,
                IsFeatured = product.IsFeatured,
                IsNew = product.IsNew,
                IsActive = product.IsActive,
                ViewCount = product.ViewCount,
                Stock = product.Inventories?.Sum(i => i.Quantity) ?? 0,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name,
                Brand = product.Brand?.Name,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };
        }
    }
}
