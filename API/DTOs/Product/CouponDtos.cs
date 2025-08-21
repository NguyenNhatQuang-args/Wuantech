using System.ComponentModel.DataAnnotations;
using WuanTech.API.Data;
using WuanTech.API.Services.Interfaces;

namespace WuanTech.API.DTOs
{
    public class CouponDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string DiscountType { get; set; } = string.Empty; // PERCENTAGE or FIXED
        public decimal DiscountValue { get; set; }
        public decimal MinOrderAmount { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateCouponDto
    {
        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [StringLength(20)]
        public string DiscountType { get; set; } = string.Empty; // PERCENTAGE, FIXED

        [Range(0, double.MaxValue)]
        public decimal DiscountValue { get; set; }

        [Range(0, double.MaxValue)]
        public decimal MinOrderAmount { get; set; } = 0;

        [Range(0, double.MaxValue)]
        public decimal? MaxDiscountAmount { get; set; }

        [Range(0, int.MaxValue)]
        public int? UsageLimit { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }

    public class UpdateCouponDto
    {
        [StringLength(500)]
        public string? Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? DiscountValue { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? MinOrderAmount { get; set; }

        public decimal? MaxDiscountAmount { get; set; }

        [Range(1, int.MaxValue)]
        public int? UsageLimit { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? IsActive { get; set; }
    }

    public class CouponValidationDto
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public decimal DiscountAmount { get; set; }
        public CouponDto? Coupon { get; set; }
    }
}