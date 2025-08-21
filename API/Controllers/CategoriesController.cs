// CategoriesController.cs - Fixed
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WuanTech.API.Data;
using WuanTech.API.DTOs;
using WuanTech.API.Services.Interfaces;

namespace WuanTech.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(ICategoryService categoryService, ILogger<CategoriesController> logger)
        {
            _categoryService = categoryService;
            _logger = logger;
        }

        /// <summary>
        /// Get all categories
        /// </summary> 
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<CategoryDto>>>> GetAllCategories()
        {
            try
            {
                var categories = await _categoryService.GetAllCategoriesAsync();
                return Ok(new ApiResponse<IEnumerable<CategoryDto>>
                {
                    Success = true,
                    Data = categories,
                    Message = "Categories retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                return StatusCode(500, new ApiResponse<IEnumerable<CategoryDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving categories"
                });
            }
        }

        /// <summary>
        /// Get category by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<CategoryDto>>> GetCategoryById(int id)
        {
            try
            {
                var category = await _categoryService.GetCategoryByIdAsync(id);
                if (category == null)
                {
                    return NotFound(new ApiResponse<CategoryDto>
                    {
                        Success = false,
                        Message = "Category not found"
                    });
                }

                return Ok(new ApiResponse<CategoryDto>
                {
                    Success = true,
                    Data = category,
                    Message = "Category retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category by id: {Id}", id);
                return StatusCode(500, new ApiResponse<CategoryDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the category"
                });
            }
        }

        /// <summary>
        /// Get category menu for navigation
        /// </summary>
        [HttpGet("menu")]
        public async Task<ActionResult<ApiResponse<IEnumerable<CategoryMenuDto>>>> GetCategoryMenu()
        {
            try
            {
                var menu = await _categoryService.GetCategoryMenuAsync();
                return Ok(new ApiResponse<IEnumerable<CategoryMenuDto>>
                {
                    Success = true,
                    Data = menu,
                    Message = "Category menu retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category menu");
                return StatusCode(500, new ApiResponse<IEnumerable<CategoryMenuDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving category menu"
                });
            }
        }

        /// <summary>
        /// Create a new category (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<CategoryDto>>> CreateCategory([FromBody] CreateCategoryDto categoryDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<CategoryDto>
                    {
                        Success = false,
                        Message = "Invalid category data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                    });
                }

                var category = await _categoryService.CreateCategoryAsync(categoryDto);
                return CreatedAtAction(nameof(GetCategoryById), new { id = category.Id }, new ApiResponse<CategoryDto>
                {
                    Success = true,
                    Data = category,
                    Message = "Category created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                return StatusCode(500, new ApiResponse<CategoryDto>
                {
                    Success = false,
                    Message = "An error occurred while creating the category"
                });
            }
        }

        /// <summary>
        /// Update a category (Admin only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<CategoryDto>>> UpdateCategory(int id, [FromBody] UpdateCategoryDto categoryDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<CategoryDto>
                    {
                        Success = false,
                        Message = "Invalid category data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                    });
                }

                var category = await _categoryService.UpdateCategoryAsync(id, categoryDto);
                if (category == null)
                {
                    return NotFound(new ApiResponse<CategoryDto>
                    {
                        Success = false,
                        Message = "Category not found"
                    });
                }

                return Ok(new ApiResponse<CategoryDto>
                {
                    Success = true,
                    Data = category,
                    Message = "Category updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category: {Id}", id);
                return StatusCode(500, new ApiResponse<CategoryDto>
                {
                    Success = false,
                    Message = "An error occurred while updating the category"
                });
            }
        }

        /// <summary>
        /// Delete a category (Admin only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteCategory(int id)
        {
            try
            {
                var result = await _categoryService.DeleteCategoryAsync(id);
                if (!result)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Category not found"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Category deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category: {Id}", id);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while deleting the category"
                });
            }
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _searchService;
        private readonly ILogger<SearchController> _logger;

        public SearchController(ISearchService searchService, ILogger<SearchController> logger)
        {
            _searchService = searchService;
            _logger = logger;
        }

        /// <summary>
        /// Search products and categories
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<SearchResultDto>>> Search([FromQuery] string q)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                {
                    return BadRequest(new ApiResponse<SearchResultDto>
                    {
                        Success = false,
                        Message = "Search query cannot be empty"
                    });
                }

                var results = await _searchService.SearchAsync(q);
                return Ok(new ApiResponse<SearchResultDto>
                {
                    Success = true,
                    Data = results,
                    Message = "Search completed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching products and categories");
                return StatusCode(500, new ApiResponse<SearchResultDto>
                {
                    Success = false,
                    Message = "An error occurred while searching"
                });
            }
        }

        /// <summary>
        /// Get search suggestions
        /// </summary>
        [HttpGet("suggestions")]
        public async Task<ActionResult<ApiResponse<IEnumerable<string>>>> GetSuggestions([FromQuery] string q)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                {
                    return BadRequest(new ApiResponse<IEnumerable<string>>
                    {
                        Success = false,
                        Message = "Search query cannot be empty"
                    });
                }

                var suggestions = await _searchService.GetSearchSuggestionsAsync(q);
                return Ok(new ApiResponse<IEnumerable<string>>
                {
                    Success = true,
                    Data = suggestions,
                    Message = "Search suggestions retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting search suggestions");
                return StatusCode(500, new ApiResponse<IEnumerable<string>>
                {
                    Success = false,
                    Message = "An error occurred while getting search suggestions"
                });
            }
        }

        /// <summary>
        /// Get popular search terms
        /// </summary>
        [HttpGet("popular")]
        public async Task<ActionResult<ApiResponse<IEnumerable<string>>>> GetPopularSearches()
        {
            try
            {
                var searches = await _searchService.GetPopularSearchesAsync();
                return Ok(new ApiResponse<IEnumerable<string>>
                {
                    Success = true,
                    Data = searches,
                    Message = "Popular search terms retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting popular search terms");
                return StatusCode(500, new ApiResponse<IEnumerable<string>>
                {
                    Success = false,
                    Message = "An error occurred while getting popular search terms"
                });
            }
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserController> _logger;

        public UserController(ApplicationDbContext context, ILogger<UserController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get user profile
        /// </summary>
        [HttpGet("profile")]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetProfile()
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound(new ApiResponse<UserDto>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                return Ok(new ApiResponse<UserDto>
                {
                    Success = true,
                    Data = new UserDto
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        FullName = user.FullName,
                        PhoneNumber = user.PhoneNumber,
                        Address = user.Address,
                        Avatar = user.Avatar,
                        Role = user.Role,
                        IsActive = user.IsActive,
                        CreatedAt = user.CreatedAt,
                        LastLogin = user.LastLogin
                    },
                    Message = "Profile retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile");
                return StatusCode(500, new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving profile"
                });
            }
        }

        /// <summary>
        /// Update user profile
        /// </summary>
        [HttpPut("profile")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateProfile([FromBody] UpdateUserProfileDto profileDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid profile data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                    });
                }

                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                user.FullName = profileDto.FullName ?? user.FullName;
                user.PhoneNumber = profileDto.PhoneNumber ?? user.PhoneNumber;
                user.Address = profileDto.Address ?? user.Address;
                user.Avatar = profileDto.Avatar ?? user.Avatar;

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Profile updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while updating profile"
                });
            }
        }

        /// <summary>
        /// Change password
        /// </summary>
        [HttpPost("change-password")]
        public async Task<ActionResult<ApiResponse<bool>>> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Invalid password data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                    });
                }

                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                if (!BCrypt.Net.BCrypt.Verify(changePasswordDto.CurrentPassword, user.PasswordHash))
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Current password is incorrect"
                    });
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Password changed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while changing password"
                });
            }
        }
    }

    // FavoritesController
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FavoritesController : ControllerBase
    {
        private readonly IFavoriteService _favoriteService;
        private readonly ILogger<FavoritesController> _logger;

        public FavoritesController(IFavoriteService favoriteService, ILogger<FavoritesController> logger)
        {
            _favoriteService = favoriteService;
            _logger = logger;
        }

        /// <summary>
        /// Get user's favorite products
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProductDto>>>> GetUserFavorites()
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                var favorites = await _favoriteService.GetUserFavoritesAsync(userId);

                return Ok(new ApiResponse<IEnumerable<ProductDto>>
                {
                    Success = true,
                    Data = favorites,
                    Message = "Favorites retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user favorites");
                return StatusCode(500, new ApiResponse<IEnumerable<ProductDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving favorites"
                });
            }
        }

        /// <summary>
        /// Add product to favorites
        /// </summary>
        [HttpPost("{productId}")]
        public async Task<ActionResult<ApiResponse<bool>>> AddToFavorites(int productId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                var result = await _favoriteService.AddToFavoritesAsync(userId, productId);

                if (!result)
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Product already in favorites or not found"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Product added to favorites successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to favorites");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while adding to favorites"
                });
            }
        }

        /// <summary>
        /// Remove product from favorites
        /// </summary>
        [HttpDelete("{productId}")]
        public async Task<ActionResult<ApiResponse<bool>>> RemoveFromFavorites(int productId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                var result = await _favoriteService.RemoveFromFavoritesAsync(userId, productId);

                if (!result)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Product not found in favorites"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Product removed from favorites successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing from favorites");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while removing from favorites"
                });
            }
        }

        /// <summary>
        /// Check if product is in favorites
        /// </summary>
        [HttpGet("{productId}/check")]
        public async Task<ActionResult<ApiResponse<bool>>> IsProductFavorite(int productId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                var result = await _favoriteService.IsProductFavoriteAsync(userId, productId);

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = result,
                    Message = result ? "Product is in favorites" : "Product is not in favorites"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if product is favorite");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while checking favorites"
                });
            }
        }
    }
}