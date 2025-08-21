// Constants/AppConstants.cs
namespace WuanTech.API.Constants
{
    public static class AppConstants
    {
        public static class Roles
        {
            public const string Admin = "Admin";
            public const string Staff = "Staff";
            public const string Customer = "Customer";
        }

        public static class OrderStatus
        {
            public const string Pending = "PENDING";
            public const string Confirmed = "CONFIRMED";
            public const string Processing = "PROCESSING";
            public const string Shipped = "SHIPPED";
            public const string Delivered = "DELIVERED";
            public const string Cancelled = "CANCELLED";
        }

        public static class PaymentStatus
        {
            public const string Pending = "PENDING";
            public const string Paid = "PAID";
            public const string Failed = "FAILED";
            public const string Refunded = "REFUNDED";
        }

        public static class TransactionTypes
        {
            public const string In = "IN";
            public const string Out = "OUT";
            public const string Transfer = "TRANSFER";
            public const string Adjust = "ADJUST";
        }

        public static class CacheKeys
        {
            public const string Categories = "categories";
            public const string FeaturedProducts = "featured_products";
            public const string NewProducts = "new_products";
            public const string BestSellers = "best_sellers";
            public const string PopularSearches = "popular_searches";
        }

        public static class Policies
        {
            public const string RequireAdminRole = "RequireAdminRole";
            public const string RequireStaffRole = "RequireStaffRole";
            public const string RequireCustomerRole = "RequireCustomerRole";
        }
    }
}