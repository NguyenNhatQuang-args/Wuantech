using Microsoft.EntityFrameworkCore;
using WuanTech.API.Data;
using WuanTech.API.DTOs;
using WuanTech.API.Services.Interfaces;
using WuanTech.Models;

namespace WuanTech.API.Services.Implementations
{
    public class WarehouseService : IWarehouseService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WarehouseService> _logger;

        public WarehouseService(ApplicationDbContext context, ILogger<WarehouseService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<WarehouseDto>> GetAllWarehousesAsync()
        {
            try
            {
                var warehouses = await _context.Set<Warehouse>()
                    .Where(w => w.IsActive)
                    .OrderBy(w => w.Name)
                    .ToListAsync();

                return warehouses.Select(w => new WarehouseDto
                {
                    Id = w.Id,
                    Code = w.Code,
                    Name = w.Name,
                    Address = w.Address,
                    Phone = w.Phone,
                    Manager = w.Manager,
                    IsActive = w.IsActive,
                    CreatedAt = w.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all warehouses");
                throw;
            }
        }

        public async Task<WarehouseDto?> GetWarehouseByIdAsync(int id)
        {
            try
            {
                var warehouse = await _context.Set<Warehouse>().FindAsync(id);
                if (warehouse == null)
                    return null;

                return new WarehouseDto
                {
                    Id = warehouse.Id,
                    Code = warehouse.Code,
                    Name = warehouse.Name,
                    Address = warehouse.Address,
                    Phone = warehouse.Phone,
                    Manager = warehouse.Manager,
                    IsActive = warehouse.IsActive,
                    CreatedAt = warehouse.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting warehouse by id: {Id}", id);
                throw;
            }
        }

        public async Task<WarehouseDto> CreateWarehouseAsync(CreateWarehouseDto warehouseDto)
        {
            try
            {
                var warehouse = new Warehouse
                {
                    Code = warehouseDto.Code,
                    Name = warehouseDto.Name,
                    Address = warehouseDto.Address,
                    Phone = warehouseDto.Phone,
                    Manager = warehouseDto.Manager,
                    IsActive = warehouseDto.IsActive,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Set<Warehouse>().Add(warehouse);
                await _context.SaveChangesAsync();

                return new WarehouseDto
                {
                    Id = warehouse.Id,
                    Code = warehouse.Code,
                    Name = warehouse.Name,
                    Address = warehouse.Address,
                    Phone = warehouse.Phone,
                    Manager = warehouse.Manager,
                    IsActive = warehouse.IsActive,
                    CreatedAt = warehouse.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating warehouse");
                throw;
            }
        }

        public async Task<WarehouseDto?> UpdateWarehouseAsync(int id, UpdateWarehouseDto warehouseDto)
        {
            try
            {
                var warehouse = await _context.Set<Warehouse>().FindAsync(id);
                if (warehouse == null)
                    return null;

                warehouse.Code = warehouseDto.Code;
                warehouse.Name = warehouseDto.Name;
                warehouse.Address = warehouseDto.Address;
                warehouse.Phone = warehouseDto.Phone;
                warehouse.Manager = warehouseDto.Manager;
                warehouse.IsActive = warehouseDto.IsActive;

                await _context.SaveChangesAsync();

                return new WarehouseDto
                {
                    Id = warehouse.Id,
                    Code = warehouse.Code,
                    Name = warehouse.Name,
                    Address = warehouse.Address,
                    Phone = warehouse.Phone,
                    Manager = warehouse.Manager,
                    IsActive = warehouse.IsActive,
                    CreatedAt = warehouse.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating warehouse: {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteWarehouseAsync(int id)
        {
            try
            {
                var warehouse = await _context.Set<Warehouse>().FindAsync(id);
                if (warehouse == null)
                    return false;

                // Check if warehouse has inventory
                var hasInventory = await _context.Set<Inventory>().AnyAsync(i => i.WarehouseId == id);
                if (hasInventory)
                    throw new InvalidOperationException("Cannot delete warehouse with inventory");

                warehouse.IsActive = false;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting warehouse: {Id}", id);
                throw;
            }
        }

        public async Task<WarehouseDto?> GetWarehouseByCodeAsync(string code)
        {
            try
            {
                var warehouse = await _context.Set<Warehouse>()
                    .FirstOrDefaultAsync(w => w.Code == code);

                if (warehouse == null)
                    return null;

                return new WarehouseDto
                {
                    Id = warehouse.Id,
                    Code = warehouse.Code,
                    Name = warehouse.Name,
                    Address = warehouse.Address,
                    Phone = warehouse.Phone,
                    Manager = warehouse.Manager,
                    IsActive = warehouse.IsActive,
                    CreatedAt = warehouse.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting warehouse by code: {Code}", code);
                throw;
            }
        }

        public async Task<IEnumerable<InventoryDto>> GetWarehouseInventoryAsync(int warehouseId)
        {
            try
            {
                var inventories = await _context.Set<Inventory>()
                    .Include(i => i.Product)
                    .Where(i => i.WarehouseId == warehouseId)
                    .ToListAsync();

                return inventories.Select(i => new InventoryDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    ProductSKU = i.Product.SKU,
                    WarehouseId = i.WarehouseId,
                    Quantity = i.Quantity,
                    MinStock = i.MinStock,
                    MaxStock = i.MaxStock,
                    LastUpdated = i.LastUpdated
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting warehouse inventory: {WarehouseId}", warehouseId);
                throw;
            }
        }
    }
}
