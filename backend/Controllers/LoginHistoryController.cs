using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Models;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LoginHistoryController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public LoginHistoryController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetLoginHistory()
        {
            var history = await _dbContext.LoginHistory
                .Include(l => l.User)
                .OrderByDescending(l => l.LoginTime)
                .Select(l => new
                {
                    l.Id,
                    l.LoginTime,
                    l.IpAddress,
                    l.UserAgent,
                    l.IsSuccessful,
                    User = l.User == null ? null : new
                    {
                        l.User.Id,
                        l.User.Matricule,
                        l.User.FirstName,
                        l.User.LastName
                    }
                })
                .Take(100)
                .ToListAsync();

            return Ok(history);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserLoginHistory(int userId)
        {
            var history = await _dbContext.LoginHistory
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.LoginTime)
                .Select(l => new
                {
                    l.Id,
                    l.LoginTime,
                    l.IpAddress,
                    l.UserAgent,
                    l.IsSuccessful
                })
                .Take(50)
                .ToListAsync();

            return Ok(history);
        }

        [HttpGet("my-history")]
        public async Task<IActionResult> GetMyLoginHistory()
        {
            if (!int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId))
            {
                return BadRequest(new { Message = "Invalid user ID" });
            }

            var history = await _dbContext.LoginHistory
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.LoginTime)
                .Select(l => new
                {
                    l.Id,
                    l.LoginTime,
                    l.IpAddress,
                    l.UserAgent,
                    l.IsSuccessful
                })
                .Take(20)
                .ToListAsync();

            return Ok(history);
        }
    }
} 