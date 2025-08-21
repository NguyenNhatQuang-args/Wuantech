using Microsoft.EntityFrameworkCore;
using WuanTech.Models;

namespace WuanTech.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets for your entities
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<ProductSpecification> ProductSpecifications { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<Todo> Todos { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<SearchQuery> SearchQueries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Product configuration
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.SKU).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.SKU).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Price).HasPrecision(18, 2).IsRequired();
                entity.Property(e => e.DiscountPrice).HasPrecision(18, 2);
                entity.Property(e => e.Cost).HasPrecision(18, 2);
                entity.Property(e => e.Rating).HasPrecision(3, 2);
                entity.Property(e => e.Weight).HasPrecision(10, 2);

                entity.HasOne(e => e.Category)
                    .WithMany(c => c.Products)
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Brand)
                    .WithMany(b => b.Products)
                    .HasForeignKey(e => e.BrandId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Indexes for performance
                entity.HasIndex(e => e.CategoryId);
                entity.HasIndex(e => e.BrandId);
                entity.HasIndex(e => new { e.Price, e.DiscountPrice });
                entity.HasIndex(e => e.IsFeatured);
                entity.HasIndex(e => e.IsNew);
                entity.HasIndex(e => e.IsActive);
            });

            // ProductImage configuration
            modelBuilder.Entity<ProductImage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ImageUrl).IsRequired().HasMaxLength(500);

                entity.HasOne(e => e.Product)
                    .WithMany(p => p.Images)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.ProductId);
                entity.HasIndex(e => new { e.ProductId, e.IsMain });
            });

            // ProductSpecification configuration
            modelBuilder.Entity<ProductSpecification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SpecKey).IsRequired().HasMaxLength(100);
                entity.Property(e => e.SpecValue).IsRequired().HasMaxLength(500);

                entity.HasOne(e => e.Product)
                    .WithMany(p => p.Specifications)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.ProductId);
            });

            // Category configuration
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Icon).HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(500);

                entity.HasOne(e => e.ParentCategory)
                    .WithMany(c => c.SubCategories)
                    .HasForeignKey(e => e.ParentCategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.ParentCategoryId);
                entity.HasIndex(e => e.IsActive);
            });

            // Brand configuration
            modelBuilder.Entity<Brand>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Logo).HasMaxLength(500);
                entity.Property(e => e.Description).HasMaxLength(500);

                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasIndex(e => e.IsActive);
            });

            // Warehouse configuration
            modelBuilder.Entity<Warehouse>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Address).HasMaxLength(500);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.Manager).HasMaxLength(100);

                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.IsActive);
            });

            // Inventory configuration
            modelBuilder.Entity<Inventory>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Product)
                    .WithMany(p => p.Inventories)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Warehouse)
                    .WithMany(w => w.Inventories)
                    .HasForeignKey(e => e.WarehouseId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => new { e.ProductId, e.WarehouseId }).IsUnique();
                entity.HasIndex(e => e.Quantity);
            });

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Username).IsUnique();
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(500);
                entity.Property(e => e.FullName).HasMaxLength(100);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.Address).HasMaxLength(500);
                entity.Property(e => e.Avatar).HasMaxLength(500);
                entity.Property(e => e.Role).HasDefaultValue("Customer").HasMaxLength(20);
                entity.Property(e => e.IsActive).HasDefaultValue(true);

                entity.HasIndex(e => e.Role);
                entity.HasIndex(e => e.IsActive);
            });

            // Customer configuration
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CustomerCode).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Gender).HasMaxLength(10);
                entity.Property(e => e.MembershipLevel).HasDefaultValue("Bronze").HasMaxLength(20);
                entity.Property(e => e.TotalPurchased).HasPrecision(18, 2).HasDefaultValue(0);

                entity.HasOne(e => e.User)
                    .WithOne(u => u.Customer)
                    .HasForeignKey<Customer>(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.UserId).IsUnique();
                entity.HasIndex(e => e.CustomerCode).IsUnique();
            });

            // Order configuration
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.OrderDate).IsRequired();
                entity.Property(e => e.Status).HasDefaultValue("PENDING").HasMaxLength(20);
                entity.Property(e => e.SubTotal).HasPrecision(18, 2).IsRequired();
                entity.Property(e => e.ShippingFee).HasPrecision(18, 2).HasDefaultValue(0);
                entity.Property(e => e.Discount).HasPrecision(18, 2).HasDefaultValue(0);
                entity.Property(e => e.Tax).HasPrecision(18, 2).HasDefaultValue(0);
                entity.Property(e => e.TotalAmount).HasPrecision(18, 2).IsRequired();
                entity.Property(e => e.PaymentMethod).HasMaxLength(50);
                entity.Property(e => e.PaymentStatus).HasDefaultValue("PENDING").HasMaxLength(20);
                entity.Property(e => e.ShippingAddress).HasMaxLength(500);
                entity.Property(e => e.ShippingPhone).HasMaxLength(20);
                entity.Property(e => e.TrackingNumber).HasMaxLength(100);
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.Property(e => e.CancelReason).HasMaxLength(500);

                entity.HasOne(e => e.Customer)
                    .WithMany(c => c.Orders)
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Add relation to User through Customer
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Orders)
                    .HasForeignKey("UserId") // Shadow property
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.OrderNumber).IsUnique();
                entity.HasIndex(e => e.CustomerId);
                entity.HasIndex(e => e.OrderDate);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.PaymentStatus);
            });

            // OrderItem configuration
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UnitPrice).HasPrecision(18, 2).IsRequired();
                entity.Property(e => e.DiscountAmount).HasPrecision(18, 2).HasDefaultValue(0);
                entity.Property(e => e.TotalPrice).HasPrecision(18, 2).IsRequired();

                entity.HasOne(e => e.Order)
                    .WithMany(o => o.OrderItems)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                    .WithMany(p => p.OrderItems)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.OrderId);
                entity.HasIndex(e => e.ProductId);
            });

            // CartItem configuration
            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.ProductId }).IsUnique();

                entity.HasOne(e => e.User)
                    .WithMany(u => u.CartItems)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                    .WithMany(p => p.CartItems)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ProductId);
            });

            // Review configuration
            modelBuilder.Entity<Review>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Rating).IsRequired();
                entity.Property(e => e.Comment).HasMaxLength(2000);

                entity.HasOne(e => e.Product)
                    .WithMany(p => p.Reviews)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Reviews)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Order)
                    .WithMany()
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.ProductId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Rating);
                entity.HasIndex(e => e.IsApproved);
            });

            // Favorite configuration
            modelBuilder.Entity<Favorite>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.UserId, e.ProductId }).IsUnique();

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Favorites)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                    .WithMany(p => p.Favorites)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ProductId);
            });

            // Todo configuration
            modelBuilder.Entity<Todo>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Text).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Priority).HasDefaultValue("Medium").HasMaxLength(20);
                entity.Property(e => e.IsCompleted).HasDefaultValue(false);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Todos)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.IsCompleted);
                entity.HasIndex(e => e.Priority);
                entity.HasIndex(e => e.DueDate);
            });

            // RefreshToken configuration
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Token).IsUnique();
                entity.Property(e => e.Token).IsRequired().HasMaxLength(500);
                entity.Property(e => e.CreatedByIp).HasMaxLength(50);
                entity.Property(e => e.RevokedByIp).HasMaxLength(50);
                entity.Property(e => e.ReplacedByToken).HasMaxLength(500);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.RefreshTokens)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ExpiresAt);
                entity.HasIndex(e => e.IsRevoked);
            });

            // PasswordResetToken configuration
            modelBuilder.Entity<PasswordResetToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Token).IsUnique();
                entity.Property(e => e.Token).IsRequired().HasMaxLength(500);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ExpiresAt);
                entity.HasIndex(e => e.IsUsed);
            });

            // Coupon configuration
            modelBuilder.Entity<Coupon>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.DiscountType).IsRequired().HasMaxLength(20);
                entity.Property(e => e.DiscountValue).HasPrecision(18, 2).IsRequired();
                entity.Property(e => e.MinOrderAmount).HasPrecision(18, 2).HasDefaultValue(0);
                entity.Property(e => e.MaxDiscountAmount).HasPrecision(18, 2);

                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => new { e.StartDate, e.EndDate });
            });

            modelBuilder.Entity<SearchQuery>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Query).IsRequired().HasMaxLength(500);
                entity.Property(e => e.IpAddress).HasMaxLength(45);
                entity.Property(e => e.UserAgent).HasMaxLength(500);

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.Query);
                entity.HasIndex(e => e.SearchDate);
                entity.HasIndex(e => e.UserId);
            });

            // Configure check constraints
            modelBuilder.Entity<Review>()
                .HasCheckConstraint("CK_Review_Rating", "Rating >= 1 AND Rating <= 5");

            modelBuilder.Entity<Product>()
                .HasCheckConstraint("CK_Product_Price", "Price > 0");

            modelBuilder.Entity<Product>()
                .HasCheckConstraint("CK_Product_DiscountPrice", "DiscountPrice IS NULL OR DiscountPrice >= 0");

            modelBuilder.Entity<CartItem>()
                .HasCheckConstraint("CK_CartItem_Quantity", "Quantity > 0");

            modelBuilder.Entity<OrderItem>()
                .HasCheckConstraint("CK_OrderItem_Quantity", "Quantity > 0");

            modelBuilder.Entity<Inventory>()
                .HasCheckConstraint("CK_Inventory_Quantity", "Quantity >= 0");
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Auto-update timestamps
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                if (entry.Entity is Product product)
                {
                    if (entry.State == EntityState.Added)
                        product.CreatedAt = DateTime.UtcNow;
                    product.UpdatedAt = DateTime.UtcNow;
                }
                else if (entry.Entity is Category category)
                {
                    if (entry.State == EntityState.Added)
                        category.CreatedAt = DateTime.UtcNow;
                    category.UpdatedAt = DateTime.UtcNow;
                }
                else if (entry.Entity is Order order)
                {
                    if (entry.State == EntityState.Added)
                        order.CreatedAt = DateTime.UtcNow;
                    order.UpdatedAt = DateTime.UtcNow;
                }
                else if (entry.Entity is CartItem cartItem && entry.State == EntityState.Modified)
                {
                    cartItem.UpdatedAt = DateTime.UtcNow;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}