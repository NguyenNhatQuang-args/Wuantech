using WuanTech.API.DTOs;
using WuanTech.API.DTOs.Product;
using WuanTech.Models;


namespace WuanTech.API.Services.Interfaces
{
    public interface IProductService
    {
        Task<PagedResult<ProductDto>> GetProductsAsync(int page, int pageSize, int? categoryId = null,
            string? search = null, decimal? minPrice = null, decimal? maxPrice = null,
            string? sortBy = "name", bool sortDesc = false);
        Task<ProductDetailDto?> GetProductByIdAsync(int id);
        Task<List<ProductDto>> GetFeaturedProductsAsync(int count = 8);
        Task<List<ProductDto>> GetNewProductsAsync(int count = 8);
        Task<List<ProductDto>> GetBestsellerProductsAsync(int count = 8);
        Task<List<ProductDto>> GetRelatedProductsAsync(int productId, int count = 4);
        Task<PagedResult<ProductDto>> SearchProductsAsync(string query, int page = 1, int pageSize = 12);
        Task<ProductDto> CreateProductAsync(CreateProductDto createProductDto);
        Task<ProductDto?> UpdateProductAsync(int id, UpdateProductDto updateProductDto);
        Task<bool> DeleteProductAsync(int id);
        Task<ReviewDto?> AddReviewAsync(int productId, int userId, CreateReviewDto createReviewDto);
        Task UpdateProductRatingAsync(int productId);
    }

    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto, string ipAddress = "");
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken, string ipAddress = "");
        Task<bool> LogoutAsync(int userId);
        Task<bool> ForgotPasswordAsync(string email);
        Task<bool> ResetPasswordAsync(string token, string newPassword);
        Task<UserDto?> GetUserByIdAsync(int userId);
        Task<bool> RevokeTokenAsync(string token, int userId, string ipAddress = "");
        Task<bool> VerifyEmailAsync(string token);
    }

    public interface ICartService
    {
        Task<CartDto> GetCartAsync(int userId);
        Task<CartItemDto?> AddToCartAsync(int userId, int productId, int quantity);
        Task<bool> UpdateCartItemAsync(int userId, int cartItemId, int quantity);
        Task<bool> RemoveFromCartAsync(int userId, int cartItemId);
        Task<bool> ClearCartAsync(int userId);
        Task<int> GetCartItemCountAsync(int userId);
        Task<bool> MergeCartAsync(int fromUserId, int toUserId);
    }

    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
        Task<CategoryDto?> GetCategoryByIdAsync(int id);
        Task<IEnumerable<CategoryMenuDto>> GetCategoryMenuAsync();
        Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto categoryDto);
        Task<CategoryDto?> UpdateCategoryAsync(int id, UpdateCategoryDto categoryDto);
        Task<bool> DeleteCategoryAsync(int id);
        Task<IEnumerable<CategoryDto>> GetCategoriesWithProductCountAsync();
    }

    public interface IOrderService
    {
        Task<IEnumerable<OrderDto>> GetUserOrdersAsync(int userId);
        Task<OrderDetailDto?> GetOrderByIdAsync(int orderId, int userId);
        Task<OrderDto?> CreateOrderAsync(int userId, CreateOrderDto orderDto);
        Task<bool> CancelOrderAsync(int orderId, int userId);
        Task<TrackingInfoDto?> GetTrackingInfoAsync(int orderId, int userId);
        Task<bool> UpdateOrderStatusAsync(int orderId, string status);
        Task<IEnumerable<OrderDto>> GetAllOrdersAsync(int page = 1, int pageSize = 20);
        Task<bool> UpdatePaymentStatusAsync(int orderId, string paymentStatus);
    }

    public interface IEmailService
    {
        Task SendOrderConfirmationEmailAsync(string email, Order? order);
        Task SendPasswordResetEmailAsync(string email, string resetToken);
        Task SendWelcomeEmailAsync(string email, string? fullName);
        Task SendOrderStatusUpdateEmailAsync(string email, Order order);
        Task SendNewsletterAsync(string email, string subject, string content);
    }

    public interface IPaymentService
    {
        Task<PaymentResultDto> ProcessPaymentAsync(PaymentRequestDto request);
        Task<bool> RefundPaymentAsync(string transactionId, decimal amount);
        Task<PaymentStatusDto> GetPaymentStatusAsync(string transactionId);
        Task<bool> ValidatePaymentAsync(string transactionId);
        Task<PaymentResultDto> ProcessStripePaymentAsync(PaymentRequestDto request);
        Task<PaymentResultDto> ProcessPayPalPaymentAsync(PaymentRequestDto request);
    }

    public interface ITodoService
    {
        Task<IEnumerable<TodoDto>> GetUserTodosAsync(int userId);
        Task<TodoDto> CreateTodoAsync(int userId, CreateTodoDto todoDto);
        Task<bool> UpdateTodoAsync(int userId, int todoId, UpdateTodoDto todoDto);
        Task<bool> DeleteTodoAsync(int userId, int todoId);
        Task<bool> ToggleTodoAsync(int userId, int todoId);
        Task<TodoStatsDto> GetTodoStatsAsync(int userId);
        Task<IEnumerable<TodoDto>> GetOverdueTodosAsync(int userId);
    }

    // Interface cho SearchService
    public interface ISearchService
    {
        Task<SearchResultDto> SearchAsync(string query);
        Task<IEnumerable<string>> GetSearchSuggestionsAsync(string query);
        Task<IEnumerable<string>> GetPopularSearchesAsync();
        Task<PagedResult<ProductDto>> AdvancedSearchAsync(ProductSearchFilterDto filters, int page = 1, int pageSize = 12);
        Task LogSearchQueryAsync(string query, int? userId = null);
    }

    // Interface cho InventoryService
    public interface IInventoryService
    {
        Task<IEnumerable<InventoryDto>> GetInventoryByProductAsync(int productId);
        Task<IEnumerable<InventoryDto>> GetInventoryByWarehouseAsync(int warehouseId);
        Task<bool> UpdateInventoryAsync(int productId, int warehouseId, int quantity);
        Task<bool> ReserveInventoryAsync(int productId, int warehouseId, int quantity);
        Task<bool> ReleaseInventoryAsync(int productId, int warehouseId, int quantity);
        Task<IEnumerable<InventoryAlertDto>> GetLowStockAlertsAsync();
        Task<bool> TransferInventoryAsync(int productId, int fromWarehouseId, int toWarehouseId, int quantity);
        Task<decimal> GetInventoryValueAsync(int? warehouseId = null);
    }

    public interface IBrandService
    {
        Task<IEnumerable<BrandDto>> GetAllBrandsAsync();
        Task<BrandDto?> GetBrandByIdAsync(int id);
        Task<BrandDto> CreateBrandAsync(CreateBrandDto brandDto);
        Task<BrandDto?> UpdateBrandAsync(int id, UpdateBrandDto brandDto);
        Task<bool> DeleteBrandAsync(int id);
        Task<IEnumerable<BrandDto>> GetBrandsWithProductCountAsync();
        Task<PagedResult<BrandDto>> GetBrandsPagedAsync(int page = 1, int pageSize = 20);
    }

    // Interface cho WarehouseService
    public interface IWarehouseService
    {
        Task<IEnumerable<WarehouseDto>> GetAllWarehousesAsync();
        Task<WarehouseDto?> GetWarehouseByIdAsync(int id);
        Task<WarehouseDto> CreateWarehouseAsync(CreateWarehouseDto warehouseDto);
        Task<WarehouseDto?> UpdateWarehouseAsync(int id, UpdateWarehouseDto warehouseDto);
        Task<bool> DeleteWarehouseAsync(int id);
        Task<WarehouseDto?> GetWarehouseByCodeAsync(string code);
        Task<IEnumerable<InventoryDto>> GetWarehouseInventoryAsync(int warehouseId);
    }

    // Interface cho FavoriteService
    public interface IFavoriteService
    {
        Task<IEnumerable<ProductDto>> GetUserFavoritesAsync(int userId);
        Task<bool> AddToFavoritesAsync(int userId, int productId);
        Task<bool> RemoveFromFavoritesAsync(int userId, int productId);
        Task<bool> IsProductFavoriteAsync(int userId, int productId);
        Task<int> GetFavoriteCountAsync(int userId);
        Task<bool> ClearFavoritesAsync(int userId);
        Task<PagedResult<ProductDto>> GetUserFavoritesPagedAsync(int userId, int page = 1, int pageSize = 12);
    }

    // Interface cho ReviewService  
    public interface IReviewService
    {
        Task<PagedResult<ReviewDto>> GetProductReviewsAsync(int productId, int page = 1, int pageSize = 10);
        Task<ReviewDto?> GetReviewByIdAsync(int reviewId);
        Task<ReviewDto> CreateReviewAsync(int productId, int userId, CreateReviewDto reviewDto);
        Task<bool> UpdateReviewAsync(int reviewId, int userId, UpdateReviewDto reviewDto);
        Task<bool> DeleteReviewAsync(int reviewId, int userId);
        Task<bool> ApproveReviewAsync(int reviewId);
        Task<IEnumerable<ReviewDto>> GetUserReviewsAsync(int userId);
        Task<bool> HasUserReviewedProductAsync(int userId, int productId);
    }

    // Interface cho CouponService
    public interface ICouponService
    {
        Task<IEnumerable<CouponDto>> GetAvailableCouponsAsync(int userId);
        Task<CouponDto?> GetCouponByCodeAsync(string code);
        Task<bool> ValidateCouponAsync(string code, decimal orderAmount);
        Task<decimal> CalculateDiscountAsync(string code, decimal orderAmount);
        Task<bool> UseCouponAsync(string code, int orderId);
        Task<CouponDto> CreateCouponAsync(CreateCouponDto couponDto);  
        Task<bool> DeactivateCouponAsync(string code);
    }

    // Interface cho ReportService
    public interface IReportService
    {
        Task<DashboardStatsDto> GetDashboardStatsAsync();
        Task<SalesReportDto> GetSalesReportAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<TopProductDto>> GetTopSellingProductsAsync(int count = 10);
        Task<IEnumerable<CustomerReportDto>> GetTopCustomersAsync(int count = 10);
        Task<InventoryReportDto> GetInventoryReportAsync();
    }

    // Additional DTOs needed
    public class SearchFilterDto
    {
        public string? Query { get; set; }
        public int? CategoryId { get; set; }
        public int? BrandId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public double? MinRating { get; set; }
        public bool? InStock { get; set; }
        public bool? OnSale { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; }
    }

    public class ReviewStatsDto
    {
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public Dictionary<int, int> RatingDistribution { get; set; } = new();
        public int VerifiedPurchases { get; set; }
    }

    public class RevenueReportDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal ProfitMargin { get; set; }
        public IEnumerable<DailyRevenueDto> DailyRevenue { get; set; } = new List<DailyRevenueDto>();
    }

    public class DailyRevenueDto
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public decimal Cost { get; set; }
        public decimal Profit { get; set; }
        public int OrderCount { get; set; }
    }

    public class ProductPerformanceReportDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int ViewCount { get; set; }
        public double ConversionRate { get; set; }
        public int ReturnCount { get; set; }
        public double ReturnRate { get; set; }
    }


}