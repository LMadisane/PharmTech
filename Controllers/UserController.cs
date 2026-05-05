using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using System.Security.Claims;

namespace PharmTech.Controllers
{
    [Authorize]  // Any logged-in user (Admin, Doctor, Pharmacist) can access
    public class UserController : Controller
    {
        private readonly PharmTechContext _context;
        private readonly ILogger<UserController> _logger;

        public UserController(PharmTechContext context, ILogger<UserController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/users/current
        [HttpGet("api/users/current")]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var user = await _context.Users
                    .Include(u => u.Facility)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                    return Ok(new { success = false, message = "User not found" });

                return Ok(new
                {
                    success = true,
                    user = new
                    {
                        user.UserId,
                        user.Name,
                        user.Email,
                        user.Role,
                        user.IsActive
                    },
                    facility = user.Facility != null ? new
                    {
                        user.Facility.FacilityId,
                        user.Facility.Name
                    } : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return Ok(new { success = false, message = "An error occurred" });
            }
        }
    }
}