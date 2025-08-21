using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using System.Collections.Concurrent;
using WuanTech.API.DTOs;

namespace WuanTech.API.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger, IWebHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred while processing the request. Path: {Path}, Method: {Method}, User: {User}",
                    context.Request.Path,
                    context.Request.Method,
                    context.User?.Identity?.Name ?? "Anonymous");

                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";

            var response = new ApiResponse
            {
                Success = false,
                Timestamp = DateTime.UtcNow
            };

            switch (ex)
            {
                case ValidationException validationEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Validation failed";
                    response.Errors = new List<string> { validationEx.Message };
                    break;

                case UnauthorizedAccessException:
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    response.Message = "Unauthorized access";
                    break;

                case ArgumentNullException argNullEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Invalid request data";
                    response.Errors = new List<string> { $"Missing required parameter: {argNullEx.ParamName}" };
                    break;

                case ArgumentException argEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Invalid argument";
                    response.Errors = new List<string> { argEx.Message };
                    break;

                case KeyNotFoundException:
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.Message = "Resource not found";
                    break;

                case InvalidOperationException invalidOpEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Invalid operation";
                    if (_environment.IsDevelopment())
                    {
                        response.Errors = new List<string> { invalidOpEx.Message };
                    }
                    break;

                case DbUpdateException dbEx:
                    context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                    response.Message = "Database operation failed";

                    if (dbEx.InnerException?.Message.Contains("UNIQUE constraint") == true)
                    {
                        response.Message = "Duplicate entry detected";
                    }
                    else if (dbEx.InnerException?.Message.Contains("FOREIGN KEY constraint") == true)
                    {
                        response.Message = "Related record not found or cannot be deleted";
                    }

                    if (_environment.IsDevelopment())
                    {
                        response.Errors = new List<string> { dbEx.Message };
                    }
                    break;

                case TimeoutException:
                    context.Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                    response.Message = "Request timeout";
                    break;

                case NotImplementedException:
                    context.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
                    response.Message = "Feature not implemented";
                    break;

                case HttpRequestException httpEx:
                    context.Response.StatusCode = (int)HttpStatusCode.BadGateway;
                    response.Message = "External service error";
                    if (_environment.IsDevelopment())
                    {
                        response.Errors = new List<string> { httpEx.Message };
                    }
                    break;

                case TaskCanceledException:
                    context.Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                    response.Message = "Request was cancelled or timed out";
                    break;

                default:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response.Message = "An unexpected error occurred";

                    if (_environment.IsDevelopment())
                    {
                        response.Errors = new List<string>
                        {
                            ex.Message,
                            ex.StackTrace ?? "No stack trace available"
                        };
                    }
                    break;
            }

            // Add correlation ID for tracking
            var correlationId = context.TraceIdentifier;
            context.Response.Headers.Add("X-Correlation-ID", correlationId);

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }

    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var startTime = DateTime.UtcNow;
            var correlationId = context.TraceIdentifier;

            // Log request
            _logger.LogInformation("Starting request: {Method} {Path} {QueryString} - CorrelationId: {CorrelationId} - User: {User} - IP: {IP}",
                context.Request.Method,
                context.Request.Path,
                context.Request.QueryString,
                correlationId,
                context.User?.Identity?.Name ?? "Anonymous",
                GetClientIpAddress(context));

            // Add correlation ID to response headers
            context.Response.Headers.Add("X-Correlation-ID", correlationId);

            try
            {
                await _next(context);
            }
            finally
            {
                var duration = DateTime.UtcNow - startTime;

                // Log response
                _logger.LogInformation("Completed request: {Method} {Path} - Status: {StatusCode} - Duration: {Duration}ms - CorrelationId: {CorrelationId}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    duration.TotalMilliseconds,
                    correlationId);

                // Log slow requests
                if (duration.TotalMilliseconds > 1000)
                {
                    _logger.LogWarning("Slow request detected: {Method} {Path} - Duration: {Duration}ms - CorrelationId: {CorrelationId}",
                        context.Request.Method,
                        context.Request.Path,
                        duration.TotalMilliseconds,
                        correlationId);
                }
            }
        }

        private static string GetClientIpAddress(HttpContext context)
        {
            // Check for X-Forwarded-For header first (in case of proxy/load balancer)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            // Check for X-Real-IP header
            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            // Fall back to RemoteIpAddress
            return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }
    }

    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Add security headers
            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Add("X-Frame-Options", "DENY");
            context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

            // Remove server header
            context.Response.Headers.Remove("Server");

            await _next(context);
        }
    }

    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private readonly ConcurrentDictionary<string, (DateTime LastRequest, int RequestCount)> _requestCounts;
        private readonly int _maxRequests;
        private readonly TimeSpan _timeWindow;

        public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger, int maxRequests = 100, int timeWindowMinutes = 1)
        {
            _next = next;
            _logger = logger;
            _requestCounts = new ConcurrentDictionary<string, (DateTime, int)>();
            _maxRequests = maxRequests;
            _timeWindow = TimeSpan.FromMinutes(timeWindowMinutes);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var clientId = GetClientId(context);
            var now = DateTime.UtcNow;

            // Clean up old entries periodically
            CleanupOldEntries(now);

            // Check current client
            var (shouldLimit, newCount) = CheckRateLimit(clientId, now);

            if (shouldLimit)
            {
                _logger.LogWarning("Rate limit exceeded for client: {ClientId}", clientId);

                context.Response.StatusCode = 429; // Too Many Requests
                context.Response.ContentType = "application/json";
                context.Response.Headers.Add("Retry-After", _timeWindow.TotalSeconds.ToString());

                var response = new ApiResponse
                {
                    Success = false,
                    Message = "Rate limit exceeded. Please try again later."
                };

                var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await context.Response.WriteAsync(jsonResponse);
                return;
            }

            // Update request count
            _requestCounts.AddOrUpdate(clientId,
                (now, 1),
                (key, value) => (now, newCount));

            await _next(context);
        }

        private (bool ShouldLimit, int NewCount) CheckRateLimit(string clientId, DateTime now)
        {
            if (_requestCounts.TryGetValue(clientId, out var clientData))
            {
                if (now - clientData.LastRequest <= _timeWindow)
                {
                    if (clientData.RequestCount >= _maxRequests)
                    {
                        return (true, clientData.RequestCount);
                    }
                    return (false, clientData.RequestCount + 1);
                }
                else
                {
                    return (false, 1);
                }
            }
            else
            {
                return (false, 1);
            }
        }

        private void CleanupOldEntries(DateTime now)
        {
            // Only cleanup every 100 requests to avoid performance impact
            if (_requestCounts.Count % 100 == 0)
            {
                var expiredKeys = _requestCounts
                    .Where(kvp => now - kvp.Value.LastRequest > _timeWindow)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    _requestCounts.TryRemove(key, out _);
                }
            }
        }

        private static string GetClientId(HttpContext context)
        {
            // Use IP + User Agent for basic rate limiting
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = context.Request.Headers["User-Agent"].ToString();
            return $"{ip}:{userAgent.GetHashCode()}";
        }
    }

    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiKeyMiddleware> _logger;
        private readonly IConfiguration _configuration;
        private const string ApiKeyHeaderName = "X-API-Key";

        public ApiKeyMiddleware(RequestDelegate next, ILogger<ApiKeyMiddleware> logger, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip API key validation for certain endpoints
            var path = context.Request.Path.Value?.ToLower();
            if (path != null && (path.Contains("/swagger") || path.Contains("/health") || path.Contains("/auth/login") || path.Contains("/auth/register")))
            {
                await _next(context);
                return;
            }

            if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
            {
                _logger.LogWarning("API Key missing from request: {Path}", context.Request.Path);

                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";

                var response = new ApiResponse
                {
                    Success = false,
                    Message = "API Key is missing"
                };

                var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await context.Response.WriteAsync(jsonResponse);
                return;
            }

            var apiKey = _configuration.GetValue<string>("Security:ApiKey");
            if (string.IsNullOrEmpty(apiKey) || !apiKey.Equals(extractedApiKey))
            {
                _logger.LogWarning("Invalid API Key used: {ApiKey}", extractedApiKey);

                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";

                var response = new ApiResponse
                {
                    Success = false,
                    Message = "Invalid API Key"
                };

                var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await context.Response.WriteAsync(jsonResponse);
                return;
            }

            await _next(context);
        }
    }
}