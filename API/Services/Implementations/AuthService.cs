using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WuanTech.API.Data;
using WuanTech.API.DTOs;
using WuanTech.API.Helpers;
using WuanTech.API.Services.Interfaces;
using WuanTech.Models;

namespace WuanTech.API.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;
        private readonly IEmailService _emailService;
        private readonly JwtHelper _jwtHelper;

        public AuthService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<AuthService> logger,
            IEmailService emailService,
            JwtHelper jwtHelper)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _emailService = emailService;
            _jwtHelper = jwtHelper;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            try
            {
                // Check if user exists
                if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Email already exists"
                    };
                }

                if (await _context.Users.AnyAsync(u => u.Username == registerDto.Username))
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Username already exists"
                    };
                }

                // Validate password strength
                if (!PasswordHelper.IsPasswordStrong(registerDto.Password))
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Password must be at least 6 characters and contain letters and numbers"
                    };
                }

                // Create new user
                var user = new User
                {
                    Username = registerDto.Username,
                    Email = registerDto.Email,
                    PasswordHash = PasswordHelper.HashPassword(registerDto.Password),
                    FullName = registerDto.FullName,
                    PhoneNumber = registerDto.PhoneNumber,
                    Role = "Customer",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Create customer record
                var customer = new Customer
                {
                    UserId = user.Id,
                    CustomerCode = GenerateCustomerCode(),
                    Points = 0,
                    MembershipLevel = "Bronze",
                    TotalPurchased = 0
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                // Generate tokens
                var token = _jwtHelper.GenerateJwtToken(user.Id, user.Email, user.Role, user.Username);
                var refreshToken = await GenerateRefreshToken(user.Id, "127.0.0.1");

                // Send welcome email
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendWelcomeEmailAsync(user.Email, user.FullName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send welcome email");
                    }
                });

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Registration successful",
                    AccessToken = token,
                    RefreshToken = refreshToken.Token,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:ExpirationInMinutes"] ?? "60")),
                    User = MapUserToDto(user)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Registration failed"
                };
            }
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto, string ipAddress = "")
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

                if (user == null || !PasswordHelper.VerifyPassword(loginDto.Password, user.PasswordHash))
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    };
                }

                // Check if user is active
                if (!user.IsActive)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Account is deactivated. Please contact support."
                    };
                }

                // Update last login
                user.LastLogin = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Revoke old refresh tokens
                await RevokeOldRefreshTokens(user.Id);

                // Generate tokens
                var token = _jwtHelper.GenerateJwtToken(user.Id, user.Email, user.Role, user.Username);
                var refreshToken = await GenerateRefreshToken(user.Id, ipAddress);

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Login successful",
                    AccessToken = token,
                    RefreshToken = refreshToken.Token,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:ExpirationInMinutes"] ?? "60")),
                    User = MapUserToDto(user)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Login failed"
                };
            }
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string token, string ipAddress = "")
        {
            try
            {
                var refreshToken = await _context.RefreshTokens
                    .Include(rt => rt.User)
                    .FirstOrDefaultAsync(rt => rt.Token == token);

                if (refreshToken == null)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Invalid refresh token"
                    };
                }

                if (refreshToken.IsRevoked)
                {
                    // Token reuse detected - possible attack
                    await RevokeDescendantRefreshTokens(refreshToken);
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Token has been revoked"
                    };
                }

                if (refreshToken.ExpiresAt < DateTime.UtcNow)
                {
                    refreshToken.IsRevoked = true;
                    refreshToken.RevokedAt = DateTime.UtcNow;
                    refreshToken.RevokedByIp = ipAddress;
                    await _context.SaveChangesAsync();

                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Token has expired"
                    };
                }

                // Rotate refresh token
                var newRefreshToken = await RotateRefreshToken(refreshToken, ipAddress);
                var jwtToken = _jwtHelper.GenerateJwtToken(refreshToken.User.Id, refreshToken.User.Email, refreshToken.User.Role, refreshToken.User.Username);

                return new AuthResponseDto
                {
                    Success = true,
                    Message = "Token refreshed successfully",
                    AccessToken = jwtToken,
                    RefreshToken = newRefreshToken.Token,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:ExpirationInMinutes"] ?? "60")),
                    User = MapUserToDto(refreshToken.User)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Failed to refresh token"
                };
            }
        }

        public async Task<bool> LogoutAsync(int userId)
        {
            try
            {
                // Revoke all active refresh tokens for the user
                var activeTokens = await _context.RefreshTokens
                    .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                    .ToListAsync();

                foreach (var token in activeTokens)
                {
                    token.IsRevoked = true;
                    token.RevokedAt = DateTime.UtcNow;
                    token.RevokedByIp = "127.0.0.1";
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> ForgotPasswordAsync(string email)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    // Don't reveal if email exists
                    return true;
                }

                // Generate reset token
                var resetToken = GeneratePasswordResetToken();

                var passwordResetToken = new PasswordResetToken
                {
                    UserId = user.Id,
                    Token = resetToken,
                    ExpiresAt = DateTime.UtcNow.AddHours(1),
                    CreatedAt = DateTime.UtcNow,
                    IsUsed = false
                };

                _context.PasswordResetTokens.Add(passwordResetToken);
                await _context.SaveChangesAsync();

                // Send reset email
                await _emailService.SendPasswordResetEmailAsync(email, resetToken);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing forgot password for {Email}", email);
                return false;
            }
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            try
            {
                var resetToken = await _context.PasswordResetTokens
                    .Include(rt => rt.User)
                    .FirstOrDefaultAsync(rt => rt.Token == token);

                if (resetToken == null || resetToken.IsUsed || resetToken.ExpiresAt < DateTime.UtcNow)
                {
                    return false;
                }

                // Validate new password
                if (!PasswordHelper.IsPasswordStrong(newPassword))
                {
                    return false;
                }

                // Update password
                resetToken.User.PasswordHash = PasswordHelper.HashPassword(newPassword);
                resetToken.IsUsed = true;

                // Revoke all refresh tokens (force re-login)
                await RevokeOldRefreshTokens(resetToken.UserId);

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                return false;
            }
        }

        public async Task<UserDto?> GetUserByIdAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                return user == null ? null : MapUserToDto(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by id: {UserId}", userId);
                return null;
            }
        }

        public async Task<bool> RevokeTokenAsync(string token, int userId, string ipAddress = "")
        {
            try
            {
                var refreshToken = await _context.RefreshTokens
                    .FirstOrDefaultAsync(rt => rt.Token == token && rt.UserId == userId);

                if (refreshToken == null || refreshToken.IsRevoked)
                    return false;

                refreshToken.IsRevoked = true;
                refreshToken.RevokedAt = DateTime.UtcNow;
                refreshToken.RevokedByIp = ipAddress;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking token");
                return false;
            }
        }

        public async Task<bool> VerifyEmailAsync(string token)
        {
            try
            {
                // Implementation for email verification
                // For now, return true
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email");
                return false;
            }
        }

        #region Private Methods

        private async Task<RefreshToken> GenerateRefreshToken(int userId, string ipAddress)
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            var refreshToken = new RefreshToken
            {
                UserId = userId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddDays(7), // Refresh token valid for 7 days
                CreatedAt = DateTime.UtcNow,
                CreatedByIp = ipAddress,
                IsRevoked = false
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return refreshToken;
        }

        private async Task<RefreshToken> RotateRefreshToken(RefreshToken refreshToken, string ipAddress)
        {
            var newRefreshToken = await GenerateRefreshToken(refreshToken.UserId, ipAddress);

            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            refreshToken.ReplacedByToken = newRefreshToken.Token;

            await _context.SaveChangesAsync();

            return newRefreshToken;
        }

        private async Task RevokeDescendantRefreshTokens(RefreshToken refreshToken)
        {
            // If token has been compromised, revoke all descended tokens
            if (!string.IsNullOrEmpty(refreshToken.ReplacedByToken))
            {
                var descendantToken = await _context.RefreshTokens
                    .FirstOrDefaultAsync(rt => rt.Token == refreshToken.ReplacedByToken);

                if (descendantToken != null)
                {
                    descendantToken.IsRevoked = true;
                    descendantToken.RevokedAt = DateTime.UtcNow;
                    descendantToken.RevokedByIp = "System";
                    await RevokeDescendantRefreshTokens(descendantToken);
                }
            }
            await _context.SaveChangesAsync();
        }

        private async Task RevokeOldRefreshTokens(int userId)
        {
            var oldTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId &&
                            !rt.IsRevoked &&
                            rt.CreatedAt < DateTime.UtcNow.AddDays(-7))
                .ToListAsync();

            foreach (var token in oldTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedByIp = "System";
            }

            await _context.SaveChangesAsync();
        }

        private string GeneratePasswordResetToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        }

        private string GenerateCustomerCode()
        {
            return $"KH{DateTime.UtcNow:yyyyMMdd}{new Random().Next(1000, 9999)}";
        }

        private UserDto MapUserToDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                Avatar = user.Avatar,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLogin = user.LastLogin
            };
        }

        #endregion
    }
}