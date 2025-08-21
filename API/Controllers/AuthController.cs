using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WuanTech.API.Services.Interfaces;
using WuanTech.API.DTOs;

namespace WuanTech.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<AuthResponseDto>
                    {
                        Success = false,
                        Message = "Invalid registration data",
                        Errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                var result = await _authService.RegisterAsync(registerDto);

                if (!result.Success)
                {
                    _logger.LogWarning("User registration failed: {Message}", result.Message);
                    return BadRequest(new ApiResponse<AuthResponseDto>
                    {
                        Success = false,
                        Message = result.Message
                    });
                }

                _logger.LogInformation("User registered successfully: {Email}", registerDto.Email);
                return Ok(new ApiResponse<AuthResponseDto>
                {
                    Success = true,
                    Data = result,
                    Message = "User registered successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user: {Email}", registerDto.Email);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<AuthResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while registering the user"
                });
            }
        }

        /// <summary>
        /// Login with email and password
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<AuthResponseDto>
                    {
                        Success = false,
                        Message = "Invalid login data",
                        Errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                var ipAddress = GetIpAddress();
                var result = await _authService.LoginAsync(loginDto, ipAddress);

                if (!result.Success)
                {
                    _logger.LogWarning("User login failed: {Email}, Reason: {Message}", loginDto.Email, result.Message);
                    return Unauthorized(new ApiResponse<AuthResponseDto>
                    {
                        Success = false,
                        Message = result.Message
                    });
                }

                _logger.LogInformation("User logged in successfully: {Email}", loginDto.Email);
                return Ok(new ApiResponse<AuthResponseDto>
                {
                    Success = true,
                    Data = result,
                    Message = "Login successful"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging in user: {Email}", loginDto.Email);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<AuthResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while logging in the user"
                });
            }
        }

        /// <summary>
        /// Refresh access token
        /// </summary>
        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<AuthResponseDto>
                    {
                        Success = false,
                        Message = "Invalid refresh token data",
                        Errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                var ipAddress = GetIpAddress();
                var result = await _authService.RefreshTokenAsync(refreshTokenDto.RefreshToken, ipAddress);

                if (!result.Success)
                {
                    _logger.LogWarning("Token refresh failed: {Message}", result.Message);
                    return Unauthorized(new ApiResponse<AuthResponseDto>
                    {
                        Success = false,
                        Message = result.Message
                    });
                }

                return Ok(new ApiResponse<AuthResponseDto>
                {
                    Success = true,
                    Data = result,
                    Message = "Token refreshed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<AuthResponseDto>
                {
                    Success = false,
                    Message = "An error occurred while refreshing the token"
                });
            }
        }

        /// <summary>
        /// Logout current user
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse>> Logout()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Unauthorized(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid user session"
                    });
                }

                await _authService.LogoutAsync(userId);
                _logger.LogInformation("User logged out successfully: {UserId}", userId);

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Logout successful"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging out user");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse
                {
                    Success = false,
                    Message = "An error occurred while logging out the user"
                });
            }
        }

        /// <summary>
        /// Request password reset
        /// </summary>  
        [HttpPost("forgot-password")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse>> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid email format",
                        Errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                var result = await _authService.ForgotPasswordAsync(forgotPasswordDto.Email);

                if (!result)
                {
                    _logger.LogWarning("Password reset request failed for email: {Email}", forgotPasswordDto.Email);
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = "Email not found"
                    });
                }

                _logger.LogInformation("Password reset email sent to: {Email}", forgotPasswordDto.Email);
                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Password reset email sent successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting password reset for email: {Email}", forgotPasswordDto.Email);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse
                {
                    Success = false,
                    Message = "An error occurred while requesting password reset"
                });
            }
        }

        /// <summary>
        /// Reset password using token
        /// </summary>
        [HttpPost("reset-password")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse>> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid reset password data",
                        Errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                var result = await _authService.ResetPasswordAsync(resetPasswordDto.Token, resetPasswordDto.NewPassword);

                if (!result)
                {
                    _logger.LogWarning("Password reset failed for token: {Token}", resetPasswordDto.Token[..8] + "...");
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid or expired reset token"
                    });
                }

                _logger.LogInformation("Password reset successful for token: {Token}", resetPasswordDto.Token[..8] + "...");
                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Password reset successful"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse
                {
                    Success = false,
                    Message = "An error occurred while resetting the password"
                });
            }
        }

        /// <summary>
        /// Get current user info
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Unauthorized(new ApiResponse<UserDto>
                    {
                        Success = false,
                        Message = "Invalid user session"
                    });
                }

                var user = await _authService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return Unauthorized(new ApiResponse<UserDto>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                return Ok(new ApiResponse<UserDto>
                {
                    Success = true,
                    Data = user,
                    Message = "User information retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user info");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving user information"
                });
            }
        }

        /// <summary>
        /// Revoke refresh token
        /// </summary>
        [HttpPost("revoke-token")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse>> RevokeToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid token data"
                    });
                }

                var userId = GetCurrentUserId();
                var ipAddress = GetIpAddress();

                var result = await _authService.RevokeTokenAsync(refreshTokenDto.RefreshToken, userId, ipAddress);

                if (!result)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid token or token already revoked"
                    });
                }

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Token revoked successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking token");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse
                {
                    Success = false,
                    Message = "An error occurred while revoking the token"
                });
            }
        }

        /// <summary>
        /// Verify email address
        /// </summary>
        [HttpPost("verify-email")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ApiResponse>> VerifyEmail([FromQuery] string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Verification token is required"
                    });
                }

                var result = await _authService.VerifyEmailAsync(token);

                if (!result)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Invalid or expired verification token"
                    });
                }

                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = "Email verified successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying email");
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse
                {
                    Success = false,
                    Message = "An error occurred while verifying email"
                });
            }
        }

        #region Helper Methods

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private string GetIpAddress()
        {
            // Check for X-Forwarded-For header first (in case of proxy/load balancer)
            var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                // X-Forwarded-For can contain multiple IPs, take the first one
                return forwardedFor.Split(',')[0].Trim();
            }

            // Check for X-Real-IP header
            var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            // Fall back to RemoteIpAddress
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        #endregion
    }
}