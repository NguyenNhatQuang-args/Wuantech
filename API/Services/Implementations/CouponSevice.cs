using Microsoft.EntityFrameworkCore;
using WuanTech.API.Data;
using WuanTech.API.DTOs;
using WuanTech.API.Services.Interfaces;
using WuanTech.Models;

namespace WuanTech.API.Services.Implementations
{
    public class CouponService : ICouponService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CouponService> _logger;

        public CouponService(ApplicationDbContext context, ILogger<CouponService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<CouponDto>> GetAvailableCouponsAsync(int userId)
        {
            try
            {
                var now = DateTime.UtcNow;
                var coupons = await _context.Set<Coupon>()
                    .Where(c => c.IsActive &&
                               (c.StartDate == null || c.StartDate <= now) &&
                               (c.EndDate == null || c.EndDate >= now) &&
                               (c.UsageLimit == null || c.UsedCount < c.UsageLimit))
                    .ToListAsync();

                return coupons.Select(c => new CouponDto
                {
                    Id = c.Id,
                    Code = c.Code,
                    Description = c.Description,
                    DiscountType = c.DiscountType,
                    DiscountValue = c.DiscountValue,
                    MinOrderAmount = c.MinOrderAmount,
                    MaxDiscountAmount = c.MaxDiscountAmount,
                    UsageLimit = c.UsageLimit,
                    UsedCount = c.UsedCount,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available coupons for user: {UserId}", userId);
                throw;
            }
        }

        public async Task<CouponDto?> GetCouponByCodeAsync(string code)
        {
            try
            {
                var coupon = await _context.Set<Coupon>()
                    .FirstOrDefaultAsync(c => c.Code == code);

                if (coupon == null)
                    return null;

                return new CouponDto
                {
                    Id = coupon.Id,
                    Code = coupon.Code,
                    Description = coupon.Description,
                    DiscountType = coupon.DiscountType,
                    DiscountValue = coupon.DiscountValue,
                    MinOrderAmount = coupon.MinOrderAmount,
                    MaxDiscountAmount = coupon.MaxDiscountAmount,
                    UsageLimit = coupon.UsageLimit,
                    UsedCount = coupon.UsedCount,
                    StartDate = coupon.StartDate,
                    EndDate = coupon.EndDate,
                    IsActive = coupon.IsActive,
                    CreatedAt = coupon.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting coupon by code: {Code}", code);
                throw;
            }
        }

        public async Task<bool> ValidateCouponAsync(string code, decimal orderAmount)
        {
            try
            {
                var now = DateTime.UtcNow;
                var coupon = await _context.Set<Coupon>()
                    .FirstOrDefaultAsync(c => c.Code == code);

                if (coupon == null || !coupon.IsActive)
                    return false;

                if (coupon.StartDate.HasValue && coupon.StartDate > now)
                    return false;

                if (coupon.EndDate.HasValue && coupon.EndDate < now)
                    return false;

                if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit)
                    return false;

                if (orderAmount < coupon.MinOrderAmount)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating coupon: {Code}", code);
                throw;
            }
        }

        public async Task<decimal> CalculateDiscountAsync(string code, decimal orderAmount)
        {
            try
            {
                var coupon = await _context.Set<Coupon>()
                    .FirstOrDefaultAsync(c => c.Code == code);

                if (coupon == null || !await ValidateCouponAsync(code, orderAmount))
                    return 0;

                decimal discount = 0;

                if (coupon.DiscountType == "PERCENTAGE")
                {
                    discount = orderAmount * (coupon.DiscountValue / 100);
                }
                else if (coupon.DiscountType == "FIXED")
                {
                    discount = coupon.DiscountValue;
                }

                if (coupon.MaxDiscountAmount.HasValue && discount > coupon.MaxDiscountAmount)
                {
                    discount = coupon.MaxDiscountAmount.Value;
                }

                return discount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating discount for coupon: {Code}", code);
                throw;
            }
        }

        public async Task<bool> UseCouponAsync(string code, int orderId)
        {
            try
            {
                var coupon = await _context.Set<Coupon>()
                    .FirstOrDefaultAsync(c => c.Code == code);

                if (coupon == null)
                    return false;

                coupon.UsedCount++;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error using coupon: {Code}", code);
                throw;
            }
        }

        
        public async Task<WuanTech.API.DTOs.CouponDto> CreateCouponAsync(
            WuanTech.API.DTOs.CreateCouponDto couponDto)
        {
            try
            {
                var coupon = new Coupon
                {
                    Code = couponDto.Code,
                    Description = couponDto.Description,
                    DiscountType = couponDto.DiscountType,
                    DiscountValue = couponDto.DiscountValue,
                    MinOrderAmount = couponDto.MinOrderAmount,
                    MaxDiscountAmount = couponDto.MaxDiscountAmount,
                    UsageLimit = couponDto.UsageLimit,
                    StartDate = couponDto.StartDate,
                    EndDate = couponDto.EndDate,
                    IsActive = true,
                    UsedCount = 0,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Set<Coupon>().Add(coupon);
                await _context.SaveChangesAsync();

                return new WuanTech.API.DTOs.CouponDto
                {
                    Id = coupon.Id,
                    Code = coupon.Code,
                    Description = coupon.Description,
                    DiscountType = coupon.DiscountType,
                    DiscountValue = coupon.DiscountValue,
                    MinOrderAmount = coupon.MinOrderAmount,
                    MaxDiscountAmount = coupon.MaxDiscountAmount,
                    UsageLimit = coupon.UsageLimit,
                    UsedCount = coupon.UsedCount,
                    StartDate = coupon.StartDate,
                    EndDate = coupon.EndDate,
                    IsActive = coupon.IsActive,
                    CreatedAt = coupon.CreatedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating coupon");
                throw;
            }
        }

        public async Task<bool> DeactivateCouponAsync(string code)
        {
            try
            {
                var coupon = await _context.Set<Coupon>()
                    .FirstOrDefaultAsync(c => c.Code == code);

                if (coupon == null)
                    return false;

                coupon.IsActive = false;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating coupon: {Code}", code);
                throw;
            }
        }
    }
}
