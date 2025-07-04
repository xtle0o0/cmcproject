using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using backend.Data;
using backend.Models;
using backend.Services;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly TokenService _tokenService;
        private readonly PasswordService _passwordService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            AppDbContext dbContext,
            TokenService tokenService,
            PasswordService passwordService,
            ILogger<AuthController> logger)
        {
            _dbContext = dbContext;
            _tokenService = tokenService;
            _passwordService = passwordService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var user = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Matricule == request.Matricule);

                var loginHistory = new LoginHistory
                {
                    UserId = user?.Id ?? 0,
                    LoginTime = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers.UserAgent.ToString(),
                    IsSuccessful = false
                };

                if (user == null)
                {
                    _logger.LogWarning("Login failed: User with matricule {Matricule} not found", request.Matricule);
                    
                    // Still record the login attempt but with userId 0
                    await _dbContext.LoginHistory.AddAsync(loginHistory);
                    await _dbContext.SaveChangesAsync();
                    
                    return Unauthorized(new { Message = "Invalid credentials" });
                }

                if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Login failed: Invalid password for user {Matricule}", request.Matricule);
                    
                    loginHistory.UserId = user.Id;
                    await _dbContext.LoginHistory.AddAsync(loginHistory);
                    await _dbContext.SaveChangesAsync();
                    
                    return Unauthorized(new { Message = "Invalid credentials" });
                }

                // Login successful
                var accessToken = await _tokenService.GenerateAccessToken(user);
                var refreshToken = _tokenService.GenerateRefreshToken();

                // Update user with refresh token
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                
                // Update login history for successful login
                loginHistory.IsSuccessful = true;
                
                await _dbContext.LoginHistory.AddAsync(loginHistory);
                await _dbContext.SaveChangesAsync();
                
                // Set secure cookie with refresh token
                Response.Cookies.Append("X-Refresh-Token", refreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                });

                // Get user roles
                var userRoles = await _dbContext.UserRoles
                    .Where(ur => ur.UserId == user.Id)
                    .Include(ur => ur.Role)
                    .Select(ur => ur.Role.Name)
                    .ToListAsync();

                return Ok(new AuthResponse
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Matricule = user.Matricule,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,  // Sending in response for non-cookie clients
                    Roles = userRoles
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, new { Message = "An error occurred during login" });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            try
            {
                // Try to get refresh token from cookie first, fall back to request body
                var refreshToken = Request.Cookies["X-Refresh-Token"] ?? request.RefreshToken;
                
                if (string.IsNullOrEmpty(refreshToken))
                {
                    return BadRequest(new { Message = "Refresh token is required" });
                }

                var user = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

                if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                {
                    return Unauthorized(new { Message = "Invalid or expired refresh token" });
                }

                var newAccessToken = await _tokenService.GenerateAccessToken(user);
                var newRefreshToken = _tokenService.GenerateRefreshToken();

                user.RefreshToken = newRefreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                
                await _dbContext.SaveChangesAsync();
                
                // Update the refresh token cookie
                Response.Cookies.Append("X-Refresh-Token", newRefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                });

                // Get user roles
                var userRoles = await _dbContext.UserRoles
                    .Where(ur => ur.UserId == user.Id)
                    .Include(ur => ur.Role)
                    .Select(ur => ur.Role.Name)
                    .ToListAsync();

                return Ok(new AuthResponse
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Matricule = user.Matricule,
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    Roles = userRoles
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return StatusCode(500, new { Message = "An error occurred during token refresh" });
            }
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userId, out var id))
                {
                    return BadRequest(new { Message = "Invalid user ID" });
                }

                var user = await _dbContext.Users.FindAsync(id);
                if (user != null)
                {
                    user.RefreshToken = null;
                    await _dbContext.SaveChangesAsync();
                }

                // Clear the refresh token cookie
                Response.Cookies.Delete("X-Refresh-Token");

                return Ok(new { Message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { Message = "An error occurred during logout" });
            }
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userId, out var id))
                {
                    return BadRequest(new { Message = "Invalid user ID" });
                }

                var user = await _dbContext.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { Message = "User not found" });
                }

                // Get user roles
                var userRoles = await _dbContext.UserRoles
                    .Where(ur => ur.UserId == id)
                    .Include(ur => ur.Role)
                    .Select(ur => ur.Role.Name)
                    .ToListAsync();

                return Ok(new
                {
                    user.Id,
                    user.FirstName,
                    user.LastName,
                    user.Matricule,
                    Roles = userRoles
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, new { Message = "An error occurred while getting user information" });
            }
        }
    }
}
