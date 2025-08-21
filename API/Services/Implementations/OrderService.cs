using Microsoft.EntityFrameworkCore;
using WuanTech.API.Data;
using WuanTech.API.DTOs;
using WuanTech.API.Services.Interfaces;
using WuanTech.Models;

namespace WuanTech.API.Services.Implementations
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderService> _logger;
        private readonly ICartService _cartService;

        public OrderService(
            ApplicationDbContext context,
            ILogger<OrderService> logger,
            ICartService cartService)
        {
            _context = context;
            _logger = logger;
            _cartService = cartService;
        }

        public async Task<IEnumerable<OrderDto>> GetUserOrdersAsync(int userId)
        {
            try
            {
                // Get customer from user
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (customer == null)
                    return new List<OrderDto>();

                var orders = await _context.Orders
                    .Include(o => o.OrderItems)
                    .Where(o => o.CustomerId == customer.Id)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                return orders.Select(o => new OrderDto
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    TotalAmount = o.TotalAmount,
                    OrderStatus = o.Status,
                    PaymentStatus = o.PaymentStatus,
                    OrderDate = o.OrderDate,
                    ShippedDate = o.ShippedDate,
                    DeliveredDate = o.DeliveredDate,
                    ItemCount = o.OrderItems.Sum(oi => oi.Quantity)
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user orders: {UserId}", userId);
                throw;
            }
        }

        public async Task<OrderDetailDto?> GetOrderByIdAsync(int orderId, int userId)
        {
            try
            {
                // Get customer from user
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (customer == null)
                    return null;

                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                            .ThenInclude(p => p.Category)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                            .ThenInclude(p => p.Brand)
                    .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == customer.Id);

                if (order == null)
                    return null;

                return new OrderDetailDto
                {
                    Id = order.Id,
                    OrderNumber = order.OrderNumber,
                    TotalAmount = order.TotalAmount,
                    OrderStatus = order.Status,
                    PaymentStatus = order.PaymentStatus,
                    OrderDate = order.OrderDate,
                    ShippedDate = order.ShippedDate,
                    DeliveredDate = order.DeliveredDate,
                    ItemCount = order.OrderItems.Sum(oi => oi.Quantity),
                    SubTotal = order.SubTotal,
                    ShippingFee = order.ShippingFee,
                    Discount = order.Discount,
                    Tax = order.Tax,
                    ShippingAddress = order.ShippingAddress,
                    PaymentMethod = order.PaymentMethod,
                    TrackingNumber = order.TrackingNumber,
                    Notes = order.Notes,
                    OrderItems = order.OrderItems.Select(oi => new OrderItemDto
                    {
                        Id = oi.Id,
                        Product = MapProductToDto(oi.Product),
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice,
                        TotalPrice = oi.TotalPrice,
                        DiscountAmount = oi.DiscountAmount
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order: {OrderId}", orderId);
                throw;
            }
        }

        public async Task<OrderDto?> CreateOrderAsync(int userId, CreateOrderDto orderDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Get or create customer
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (customer == null)
                {
                    // Create new customer
                    customer = new Customer
                    {
                        UserId = userId,
                        CustomerCode = GenerateCustomerCode(),
                        Points = 0,
                        MembershipLevel = "Bronze",
                        TotalPurchased = 0
                    };

                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();
                }

                // Get cart items
                var cart = await _cartService.GetCartAsync(userId);
                if (!cart.Items.Any())
                    return null;

                // Validate stock availability
                foreach (var cartItem in cart.Items)
                {
                    var product = await _context.Products
                        .Include(p => p.Inventories)
                        .FirstOrDefaultAsync(p => p.Id == cartItem.Product.Id);

                    if (product == null || !product.IsActive)
                    {
                        throw new InvalidOperationException($"Product {cartItem.Product.Name} is no longer available");
                    }

                    var totalStock = product.Inventories?.Sum(i => i.Quantity) ?? 0;
                    if (totalStock < cartItem.Quantity)
                    {
                        throw new InvalidOperationException($"Insufficient stock for product {cartItem.Product.Name}");
                    }
                }

                // Create order
                var order = new Order
                {
                    OrderNumber = GenerateOrderNumber(),
                    CustomerId = customer.Id,
                    OrderDate = DateTime.UtcNow,
                    Status = "PENDING",
                    PaymentStatus = "PENDING",
                    ShippingAddress = orderDto.ShippingAddress,
                    PaymentMethod = orderDto.PaymentMethod,
                    Notes = orderDto.Notes,
                    SubTotal = cart.SubTotal,
                    ShippingFee = cart.ShippingFee,
                    Tax = cart.Tax,
                    Discount = cart.Discount,
                    TotalAmount = cart.Total,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Create order items and update stock
                foreach (var cartItem in cart.Items)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = cartItem.Product.Id,
                        Quantity = cartItem.Quantity,
                        UnitPrice = cartItem.UnitPrice,
                        TotalPrice = cartItem.TotalPrice,
                        DiscountAmount = 0 // Can be updated later for item-specific discounts
                    };

                    _context.OrderItems.Add(orderItem);

                    // Update inventory - deduct from the warehouse with highest stock
                    await DeductInventoryAsync(cartItem.Product.Id, cartItem.Quantity);
                }

                await _context.SaveChangesAsync();

                // Update customer statistics
                customer.TotalPurchased += order.TotalAmount;
                customer.Points += (int)(order.TotalAmount / 10000); // 1 point per 10K VND

                // Update membership level based on total purchased
                if (customer.TotalPurchased >= 50000000) // 50M VND
                    customer.MembershipLevel = "Platinum";
                else if (customer.TotalPurchased >= 20000000) // 20M VND
                    customer.MembershipLevel = "Gold";
                else if (customer.TotalPurchased >= 5000000) // 5M VND
                    customer.MembershipLevel = "Silver";

                await _context.SaveChangesAsync();

                // Clear cart
                await _cartService.ClearCartAsync(userId);

                await transaction.CommitAsync();

                return new OrderDto
                {
                    Id = order.Id,
                    OrderNumber = order.OrderNumber,
                    TotalAmount = order.TotalAmount,
                    OrderStatus = order.Status,
                    PaymentStatus = order.PaymentStatus,
                    OrderDate = order.OrderDate,
                    ItemCount = cart.Items.Sum(i => i.Quantity)
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating order for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> CancelOrderAsync(int orderId, int userId)
        {
            try
            {
                // Get customer from user
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (customer == null)
                    return false;

                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == customer.Id);

                if (order == null || !CanCancelOrder(order.Status))
                    return false;

                order.Status = "CANCELLED";
                order.CancelledDate = DateTime.UtcNow;
                order.UpdatedAt = DateTime.UtcNow;

                // Restore inventory
                foreach (var item in order.OrderItems)
                {
                    await RestoreInventoryAsync(item.ProductId, item.Quantity);
                }

                // Update customer statistics
                customer.TotalPurchased -= order.TotalAmount;
                customer.Points = Math.Max(0, customer.Points - (int)(order.TotalAmount / 10000));

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order: {OrderId}", orderId);
                throw;
            }
        }

        public async Task<TrackingInfoDto?> GetTrackingInfoAsync(int orderId, int userId)
        {
            try
            {
                // Get customer from user
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (customer == null)
                    return null;

                var order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == customer.Id);

                if (order == null)
                    return null;

                var events = new List<TrackingEventDto>();

                // Add tracking events based on order status
                events.Add(new TrackingEventDto
                {
                    Status = "Order Placed",
                    DateTime = order.OrderDate,
                    Description = "Your order has been placed successfully",
                    Location = "Online"
                });

                if (order.Status != "PENDING")
                {
                    events.Add(new TrackingEventDto
                    {
                        Status = "Order Confirmed",
                        DateTime = order.OrderDate.AddHours(1),
                        Description = "Your order has been confirmed and is being prepared",
                        Location = "Warehouse"
                    });
                }

                if (order.Status == "PROCESSING")
                {
                    events.Add(new TrackingEventDto
                    {
                        Status = "Processing",
                        DateTime = order.OrderDate.AddHours(6),
                        Description = "Your order is being processed",
                        Location = "Warehouse"
                    });
                }

                if (order.ShippedDate.HasValue)
                {
                    events.Add(new TrackingEventDto
                    {
                        Status = "Shipped",
                        DateTime = order.ShippedDate.Value,
                        Description = "Your order has been shipped",
                        Location = "In Transit"
                    });
                }

                if (order.DeliveredDate.HasValue)
                {
                    events.Add(new TrackingEventDto
                    {
                        Status = "Delivered",
                        DateTime = order.DeliveredDate.Value,
                        Description = "Your order has been delivered successfully",
                        Location = "Destination"
                    });
                }

                if (order.Status == "CANCELLED")
                {
                    events.Add(new TrackingEventDto
                    {
                        Status = "Cancelled",
                        DateTime = order.CancelledDate ?? order.UpdatedAt,
                        Description = string.IsNullOrEmpty(order.CancelReason) ? "Order has been cancelled" : $"Order cancelled: {order.CancelReason}",
                        Location = "System"
                    });
                }

                return new TrackingInfoDto
                {
                    OrderNumber = order.OrderNumber,
                    TrackingNumber = order.TrackingNumber,
                    Status = order.Status,
                    EstimatedDelivery = CalculateEstimatedDelivery(order.OrderDate, order.Status),
                    Events = events.OrderByDescending(e => e.DateTime).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tracking info: {OrderId}", orderId);
                throw;
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string status)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                    return false;

                var oldStatus = order.Status;
                order.Status = status;
                order.UpdatedAt = DateTime.UtcNow;

                // Update specific dates based on status
                switch (status.ToUpper())
                {
                    case "SHIPPED":
                        if (!order.ShippedDate.HasValue)
                        {
                            order.ShippedDate = DateTime.UtcNow;
                            order.TrackingNumber ??= GenerateTrackingNumber();
                        }
                        break;
                    case "DELIVERED":
                        if (!order.DeliveredDate.HasValue)
                        {
                            order.DeliveredDate = DateTime.UtcNow;
                            order.PaymentStatus = "PAID"; // Mark as paid when delivered for COD
                        }
                        break;
                    case "CANCELLED":
                        if (!order.CancelledDate.HasValue)
                        {
                            order.CancelledDate = DateTime.UtcNow;
                        }
                        break;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Order {OrderId} status updated from {OldStatus} to {NewStatus}",
                    orderId, oldStatus, status);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status: {OrderId}", orderId);
                throw;
            }
        }

        public async Task<IEnumerable<OrderDto>> GetAllOrdersAsync(int page = 1, int pageSize = 20)
        {
            try
            {
                var orders = await _context.Orders
                    .Include(o => o.OrderItems)
                    .Include(o => o.Customer)
                        .ThenInclude(c => c.User)
                    .OrderByDescending(o => o.OrderDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return orders.Select(o => new OrderDto
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    TotalAmount = o.TotalAmount,
                    OrderStatus = o.Status,
                    PaymentStatus = o.PaymentStatus,
                    OrderDate = o.OrderDate,
                    ShippedDate = o.ShippedDate,
                    DeliveredDate = o.DeliveredDate,
                    ItemCount = o.OrderItems.Sum(oi => oi.Quantity)
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all orders");
                throw;
            }
        }

        public async Task<bool> UpdatePaymentStatusAsync(int orderId, string paymentStatus)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                    return false;

                order.PaymentStatus = paymentStatus;
                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment status for order: {OrderId}", orderId);
                throw;
            }
        }

        #region Private Methods

        private async Task DeductInventoryAsync(int productId, int quantity)
        {
            var inventories = await _context.Inventories
                .Where(i => i.ProductId == productId && i.Quantity > 0)
                .OrderByDescending(i => i.Quantity)
                .ToListAsync();

            var remainingQuantity = quantity;
            foreach (var inventory in inventories)
            {
                if (remainingQuantity <= 0) break;

                var deductAmount = Math.Min(inventory.Quantity, remainingQuantity);
                inventory.Quantity -= deductAmount;
                inventory.LastUpdated = DateTime.UtcNow;
                remainingQuantity -= deductAmount;
            }

            if (remainingQuantity > 0)
            {
                throw new InvalidOperationException($"Insufficient stock for product ID {productId}");
            }
        }

        private async Task RestoreInventoryAsync(int productId, int quantity)
        {
            // Find the warehouse with the least stock to restore to
            var inventory = await _context.Inventories
                .Where(i => i.ProductId == productId)
                .OrderBy(i => i.Quantity)
                .FirstOrDefaultAsync();

            if (inventory != null)
            {
                inventory.Quantity += quantity;
                inventory.LastUpdated = DateTime.UtcNow;
            }
        }

        private static bool CanCancelOrder(string status)
        {
            return status.ToUpper() is "PENDING" or "CONFIRMED";
        }

        private static string CalculateEstimatedDelivery(DateTime orderDate, string status)
        {
            var estimatedDays = status.ToUpper() switch
            {
                "PENDING" => 3,
                "CONFIRMED" => 3,
                "PROCESSING" => 2,
                "SHIPPED" => 1,
                "DELIVERED" => 0,
                _ => 3
            };

            return orderDate.AddDays(estimatedDays).ToString("yyyy-MM-dd");
        }

        private string GenerateOrderNumber()
        {
            return $"ORD{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
        }

        private string GenerateCustomerCode()
        {
            return $"KH{DateTime.UtcNow:yyyyMMdd}{new Random().Next(1000, 9999)}";
        }

        private string GenerateTrackingNumber()
        {
            return $"TRK{DateTime.UtcNow:yyyyMMdd}{new Random().Next(100000, 999999)}";
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
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name,
                Brand = product.Brand?.Name,
                Rating = product.Rating,
                ReviewCount = product.ReviewCount
            };
        }

        #endregion
    }
}