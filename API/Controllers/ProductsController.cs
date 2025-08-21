// Controllers/ProductsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WuanTech.API.Services.Interfaces;
using WuanTech.Models;
using WuanTech.API.DTOs;
using WuanTech.API.DTOs.Product;

namespace WuanTech.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IProductService productService, ILogger<ProductsController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        /// <summary>
        /// Get all products with optional filtering and pagination
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<ProductDto>>>> GetProducts(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12,
            [FromQuery] int? categoryId = null,
            [FromQuery] string? search = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] string? sortBy = "name",
            [FromQuery] bool sortDesc = false)
        {
            try
            {
                var result = await _productService.GetProductsAsync(
                    page, pageSize, categoryId, search, minPrice, maxPrice, sortBy, sortDesc);

                return Ok(new ApiResponse<PagedResult<ProductDto>>
                {
                    Success = true,
                    Data = result,
                    Message = "Products retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products");
                return StatusCode(500, new ApiResponse<PagedResult<ProductDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving products"
                });
            }
        }

        /// <summary>
        /// Get product by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<ProductDetailDto>>> GetProduct(int id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                
                if (product == null)
                {
                    return NotFound(new ApiResponse<ProductDetailDto>
                    {
                        Success = false,
                        Message = "Product not found"
                    });
                }

                return Ok(new ApiResponse<ProductDetailDto>
                {
                    Success = true,
                    Data = product,
                    Message = "Product retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product with ID {ProductId}", id);
                return StatusCode(500, new ApiResponse<ProductDetailDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the product"
                });
            }
        }

        /// <summary>
        /// Get featured products
        /// </summary>
        [HttpGet("featured")]
        public async Task<ActionResult<ApiResponse<List<ProductDto>>>> GetFeaturedProducts()
        {
            try
            {
                var products = await _productService.GetFeaturedProductsAsync();
                
                return Ok(new ApiResponse<List<ProductDto>>
                {
                    Success = true,
                    Data = products,
                    Message = "Featured products retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting featured products");
                return StatusCode(500, new ApiResponse<List<ProductDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving featured products"
                });
            }
        }

        /// <summary>
        /// Get new products
        /// </summary>
        [HttpGet("new")]
        public async Task<ActionResult<ApiResponse<List<ProductDto>>>> GetNewProducts()
        {
            try
            {
                var products = await _productService.GetNewProductsAsync();
                
                return Ok(new ApiResponse<List<ProductDto>>
                {
                    Success = true,
                    Data = products,
                    Message = "New products retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting new products");
                return StatusCode(500, new ApiResponse<List<ProductDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving new products"
                });
            }
        }

        /// <summary>
        /// Get bestseller products
        /// </summary>
        [HttpGet("bestsellers")]
        public async Task<ActionResult<ApiResponse<List<ProductDto>>>> GetBestsellerProducts()
        {
            try
            {
                var products = await _productService.GetBestsellerProductsAsync();
                
                return Ok(new ApiResponse<List<ProductDto>>
                {
                    Success = true,
                    Data = products,
                    Message = "Bestseller products retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bestseller products");
                return StatusCode(500, new ApiResponse<List<ProductDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving bestseller products"
                });
            }
        }

        /// <summary>
        /// Create a new product (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct([FromBody] CreateProductDto createProductDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<ProductDto>
                    {
                        Success = false,
                        Message = "Invalid product data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                    });
                }

                var product = await _productService.CreateProductAsync(createProductDto);
                
                return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, new ApiResponse<ProductDto>
                {
                    Success = true,
                    Data = product,
                    Message = "Product created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return StatusCode(500, new ApiResponse<ProductDto>
                {
                    Success = false,
                    Message = "An error occurred while creating the product"
                });
            }
        }

        /// <summary>
        /// Update a product (Admin only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProduct(int id, [FromBody] UpdateProductDto updateProductDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<ProductDto>
                    {
                        Success = false,
                        Message = "Invalid product data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                    });
                }

                var product = await _productService.UpdateProductAsync(id, updateProductDto);
                
                if (product == null)
                {
                    return NotFound(new ApiResponse<ProductDto>
                    {
                        Success = false,
                        Message = "Product not found"
                    });
                }

                return Ok(new ApiResponse<ProductDto>
                {
                    Success = true,
                    Data = product,
                    Message = "Product updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product with ID {ProductId}", id);
                return StatusCode(500, new ApiResponse<ProductDto>
                {
                    Success = false,
                    Message = "An error occurred while updating the product"
                });
            }
        }

        /// <summary>
        /// Delete a product (Admin only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteProduct(int id)
        {
            try
            {
                var result = await _productService.DeleteProductAsync(id);
                
                if (!result)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Product not found"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Product deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product with ID {ProductId}", id);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while deleting the product"
                });
            }
        }

        /// <summary>
        /// Add a review to a product
        /// </summary>
        [HttpPost("{id}/reviews")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<ReviewDto>>> AddReview(int id, [FromBody] CreateReviewDto createReviewDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<ReviewDto>
                    {
                        Success = false,
                        Message = "Invalid review data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                    });
                }

                // Get user ID from JWT token
                var userIdClaim = User.FindFirst("userId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new ApiResponse<ReviewDto>
                    {
                        Success = false,
                        Message = "Invalid user credentials"
                    });
                }

                var review = await _productService.AddReviewAsync(id, userId, createReviewDto);
                
                if (review == null)
                {
                    return NotFound(new ApiResponse<ReviewDto>
                    {
                        Success = false,
                        Message = "Product not found"
                    });
                }

                return Ok(new ApiResponse<ReviewDto>
                {
                    Success = true,
                    Data = review,
                    Message = "Review added successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding review to product with ID {ProductId}", id);
                return StatusCode(500, new ApiResponse<ReviewDto>
                {
                    Success = false,
                    Message = "An error occurred while adding the review"
                });
            }
        }

        /// <summary>
        /// Get related products
        /// </summary>
        [HttpGet("{id}/related")]
        public async Task<ActionResult<ApiResponse<List<ProductDto>>>> GetRelatedProducts(int id, [FromQuery] int count = 4)
        {
            try
            {
                var products = await _productService.GetRelatedProductsAsync(id, count);
                
                return Ok(new ApiResponse<List<ProductDto>>
                {
                    Success = true,
                    Data = products,
                    Message = "Related products retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting related products for product ID {ProductId}", id);
                return StatusCode(500, new ApiResponse<List<ProductDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving related products"
                });
            }
        }

        /// <summary>
        /// Search products
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<PagedResult<ProductDto>>>> SearchProducts(
            [FromQuery] string query,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest(new ApiResponse<PagedResult<ProductDto>>
                    {
                        Success = false,
                        Message = "Search query is required"
                    });
                }

                var result = await _productService.SearchProductsAsync(query, page, pageSize);
                
                return Ok(new ApiResponse<PagedResult<ProductDto>>
                {
                    Success = true,
                    Data = result,
                    Message = "Search completed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products with query: {Query}", query);
                return StatusCode(500, new ApiResponse<PagedResult<ProductDto>>
                {
                    Success = false,
                    Message = "An error occurred while searching products"
                });
            }
        }
    }
}