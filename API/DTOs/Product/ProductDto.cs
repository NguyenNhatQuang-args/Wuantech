using System.ComponentModel.DataAnnotations;
using WuanTech.API.DTOs.Product;
using WuanTech.API.Validation;

namespace WuanTech.API.DTOs
{

    // Auth DTOs
    public class RegisterDto
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
        public string FullName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number format")]
        public string? PhoneNumber { get; set; }
    }

    public class LoginDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public UserDto? User { get; set; }
    }

    public class RefreshTokenDto
    {
        [Required(ErrorMessage = "Refresh token is required")]
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordDto
    {
        [Required(ErrorMessage = "Token is required")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string NewPassword { get; set; } = string.Empty;
    }

    // User DTOs
    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? Avatar { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
    }

    public class UpdateUserProfileDto
    {
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
        public string? FullName { get; set; }

        [Phone(ErrorMessage = "Invalid phone number format")]
        public string? PhoneNumber { get; set; }

        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string? Address { get; set; }

        [Url(ErrorMessage = "Invalid avatar URL")]
        public string? Avatar { get; set; }
    }

    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Current password is required")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string NewPassword { get; set; } = string.Empty;
    }

    // Product DTOs
    public class ProductDto
    {
        public int Id { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public decimal FinalPrice => DiscountPrice ?? Price;
        public string? ImageUrl { get; set; }
        public decimal? Weight { get; set; }
        public string? Dimensions { get; set; }
        public double Rating { get; set; }
        public int ReviewCount { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsNew { get; set; }
        public bool IsActive { get; set; }
        public int ViewCount { get; set; }
        public int Stock { get; set; }
        public bool InStock => Stock > 0;
        public bool HasDiscount => DiscountPrice.HasValue && DiscountPrice < Price;
        public decimal DiscountPercentage => HasDiscount ? Math.Round((Price - DiscountPrice!.Value) / Price * 100, 2) : 0;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Category info
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }

        // Brand info
        public string? Brand { get; set; }

        // Additional images
        public List<string> Images { get; set; } = new();
    }

    public class ProductDetailDto : ProductDto
    {
        public List<ProductSpecificationDto> Specifications { get; set; } = new();
        public List<ReviewDto> Reviews { get; set; } = new();
        public CategoryDto? Category { get; set; }
    }

    public class ProductSpecificationDto
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }

    public class CreateProductDto
    {
        [Required(ErrorMessage = "SKU is required")]
        [StringLength(50, ErrorMessage = "SKU cannot exceed 50 characters")]
        public string SKU { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Discount price must be greater than 0")]
        [LessThan(nameof(Price), ErrorMessage = "Discount price must be less than regular price")]
        public decimal? DiscountPrice { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public int CategoryId { get; set; }

        public string? Brand { get; set; }

        [Url(ErrorMessage = "Invalid image URL")]
        public string? ImageUrl { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Weight must be non-negative")]
        public decimal? Weight { get; set; }

        public string? Dimensions { get; set; }

        public bool IsFeatured { get; set; } = false;

        public bool IsNew { get; set; } = false;

        [Required(ErrorMessage = "Stock is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock must be non-negative")]
        public int Stock { get; set; }

        public List<string> Images { get; set; } = new();

        public List<CreateProductSpecificationDto> Specifications { get; set; } = new();
    }

    public class UpdateProductDto
    {
        [Required(ErrorMessage = "SKU is required")]
        [StringLength(50, ErrorMessage = "SKU cannot exceed 50 characters")]
        public string SKU { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Discount price must be greater than 0")]
        [LessThan(nameof(Price), ErrorMessage = "Discount price must be less than regular price")]
        public decimal? DiscountPrice { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public int CategoryId { get; set; }

        public string? Brand { get; set; }

        [Url(ErrorMessage = "Invalid image URL")]
        public string? ImageUrl { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Weight must be non-negative")]
        public decimal? Weight { get; set; }

        public string? Dimensions { get; set; }

        public bool IsFeatured { get; set; }

        public bool IsNew { get; set; }

        [Required(ErrorMessage = "Stock is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock must be non-negative")]
        public int Stock { get; set; }

        public List<string> Images { get; set; } = new();

        public List<CreateProductSpecificationDto> Specifications { get; set; } = new();
    }

    public class CreateProductSpecificationDto
    {
        [Required(ErrorMessage = "Key is required")]
        [StringLength(100, ErrorMessage = "Key cannot exceed 100 characters")]
        public string Key { get; set; } = string.Empty;

        [Required(ErrorMessage = "Value is required")]
        [StringLength(500, ErrorMessage = "Value cannot exceed 500 characters")]
        public string Value { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;
    }

    // Category DTOs
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? Description { get; set; }
        public int? ParentCategoryId { get; set; }
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<CategoryDto> SubCategories { get; set; } = new();
        public int ProductCount { get; set; }
    }

    public class CategoryMenuDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public List<CategoryMenuDto> SubCategories { get; set; } = new();
    }

    public class CreateCategoryDto
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Icon cannot exceed 50 characters")]
        public string? Icon { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        public int? ParentCategoryId { get; set; }

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; } = 0;
    }

    public class UpdateCategoryDto
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Icon cannot exceed 50 characters")]
        public string? Icon { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        public int? ParentCategoryId { get; set; }

        public bool IsActive { get; set; }

        public int DisplayOrder { get; set; }
    }

    // Cart DTOs
    public class CartDto
    {
        public List<CartItemDto> Items { get; set; } = new();
        public decimal SubTotal { get; set; }
        public decimal Tax { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public int ItemCount => Items.Sum(i => i.Quantity);
    }

    public class CartItemDto
    {
        public int Id { get; set; }
        public ProductDto Product { get; set; } = new();
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime AddedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class AddToCartDto
    {
        [Required(ErrorMessage = "Product ID is required")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; } = 1;
    }

    public class UpdateCartItemDto
    {
        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }
    }

    // Order DTOs
    public class OrderDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public DateTime? ShippedDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
        public int ItemCount { get; set; }
    }

    public class OrderDetailDto : OrderDto
    {
        public List<OrderItemDto> OrderItems { get; set; } = new();
        public decimal SubTotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public string? ShippingAddress { get; set; }
        public string? PaymentMethod { get; set; }
        public string? TrackingNumber { get; set; }
        public string? Notes { get; set; }
    }

    public class OrderItemDto
    {
        public int Id { get; set; }
        public ProductDto Product { get; set; } = new();
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal DiscountAmount { get; set; }
    }

    public class CreateOrderDto
    {
        [Required(ErrorMessage = "Shipping address is required")]
        [StringLength(500, ErrorMessage = "Shipping address cannot exceed 500 characters")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Payment method is required")]
        [StringLength(50, ErrorMessage = "Payment method cannot exceed 50 characters")]
        public string PaymentMethod { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }

        public string? CouponCode { get; set; }
    }

    

    // Todo DTOs
    public class TodoDto
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public string Priority { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class CreateTodoDto
    {
        [Required(ErrorMessage = "Text is required")]
        [StringLength(500, ErrorMessage = "Text cannot exceed 500 characters")]
        public string Text { get; set; } = string.Empty;

        [Required(ErrorMessage = "Priority is required")]
        public string Priority { get; set; } = "Medium";

        [FutureDate(ErrorMessage = "Due date must be in the future")]
        public DateTime? DueDate { get; set; }
    }

    public class UpdateTodoDto
    {
        [Required(ErrorMessage = "Text is required")]
        [StringLength(500, ErrorMessage = "Text cannot exceed 500 characters")]
        public string Text { get; set; } = string.Empty;

        [Required(ErrorMessage = "Priority is required")]
        public string Priority { get; set; } = "Medium";

        [FutureDate(ErrorMessage = "Due date must be in the future")]
        public DateTime? DueDate { get; set; }

        public bool IsCompleted { get; set; }
    }

    // Search DTOs
    public class SearchResultDto
    {
        public List<ProductDto> Products { get; set; } = new();
        public List<CategoryDto> Categories { get; set; } = new();
        public int TotalResults { get; set; }
        public string Query { get; set; } = string.Empty;
        public TimeSpan SearchTime { get; set; }
    }

    public class ProductSearchFilterDto
    {
        public string? Query { get; set; }
        public int? CategoryId { get; set; }
        public int? BrandId { get; set; }           // ← CÓ BrandId vì Product có BrandId
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public double? MinRating { get; set; }      // ← double vì Rating trong Product là double
        public bool? InStock { get; set; }
        public bool? OnSale { get; set; }
        public string? SortBy { get; set; } = "name";
        public bool SortDescending { get; set; } = false;
    }

    public class SearchSuggestionDto
    {
        public string Text { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // product, category, brand
        public int Count { get; set; }
    }

    public class PopularSearchDto
    {
        public string Query { get; set; } = string.Empty;
        public int SearchCount { get; set; }
        public DateTime LastSearched { get; set; }
    }

    // Tracking DTOs
    public class TrackingInfoDto
    {
        public string OrderNumber { get; set; } = string.Empty;
        public string? TrackingNumber { get; set; }
        public string Status { get; set; } = string.Empty;
        public string EstimatedDelivery { get; set; } = string.Empty;
        public List<TrackingEventDto> Events { get; set; } = new();
    }

    public class TrackingEventDto
    {
        public string Status { get; set; } = string.Empty;
        public DateTime DateTime { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? Location { get; set; }
    }

    // Payment DTOs
    public class PaymentRequestDto
    {
        public string OrderNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "VND";
        public string PaymentMethod { get; set; } = string.Empty;
        public string? ReturnUrl { get; set; }
        public string? CancelUrl { get; set; }
    }

    public class PaymentResultDto
    {
        public bool Success { get; set; }
        public string? TransactionId { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
        public string? PaymentUrl { get; set; }
    }

    public class PaymentStatusDto
    {
        public string TransactionId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
    }

    // Additional DTOs for advanced features
    public class InventoryDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;  // ← THÊM PROPERTY NÀY
        public string ProductSKU { get; set; } = string.Empty;   // ← THÊM PROPERTY NÀY
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int MinStock { get; set; }
        public int MaxStock { get; set; }
        public DateTime LastUpdated { get; set; }

        // Additional useful properties
        public decimal? ProductCost { get; set; }
        public decimal? ProductPrice { get; set; }
        public decimal StockValue => (ProductCost ?? 0) * Quantity;
        public string StockStatus => Quantity <= MinStock ? "Low Stock" :
                                   Quantity >= MaxStock ? "Over Stock" : "Normal";
    }

    public class CreateInventoryDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        public int WarehouseId { get; set; }

        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }

        [Range(0, int.MaxValue)]
        public int MinStock { get; set; } = 10;

        [Range(0, int.MaxValue)]
        public int MaxStock { get; set; } = 1000;
    }

    public class UpdateInventoryDto
    {
        [Range(0, int.MaxValue)]
        public int? Quantity { get; set; }

        [Range(0, int.MaxValue)]
        public int? MinStock { get; set; }

        [Range(0, int.MaxValue)]
        public int? MaxStock { get; set; }
    }

    public class InventoryTransferDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        public int FromWarehouseId { get; set; }

        [Required]
        public int ToWarehouseId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        public string? Notes { get; set; }
    }

    public class InventoryAlertDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public int CurrentStock { get; set; }
        public int MinStock { get; set; }
        public string AlertType { get; set; } = string.Empty; // LOW_STOCK, OUT_OF_STOCK
    }

    public class BrandDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Logo { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ProductCount { get; set; }
    }

    public class CreateBrandDto
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Url(ErrorMessage = "Invalid logo URL")]
        public string? Logo { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class UpdateBrandDto
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Url(ErrorMessage = "Invalid logo URL")]
        public string? Logo { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        public bool IsActive { get; set; }
    }

    public class WarehouseDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Manager { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateWarehouseDto
    {
        [Required(ErrorMessage = "Code is required")]
        [StringLength(20, ErrorMessage = "Code cannot exceed 20 characters")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string? Address { get; set; }

        [Phone(ErrorMessage = "Invalid phone number")]
        public string? Phone { get; set; }

        [StringLength(100, ErrorMessage = "Manager name cannot exceed 100 characters")]
        public string? Manager { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class UpdateWarehouseDto
    {
        [Required(ErrorMessage = "Code is required")]
        [StringLength(20, ErrorMessage = "Code cannot exceed 20 characters")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string? Address { get; set; }

        [Phone(ErrorMessage = "Invalid phone number")]
        public string? Phone { get; set; }

        [StringLength(100, ErrorMessage = "Manager name cannot exceed 100 characters")]
        public string? Manager { get; set; }

        public bool IsActive { get; set; }
    }

    // Report DTOs
    public class DashboardStatsDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalProducts { get; set; }
        public int TotalCustomers { get; set; }
        public decimal AverageOrderValue { get; set; }
        public List<MonthlySalesDto> MonthlySales { get; set; } = new();
        public List<TopProductDto> TopProducts { get; set; } = new();
    }

    public class MonthlySalesDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class SalesReportDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public List<DailySalesDto> DailySales { get; set; } = new();
    }

    public class DailySalesDto
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class TopProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class CustomerReportDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime LastOrderDate { get; set; }
    }

    public class InventoryReportDto
    {
        public decimal TotalInventoryValue { get; set; }
        public int TotalProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public List<InventoryAlertDto> Alerts { get; set; } = new();
    }

    

    public class TodoStatsDto
    {
        public int TotalTodos { get; set; }
        public int CompletedTodos { get; set; }
        public int PendingTodos { get; set; }
        public int HighPriorityTodos { get; set; }
        public int MediumPriorityTodos { get; set; }
        public int LowPriorityTodos { get; set; }
        public int OverdueTodos { get; set; }
        public double CompletionRate { get; set; }
    }

}