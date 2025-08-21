using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using WuanTech.API.Data;
using WuanTech.API.Middleware;
using WuanTech.API.Services.Interfaces;
using WuanTech.API.Services.Implementations;
using WuanTech.API.Helpers;
using WuanTech.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WuanTech E-Commerce API",
        Version = "v1",
        Description = "API for WuanTech Technology Store"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Database configuration with retry policy
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
            sqlOptions.CommandTimeout(120);
        });

    // Enable sensitive data logging in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// JWT authentication configuration
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException("JWT Key is not configured");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

// Add Authorization
builder.Services.AddAuthorization();

// Register services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITodoService, TodoService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IBrandService, BrandService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<ICouponService, CouponService>();
builder.Services.AddScoped<IReportService, ReportService>();
// Add helper services
builder.Services.AddScoped<JwtHelper>();

// Add HttpClient for external services
builder.Services.AddHttpClient<IPaymentService, PaymentService>();

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5500",   // Live Server HTTP
                "https://localhost:5500",  // Live Server HTTPS 
                "http://127.0.0.1:5500",   // Alternative localhost
                "https://127.0.0.1:5500",  // Alternative localhost HTTPS
                "http://localhost:3000",   // React/Vue dev server
                "https://localhost:3000"   // React/Vue HTTPS
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials(); // Important for authentication
    });
});

// Cache configuration
builder.Services.AddMemoryCache();
builder.Services.AddResponseCaching();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "WuanTech E-Commerce API v1");
        c.RoutePrefix = string.Empty; // Swagger at root URL
    });

    // Allow HTTP in development
    app.UseHttpsRedirection();
}
else
{
    app.UseHttpsRedirection();
    app.UseHsts(); // Add HSTS for production
}

// IMPORTANT: Order matters for middleware pipeline
app.UseCors("AllowAll"); // CORS must be before Auth

app.UseAuthentication();
app.UseAuthorization();
app.UseResponseCaching();

// Custom middleware (should be after auth)
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.MapControllers();

// ========================================
// AUTO-MIGRATE DATABASE & SEED DATA
// ========================================
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("🔄 Starting database migration and seeding...");

            // Ensure database is created
            context.Database.EnsureCreated();

            // Seed initial data
            await SeedData(context, logger);

            logger.LogInformation("✅ Database migration and seeding completed successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ An error occurred while migrating/seeding the database.");
        }
    }
}

// Configure to listen on both HTTP and HTTPS in development
if (app.Environment.IsDevelopment())
{
    app.Urls.Add("https://localhost:7102");
    app.Urls.Add("http://localhost:5275");
}

app.Run();

// ========================================
// DATA SEEDING METHOD
// ========================================
static async Task SeedData(ApplicationDbContext context, ILogger logger)
{
    try
    {
        logger.LogInformation("🌱 Starting data seeding process...");

        // ========================================
        // 1. SEED USERS & CUSTOMERS
        // ========================================
        if (!context.Users.Any())
        {
            logger.LogInformation("👥 Seeding users...");

            var users = new[]
            {
                new User
                {
                    Username = "admin",
                    Email = "admin@wuantech.com",
                    PasswordHash = "$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/NF/rZ5DYkKlPDZKKO", // admin123
                    FullName = "Administrator",
                    PhoneNumber = "0901234567",
                    Role = "Admin",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "staff1",
                    Email = "staff@wuantech.com",
                    PasswordHash = "$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/NF/rZ5DYkKlPDZKKO", // staff123
                    FullName = "Nhân viên 1",
                    PhoneNumber = "0901234568",
                    Role = "Staff",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "customer1",
                    Email = "customer@gmail.com",
                    PasswordHash = "$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/NF/rZ5DYkKlPDZKKO", // customer123
                    FullName = "Khách hàng 1",
                    PhoneNumber = "0901234569",
                    Role = "Customer",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            context.Users.AddRange(users);
            await context.SaveChangesAsync();

            // Create customer record for customer user
            var customerUser = users.First(u => u.Role == "Customer");
            var customer = new Customer
            {
                UserId = customerUser.Id,
                CustomerCode = $"KH{DateTime.UtcNow:yyyyMMdd}001",
                Points = 0,
                MembershipLevel = "Bronze",
                TotalPurchased = 0
            };

            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            logger.LogInformation($"✅ Created {users.Length} users and 1 customer");
        }

        // ========================================
        // 2. SEED BRANDS
        // ========================================
        if (!context.Brands.Any())
        {
            logger.LogInformation("🏷️ Seeding brands...");

            var brands = new[]
            {
                new Brand { Name = "Apple", Description = "Thương hiệu công nghệ hàng đầu thế giới", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Brand { Name = "Samsung", Description = "Tập đoàn công nghệ đa quốc gia Hàn Quốc", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Brand { Name = "ASUS", Description = "Thương hiệu máy tính và linh kiện hàng đầu", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Brand { Name = "Dell", Description = "Thương hiệu máy tính và server nổi tiếng", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Brand { Name = "HP", Description = "Hewlett-Packard - Thương hiệu máy tính lâu đời", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Brand { Name = "Logitech", Description = "Thương hiệu phụ kiện máy tính hàng đầu", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Brand { Name = "Sony", Description = "Tập đoàn giải trí và công nghệ Nhật Bản", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Brand { Name = "LG", Description = "Thương hiệu điện tử tiêu dùng Hàn Quốc", IsActive = true, CreatedAt = DateTime.UtcNow }
            };

            context.Brands.AddRange(brands);
            await context.SaveChangesAsync();

            logger.LogInformation($"✅ Created {brands.Length} brands");
        }

        // ========================================
        // 3. SEED CATEGORIES
        // ========================================
        if (!context.Categories.Any())
        {
            logger.LogInformation("📂 Seeding categories...");

            // Main categories
            var mainCategories = new[]
            {
                new Category { Name = "Điện thoại", Icon = "smartphone", Description = "Điện thoại thông minh các loại", DisplayOrder = 1, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Category { Name = "Laptop", Icon = "laptop", Description = "Máy tính xách tay", DisplayOrder = 2, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Category { Name = "PC & Linh kiện", Icon = "desktop", Description = "Máy tính để bàn và linh kiện", DisplayOrder = 3, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Category { Name = "Phụ kiện", Icon = "accessories", Description = "Phụ kiện điện tử", DisplayOrder = 4, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Category { Name = "Âm thanh", Icon = "headphones", Description = "Thiết bị âm thanh", DisplayOrder = 5, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Category { Name = "Gaming", Icon = "gamepad", Description = "Thiết bị gaming", DisplayOrder = 6, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            };

            context.Categories.AddRange(mainCategories);
            await context.SaveChangesAsync();

            // Subcategories
            var phoneCategory = context.Categories.First(c => c.Name == "Điện thoại");
            var laptopCategory = context.Categories.First(c => c.Name == "Laptop");
            var pcCategory = context.Categories.First(c => c.Name == "PC & Linh kiện");

            var subCategories = new[]
            {
                // Phone subcategories
                new Category { Name = "iPhone", ParentCategoryId = phoneCategory.Id, DisplayOrder = 1, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Category { Name = "Samsung Galaxy", ParentCategoryId = phoneCategory.Id, DisplayOrder = 2, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                
                // Laptop subcategories
                new Category { Name = "Laptop Gaming", ParentCategoryId = laptopCategory.Id, DisplayOrder = 1, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Category { Name = "Laptop Văn phòng", ParentCategoryId = laptopCategory.Id, DisplayOrder = 2, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                
                // PC subcategories
                new Category { Name = "CPU", ParentCategoryId = pcCategory.Id, DisplayOrder = 1, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Category { Name = "VGA", ParentCategoryId = pcCategory.Id, DisplayOrder = 2, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Category { Name = "RAM", ParentCategoryId = pcCategory.Id, DisplayOrder = 3, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Category { Name = "Ổ cứng", ParentCategoryId = pcCategory.Id, DisplayOrder = 4, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            };

            context.Categories.AddRange(subCategories);
            await context.SaveChangesAsync();

            logger.LogInformation($"✅ Created {mainCategories.Length + subCategories.Length} categories");
        }

        // ========================================
        // 4. SEED WAREHOUSES
        // ========================================
        if (!context.Warehouses.Any())
        {
            logger.LogInformation("🏭 Seeding warehouses...");

            var warehouses = new[]
            {
                new Warehouse { Code = "WH001", Name = "Kho Hồ Chí Minh", Address = "123 Nguyễn Văn Cừ, Q.5, TP.HCM", Phone = "0283123456", Manager = "Nguyễn Văn A", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Warehouse { Code = "WH002", Name = "Kho Hà Nội", Address = "456 Giải Phóng, Hai Bà Trưng, Hà Nội", Phone = "0243123456", Manager = "Trần Thị B", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Warehouse { Code = "WH003", Name = "Kho Đà Nẵng", Address = "789 Nguyễn Văn Linh, Hải Châu, Đà Nẵng", Phone = "0233123456", Manager = "Lê Văn C", IsActive = true, CreatedAt = DateTime.UtcNow }
            };

            context.Warehouses.AddRange(warehouses);
            await context.SaveChangesAsync();

            logger.LogInformation($"✅ Created {warehouses.Length} warehouses");
        }

        // ========================================
        // 5. SEED PRODUCTS
        // ========================================
        if (!context.Products.Any())
        {
            logger.LogInformation("📱 Seeding products...");

            // Get references
            var appleBrand = context.Brands.First(b => b.Name == "Apple");
            var samsungBrand = context.Brands.First(b => b.Name == "Samsung");
            var asusBrand = context.Brands.First(b => b.Name == "ASUS");
            var dellBrand = context.Brands.First(b => b.Name == "Dell");
            var logitechBrand = context.Brands.First(b => b.Name == "Logitech");

            var iPhoneCategory = context.Categories.First(c => c.Name == "iPhone");
            var galaxyCategory = context.Categories.First(c => c.Name == "Samsung Galaxy");
            var gamingLaptopCategory = context.Categories.First(c => c.Name == "Laptop Gaming");
            var officeLaptopCategory = context.Categories.First(c => c.Name == "Laptop Văn phòng");
            var accessoryCategory = context.Categories.First(c => c.Name == "Phụ kiện");

            var products = new[]
            {
                // iPhone Products
                new Product
                {
                    SKU = "IP15PM256",
                    Name = "iPhone 15 Pro Max 256GB",
                    Description = "iPhone 15 Pro Max với chip A17 Pro mạnh mẽ, camera 48MP, màn hình 6.7 inch Super Retina XDR",
                    CategoryId = iPhoneCategory.Id,
                    BrandId = appleBrand.Id,
                    Price = 34990000,
                    DiscountPrice = 33490000,
                    ImageUrl = "https://images.unsplash.com/photo-1592750475338-74b7b21085ab?w=400",
                    Weight = 0.221m,
                    Dimensions = "159.9 x 76.7 x 8.25 mm",
                    Rating = 4.8,
                    ReviewCount = 0,
                    IsFeatured = true,
                    IsNew = true,
                    IsActive = true,
                    ViewCount = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Product
                {
                    SKU = "IP15-128",
                    Name = "iPhone 15 128GB",
                    Description = "iPhone 15 với chip A16 Bionic, camera 48MP, Dynamic Island",
                    CategoryId = iPhoneCategory.Id,
                    BrandId = appleBrand.Id,
                    Price = 22990000,
                    DiscountPrice = 21990000,
                    ImageUrl = "https://images.unsplash.com/photo-1556656793-08538906a9f8?w=400",
                    Weight = 0.171m,
                    Dimensions = "147.6 x 71.6 x 7.8 mm",
                    Rating = 4.7,
                    ReviewCount = 0,
                    IsFeatured = true,
                    IsNew = true,
                    IsActive = true,
                    ViewCount = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                
                // Samsung Products
                new Product
                {
                    SKU = "SS-S24U-512",
                    Name = "Samsung Galaxy S24 Ultra 512GB",
                    Description = "Galaxy S24 Ultra với S Pen tích hợp, camera 200MP, AI photography",
                    CategoryId = galaxyCategory.Id,
                    BrandId = samsungBrand.Id,
                    Price = 31990000,
                    DiscountPrice = 29990000,
                    ImageUrl = "https://images.unsplash.com/photo-1610945265064-0e34e5519bbf?w=400",
                    Weight = 0.232m,
                    Dimensions = "162.3 x 79.0 x 8.6 mm",
                    Rating = 4.6,
                    ReviewCount = 0,
                    IsFeatured = true,
                    IsNew = true,
                    IsActive = true,
                    ViewCount = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                
                // Laptop Products
                new Product
                {
                    SKU = "ASUS-ROG-G16",
                    Name = "ASUS ROG Strix G16 Gaming Laptop",
                    Description = "Gaming laptop mạnh mẽ với RTX 4060, Intel i7-13650HX, 16GB RAM",
                    CategoryId = gamingLaptopCategory.Id,
                    BrandId = asusBrand.Id,
                    Price = 28990000,
                    DiscountPrice = 26990000,
                    ImageUrl = "https://images.unsplash.com/photo-1603302576837-37561b2e2302?w=400",
                    Weight = 2.5m,
                    Dimensions = "354.9 x 259.9 x 22.9 mm",
                    Rating = 4.5,
                    ReviewCount = 0,
                    IsFeatured = true,
                    IsNew = false,
                    IsActive = true,
                    ViewCount = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Product
                {
                    SKU = "MBA-M3-256",
                    Name = "MacBook Air M3 256GB",
                    Description = "MacBook Air siêu mỏng nhẹ với chip M3, hiệu năng vượt trội, pin cả ngày",
                    CategoryId = officeLaptopCategory.Id,
                    BrandId = appleBrand.Id,
                    Price = 27990000,
                    DiscountPrice = null,
                    ImageUrl = "https://images.unsplash.com/photo-1541807084-5c52b6b3adef?w=400",
                    Weight = 1.24m,
                    Dimensions = "304.1 x 215 x 11.3 mm",
                    Rating = 4.9,
                    ReviewCount = 0,
                    IsFeatured = true,
                    IsNew = true,
                    IsActive = true,
                    ViewCount = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                
                // Accessories
                new Product
                {
                    SKU = "APP-2ND-GEN",
                    Name = "AirPods Pro 2nd Generation",
                    Description = "Tai nghe không dây cao cấp với chống ồn chủ động, Spatial Audio",
                    CategoryId = accessoryCategory.Id,
                    BrandId = appleBrand.Id,
                    Price = 6990000,
                    DiscountPrice = 6490000,
                    ImageUrl = "https://images.unsplash.com/photo-1606220945770-b5b6c2c55bf1?w=400",
                    Weight = 0.056m,
                    Dimensions = "45.2 x 60.9 x 21.7 mm",
                    Rating = 4.4,
                    ReviewCount = 0,
                    IsFeatured = false,
                    IsNew = false,
                    IsActive = true,
                    ViewCount = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Product
                {
                    SKU = "LG-GPRO-X",
                    Name = "Logitech G Pro X Gaming Headset",
                    Description = "Tai nghe gaming chuyên nghiệp với Blue VO!CE filters",
                    CategoryId = accessoryCategory.Id,
                    BrandId = logitechBrand.Id,
                    Price = 3290000,
                    DiscountPrice = 2990000,
                    ImageUrl = "https://images.unsplash.com/photo-1583394838336-acd977736f90?w=400",
                    Weight = 0.32m,
                    Dimensions = "191 x 97 x 182 mm",
                    Rating = 4.3,
                    ReviewCount = 0,
                    IsFeatured = false,
                    IsNew = false,
                    IsActive = true,
                    ViewCount = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            context.Products.AddRange(products);
            await context.SaveChangesAsync();

            logger.LogInformation($"✅ Created {products.Length} products");

            // ========================================
            // 6. SEED PRODUCT IMAGES
            // ========================================
            logger.LogInformation("🖼️ Seeding product images...");

            var productImages = new List<ProductImage>();

            foreach (var product in products)
            {
                productImages.AddRange(new[]
                {
                    new ProductImage { ProductId = product.Id, ImageUrl = product.ImageUrl, IsMain = true, DisplayOrder = 0 },
                    new ProductImage { ProductId = product.Id, ImageUrl = "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=400", IsMain = false, DisplayOrder = 1 },
                    new ProductImage { ProductId = product.Id, ImageUrl = "https://images.unsplash.com/photo-1510557880182-3d4d3cba35a5?w=400", IsMain = false, DisplayOrder = 2 }
                });
            }

            context.ProductImages.AddRange(productImages);
            await context.SaveChangesAsync();

            // ========================================
            // 7. SEED PRODUCT SPECIFICATIONS
            // ========================================
            logger.LogInformation("⚙️ Seeding product specifications...");

            var specifications = new List<ProductSpecification>();

            // iPhone 15 Pro Max specs
            var iPhone15ProMax = products.First(p => p.SKU == "IP15PM256");
            specifications.AddRange(new[]
            {
                new ProductSpecification { ProductId = iPhone15ProMax.Id, SpecKey = "Màn hình", SpecValue = "6.7\" Super Retina XDR OLED", DisplayOrder = 1 },
                new ProductSpecification { ProductId = iPhone15ProMax.Id, SpecKey = "Chip xử lý", SpecValue = "A17 Pro", DisplayOrder = 2 },
                new ProductSpecification { ProductId = iPhone15ProMax.Id, SpecKey = "RAM", SpecValue = "8GB", DisplayOrder = 3 },
                new ProductSpecification { ProductId = iPhone15ProMax.Id, SpecKey = "Bộ nhớ trong", SpecValue = "256GB", DisplayOrder = 4 },
                new ProductSpecification { ProductId = iPhone15ProMax.Id, SpecKey = "Camera chính", SpecValue = "48MP + 12MP + 12MP", DisplayOrder = 5 },
                new ProductSpecification { ProductId = iPhone15ProMax.Id, SpecKey = "Pin", SpecValue = "4441mAh", DisplayOrder = 6 },
                new ProductSpecification { ProductId = iPhone15ProMax.Id, SpecKey = "Hệ điều hành", SpecValue = "iOS 17", DisplayOrder = 7 }
            });

            // Galaxy S24 Ultra specs
            var galaxyS24Ultra = products.First(p => p.SKU == "SS-S24U-512");
            specifications.AddRange(new[]
            {
                new ProductSpecification { ProductId = galaxyS24Ultra.Id, SpecKey = "Màn hình", SpecValue = "6.8\" Dynamic AMOLED 2X", DisplayOrder = 1 },
                new ProductSpecification { ProductId = galaxyS24Ultra.Id, SpecKey = "Chip xử lý", SpecValue = "Snapdragon 8 Gen 3", DisplayOrder = 2 },
                new ProductSpecification { ProductId = galaxyS24Ultra.Id, SpecKey = "RAM", SpecValue = "12GB", DisplayOrder = 3 },
                new ProductSpecification { ProductId = galaxyS24Ultra.Id, SpecKey = "Bộ nhớ trong", SpecValue = "512GB", DisplayOrder = 4 },
                new ProductSpecification { ProductId = galaxyS24Ultra.Id, SpecKey = "Camera chính", SpecValue = "200MP + 50MP + 12MP + 10MP", DisplayOrder = 5 },
                new ProductSpecification { ProductId = galaxyS24Ultra.Id, SpecKey = "Pin", SpecValue = "5000mAh", DisplayOrder = 6 },
                new ProductSpecification { ProductId = galaxyS24Ultra.Id, SpecKey = "Hệ điều hành", SpecValue = "Android 14, One UI 6.1", DisplayOrder = 7 },
                new ProductSpecification { ProductId = galaxyS24Ultra.Id, SpecKey = "S Pen", SpecValue = "Có", DisplayOrder = 8 }
            });

            // ASUS ROG G16 specs
            var asusROG = products.First(p => p.SKU == "ASUS-ROG-G16");
            specifications.AddRange(new[]
            {
                new ProductSpecification { ProductId = asusROG.Id, SpecKey = "Màn hình", SpecValue = "16\" QHD+ 165Hz", DisplayOrder = 1 },
                new ProductSpecification { ProductId = asusROG.Id, SpecKey = "CPU", SpecValue = "Intel Core i7-13650HX", DisplayOrder = 2 },
                new ProductSpecification { ProductId = asusROG.Id, SpecKey = "RAM", SpecValue = "16GB DDR5", DisplayOrder = 3 },
                new ProductSpecification { ProductId = asusROG.Id, SpecKey = "GPU", SpecValue = "NVIDIA RTX 4060 8GB", DisplayOrder = 4 },
                new ProductSpecification { ProductId = asusROG.Id, SpecKey = "Ổ cứng", SpecValue = "512GB PCIe SSD", DisplayOrder = 5 },
                new ProductSpecification { ProductId = asusROG.Id, SpecKey = "Hệ điều hành", SpecValue = "Windows 11 Home", DisplayOrder = 6 },
                new ProductSpecification { ProductId = asusROG.Id, SpecKey = "Bàn phím", SpecValue = "RGB Per-key", DisplayOrder = 7 }
            });

            // MacBook Air M3 specs
            var macBookAir = products.First(p => p.SKU == "MBA-M3-256");
            specifications.AddRange(new[]
            {
                new ProductSpecification { ProductId = macBookAir.Id, SpecKey = "Chip", SpecValue = "Apple M3", DisplayOrder = 1 },
                new ProductSpecification { ProductId = macBookAir.Id, SpecKey = "RAM", SpecValue = "8GB Unified Memory", DisplayOrder = 2 },
                new ProductSpecification { ProductId = macBookAir.Id, SpecKey = "Ổ cứng", SpecValue = "256GB SSD", DisplayOrder = 3 },
                new ProductSpecification { ProductId = macBookAir.Id, SpecKey = "Màn hình", SpecValue = "13.6\" Liquid Retina", DisplayOrder = 4 },
                new ProductSpecification { ProductId = macBookAir.Id, SpecKey = "Pin", SpecValue = "Lên đến 18 giờ", DisplayOrder = 5 }
            });

            context.ProductSpecifications.AddRange(specifications);
            await context.SaveChangesAsync();

            // ========================================
            // 8. SEED INVENTORIES
            // ========================================
            logger.LogInformation("📦 Seeding inventories...");

            var warehouses = context.Warehouses.ToList();
            var inventories = new List<Inventory>();

            foreach (var product in products)
            {
                foreach (var warehouse in warehouses)
                {
                    var quantity = product.SKU switch
                    {
                        "IP15PM256" => warehouse.Code == "WH001" ? 50 : warehouse.Code == "WH002" ? 30 : 20,
                        "IP15-128" => warehouse.Code == "WH001" ? 80 : warehouse.Code == "WH002" ? 60 : 0,
                        "SS-S24U-512" => warehouse.Code == "WH001" ? 40 : warehouse.Code == "WH002" ? 25 : 0,
                        "ASUS-ROG-G16" => warehouse.Code == "WH001" ? 25 : warehouse.Code == "WH002" ? 15 : 0,
                        "MBA-M3-256" => warehouse.Code == "WH001" ? 35 : warehouse.Code == "WH002" ? 20 : 0,
                        "APP-2ND-GEN" => warehouse.Code == "WH001" ? 100 : warehouse.Code == "WH002" ? 80 : 50,
                        "LG-GPRO-X" => warehouse.Code == "WH001" ? 45 : warehouse.Code == "WH002" ? 30 : 0,
                        _ => 0
                    };

                    if (quantity > 0)
                    {
                        inventories.Add(new Inventory
                        {
                            ProductId = product.Id,
                            WarehouseId = warehouse.Id,
                            Quantity = quantity,
                            MinStock = quantity < 50 ? 5 : 10,
                            MaxStock = quantity * 4,
                            LastUpdated = DateTime.UtcNow
                        });
                    }
                }
            }

            context.Inventories.AddRange(inventories);
            await context.SaveChangesAsync();

            logger.LogInformation($"✅ Created {inventories.Count} inventory records");
        }

        // ========================================
        // 9. SEED COUPONS
        // ========================================
        if (!context.Coupons.Any())
        {
            logger.LogInformation("🎟️ Seeding coupons...");

            var coupons = new[]
            {
                new Coupon
                {
                    Code = "WELCOME10",
                    Description = "Giảm 10% cho khách hàng mới",
                    DiscountType = "PERCENTAGE",
                    DiscountValue = 10,
                    MinOrderAmount = 1000000,
                    MaxDiscountAmount = 500000,
                    UsageLimit = 100,
                    UsedCount = 0,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddMonths(3),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Coupon
                {
                    Code = "NEWBIE500",
                    Description = "Giảm 500K cho đơn hàng đầu tiên",
                    DiscountType = "FIXED",
                    DiscountValue = 500000,
                    MinOrderAmount = 5000000,
                    MaxDiscountAmount = null,
                    UsageLimit = 50,
                    UsedCount = 0,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddMonths(1),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Coupon
                {
                    Code = "SUMMER2024",
                    Description = "Khuyến mãi hè 2024",
                    DiscountType = "PERCENTAGE",
                    DiscountValue = 15,
                    MinOrderAmount = 2000000,
                    MaxDiscountAmount = 1000000,
                    UsageLimit = 200,
                    UsedCount = 0,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddMonths(2),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Coupon
                {
                    Code = "FREESHIP",
                    Description = "Miễn phí vận chuyển",
                    DiscountType = "FIXED",
                    DiscountValue = 30000,
                    MinOrderAmount = 500000,
                    MaxDiscountAmount = null,
                    UsageLimit = 1000,
                    UsedCount = 0,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddMonths(6),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            context.Coupons.AddRange(coupons);
            await context.SaveChangesAsync();

            logger.LogInformation($"✅ Created {coupons.Length} coupons");
        }

        // ========================================
        // 10. FINAL STATISTICS
        // ========================================
        var stats = new
        {
            Users = context.Users.Count(),
            Customers = context.Customers.Count(),
            Brands = context.Brands.Count(),
            Categories = context.Categories.Count(),
            Warehouses = context.Warehouses.Count(),
            Products = context.Products.Count(),
            ProductImages = context.ProductImages.Count(),
            ProductSpecifications = context.ProductSpecifications.Count(),
            Inventories = context.Inventories.Count(),
            Coupons = context.Coupons.Count()
        };

        logger.LogInformation("📊 Final Statistics:");
        logger.LogInformation($"   👥 Users: {stats.Users}");
        logger.LogInformation($"   🛒 Customers: {stats.Customers}");
        logger.LogInformation($"   🏷️ Brands: {stats.Brands}");
        logger.LogInformation($"   📂 Categories: {stats.Categories}");
        logger.LogInformation($"   🏭 Warehouses: {stats.Warehouses}");
        logger.LogInformation($"   📱 Products: {stats.Products}");
        logger.LogInformation($"   🖼️ Product Images: {stats.ProductImages}");
        logger.LogInformation($"   ⚙️ Product Specs: {stats.ProductSpecifications}");
        logger.LogInformation($"   📦 Inventories: {stats.Inventories}");
        logger.LogInformation($"   🎟️ Coupons: {stats.Coupons}");

        logger.LogInformation("🎉 Data seeding completed successfully!");

        // Test query to verify products are ready for API
        var productCount = context.Products.Count(p => p.IsActive);
        var featuredCount = context.Products.Count(p => p.IsFeatured && p.IsActive);
        var newCount = context.Products.Count(p => p.IsNew && p.IsActive);
        var totalStock = context.Inventories.Sum(i => i.Quantity);

        logger.LogInformation("🔍 API Ready Check:");
        logger.LogInformation($"   ✅ Active Products: {productCount}");
        logger.LogInformation($"   ⭐ Featured Products: {featuredCount}");
        logger.LogInformation($"   🆕 New Products: {newCount}");
        logger.LogInformation($"   📦 Total Stock: {totalStock}");

        if (productCount > 0)
        {
            logger.LogInformation("🚀 API is ready! Products will be available at /api/products");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error during data seeding");
        throw;
    }
}