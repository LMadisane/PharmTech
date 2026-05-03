using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;
using PharmTech.Models.DTOs;
using System.Security.Cryptography;
using System.Text;

namespace PharmTech.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserManagementController : Controller
    {
        private readonly PharmTechContext _context;
        private readonly ILogger<UserManagementController> _logger;

        public UserManagementController(PharmTechContext context, ILogger<UserManagementController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==================== VIEWS ====================

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult CreateUser()
        {
            return View();
        }
        
        public IActionResult EditUser(int id)
        {
            ViewBag.UserId = id;
            return View();
        }

        // ==================== API ENDPOINTS ====================

        [HttpGet("api/usermanagement/users")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _context.Users
                    .Include(u => u.Facility)
                    .Select(u => new
                    {
                        u.UserId,
                        u.Name,
                        u.Email,
                        u.Role,
                        u.IsActive,
                        facility = u.Facility != null ? u.Facility.Name : null
                    })
                    .OrderBy(u => u.Role)
                    .ThenBy(u => u.Name)
                    .ToListAsync();

                return Ok(new { success = true, users });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching users");
                return Ok(new { success = true, users = new List<object>() });
            }
        }

        [HttpGet("api/usermanagement/users/{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Facility)
                    .FirstOrDefaultAsync(u => u.UserId == id);

                if (user == null)
                {
                    return Ok(new { success = false, message = "User not found" });
                }

                return Ok(new
                {
                    success = true,
                    user = new
                    {
                        user.UserId,
                        user.Name,
                        user.Email,
                        user.Role,
                        user.IsActive,
                        user.FacilityId
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user {Id}", id);
                return Ok(new { success = false, message = "An error occurred" });
            }
        }

        [HttpPost("api/usermanagement/users")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Invalid request data" });

                // Check if email already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (existingUser != null)
                    return BadRequest(new { success = false, message = "A user with this email already exists" });

                // Validate role
                var allowedRoles = new[] { "Doctor", "Pharmacist", "Patient" };
                if (!allowedRoles.Contains(request.Role))
                    return BadRequest(new { success = false, message = "Invalid role. Allowed roles: Doctor, Pharmacist, Patient" });

                // Handle password based on role
                string passwordHash;
                if (request.Role == "Patient")
                {
                    // Patients don't login, so use a placeholder hash (they can't authenticate)
                    passwordHash = HashPassword(Guid.NewGuid().ToString());
                }
                else
                {
                    // Doctors and Pharmacists need valid passwords
                    if (string.IsNullOrEmpty(request.Password) || request.Password.Length < 6)
                        return BadRequest(new { success = false, message = "Password must be at least 6 characters" });

                    passwordHash = HashPassword(request.Password);
                }

                var user = new User
                {
                    Name = request.Name,
                    Email = request.Email,
                    PasswordHash = passwordHash,
                    Role = request.Role,
                    IsActive = true,
                    FacilityId = request.FacilityId
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "User created successfully",
                    user = new
                    {
                        user.UserId,
                        user.Name,
                        user.Email,
                        user.Role,
                        user.IsActive
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, new { success = false, message = "An error occurred while creating user" });
            }
        }

        [HttpPut("api/usermanagement/users/{id}/deactivate")]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);

                if (user == null)
                    return NotFound(new { success = false, message = "User not found" });

                if (user.Email == "admin@pharmtech.com" && user.Role == "Admin")
                    return BadRequest(new { success = false, message = "Cannot deactivate the system admin account" });

                if (!user.IsActive)
                    return BadRequest(new { success = false, message = "User is already inactive" });

                user.IsActive = false;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "User deactivated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user {Id}", id);
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        [HttpPut("api/usermanagement/users/{id}/reactivate")]
        public async Task<IActionResult> ReactivateUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);

                if (user == null)
                    return NotFound(new { success = false, message = "User not found" });

                if (user.IsActive)
                    return BadRequest(new { success = false, message = "User is already active" });

                user.IsActive = true;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "User reactivated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reactivating user {Id}", id);
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        [HttpPut("api/usermanagement/users/{id}/reset-password")]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);

                if (user == null)
                    return NotFound(new { success = false, message = "User not found" });

                user.PasswordHash = HashPassword(request.NewPassword);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Password reset successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user {Id}", id);
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        [HttpGet("api/usermanagement/facilities")]
        public async Task<IActionResult> GetFacilities()
        {
            try
            {
                var facilities = await _context.Facilities
                    .Select(f => new { f.FacilityId, f.Name })
                    .ToListAsync();

                return Ok(new { success = true, facilities });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching facilities");
                return Ok(new { success = true, facilities = new List<object>() });
            }
        }

        // PUT: api/usermanagement/users/{id}
        [HttpPut("api/usermanagement/users/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);

                if (user == null)
                    return NotFound(new { success = false, message = "User not found" });

                // Check if email is already taken by another user
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.UserId != id);

                if (existingUser != null)
                    return BadRequest(new { success = false, message = "Email already in use by another user" });

                user.Name = request.Name;
                user.Email = request.Email;
                user.FacilityId = request.FacilityId;
                user.IsActive = request.IsActive;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "User updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {Id}", id);
                return StatusCode(500, new { success = false, message = "An error occurred while updating user" });
            }
        }

        // DELETE: api/usermanagement/users/{id}
        [HttpDelete("api/usermanagement/users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);

                if (user == null)
                    return NotFound(new { success = false, message = "User not found" });

                // Prevent deleting the main admin account
                if (user.Email == "admin@pharmtech.com" && user.Role == "Admin")
                    return BadRequest(new { success = false, message = "Cannot delete the system admin account" });

                // Check if user has any related records
                var hasPrescriptions = await _context.Prescriptions.AnyAsync(p => p.PatientId == id);
                var hasOrderRequests = await _context.OrderRequests.AnyAsync(o => o.RequestedById == id);

                if (hasPrescriptions || hasOrderRequests)
                {
                    // Soft delete - deactivate instead
                    user.IsActive = false;
                    await _context.SaveChangesAsync();
                    return Ok(new { success = true, message = "User has existing records. Account has been deactivated instead." });
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {Id}", id);
                return StatusCode(500, new { success = false, message = "An error occurred while deleting user" });
            }
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}