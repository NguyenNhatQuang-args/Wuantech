// Extensions/ServiceCollectionExtensions.cs
using Microsoft.EntityFrameworkCore;
using WuanTech.API.Data;
using WuanTech.API.Services.Interfaces;
using WuanTech.API.Services.Implementations;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WuanTech.API.DTOs;

namespace WuanTech.API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Core services
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<ICartService, CartService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITodoService, TodoService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<ISearchService, SearchService>();

            // Additional services
            services.AddScoped<IInventoryService, InventoryService>();
            services.AddScoped<IBrandService, BrandService>();
            services.AddScoped<IWarehouseService, WarehouseService>();
            services.AddScoped<IFavoriteService, FavoriteService>();
            services.AddScoped<IReviewService, ReviewService>();
            services.AddScoped<ICouponService, CouponService>();
            services.AddScoped<IReportService, ReportService>();

            return services;
        }

        public static IServiceCollection AddCustomDbContext(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                    sqlOptions.CommandTimeout(120);
                });

                // Development-specific configurations
                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                    options.LogTo(Console.WriteLine, LogLevel.Information);
                }
            });

            return services;
        }

        public static IServiceCollection AddCustomCors(this IServiceCollection services, IConfiguration configuration)
        {
            var allowedOrigins = configuration.GetSection("Security:Cors:AllowedOrigins").Get<string[]>()
                               ?? new[] { "http://localhost:3000", "https://localhost:3000" };

            services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigins", policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials()
                          .WithExposedHeaders("X-Pagination", "X-Total-Count");
                });

                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            return services;
        }
    }
}

