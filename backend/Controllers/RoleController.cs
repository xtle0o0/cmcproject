using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using backend.Data;
using backend.Models;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin")]
    public class RoleController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<RoleController> _logger;

        public RoleController(
            AppDbContext dbContext,
            ILogger<RoleController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRoles()
        {
            var roles = await _dbContext.Roles.ToListAsync();
            return Ok(roles);
        }
        
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserRoles(int userId)
        {
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }
            
            var userRoles = await _dbContext.UserRoles
                .Where(ur => ur.UserId == userId)
                .Include(ur => ur.Role)
                .Select(ur => ur.Role)
                .ToListAsync();
                
            return Ok(userRoles);
        }
        
        [HttpPost("assign")]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request)
        {
            var user = await _dbContext.Users.FindAsync(request.UserId);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }
            
            var role = await _dbContext.Roles.FindAsync(request.RoleId);
            if (role == null)
            {
                return NotFound(new { Message = "Role not found" });
            }
            
            // Check if user already has this role
            var existingUserRole = await _dbContext.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == request.UserId && ur.RoleId == request.RoleId);
                
            if (existingUserRole != null)
            {
                return BadRequest(new { Message = "User already has this role" });
            }
            
            var userRole = new UserRole
            {
                UserId = request.UserId,
                RoleId = request.RoleId
            };
            
            await _dbContext.UserRoles.AddAsync(userRole);
            await _dbContext.SaveChangesAsync();
            
            return Ok(new { Message = "Role assigned successfully" });
        }
        
        [HttpDelete("remove")]
        public async Task<IActionResult> RemoveRole([FromBody] AssignRoleRequest request)
        {
            var userRole = await _dbContext.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == request.UserId && ur.RoleId == request.RoleId);
                
            if (userRole == null)
            {
                return NotFound(new { Message = "User does not have this role" });
            }
            
            _dbContext.UserRoles.Remove(userRole);
            await _dbContext.SaveChangesAsync();
            
            return Ok(new { Message = "Role removed successfully" });
        }
    }
    
    public class AssignRoleRequest
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }
    }
} 