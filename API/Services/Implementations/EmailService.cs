using System.Net;
using System.Net.Mail;
using System.Text;
using WuanTech.API.Services.Interfaces;
using WuanTech.Models;

namespace WuanTech.API.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly SmtpClient _smtpClient;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly string _appUrl;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // Configure SMTP client
            var smtpServer = _configuration["Email:SmtpServer"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var smtpUsername = _configuration["Email:SmtpUsername"];
            var smtpPassword = _configuration["Email:SmtpPassword"];

            _fromEmail = smtpUsername;
            _fromName = "WuanTech Store";
            _appUrl = _configuration["AppUrl"] ?? "https://wuantech.com";

            _smtpClient = new SmtpClient(smtpServer)
            {
                Port = smtpPort,
                Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };
        }

        public async Task SendOrderConfirmationEmailAsync(string email, Order order)
        {
            try
            {
                var subject = $"Xác nhận đơn hàng #{order?.OrderNumber ?? "N/A"} - WuanTech Store";
                var body = BuildOrderConfirmationEmail(order);

                await SendEmailAsync(email, subject, body, true);

                _logger.LogInformation("Order confirmation email sent to {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order confirmation email to {Email}", email);
                throw;
            }
        }

        public async Task SendPasswordResetEmailAsync(string email, string resetToken)
        {
            try
            {
                var resetUrl = $"{_appUrl}/reset-password?token={resetToken}";
                var subject = "Reset mật khẩu - WuanTech Store";

                var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .button {{ display: inline-block; padding: 12px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                        .button:hover {{ background: #5a67d8; }}
                        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
                        .warning {{ background: #fff3cd; border: 1px solid #ffc107; padding: 10px; border-radius: 5px; margin-top: 20px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Reset Mật Khẩu</h1>
                        </div>
                        <div class='content'>
                            <p>Xin chào,</p>
                            <p>Chúng tôi nhận được yêu cầu reset mật khẩu cho tài khoản của bạn tại WuanTech Store.</p>
                            <p>Vui lòng click vào nút bên dưới để đặt lại mật khẩu:</p>
                            <center>
                                <a href='{resetUrl}' class='button'>Reset Mật Khẩu</a>
                            </center>
                            <p><small>Hoặc copy link sau: {resetUrl}</small></p>
                            <div class='warning'>
                                <strong>⚠️ Lưu ý:</strong>
                                <ul>
                                    <li>Link này sẽ hết hạn sau 1 giờ</li>
                                    <li>Không chia sẻ link này với bất kỳ ai</li>
                                    <li>Nếu bạn không yêu cầu reset mật khẩu, vui lòng bỏ qua email này</li>
                                </ul>
                            </div>
                        </div>
                        <div class='footer'>
                            <p>© 2024 WuanTech Store. All rights reserved.</p>
                            <p>📧 support@wuantech.com | 📞 1900-xxxx</p>
                        </div>
                    </div>
                </body>
                </html>";

                await SendEmailAsync(email, subject, body, true);

                _logger.LogInformation("Password reset email sent to {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email to {Email}", email);
                throw;
            }
        }

        public async Task SendWelcomeEmailAsync(string email, string fullName)
        {
            try
            {
                var subject = "Chào mừng bạn đến với WuanTech Store!";

                var body = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 40px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .features {{ margin: 20px 0; }}
                        .feature {{ background: white; padding: 15px; margin: 10px 0; border-radius: 5px; border-left: 4px solid #667eea; }}
                        .button {{ display: inline-block; padding: 12px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                        .button:hover {{ background: #5a67d8; }}
                        .coupon {{ background: #28a745; color: white; padding: 15px; border-radius: 5px; text-align: center; margin: 20px 0; }}
                        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Chào mừng {fullName}!</h1>
                            <p>Cảm ơn bạn đã đăng ký tài khoản tại WuanTech Store</p>
                        </div>
                        <div class='content'>
                            <h2>🎉 Tài khoản của bạn đã được tạo thành công!</h2>
                            
                            <div class='features'>
                                <h3>Với tài khoản WuanTech, bạn có thể:</h3>
                                <div class='feature'>
                                    <strong>🛍️ Mua sắm dễ dàng</strong>
                                    <p>Truy cập hàng ngàn sản phẩm công nghệ chính hãng với giá tốt nhất</p>
                                </div>
                                <div class='feature'>
                                    <strong>🚚 Theo dõi đơn hàng</strong>
                                    <p>Cập nhật trạng thái đơn hàng và thông tin giao hàng realtime</p>
                                </div>
                                <div class='feature'>
                                    <strong>💰 Nhận ưu đãi độc quyền</strong>
                                    <p>Khuyến mãi và giảm giá đặc biệt chỉ dành cho thành viên</p>
                                </div>
                                <div class='feature'>
                                    <strong>⭐ Tích điểm thưởng</strong>
                                    <p>Tích lũy điểm với mỗi đơn hàng và đổi thành voucher giảm giá</p>
                                </div>
                            </div>
                            
                            <div class='coupon'>
                                <h3>🎁 QUÀ TẶNG CHÀO MỪNG</h3>
                                <p style='font-size: 24px; margin: 10px 0;'><strong>WELCOME10</strong></p>
                                <p>Giảm 10% cho đơn hàng đầu tiên (Tối đa 500.000đ)</p>
                            </div>
                            
                            <center>
                                <a href='{_appUrl}' class='button'>Bắt Đầu Mua Sắm</a>
                            </center>
                            
                            <p style='margin-top: 30px;'>
                                <strong>Cần hỗ trợ?</strong><br>
                                Đội ngũ chăm sóc khách hàng của chúng tôi luôn sẵn sàng hỗ trợ bạn.<br>
                                Email: support@wuantech.com | Hotline: 1900-xxxx
                            </p>
                        </div>
                        <div class='footer'>
                            <p>© 2024 WuanTech Store. All rights reserved.</p>
                            <p>Bạn nhận được email này vì đã đăng ký tài khoản tại WuanTech Store</p>
                        </div>
                    </div>
                </body>
                </html>";

                await SendEmailAsync(email, subject, body, true);

                _logger.LogInformation("Welcome email sent to {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending welcome email to {Email}", email);
                throw;
            }
        }

        // =======================================
        // THÊM 2 METHODS THIẾU TRONG INTERFACE
        // =======================================

        public async Task SendOrderStatusUpdateEmailAsync(string email, Order order)
        {
            try
            {
                var subject = $"Cập nhật đơn hàng #{order.OrderNumber} - {GetStatusDisplayName(order.Status)}";
                var body = BuildOrderStatusUpdateEmail(order);

                await SendEmailAsync(email, subject, body, true);

                _logger.LogInformation("Order status update email sent to {Email} for order {OrderNumber}",
                    email, order.OrderNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order status update email to {Email}", email);
                throw;
            }
        }

        public async Task SendNewsletterAsync(string email, string subject, string content)
        {
            try
            {
                var newsletterBody = BuildNewsletterEmail(subject, content);
                await SendEmailAsync(email, subject, newsletterBody, true);

                _logger.LogInformation("Newsletter sent to {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending newsletter to {Email}", email);
                throw;
            }
        }

        // =======================================
        // HELPER METHODS
        // =======================================

        private string GetStatusDisplayName(string status)
        {
            return status.ToUpper() switch
            {
                "PENDING" => "Chờ xử lý",
                "CONFIRMED" => "Đã xác nhận",
                "PROCESSING" => "Đang xử lý",
                "SHIPPED" => "Đã giao vận",
                "DELIVERED" => "Đã giao hàng",
                "CANCELLED" => "Đã hủy",
                _ => "Cập nhật trạng thái"
            };
        }

        private string BuildOrderStatusUpdateEmail(Order order)
        {
            var statusMessage = order.Status.ToUpper() switch
            {
                "CONFIRMED" => "Đơn hàng của bạn đã được xác nhận và đang được chuẩn bị.",
                "PROCESSING" => "Đơn hàng của bạn đang được xử lý tại kho.",
                "SHIPPED" => $"Đơn hàng của bạn đã được giao cho đơn vị vận chuyển. Mã vận đơn: {order.TrackingNumber}",
                "DELIVERED" => "Đơn hàng của bạn đã được giao thành công. Cảm ơn bạn đã mua hàng!",
                "CANCELLED" => $"Đơn hàng của bạn đã bị hủy. Lý do: {order.CancelReason ?? "Không có lý do cụ thể"}",
                _ => "Trạng thái đơn hàng của bạn đã được cập nhật."
            };

            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .status-update {{ background: #e3f2fd; border-left: 4px solid #2196f3; padding: 15px; margin: 20px 0; }}
                        .button {{ display: inline-block; padding: 12px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Cập nhật đơn hàng</h1>
                            <p>#{order.OrderNumber}</p>
                        </div>
                        <div class='content'>
                            <div class='status-update'>
                                <h3>📦 Trạng thái: {GetStatusDisplayName(order.Status)}</h3>
                                <p>{statusMessage}</p>
                                <p><strong>Ngày cập nhật:</strong> {order.UpdatedAt:dd/MM/yyyy HH:mm}</p>
                            </div>
                            
                            <center>
                                <a href='{_appUrl}/orders/{order.Id}' class='button'>Xem Chi Tiết Đơn Hàng</a>
                            </center>
                        </div>
                        <div class='footer'>
                            <p>© 2024 WuanTech Store. All rights reserved.</p>
                            <p>📧 support@wuantech.com | 📞 1900-xxxx</p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        private string BuildNewsletterEmail(string subject, string content)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>WuanTech Store</h1>
                            <p>Newsletter</p>
                        </div>
                        <div class='content'>
                            <h2>{subject}</h2>
                            <div>{content}</div>
                        </div>
                        <div class='footer'>
                            <p>© 2024 WuanTech Store. All rights reserved.</p>
                            <p>📧 support@wuantech.com | 📞 1900-xxxx</p>
                            <p><a href='{_appUrl}/unsubscribe'>Hủy đăng ký nhận email</a></p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        private string BuildOrderConfirmationEmail(Order order)
        {
            var itemsHtml = new StringBuilder();
            decimal subtotal = 0;

            if (order?.OrderItems != null)
            {
                foreach (var item in order.OrderItems)
                {
                    subtotal += item.TotalPrice;
                    itemsHtml.Append($@"
                        <tr>
                            <td style='padding: 10px; border-bottom: 1px solid #ddd;'>{item.Product?.Name ?? "N/A"}</td>
                            <td style='padding: 10px; border-bottom: 1px solid #ddd; text-align: center;'>{item.Quantity}</td>
                            <td style='padding: 10px; border-bottom: 1px solid #ddd; text-align: right;'>{item.UnitPrice:N0}đ</td>
                            <td style='padding: 10px; border-bottom: 1px solid #ddd; text-align: right;'>{item.TotalPrice:N0}đ</td>
                        </tr>");
                }
            }

            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
                        .container {{ max-width: 700px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .order-info {{ background: white; padding: 20px; border-radius: 5px; margin: 20px 0; }}
                        .order-details {{ margin: 20px 0; }}
                        table {{ width: 100%; border-collapse: collapse; background: white; }}
                        th {{ background: #e9ecef; padding: 10px; text-align: left; }}
                        .button {{ display: inline-block; padding: 12px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                        .button:hover {{ background: #5a67d8; }}
                        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
                        .success-icon {{ color: #28a745; font-size: 48px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <div class='success-icon'>✓</div>
                            <h1>Đặt Hàng Thành Công!</h1>
                            <p>Cảm ơn bạn đã mua hàng tại WuanTech Store</p>
                        </div>
                        <div class='content'>
                            <div class='order-info'>
                                <h2>Thông tin đơn hàng</h2>
                                <p><strong>Mã đơn hàng:</strong> #{order?.OrderNumber ?? "N/A"}</p>
                                <p><strong>Ngày đặt:</strong> {order?.OrderDate:dd/MM/yyyy HH:mm}</p>
                                <p><strong>Địa chỉ giao hàng:</strong> {order?.ShippingAddress ?? "N/A"}</p>
                                <p><strong>Phương thức thanh toán:</strong> {order?.PaymentMethod ?? "N/A"}</p>
                                <p><strong>Trạng thái:</strong> <span style='color: #28a745;'>Đang xử lý</span></p>
                            </div>
                            
                            <div class='order-details'>
                                <h3>Chi tiết sản phẩm</h3>
                                <table>
                                    <thead>
                                        <tr>
                                            <th>Sản phẩm</th>
                                            <th style='text-align: center;'>SL</th>
                                            <th style='text-align: right;'>Đơn giá</th>
                                            <th style='text-align: right;'>Thành tiền</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {itemsHtml}
                                    </tbody>
                                    <tfoot>
                                        <tr>
                                            <td colspan='3' style='padding: 10px; text-align: right;'><strong>Tạm tính:</strong></td>
                                            <td style='padding: 10px; text-align: right;'>{subtotal:N0}đ</td>
                                        </tr>
                                        <tr>
                                            <td colspan='3' style='padding: 10px; text-align: right;'>Phí vận chuyển:</td>
                                            <td style='padding: 10px; text-align: right;'>{order?.ShippingFee:N0}đ</td>
                                        </tr>
                                        {(order?.Discount > 0 ? $@"
                                        <tr>
                                            <td colspan='3' style='padding: 10px; text-align: right;'>Giảm giá:</td>
                                            <td style='padding: 10px; text-align: right; color: #28a745;'>-{order?.Discount:N0}đ</td>
                                        </tr>" : "")}
                                        <tr style='background: #e9ecef; font-size: 18px;'>
                                            <td colspan='3' style='padding: 15px; text-align: right;'><strong>Tổng cộng:</strong></td>
                                            <td style='padding: 15px; text-align: right; color: #667eea;'><strong>{order?.TotalAmount:N0}đ</strong></td>
                                        </tr>
                                    </tfoot>
                                </table>
                            </div>
                            
                            <center>
                                <a href='{_appUrl}/orders/{order?.Id}' class='button'>Theo Dõi Đơn Hàng</a>
                            </center>
                            
                            <div style='background: #d4edda; border: 1px solid #c3e6cb; padding: 15px; border-radius: 5px; margin-top: 20px;'>
                                <strong>📦 Thông tin giao hàng:</strong>
                                <ul>
                                    <li>Thời gian giao hàng dự kiến: 2-3 ngày làm việc</li>
                                    <li>Bạn sẽ nhận được SMS/Email khi đơn hàng được giao cho đơn vị vận chuyển</li>
                                    <li>Vui lòng kiểm tra hàng trước khi thanh toán</li>
                                </ul>
                            </div>
                        </div>
                        <div class='footer'>
                            <p>© 2024 WuanTech Store. All rights reserved.</p>
                            <p>📧 support@wuantech.com | 📞 1900-xxxx</p>
                            <p>Bạn nhận được email này vì đã đặt hàng tại WuanTech Store</p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        private async Task SendEmailAsync(string to, string subject, string body, bool isHtml = false)
        {
            try
            {
                using var message = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8
                };

                message.To.Add(new MailAddress(to));

                await _smtpClient.SendMailAsync(message);

                _logger.LogInformation("Email sent successfully to {Email}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", to);
                throw;
            }
        }

        public void Dispose()
        {
            _smtpClient?.Dispose();
        }
    }
}