using Microsoft.EntityFrameworkCore;
using WuanTech.API.Data;
using WuanTech.API.DTOs;
using WuanTech.API.Services.Interfaces;
using WuanTech.Models;

namespace WuanTech.API.Services.Implementations
{
    public class InventoryService : IInventoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InventoryService> _logger;

        public InventoryService(ApplicationDbContext context, ILogger<InventoryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<InventoryDto>> GetInventoryByProductAsync(int productId)
        {
            try
            {
                var inventories = await _context.Set<Inventory>()
                    .Include(i => i.Warehouse)
                    .Where(i => i.ProductId == productId)
                    .ToListAsync();

                return inventories.Select(i => new InventoryDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    WarehouseId = i.WarehouseId,
                    WarehouseName = i.Warehouse.Name,
                    Quantity = i.Quantity,
                    MinStock = i.MinStock,
                    MaxStock = i.MaxStock,
                    LastUpdated = i.LastUpdated
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inventory for product: {ProductId}", productId);
                throw;
            }
        }

        public async Task<bool> UpdateInventoryAsync(int productId, int warehouseId, int quantity)
        {
            try
            {
                var inventory = await _context.Set<Inventory>()
                    .FirstOrDefaultAsync(i => i.ProductId == productId && i.WarehouseId == warehouseId);

                if (inventory == null)
                {
                    inventory = new Inventory
                    {
                        ProductId = productId,
                        WarehouseId = warehouseId,
                        Quantity = quantity,
                        LastUpdated = DateTime.UtcNow
                    };
                    _context.Set<Inventory>().Add(inventory);
                }
                else
                {
                    inventory.Quantity = quantity;
                    inventory.LastUpdated = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating inventory");
                throw;
            }
        }

        public async Task<bool> ReserveInventoryAsync(int productId, int warehouseId, int quantity)
        {
            try
            {
                var inventory = await _context.Set<Inventory>()
                    .FirstOrDefaultAsync(i => i.ProductId == productId && i.WarehouseId == warehouseId);

                if (inventory == null || inventory.Quantity < quantity)
                    return false;

                inventory.Quantity -= quantity;
                inventory.LastUpdated = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reserving inventory");
                throw;
            }
        }

        public async Task<bool> ReleaseInventoryAsync(int productId, int warehouseId, int quantity)
        {
            try
            {
                var inventory = await _context.Set<Inventory>()
                    .FirstOrDefaultAsync(i => i.ProductId == productId && i.WarehouseId == warehouseId);

                if (inventory == null)
                    return false;

                inventory.Quantity += quantity;
                inventory.LastUpdated = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error releasing inventory");
                throw;
            }
        }

        public async Task<IEnumerable<InventoryAlertDto>> GetLowStockAlertsAsync()
        {
            try
            {
                var lowStockItems = await _context.Set<Inventory>()
                    .Include(i => i.Product)
                    .Include(i => i.Warehouse)
                    .Where(i => i.Quantity <= i.MinStock)
                    .ToListAsync();

                return lowStockItems.Select(i => new InventoryAlertDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    SKU = i.Product.SKU,
                    WarehouseId = i.WarehouseId,
                    WarehouseName = i.Warehouse.Name,
                    CurrentStock = i.Quantity,
                    MinStock = i.MinStock,
                    AlertType = i.Quantity == 0 ? "OUT_OF_STOCK" : "LOW_STOCK"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting low stock alerts");
                throw;
            }
        }

        public async Task<IEnumerable<InventoryDto>> GetInventoryByWarehouseAsync(int warehouseId)
        {
            try
            {
                var inventories = await _context.Set<Inventory>()
                    .Include(i => i.Product)
                    .Include(i => i.Warehouse)
                    .Where(i => i.WarehouseId == warehouseId)
                    .ToListAsync();

                return inventories.Select(i => new InventoryDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    ProductSKU = i.Product.SKU,
                    WarehouseId = i.WarehouseId,
                    WarehouseName = i.Warehouse.Name,
                    Quantity = i.Quantity,
                    MinStock = i.MinStock,
                    MaxStock = i.MaxStock,
                    LastUpdated = i.LastUpdated
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inventory for warehouse: {WarehouseId}", warehouseId);
                throw;
            }
        }

        public async Task<bool> TransferInventoryAsync(int productId, int fromWarehouseId, int toWarehouseId, int quantity)
        {
            try
            {
                // Check if source has enough stock
                var sourceInventory = await _context.Set<Inventory>()
                    .FirstOrDefaultAsync(i => i.ProductId == productId && i.WarehouseId == fromWarehouseId);

                if (sourceInventory == null || sourceInventory.Quantity < quantity)
                    return false;

                // Deduct from source
                sourceInventory.Quantity -= quantity;
                sourceInventory.LastUpdated = DateTime.UtcNow;

                // Add to destination
                var destInventory = await _context.Set<Inventory>()
                    .FirstOrDefaultAsync(i => i.ProductId == productId && i.WarehouseId == toWarehouseId);

                if (destInventory == null)
                {
                    destInventory = new Inventory
                    {
                        ProductId = productId,
                        WarehouseId = toWarehouseId,
                        Quantity = quantity,
                        LastUpdated = DateTime.UtcNow
                    };
                    _context.Set<Inventory>().Add(destInventory);
                }
                else
                {
                    destInventory.Quantity += quantity;
                    destInventory.LastUpdated = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transferring inventory");
                throw;
            }
        }

        public async Task<decimal> GetInventoryValueAsync(int? warehouseId = null)
        {
            try
            {
                var query = _context.Set<Inventory>()
                    .Include(i => i.Product)
                    .AsQueryable();

                if (warehouseId.HasValue)
                    query = query.Where(i => i.WarehouseId == warehouseId.Value);

                var totalValue = await query
                    .SumAsync(i => (i.Product.Cost ?? 0) * i.Quantity);

                return totalValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting inventory value");
                throw;
            }
        }
    }
}
