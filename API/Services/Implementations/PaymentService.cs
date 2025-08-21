using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json;
using WuanTech.API.DTOs;
using WuanTech.API.Services.Interfaces;

namespace WuanTech.API.Services.Implementations
{
    public class PaymentService : IPaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentService> _logger;
        private readonly HttpClient _httpClient;

        public PaymentService(IConfiguration configuration, ILogger<PaymentService> logger, HttpClient httpClient)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<PaymentResultDto> ProcessPaymentAsync(PaymentRequestDto request)
        {
            try
            {
                _logger.LogInformation("Processing payment for order {OrderNumber} with method {PaymentMethod}",
                    request.OrderNumber, request.PaymentMethod);

                return request.PaymentMethod.ToLower() switch
                {
                    "stripe" => await ProcessStripePaymentAsync(request),
                    "paypal" => await ProcessPayPalPaymentAsync(request),
                    "vnpay" => await ProcessVNPayPaymentAsync(request),
                    "cod" => ProcessCODPayment(request),
                    _ => new PaymentResultDto
                    {
                        Success = false,
                        Message = "Unsupported payment method",
                        ProcessedAt = DateTime.UtcNow
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment for order {OrderNumber}", request.OrderNumber);
                return new PaymentResultDto
                {
                    Success = false,
                    Message = "Payment processing failed",
                    ProcessedAt = DateTime.UtcNow
                };
            }
        }

        public async Task<PaymentResultDto> ProcessStripePaymentAsync(PaymentRequestDto request)
        {
            try
            {
                // Simulate Stripe payment processing
                await Task.Delay(1000);

                var isSuccess = new Random().Next(1, 11) > 2; // 80% success rate for demo

                return new PaymentResultDto
                {
                    Success = isSuccess,
                    TransactionId = $"stripe_{Guid.NewGuid():N}",
                    Message = isSuccess ? "Payment processed successfully via Stripe" : "Payment failed - Insufficient funds",
                    ProcessedAt = DateTime.UtcNow,
                    PaymentUrl = isSuccess ? null : $"https://stripe.com/retry/{request.OrderNumber}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Stripe payment");
                throw;
            }
        }

        public async Task<PaymentResultDto> ProcessPayPalPaymentAsync(PaymentRequestDto request)
        {
            try
            {
                // Simulate PayPal payment processing
                await Task.Delay(1500);

                var isSuccess = new Random().Next(1, 11) > 3; // 70% success rate for demo

                return new PaymentResultDto
                {
                    Success = isSuccess,
                    TransactionId = $"paypal_{Guid.NewGuid():N}",
                    Message = isSuccess ? "Payment processed successfully via PayPal" : "Payment failed - Please try again",
                    ProcessedAt = DateTime.UtcNow,
                    PaymentUrl = isSuccess ? null : $"https://paypal.com/checkout/{request.OrderNumber}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PayPal payment");
                throw;
            }
        }

        public async Task<PaymentResultDto> ProcessVNPayPaymentAsync(PaymentRequestDto request)
        {
            try
            {
                // VNPay integration
                var vnpayUrl = GenerateVNPayPaymentUrl(request);

                return new PaymentResultDto
                {
                    Success = true,
                    TransactionId = $"vnpay_{DateTime.UtcNow:yyyyMMddHHmmss}",
                    Message = "Redirect to VNPay for payment",
                    ProcessedAt = DateTime.UtcNow,
                    PaymentUrl = vnpayUrl
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing VNPay payment");
                throw;
            }
        }

        private PaymentResultDto ProcessCODPayment(PaymentRequestDto request)
        {
            return new PaymentResultDto
            {
                Success = true,
                TransactionId = $"cod_{Guid.NewGuid():N}",
                Message = "Cash on Delivery order confirmed",
                ProcessedAt = DateTime.UtcNow
            };
        }

        public async Task<bool> RefundPaymentAsync(string transactionId, decimal amount)
        {
            try
            {
                _logger.LogInformation("Processing refund for transaction {TransactionId}, amount {Amount}",
                    transactionId, amount);

                // Determine payment method from transaction ID
                if (transactionId.StartsWith("stripe_"))
                {
                    return await ProcessStripeRefundAsync(transactionId, amount);
                }
                else if (transactionId.StartsWith("paypal_"))
                {
                    return await ProcessPayPalRefundAsync(transactionId, amount);
                }
                else if (transactionId.StartsWith("vnpay_"))
                {
                    return await ProcessVNPayRefundAsync(transactionId, amount);
                }
                else if (transactionId.StartsWith("cod_"))
                {
                    // COD refunds are handled manually
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refund for transaction {TransactionId}", transactionId);
                return false;
            }
        }

        public async Task<PaymentStatusDto> GetPaymentStatusAsync(string transactionId)
        {
            try
            {
                // Simulate checking payment status
                await Task.Delay(500);

                var statuses = new[] { "PENDING", "COMPLETED", "FAILED", "REFUNDED" };
                var randomStatus = statuses[new Random().Next(statuses.Length)];

                return new PaymentStatusDto
                {
                    TransactionId = transactionId,
                    Status = randomStatus,
                    Amount = 1000000, // Demo amount
                    Currency = "VND",
                    ProcessedAt = DateTime.UtcNow.AddMinutes(-new Random().Next(1, 60))
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment status for transaction {TransactionId}", transactionId);
                throw;
            }
        }

        public async Task<bool> ValidatePaymentAsync(string transactionId)
        {
            try
            {
                var status = await GetPaymentStatusAsync(transactionId);
                return status.Status == "COMPLETED";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating payment for transaction {TransactionId}", transactionId);
                return false;
            }
        }

        #region Private Methods

        private async Task<bool> ProcessStripeRefundAsync(string transactionId, decimal amount)
        {
            try
            {
                // Implement Stripe refund logic
                await Task.Delay(1000);
                return new Random().Next(1, 11) > 2; // 80% success rate
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Stripe refund");
                return false;
            }
        }

        private async Task<bool> ProcessPayPalRefundAsync(string transactionId, decimal amount)
        {
            try
            {
                // Implement PayPal refund logic
                await Task.Delay(1000);
                return new Random().Next(1, 11) > 3; // 70% success rate
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PayPal refund");
                return false;
            }
        }

        private async Task<bool> ProcessVNPayRefundAsync(string transactionId, decimal amount)
        {
            try
            {
                // Implement VNPay refund logic
                await Task.Delay(1000);
                return true; // VNPay refunds are usually manual
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing VNPay refund");
                return false;
            }
        }

        private string GenerateVNPayPaymentUrl(PaymentRequestDto request)
        {
            var vnpayConfig = _configuration.GetSection("Payment:VNPay");
            var tmnCode = vnpayConfig["TmnCode"];
            var hashSecret = vnpayConfig["HashSecret"];
            var paymentUrl = vnpayConfig["PaymentUrl"];
            var returnUrl = vnpayConfig["ReturnUrl"];

            var vnp_Params = new SortedDictionary<string, string>
            {
                ["vnp_Version"] = "2.1.0",
                ["vnp_Command"] = "pay",
                ["vnp_TmnCode"] = tmnCode!,
                ["vnp_Amount"] = ((long)(request.Amount * 100)).ToString(),
                ["vnp_CurrCode"] = "VND",
                ["vnp_TxnRef"] = request.OrderNumber,
                ["vnp_OrderInfo"] = $"Thanh toan don hang {request.OrderNumber}",
                ["vnp_OrderType"] = "other",
                ["vnp_Locale"] = "vn",
                ["vnp_ReturnUrl"] = returnUrl!,
                ["vnp_IpAddr"] = "127.0.0.1",
                ["vnp_CreateDate"] = DateTime.UtcNow.ToString("yyyyMMddHHmmss")
            };

            var query = string.Join("&", vnp_Params.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            var hashData = ComputeHmacSha512(hashSecret!, query);

            return $"{paymentUrl}?{query}&vnp_SecureHash={hashData}";
        }

        private static string ComputeHmacSha512(string key, string data)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var dataBytes = Encoding.UTF8.GetBytes(data);

            using var hmac = new HMACSHA512(keyBytes);
            var hashBytes = hmac.ComputeHash(dataBytes);
            return Convert.ToHexString(hashBytes).ToLower();
        }

        #endregion
    }
}