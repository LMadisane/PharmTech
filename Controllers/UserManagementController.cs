using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;

namespace PharmTech.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // Only Admin can manage users
    public class UserManagementController(PharmTechContext context) : ControllerBase
    {
        private readonly PharmTechContext _context = context;

        // Create a new Doctor or Pharmacist account
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] User user)
        {
            // Only Doctor and Pharmacist accounts can be created here
            var allowedRoles = new[] { "Doctor", "Pharmacist" };
            if (!allowedRoles.Contains(user.Role))
                return BadRequest("You can only create Doctor or Pharmacist accounts.");

            // Check if email is already in use
            var exists = await _context.Users.AnyAsync(u => u.Email == user.Email);
            if (exists)
                return BadRequest("A user with this email already exists.");

            // Account is active by default
            user.IsActive = true;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User created successfully", user });
        }

        // Get all users
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.UserId,
                    u.Name,
                    u.Email,
                    u.Role,
                    u.IsActive,
                    u.FacilityId
                })
                .ToListAsync();

            return Ok(users);
        }

        // Deactivate a user account
        [HttpPut("{id}/deactivate")]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                return NotFound();

            // Prevent deactivating an Admin account
            if (user.Role == "Admin")
                return BadRequest("Admin accounts cannot be deactivated.");

            if (!user.IsActive)
                return BadRequest("User is already inactive.");

            user.IsActive = false;

            await _context.SaveChangesAsync();

            return Ok(new { message = "User deactivated successfully" });
        }

        // Reactivate a user account
        [HttpPut("{id}/reactivate")]
        public async Task<IActionResult> ReactivateUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                return NotFound();

            if (user.IsActive)
                return BadRequest("User is already active.");

            user.IsActive = true;

            await _context.SaveChangesAsync();

            return Ok(new { message = "User reactivated successfully" });
        }
    }
}