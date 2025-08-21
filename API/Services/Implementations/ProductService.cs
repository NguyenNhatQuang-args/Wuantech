using Microsoft.EntityFrameworkCore;
using WuanTech.API.Data;
using WuanTech.API.DTOs;
using WuanTech.API.DTOs.Product;
using WuanTech.API.Services.Interfaces;
using WuanTech.Models;

namespace WuanTech.API.Services.Implementations
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductService> _logger;

        public ProductService(ApplicationDbContext context, ILogger<ProductService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PagedResult<ProductDto>> GetProductsAsync(int page, int pageSize, int? categoryId = null,
            string? search = null, decimal? minPrice = null, decimal? maxPrice = null,
            string? sortBy = "name", bool sortDesc = false)
        {
            try
            {
                var query = _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Images)
                    .Where(p => p.IsActive)
                    .AsQueryable();

                // Apply filters
                if (categoryId.HasValue)
                {
                    query = query.Where(p => p.CategoryId == categoryId.Value);
                }

                if (!string.IsNullOrEmpty(search))
                {
                    var searchLower = search.ToLower();
                    query = query.Where(p =>
                        p.Name.ToLower().Contains(searchLower) ||
                        p.Description.ToLower().Contains(searchLower) ||
                        (p.Brand != null && p.Brand.Name.ToLower().Contains(searchLower)) ||
                        p.SKU.ToLower().Contains(searchLower));
                }

                if (minPrice.HasValue)
                {
                    query = query.Where(p => (p.DiscountPrice ?? p.Price) >= minPrice.Value);
                }

                if (maxPrice.HasValue)
                {
                    query = query.Where(p => (p.DiscountPrice ?? p.Price) <= maxPrice.Value);
                }

                // Apply sorting
                query = sortBy?.ToLower() switch
                {
                    "price" => sortDesc ? query.OrderByDescending(p => p.DiscountPrice ?? p.Price)
                                        : query.OrderBy(p => p.DiscountPrice ?? p.Price),
                    "rating" => sortDesc ? query.OrderByDescending(p => p.Rating)
                                         : query.OrderBy(p => p.Rating),
                    "newest" => query.OrderByDescending(p => p.CreatedAt),
                    "popular" => query.OrderByDescending(p => p.ViewCount),
                    _ => sortDesc ? query.OrderByDescending(p => p.Name)
                                  : query.OrderBy(p => p.Name)
                };

                var totalCount = await query.CountAsync();

                var products = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var productDtos = products.Select(MapProductToDto).ToList();

                return new PagedResult<ProductDto>(productDtos, totalCount, page, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products");
                throw;
            }
        }

        public async Task<ProductDetailDto?> GetProductByIdAsync(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Images)
                    .Include(p => p.Specifications)
                    .Include(p => p.Reviews)
                        .ThenInclude(r => r.User)
                    .Include(p => p.Inventories)
                        .ThenInclude(i => i.Warehouse)
                    .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

                if (product == null)
                    return null;

                // Increment view count
                product.ViewCount++;
                await _context.SaveChangesAsync();

                return new ProductDetailDto
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
                    CategoryId = product.CategoryId,
                    CategoryName = product.Category?.Name,
                    Brand = product.Brand?.Name,
                    CreatedAt = product.CreatedAt,
                    UpdatedAt = product.UpdatedAt,
                    Images = product.Images?.Select(i => i.ImageUrl).ToList() ?? new List<string>(),
                    Specifications = product.Specifications?.Select(s => new ProductSpecificationDto
                    {
                        Id = s.Id,
                        Key = s.SpecKey,
                        Value = s.SpecValue,
                        DisplayOrder = s.DisplayOrder
                    }).OrderBy(s => s.DisplayOrder).ToList() ?? new List<ProductSpecificationDto>(),
                    Reviews = product.Reviews?.Where(r => r.IsApproved).Select(r => new ReviewDto
                    {
                        Id = r.Id,
                        ProductId = r.ProductId,
                        User = new UserDto
                        {
                            Id = r.User.Id,
                            Username = r.User.Username,
                            FullName = r.User.FullName,
                            Avatar = r.User.Avatar
                        },
                        Rating = r.Rating,
                        Comment = r.Comment,
                        IsVerifiedPurchase = r.IsVerifiedPurchase,
                        IsApproved = r.IsApproved,
                        CreatedAt = r.CreatedAt,
                        Images = new List<string>()
                    }).OrderByDescending(r => r.CreatedAt).ToList() ?? new List<ReviewDto>(),
                    Category = product.Category != null ? new CategoryDto
                    {
                        Id = product.Category.Id,
                        Name = product.Category.Name,
                        Icon = product.Category.Icon,
                        Description = product.Category.Description
                    } : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product by id: {ProductId}", id);
                throw;
            }
        }

        public async Task<List<ProductDto>> GetFeaturedProductsAsync(int count = 8)
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Where(p => p.IsFeatured && p.IsActive)
                    .OrderByDescending(p => p.Rating)
                    .ThenByDescending(p => p.CreatedAt)
                    .Take(count)
                    .ToListAsync();

                return products.Select(MapProductToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting featured products");
                throw;
            }
        }

        public async Task<List<ProductDto>> GetNewProductsAsync(int count = 8)
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Where(p => p.IsNew && p.IsActive)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(count)
                    .ToListAsync();

                return products.Select(MapProductToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting new products");
                throw;
            }
        }

        public async Task<List<ProductDto>> GetBestsellerProductsAsync(int count = 8)
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.ReviewCount)
                    .ThenByDescending(p => p.Rating)
                    .Take(count)
                    .ToListAsync();

                return products.Select(MapProductToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bestseller products");
                throw;
            }
        }

        public async Task<List<ProductDto>> GetRelatedProductsAsync(int productId, int count = 4)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                    return new List<ProductDto>();

                var relatedProducts = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Where(p => p.Id != productId &&
                               p.CategoryId == product.CategoryId &&
                               p.IsActive)
                    .OrderByDescending(p => p.Rating)
                    .Take(count)
                    .ToListAsync();

                return relatedProducts.Select(MapProductToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting related products for product {ProductId}", productId);
                throw;
            }
        }

        public async Task<PagedResult<ProductDto>> SearchProductsAsync(string query, int page = 1, int pageSize = 12)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return new PagedResult<ProductDto>(new List<ProductDto>(), 0, page, pageSize);
                }

                var searchLower = query.ToLower();
                var productsQuery = _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Where(p => p.IsActive && (
                        p.Name.ToLower().Contains(searchLower) ||
                        p.Description.ToLower().Contains(searchLower) ||
                        (p.Brand != null && p.Brand.Name.ToLower().Contains(searchLower)) ||
                        p.SKU.ToLower().Contains(searchLower) ||
                        (p.Category != null && p.Category.Name.ToLower().Contains(searchLower))
                    ))
                    .OrderByDescending(p => p.Rating)
                    .ThenByDescending(p => p.ReviewCount);

                var totalCount = await productsQuery.CountAsync();
                var products = await productsQuery
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var productDtos = products.Select(MapProductToDto).ToList();

                return new PagedResult<ProductDto>(productDtos, totalCount, page, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products with query: {Query}", query);
                throw;
            }
        }

        public async Task<ProductDto> CreateProductAsync(CreateProductDto createProductDto)
        {
            try
            {
                // Check if SKU already exists
                if (await _context.Products.AnyAsync(p => p.SKU == createProductDto.SKU))
                {
                    throw new InvalidOperationException($"Product with SKU '{createProductDto.SKU}' already exists");
                }

                var product = new Product
                {
                    SKU = createProductDto.SKU,
                    Name = createProductDto.Name,
                    Description = createProductDto.Description,
                    Price = createProductDto.Price,
                    DiscountPrice = createProductDto.DiscountPrice,
                    CategoryId = createProductDto.CategoryId,
                    ImageUrl = createProductDto.ImageUrl,
                    Weight = createProductDto.Weight,
                    Dimensions = createProductDto.Dimensions,
                    IsFeatured = createProductDto.IsFeatured,
                    IsNew = createProductDto.IsNew,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Set brand if provided
                if (!string.IsNullOrEmpty(createProductDto.Brand))
                {
                    var brand = await _context.Brands.FirstOrDefaultAsync(b => b.Name == createProductDto.Brand);
                    if (brand != null)
                    {
                        product.BrandId = brand.Id;
                    }
                }

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // Add product images
                if (createProductDto.Images?.Any() == true)
                {
                    var images = createProductDto.Images.Select((img, index) => new ProductImage
                    {
                        ProductId = product.Id,
                        ImageUrl = img,
                        IsMain = index == 0,
                        DisplayOrder = index
                    }).ToList();

                    _context.ProductImages.AddRange(images);
                }

                // Add specifications
                if (createProductDto.Specifications?.Any() == true)
                {
                    var specifications = createProductDto.Specifications.Select(spec => new ProductSpecification
                    {
                        ProductId = product.Id,
                        SpecKey = spec.Key,
                        SpecValue = spec.Value,
                        DisplayOrder = spec.DisplayOrder
                    }).ToList();

                    _context.ProductSpecifications.AddRange(specifications);
                }

                await _context.SaveChangesAsync();

                // Return created product
                return await GetProductDtoByIdAsync(product.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                throw;
            }
        }

        public async Task<ProductDto?> UpdateProductAsync(int id, UpdateProductDto updateProductDto)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Images)
                    .Include(p => p.Specifications)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                    return null;

                // Check if SKU change conflicts with existing product
                if (product.SKU != updateProductDto.SKU)
                {
                    if (await _context.Products.AnyAsync(p => p.SKU == updateProductDto.SKU && p.Id != id))
                    {
                        throw new InvalidOperationException($"Product with SKU '{updateProductDto.SKU}' already exists");
                    }
                }

                // Update basic properties
                product.SKU = updateProductDto.SKU;
                product.Name = updateProductDto.Name;
                product.Description = updateProductDto.Description;
                product.Price = updateProductDto.Price;
                product.DiscountPrice = updateProductDto.DiscountPrice;
                product.CategoryId = updateProductDto.CategoryId;
                product.ImageUrl = updateProductDto.ImageUrl;
                product.Weight = updateProductDto.Weight;
                product.Dimensions = updateProductDto.Dimensions;
                product.IsFeatured = updateProductDto.IsFeatured;
                product.IsNew = updateProductDto.IsNew;
                product.UpdatedAt = DateTime.UtcNow;

                // Update brand
                if (!string.IsNullOrEmpty(updateProductDto.Brand))
                {
                    var brand = await _context.Brands.FirstOrDefaultAsync(b => b.Name == updateProductDto.Brand);
                    product.BrandId = brand?.Id;
                }

                // Update images
                if (updateProductDto.Images?.Any() == true)
                {
                    // Remove existing images
                    _context.ProductImages.RemoveRange(product.Images);

                    // Add new images
                    var newImages = updateProductDto.Images.Select((img, index) => new ProductImage
                    {
                        ProductId = product.Id,
                        ImageUrl = img,
                        IsMain = index == 0,
                        DisplayOrder = index
                    }).ToList();

                    _context.ProductImages.AddRange(newImages);
                }

                // Update specifications
                if (updateProductDto.Specifications?.Any() == true)
                {
                    // Remove existing specifications
                    _context.ProductSpecifications.RemoveRange(product.Specifications);

                    // Add new specifications
                    var newSpecs = updateProductDto.Specifications.Select(spec => new ProductSpecification
                    {
                        ProductId = product.Id,
                        SpecKey = spec.Key,
                        SpecValue = spec.Value,
                        DisplayOrder = spec.DisplayOrder
                    }).ToList();

                    _context.ProductSpecifications.AddRange(newSpecs);
                }

                await _context.SaveChangesAsync();

                return await GetProductDtoByIdAsync(product.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {ProductId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    return false;

                // Soft delete
                product.IsActive = false;
                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {ProductId}", id);
                throw;
            }
        }

        public async Task<ReviewDto?> AddReviewAsync(int productId, int userId, CreateReviewDto createReviewDto)
        {
            try
            {
                // Check if product exists
                var productExists = await _context.Products.AnyAsync(p => p.Id == productId && p.IsActive);
                if (!productExists)
                    return null;

                // Check if user has already reviewed this product
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId);

                if (existingReview != null)
                    throw new InvalidOperationException("User has already reviewed this product");

                // Check if user has purchased this product
                var hasPurchased = await _context.OrderItems
                    .Include(oi => oi.Order)
                        .ThenInclude(o => o.Customer)
                    .AnyAsync(oi => oi.ProductId == productId &&
                                  oi.Order.Customer != null &&
                                  oi.Order.Customer.UserId == userId &&
                                  oi.Order.Status == "DELIVERED");

                var review = new Review
                {
                    ProductId = productId,
                    UserId = userId,
                    Rating = createReviewDto.Rating,
                    Comment = createReviewDto.Comment,
                    IsVerifiedPurchase = hasPurchased,
                    IsApproved = true, // Auto-approve for now
                    CreatedAt = DateTime.UtcNow
                };

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                // Update product rating
                await UpdateProductRatingAsync(productId);

                // Load user data for response
                await _context.Entry(review).Reference(r => r.User).LoadAsync();

                return new ReviewDto
                {
                    Id = review.Id,
                    ProductId = review.ProductId,
                    User = new UserDto
                    {
                        Id = review.User.Id,
                        Username = review.User.Username,
                        FullName = review.User.FullName,
                        Avatar = review.User.Avatar
                    },
                    Rating = review.Rating,
                    Comment = review.Comment,
                    IsVerifiedPurchase = review.IsVerifiedPurchase,
                    IsApproved = review.IsApproved,
                    CreatedAt = review.CreatedAt,
                    Images = createReviewDto.Images ?? new List<string>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding review for product {ProductId}", productId);
                throw;
            }
        }

        public async Task UpdateProductRatingAsync(int productId)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                    return;

                var reviews = await _context.Reviews
                    .Where(r => r.ProductId == productId && r.IsApproved)
                    .ToListAsync();

                if (reviews.Any())
                {
                    product.Rating = reviews.Average(r => r.Rating);
                    product.ReviewCount = reviews.Count;
                }
                else
                {
                    product.Rating = 0;
                    product.ReviewCount = 0;
                }

                product.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product rating for product {ProductId}", productId);
                throw;
            }
        }

        #region Private Methods

        private async Task<ProductDto> GetProductDtoByIdAsync(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            return product == null ? throw new InvalidOperationException("Product not found") : MapProductToDto(product);
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
                Stock = product.TotalStock, // Using computed property from model
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name,
                Brand = product.Brand?.Name,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt,
                Images = product.Images?.Select(i => i.ImageUrl).ToList() ?? new List<string>()
            };
        }

        #endregion
    }
}