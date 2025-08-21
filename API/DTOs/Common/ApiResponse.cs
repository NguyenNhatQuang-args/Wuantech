// File: DTOs/Common/ApiResponse.cs
// Mục đích: Tạo response wrapper cho tất cả API endpoints
// Bao gồm: Generic ApiResponse, PagedResult, Validation errors

namespace WuanTech.API.DTOs
{
    /// <summary>
    /// Generic API Response wrapper cho tất cả endpoint responses
    /// </summary>
    /// <typeparam name="T">Kiểu dữ liệu trả về</typeparam>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Constructor mặc định
        public ApiResponse() { }

        // Constructor cho success response
        public ApiResponse(T data, string message = "Success")
        {
            Success = true;
            Data = data;
            Message = message;
        }

        // Constructor cho error response
        public ApiResponse(string message, List<string>? errors = null)
        {
            Success = false;
            Message = message;
            Errors = errors;
        }
    }

    /// <summary>
    /// Non-generic API Response cho các endpoint không cần trả data
    /// </summary>
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string>? Errors { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public ApiResponse() { }

        public ApiResponse(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }

    /// <summary>
    /// Wrapper cho paginated results (phân trang)
    /// </summary>
    /// <typeparam name="T">Kiểu dữ liệu của items</typeparam>
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;

        public PagedResult(List<T> items, int totalCount, int page, int pageSize)
        {
            Items = items;
            TotalCount = totalCount;
            Page = page;
            PageSize = pageSize;
        }
    }

    /// <summary>
    /// Error response cho validation errors
    /// </summary>
    public class ValidationErrorResponse
    {
        public string Field { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public object? AttemptedValue { get; set; }
    }

    /// <summary>
    /// Standard API error codes sử dụng trong toàn hệ thống
    /// </summary>
    public static class ApiErrorCodes
    {
        public const string ValidationError = "VALIDATION_ERROR";
        public const string NotFound = "NOT_FOUND";
        public const string Unauthorized = "UNAUTHORIZED";
        public const string Forbidden = "FORBIDDEN";
        public const string InternalError = "INTERNAL_ERROR";
        public const string DuplicateEntry = "DUPLICATE_ENTRY";
        public const string InvalidOperation = "INVALID_OPERATION";
        public const string InsufficientStock = "INSUFFICIENT_STOCK";
        public const string PaymentFailed = "PAYMENT_FAILED";
        public const string OrderNotFound = "ORDER_NOT_FOUND";
        public const string ProductNotFound = "PRODUCT_NOT_FOUND";
    }
}