using Microsoft.EntityFrameworkCore;
using WuanTech.API.Data;
using WuanTech.API.DTOs;
using WuanTech.API.Services.Interfaces;
using WuanTech.Models;

namespace WuanTech.API.Services.Implementations
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReportService> _logger;

        public ReportService(ApplicationDbContext context, ILogger<ReportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync()
        {
            try
            {
                var totalRevenue = await _context.Orders
                    .Where(o => o.Status == "DELIVERED")
                    .SumAsync(o => o.TotalAmount);

                var totalOrders = await _context.Orders.CountAsync();
                var totalProducts = await _context.Products.CountAsync(p => p.IsActive);
                var totalCustomers = await _context.Users.CountAsync(u => u.Role == "Customer");

                var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

                var monthlySales = await _context.Orders
                    .Where(o => o.Status == "DELIVERED" && o.OrderDate >= DateTime.UtcNow.AddMonths(-12))
                    .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                    .Select(g => new MonthlySalesDto
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Revenue = g.Sum(o => o.TotalAmount),
                        OrderCount = g.Count()
                    })
                    .OrderBy(m => m.Year).ThenBy(m => m.Month)
                    .ToListAsync();

                var topProducts = await GetTopSellingProductsAsync(5);

                return new DashboardStatsDto
                {
                    TotalRevenue = totalRevenue,
                    TotalOrders = totalOrders,
                    TotalProducts = totalProducts,
                    TotalCustomers = totalCustomers,
                    AverageOrderValue = averageOrderValue,
                    MonthlySales = monthlySales.ToList(),
                    TopProducts = topProducts.ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats");
                throw;
            }
        }

        public async Task<SalesReportDto> GetSalesReportAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var orders = await _context.Orders
                    .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate && o.Status == "DELIVERED")
                    .ToListAsync();

                var totalRevenue = orders.Sum(o => o.TotalAmount);
                var totalOrders = orders.Count;
                var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

                var dailySales = orders
                    .GroupBy(o => o.OrderDate.Date)
                    .Select(g => new DailySalesDto
                    {
                        Date = g.Key,
                        Revenue = g.Sum(o => o.TotalAmount),
                        OrderCount = g.Count()
                    })
                    .OrderBy(d => d.Date)
                    .ToList();

                return new SalesReportDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalRevenue = totalRevenue,
                    TotalOrders = totalOrders,
                    AverageOrderValue = averageOrderValue,
                    DailySales = dailySales
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales report");
                throw;
            }
        }

        public async Task<IEnumerable<TopProductDto>> GetTopSellingProductsAsync(int count = 10)
        {
            try
            {
                var topProducts = await _context.OrderItems
                    .Include(oi => oi.Product)
                    .Where(oi => oi.Order.Status == "DELIVERED")
                    .GroupBy(oi => new { oi.ProductId, oi.Product.Name, oi.Product.SKU })
                    .Select(g => new TopProductDto
                    {
                        ProductId = g.Key.ProductId,
                        ProductName = g.Key.Name,
                        SKU = g.Key.SKU,
                        QuantitySold = g.Sum(oi => oi.Quantity),
                        Revenue = g.Sum(oi => oi.TotalPrice)
                    })
                    .OrderByDescending(tp => tp.QuantitySold)
                    .Take(count)
                    .ToListAsync();

                return topProducts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top selling products");
                throw;
            }
        }

        public async Task<IEnumerable<CustomerReportDto>> GetTopCustomersAsync(int count = 10)
        {
            try
            {
                var topCustomers = await _context.Orders
                    .Include(o => o.Customer)
                        .ThenInclude(c => c.User)
                    .Where(o => o.Status == "DELIVERED")
                    .GroupBy(o => new { o.CustomerId, o.Customer.User.FullName, o.Customer.User.Email })
                    .Select(g => new CustomerReportDto
                    {
                        CustomerId = g.Key.CustomerId,
                        CustomerName = g.Key.FullName ?? "N/A",
                        Email = g.Key.Email,
                        TotalOrders = g.Count(),
                        TotalSpent = g.Sum(o => o.TotalAmount),
                        LastOrderDate = g.Max(o => o.OrderDate)
                    })
                    .OrderByDescending(c => c.TotalSpent)
                    .Take(count)
                    .ToListAsync();

                return topCustomers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top customers");
                throw;
            }
        }

        public async Task<InventoryReportDto> GetInventoryReportAsync()
        {
            try
            {
                var inventories = await _context.Set<Inventory>()
                    .Include(i => i.Product)
                    .ToListAsync();

                var totalInventoryValue = inventories.Sum(i => (i.Product.Cost ?? 0) * i.Quantity);
                var totalProducts = inventories.Select(i => i.ProductId).Distinct().Count();
                var lowStockProducts = inventories.Count(i => i.Quantity <= i.MinStock);
                var outOfStockProducts = inventories.Count(i => i.Quantity == 0);

                var alerts = inventories
                    .Where(i => i.Quantity <= i.MinStock)
                    .Select(i => new InventoryAlertDto
                    {
                        ProductId = i.ProductId,
                        ProductName = i.Product.Name,
                        SKU = i.Product.SKU,
                        WarehouseId = i.WarehouseId,
                        CurrentStock = i.Quantity,
                        MinStock = i.MinStock,
                        AlertType = i.Quantity == 0 ? "OUT_OF_STOCK" : "LOW_STOCK"
                    })
                    .ToList();

                return new InventoryReportDto
                {
                    TotalInventoryValue = totalInventoryValue,
                    TotalProducts = totalProducts,
                    LowStockProducts = lowStockProducts,
                    OutOfStockProducts = outOfStockProducts,
                    Alerts = alerts
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inventory report");
                throw;
            }
        }
    }
}
