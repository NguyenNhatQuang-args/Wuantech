using Microsoft.EntityFrameworkCore;
using WuanTech.API.Data;
using WuanTech.API.DTOs;
using WuanTech.API.DTOs.Product;
using WuanTech.API.Services.Interfaces;
using WuanTech.Models;

namespace WuanTech.API.Services.Implementations
{
    public class ReviewService : IReviewService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReviewService> _logger;

        public ReviewService(ApplicationDbContext context, ILogger<ReviewService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PagedResult<ReviewDto>> GetProductReviewsAsync(int productId, int page = 1, int pageSize = 10)
        {
            try
            {
                var query = _context.Reviews
                    .Include(r => r.User)
                    .Include(r => r.Product)
                    .Where(r => r.ProductId == productId && r.IsApproved)
                    .OrderByDescending(r => r.CreatedAt);

                var totalCount = await query.CountAsync();

                var reviews = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var reviewDtos = reviews.Select(r => new ReviewDto
                {
                    Id = r.Id,
                    ProductId = r.ProductId,
                    ProductName = r.Product.Name,    // ← ĐÃ CÓ PROPERTY
                    User = new UserDto
                    {
                        Id = r.User.Id,
                        Username = r.User.Username,
                        FullName = r.User.FullName,
                        Avatar = r.User.Avatar
                    },
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    IsVerifiedPurchase = r.IsVerifiedPurchase,
                    IsApproved = r.IsApproved
                }).ToList();

                return new PagedResult<ReviewDto>(reviewDtos, totalCount, page, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product reviews: {ProductId}", productId);
                throw;
            }
        }

        public async Task<ReviewDto?> GetReviewByIdAsync(int reviewId)
        {
            try
            {
                var review = await _context.Reviews
                    .Include(r => r.User)
                    .Include(r => r.Product)
                    .FirstOrDefaultAsync(r => r.Id == reviewId);

                if (review == null)
                    return null;

                return new ReviewDto
                {
                    Id = review.Id,
                    ProductId = review.ProductId,
                    ProductName = review.Product.Name,    // ← ĐÃ CÓ PROPERTY
                    User = new UserDto
                    {
                        Id = review.User.Id,
                        Username = review.User.Username,
                        FullName = review.User.FullName,
                        Avatar = review.User.Avatar
                    },
                    Rating = review.Rating,
                    Comment = review.Comment,
                    CreatedAt = review.CreatedAt,
                    IsVerifiedPurchase = review.IsVerifiedPurchase,
                    IsApproved = review.IsApproved
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting review by id: {ReviewId}", reviewId);
                throw;
            }
        }

        public async Task<ReviewDto> CreateReviewAsync(int productId, int userId, CreateReviewDto reviewDto)
        {
            try
            {
                // Check if user already reviewed this product
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId);

                if (existingReview != null)
                    throw new InvalidOperationException("User has already reviewed this product");

                var review = new Review
                {
                    ProductId = productId,
                    UserId = userId,
                    Rating = reviewDto.Rating,
                    Comment = reviewDto.Comment,
                    CreatedAt = DateTime.UtcNow,
                    IsVerifiedPurchase = await _context.OrderItems
                        .AnyAsync(oi => oi.Order.Customer.UserId == userId && oi.ProductId == productId),
                    IsApproved = true
                };

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                // Update product rating
                await UpdateProductRatingAsync(productId);

                // Load related data
                await _context.Entry(review).Reference(r => r.User).LoadAsync();
                await _context.Entry(review).Reference(r => r.Product).LoadAsync();

                return new ReviewDto
                {
                    Id = review.Id,
                    ProductId = review.ProductId,
                    ProductName = review.Product.Name,    // ← ĐÃ CÓ PROPERTY
                    User = new UserDto
                    {
                        Id = review.User.Id,
                        Username = review.User.Username,
                        FullName = review.User.FullName,
                        Avatar = review.User.Avatar
                    },
                    Rating = review.Rating,
                    Comment = review.Comment,
                    CreatedAt = review.CreatedAt,
                    IsVerifiedPurchase = review.IsVerifiedPurchase,
                    IsApproved = review.IsApproved
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating review");
                throw;
            }
        }

        public async Task<bool> UpdateReviewAsync(int reviewId, int userId, UpdateReviewDto reviewDto)
        {
            try
            {
                var review = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == userId);

                if (review == null)
                    return false;

                review.Rating = reviewDto.Rating;
                review.Comment = reviewDto.Comment;

                await _context.SaveChangesAsync();

                // Update product rating
                await UpdateProductRatingAsync(review.ProductId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating review: {ReviewId}", reviewId);
                throw;
            }
        }

        public async Task<bool> DeleteReviewAsync(int reviewId, int userId)
        {
            try
            {
                var review = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == userId);

                if (review == null)
                    return false;

                var productId = review.ProductId;
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();

                // Update product rating
                await UpdateProductRatingAsync(productId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting review: {ReviewId}", reviewId);
                throw;
            }
        }

        public async Task<bool> ApproveReviewAsync(int reviewId)
        {
            try
            {
                var review = await _context.Reviews.FindAsync(reviewId);
                if (review == null)
                    return false;

                review.IsApproved = true;
                await _context.SaveChangesAsync();

                // Update product rating
                await UpdateProductRatingAsync(review.ProductId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving review: {ReviewId}", reviewId);
                throw;
            }
        }

        public async Task<IEnumerable<ReviewDto>> GetUserReviewsAsync(int userId)
        {
            try
            {
                var reviews = await _context.Reviews
                    .Include(r => r.Product)
                    .Where(r => r.UserId == userId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                return reviews.Select(r => new ReviewDto
                {
                    Id = r.Id,
                    ProductId = r.ProductId,
                    ProductName = r.Product.Name,    // ← ĐÃ CÓ PROPERTY
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    IsVerifiedPurchase = r.IsVerifiedPurchase,
                    IsApproved = r.IsApproved
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user reviews: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> HasUserReviewedProductAsync(int userId, int productId)
        {
            try
            {
                return await _context.Reviews
                    .AnyAsync(r => r.UserId == userId && r.ProductId == productId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user reviewed product");
                throw;
            }
        }

        private async Task UpdateProductRatingAsync(int productId)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null) return;

                var reviews = await _context.Reviews
                    .Where(r => r.ProductId == productId && r.IsApproved)
                    .ToListAsync();

                // SỬA: Cast sang double trước rồi mới cast sang decimal
                product.Rating = reviews.Any() ? (double)reviews.Average(r => (decimal)r.Rating) : 0;
                product.ReviewCount = reviews.Count;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product rating: {ProductId}", productId);
            }
        }
    }
}
