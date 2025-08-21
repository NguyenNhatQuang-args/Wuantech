using Microsoft.EntityFrameworkCore;
using WuanTech.API.Data;
using WuanTech.API.DTOs;
using WuanTech.API.Services.Interfaces;
using WuanTech.Models;

namespace WuanTech.API.Services.Implementations
{
    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CartService> _logger;

        public CartService(ApplicationDbContext context, ILogger<CartService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<CartDto> GetCartAsync(int userId)
        {
            try
            {
                var cartItems = await _context.CartItems
                    .Include(ci => ci.Product)
                        .ThenInclude(p => p.Category)
                    .Include(ci => ci.Product)
                        .ThenInclude(p => p.Brand)
                    .Include(ci => ci.Product)
                        .ThenInclude(p => p.Inventories)
                    .Where(ci => ci.UserId == userId)
                    .ToListAsync();

                var cartDto = new CartDto
                {
                    Items = cartItems.Select(ci => new CartItemDto
                    {
                        Id = ci.Id,
                        Product = MapProductToDto(ci.Product),
                        Quantity = ci.Quantity,
                        UnitPrice = ci.Product.DiscountPrice ?? ci.Product.Price,
                        TotalPrice = (ci.Product.DiscountPrice ?? ci.Product.Price) * ci.Quantity,
                        AddedAt = ci.AddedAt,
                        UpdatedAt = ci.UpdatedAt
                    }).ToList()
                };

                CalculateCartTotals(cartDto);
                return cartDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<CartItemDto?> AddToCartAsync(int userId, int productId, int quantity)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Inventories)
                    .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);

                if (product == null)
                    return null;

                // Check stock availability
                var totalStock = product.Inventories?.Sum(i => i.Quantity) ?? 0;
                if (totalStock < quantity)
                    return null;

                var existingItem = await _context.CartItems
                    .FirstOrDefaultAsync(ci => ci.UserId == userId && ci.ProductId == productId);

                if (existingItem != null)
                {
                    // Check if new quantity exceeds stock
                    if (totalStock < existingItem.Quantity + quantity)
                        return null;

                    existingItem.Quantity += quantity;
                    existingItem.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    existingItem = new CartItem
                    {
                        UserId = userId,
                        ProductId = productId,
                        Quantity = quantity,
                        AddedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.CartItems.Add(existingItem);
                }

                await _context.SaveChangesAsync();

                return new CartItemDto
                {
                    Id = existingItem.Id,
                    Product = MapProductToDto(product),
                    Quantity = existingItem.Quantity,
                    UnitPrice = product.DiscountPrice ?? product.Price,
                    TotalPrice = (product.DiscountPrice ?? product.Price) * existingItem.Quantity,
                    AddedAt = existingItem.AddedAt,
                    UpdatedAt = existingItem.UpdatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to cart: UserId={UserId}, ProductId={ProductId}", userId, productId);
                throw;
            }
        }

        public async Task<bool> UpdateCartItemAsync(int userId, int cartItemId, int quantity)
        {
            try
            {
                var cartItem = await _context.CartItems
                    .Include(ci => ci.Product)
                        .ThenInclude(p => p.Inventories)
                    .FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.UserId == userId);

                if (cartItem == null)
                    return false;

                // Check stock availability
                var totalStock = cartItem.Product.Inventories?.Sum(i => i.Quantity) ?? 0;
                if (totalStock < quantity)
                    return false;

                cartItem.Quantity = quantity;
                cartItem.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart item: {CartItemId}", cartItemId);
                throw;
            }
        }

        public async Task<bool> RemoveFromCartAsync(int userId, int cartItemId)
        {
            try
            {
                var cartItem = await _context.CartItems
                    .FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.UserId == userId);

                if (cartItem == null)
                    return false;

                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing from cart: {CartItemId}", cartItemId);
                throw;
            }
        }

        public async Task<bool> ClearCartAsync(int userId)
        {
            try
            {
                var cartItems = await _context.CartItems
                    .Where(ci => ci.UserId == userId)
                    .ToListAsync();

                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<int> GetCartItemCountAsync(int userId)
        {
            try
            {
                return await _context.CartItems
                    .Where(ci => ci.UserId == userId)
                    .SumAsync(ci => ci.Quantity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart item count for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> MergeCartAsync(int fromUserId, int toUserId)
        {
            try
            {
                var fromCartItems = await _context.CartItems
                    .Where(ci => ci.UserId == fromUserId)
                    .ToListAsync();

                var toCartItems = await _context.CartItems
                    .Where(ci => ci.UserId == toUserId)
                    .ToListAsync();

                foreach (var fromItem in fromCartItems)
                {
                    var existingToItem = toCartItems.FirstOrDefault(ci => ci.ProductId == fromItem.ProductId);

                    if (existingToItem != null)
                    {
                        existingToItem.Quantity += fromItem.Quantity;
                        existingToItem.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        fromItem.UserId = toUserId;
                        fromItem.UpdatedAt = DateTime.UtcNow;
                    }
                }

                // Remove duplicate items
                var duplicateItems = fromCartItems.Where(fi =>
                    toCartItems.Any(ti => ti.ProductId == fi.ProductId)).ToList();

                _context.CartItems.RemoveRange(duplicateItems);

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error merging cart from user {FromUserId} to user {ToUserId}", fromUserId, toUserId);
                throw;
            }
        }

        #region Private Methods

        private void CalculateCartTotals(CartDto cart)
        {
            cart.SubTotal = cart.Items.Sum(i => i.TotalPrice);
            cart.Tax = cart.SubTotal * 0.1m; // 10% VAT
            cart.ShippingFee = cart.SubTotal > 500000 ? 0 : 30000; // Free shipping over 500k VND
            cart.Discount = 0; // Apply coupon discounts here
            cart.Total = cart.SubTotal + cart.Tax + cart.ShippingFee - cart.Discount;
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

        #endregion
    }
}