// Helpers/EmailTemplateHelper.cs
using System.Text;

namespace WuanTech.API.Helpers
{
    public static class EmailTemplateHelper
    {
        public static string GenerateWelcomeEmailHtml(string fullName, string username)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <title>Welcome to WuanTech Store</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .button {{ display: inline-block; padding: 12px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; }}
                        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Welcome to WuanTech Store!</h1>
                        </div>
                        <div class='content'>
                            <h2>Hello {fullName},</h2>
                            <p>Thank you for creating an account with us. Your username is: <strong>{username}</strong></p>
                            <p>You can now browse our products and start shopping!</p>
                            <center>
                                <a href='https://wuantech.com' class='button'>Start Shopping</a>
                            </center>
                        </div>
                        <div class='footer'>
                            <p>© 2024 WuanTech Store. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";
        }

        public static string GeneratePasswordResetEmailHtml(string resetLink)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <title>Password Reset - WuanTech Store</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: #dc3545; color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .button {{ display: inline-block; padding: 12px 30px; background: #dc3545; color: white; text-decoration: none; border-radius: 5px; }}
                        .warning {{ background: #fff3cd; border: 1px solid #ffc107; padding: 10px; border-radius: 5px; margin: 20px 0; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Password Reset Request</h1>
                        </div>
                        <div class='content'>
                            <p>You have requested to reset your password. Click the button below to reset it:</p>
                            <center>
                                <a href='{resetLink}' class='button'>Reset Password</a>
                            </center>
                            <div class='warning'>
                                <strong>Security Notice:</strong>
                                <ul>
                                    <li>This link will expire in 1 hour</li>
                                    <li>Do not share this link with anyone</li>
                                    <li>If you didn't request this, please ignore this email</li>
                                </ul>
                            </div>
                        </div>
                    </div>
                </body>
                </html>";
        }
    }
}