using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WuanTech.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string SKU { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public int? BrandId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Cost { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? Weight { get; set; }

        [StringLength(100)]
        public string? Dimensions { get; set; }

        [Column(TypeName = "decimal(3,2)")]
        public double Rating { get; set; } = 0;

        public int ReviewCount { get; set; } = 0;

        public bool IsFeatured { get; set; } = false;

        public bool IsNew { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public int ViewCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Category Category { get; set; } = null!;
        public virtual Brand? Brand { get; set; }
        public virtual ICollection<ProductSpecification> Specifications { get; set; } = new List<ProductSpecification>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
        public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

        // Computed properties
        [NotMapped]
        public decimal FinalPrice => DiscountPrice ?? Price;

        [NotMapped]
        public bool HasDiscount => DiscountPrice.HasValue && DiscountPrice < Price;

        [NotMapped]
        public decimal DiscountPercentage => HasDiscount ? Math.Round((Price - DiscountPrice!.Value) / Price * 100, 2) : 0;

        [NotMapped]
        public int TotalStock => Inventories?.Sum(i => i.Quantity) ?? 0;

        [NotMapped]
        public bool InStock => TotalStock > 0;
    }

    public class ProductImage
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        [StringLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        public bool IsMain { get; set; } = false;

        public int DisplayOrder { get; set; } = 0;

        // Navigation properties
        public virtual Product Product { get; set; } = null!;
    }

    public class ProductSpecification
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        [StringLength(100)]
        public string SpecKey { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string SpecValue { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;

        // Navigation properties
        public virtual Product Product { get; set; } = null!;
    }

    public class Category
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Icon { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public int? ParentCategoryId { get; set; }

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Category? ParentCategory { get; set; }
        public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }

    public class Brand
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Logo { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }

    public class Warehouse
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? Manager { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    }

    public class Inventory
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int WarehouseId { get; set; }

        public int Quantity { get; set; } = 0;

        public int MinStock { get; set; } = 10;

        public int MaxStock { get; set; } = 1000;

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Product Product { get; set; } = null!;
        public virtual Warehouse Warehouse { get; set; } = null!;

        // Computed properties
        [NotMapped]
        public string StockStatus => Quantity <= MinStock ? "Low Stock" :
                                   Quantity >= MaxStock ? "Over Stock" : "Normal";
    }

    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string PasswordHash { get; set; } = string.Empty;

        [StringLength(100)]
        public string? FullName { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(500)]
        public string? Avatar { get; set; }

        [StringLength(20)]
        public string Role { get; set; } = "Customer";

        public bool IsActive { get; set; } = true;

        public DateTime? LastLogin { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Customer? Customer { get; set; }
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<Todo> Todos { get; set; } = new List<Todo>();
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }

    public class Customer
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(20)]
        public string CustomerCode { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; }

        public int Points { get; set; } = 0;

        [StringLength(20)]
        public string MembershipLevel { get; set; } = "Bronze";

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPurchased { get; set; } = 0;

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }

    public class CartItem
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int ProductId { get; set; }

        public int Quantity { get; set; } = 1;

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;

        // Computed properties
        [NotMapped]
        public decimal UnitPrice => Product?.FinalPrice ?? 0;

        [NotMapped]
        public decimal TotalPrice => UnitPrice * Quantity;
    }

    public class Order
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string OrderNumber { get; set; } = string.Empty;

        [Required]
        public int CustomerId { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [StringLength(20)]
        public string Status { get; set; } = "PENDING";

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingFee { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Tax { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [StringLength(50)]
        public string? PaymentMethod { get; set; }

        [StringLength(20)]
        public string PaymentStatus { get; set; } = "PENDING";

        [StringLength(500)]
        public string? ShippingAddress { get; set; }

        [StringLength(20)]
        public string? ShippingPhone { get; set; }

        [StringLength(100)]
        public string? TrackingNumber { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime? ShippedDate { get; set; }

        public DateTime? DeliveredDate { get; set; }

        public DateTime? CancelledDate { get; set; }

        [StringLength(500)]
        public string? CancelReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Customer Customer { get; set; } = null!;
        public virtual User User { get; set; } = null!;
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        // Computed properties
        [NotMapped]
        public int ItemCount => OrderItems?.Sum(oi => oi.Quantity) ?? 0;
    }

    public class OrderItem
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public int ProductId { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        // Navigation properties
        public virtual Order Order { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }

    public class Review
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int UserId { get; set; }

        public int? OrderId { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        public string? Comment { get; set; }

        public bool IsVerifiedPurchase { get; set; } = false;

        public bool IsApproved { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Product Product { get; set; } = null!;
        public virtual User User { get; set; } = null!;
        public virtual Order? Order { get; set; }

        // Additional properties for images (stored as JSON or separate table)
        [NotMapped]
        public List<string> Images { get; set; } = new();
    }

    public class Favorite
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int ProductId { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }

    public class Todo
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(500)]
        public string Text { get; set; } = string.Empty;

        public bool IsCompleted { get; set; } = false;

        [StringLength(20)]
        public string Priority { get; set; } = "Medium";

        public DateTime? DueDate { get; set; }

        public DateTime? CompletedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User User { get; set; } = null!;

        // Computed properties
        [NotMapped]
        public bool IsOverdue => DueDate.HasValue && DueDate < DateTime.UtcNow && !IsCompleted;
    }

    public class RefreshToken
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(500)]
        public string Token { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public bool IsRevoked { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string? CreatedByIp { get; set; }

        public DateTime? RevokedAt { get; set; }

        [StringLength(50)]
        public string? RevokedByIp { get; set; }

        [StringLength(500)]
        public string? ReplacedByToken { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;

        // Computed properties
        [NotMapped]
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

        [NotMapped]
        public bool IsActive => !IsRevoked && !IsExpired;
    }

    public class PasswordResetToken
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(500)]
        public string Token { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public bool IsUsed { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User User { get; set; } = null!;

        // Computed properties
        [NotMapped]
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

        [NotMapped]
        public bool IsValid => !IsUsed && !IsExpired;
    }

    public class Coupon
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [StringLength(20)]
        public string DiscountType { get; set; } = string.Empty; // PERCENTAGE, FIXED

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MinOrderAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxDiscountAmount { get; set; }

        public int? UsageLimit { get; set; }

        public int UsedCount { get; set; } = 0;

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Computed properties
        [NotMapped]
        public bool IsValid => IsActive &&
                              (!StartDate.HasValue || StartDate <= DateTime.UtcNow) &&
                              (!EndDate.HasValue || EndDate >= DateTime.UtcNow) &&
                              (!UsageLimit.HasValue || UsedCount < UsageLimit);
    }
    public class SearchQuery
    {
        public int Id { get; set; }

        [Required]
        [StringLength(500)]
        public string Query { get; set; } = string.Empty;

        public int? UserId { get; set; }
        public User? User { get; set; }

        public DateTime SearchDate { get; set; }
        public int ResultCount { get; set; }

        [StringLength(45)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }
    }
}