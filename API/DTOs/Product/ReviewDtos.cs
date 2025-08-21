using System.ComponentModel.DataAnnotations;

namespace WuanTech.API.DTOs.Product
{
    public class ReviewDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public UserDto? User { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Images { get; set; } = new();
        public bool IsVerifiedPurchase { get; set; }
        public bool IsApproved { get; set; } = true;
    }

    public class CreateReviewDto
    {
        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [StringLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters")]
        public string? Comment { get; set; }

        public List<string>? Images { get; set; }
    }

    public class UpdateReviewDto
    {
        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [StringLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters")]
        public string? Comment { get; set; }
    }

    public class ReviewStatsDto
    {
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public Dictionary<int, int> RatingDistribution { get; set; } = new();
        public int VerifiedPurchases { get; set; }

        public double OneStarPercentage => GetRatingPercentage(1);
        public double TwoStarPercentage => GetRatingPercentage(2);
        public double ThreeStarPercentage => GetRatingPercentage(3);
        public double FourStarPercentage => GetRatingPercentage(4);
        public double FiveStarPercentage => GetRatingPercentage(5);

        private double GetRatingPercentage(int rating)
        {
            if (TotalReviews == 0) return 0;
            return RatingDistribution.ContainsKey(rating) ?
                   (double)RatingDistribution[rating] / TotalReviews * 100 : 0;
        }
    }

}
