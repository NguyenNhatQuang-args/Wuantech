using Microsoft.EntityFrameworkCore;
using WuanTech.API.Data;
using WuanTech.API.DTOs;
using WuanTech.API.Services.Interfaces;
using WuanTech.Models;

namespace WuanTech.API.Services.Implementations
{
    public class BrandService : IBrandService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BrandService> _logger;

        public BrandService(ApplicationDbContext context, ILogger<BrandService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<BrandDto>> GetAllBrandsAsync()
        {
            try
            {
                var brands = await _context.Set<Brand>()
                    .Include(b => b.Products)
                    .Where(b => b.IsActive)
                    .OrderBy(b => b.Name)
                    .ToListAsync();

                return brands.Select(b => new BrandDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Logo = b.Logo,
                    Description = b.Description,
                    IsActive = b.IsActive,
                    CreatedAt = b.CreatedAt,
                    ProductCount = b.Products.Count(p => p.IsActive)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all brands");
                throw;
            }
        }

        public async Task<BrandDto?> GetBrandByIdAsync(int id)
        {
            try
            {
                var brand = await _context.Set<Brand>()
                    .Include(b => b.Products)
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (brand == null)
                    return null;

                return new BrandDto
                {
                    Id = brand.Id,
                    Name = brand.Name,
                    Logo = brand.Logo,
                    Description = brand.Description,
                    IsActive = brand.IsActive,
                    CreatedAt = brand.CreatedAt,
                    ProductCount = brand.Products.Count(p => p.IsActive)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting brand by id: {Id}", id);
                throw;
            }
        }

        public async Task<BrandDto> CreateBrandAsync(CreateBrandDto brandDto)
        {
            try
            {
                var brand = new Brand
                {
                    Name = brandDto.Name,
                    Logo = brandDto.Logo,
                    Description = brandDto.Description,
                    IsActive = brandDto.IsActive,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Set<Brand>().Add(brand);
                await _context.SaveChangesAsync();

                return new BrandDto
                {
                    Id = brand.Id,
                    Name = brand.Name,
                    Logo = brand.Logo,
                    Description = brand.Description,
                    IsActive = brand.IsActive,
                    CreatedAt = brand.CreatedAt,
                    ProductCount = 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating brand");
                throw;
            }
        }

        public async Task<BrandDto?> UpdateBrandAsync(int id, UpdateBrandDto brandDto)
        {
            try
            {
                var brand = await _context.Set<Brand>().FindAsync(id);
                if (brand == null)
                    return null;

                brand.Name = brandDto.Name;
                brand.Logo = brandDto.Logo;
                brand.Description = brandDto.Description;
                brand.IsActive = brandDto.IsActive;

                await _context.SaveChangesAsync();

                return new BrandDto
                {
                    Id = brand.Id,
                    Name = brand.Name,
                    Logo = brand.Logo,
                    Description = brand.Description,
                    IsActive = brand.IsActive,
                    CreatedAt = brand.CreatedAt,
                    ProductCount = brand.Products?.Count(p => p.IsActive) ?? 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating brand: {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteBrandAsync(int id)
        {
            try
            {
                var brand = await _context.Set<Brand>().FindAsync(id);
                if (brand == null)
                    return false;

                // Check if brand has products
                var hasProducts = await _context.Products.AnyAsync(p => p.BrandId == id && p.IsActive);
                if (hasProducts)
                    throw new InvalidOperationException("Cannot delete brand with active products");

                brand.IsActive = false;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting brand: {Id}", id);
                throw;
            }
        }

        // =======================================
        // THÊM 2 METHODS THIẾU TRONG INTERFACE
        // =======================================

        public async Task<IEnumerable<BrandDto>> GetBrandsWithProductCountAsync()
        {
            try
            {
                var brands = await _context.Set<Brand>()
                    .Include(b => b.Products.Where(p => p.IsActive))
                    .Where(b => b.IsActive)
                    .OrderBy(b => b.Name)
                    .ToListAsync();

                return brands.Select(b => new BrandDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Logo = b.Logo,
                    Description = b.Description,
                    IsActive = b.IsActive,
                    CreatedAt = b.CreatedAt,
                    ProductCount = b.Products.Count(p => p.IsActive)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting brands with product count");
                throw;
            }
        }

        public async Task<PagedResult<BrandDto>> GetBrandsPagedAsync(int page = 1, int pageSize = 20)
        {
            try
            {
                var query = _context.Set<Brand>()
                    .Include(b => b.Products.Where(p => p.IsActive))
                    .Where(b => b.IsActive)
                    .OrderBy(b => b.Name);

                var totalCount = await query.CountAsync();

                var brands = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var brandDtos = brands.Select(b => new BrandDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Logo = b.Logo,
                    Description = b.Description,
                    IsActive = b.IsActive,
                    CreatedAt = b.CreatedAt,
                    ProductCount = b.Products.Count(p => p.IsActive)
                }).ToList();

                return new PagedResult<BrandDto>(brandDtos, totalCount, page, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting brands paged");
                throw;
            }
        }
    }
}
