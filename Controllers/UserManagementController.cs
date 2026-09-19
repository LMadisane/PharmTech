using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;
using PharmTech.Models.DTOs;
using PharmTech.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace PharmTech.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserManagementController : Controller
    {
        private readonly PharmTechContext _context;
        private readonly ILogger<UserManagementController> _logger;
        private readonly IAuditLogService _auditLogService;
        private readonly IPasswordHasher<User> _passwordHasher;

        public UserManagementController(
            PharmTechContext context,
            ILogger<UserManagementController> logger,
            IAuditLogService auditLogService,
            IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _logger = logger;
            _auditLogService = auditLogService;
            _passwordHasher = passwordHasher;
        }

        // ========= VIEWS

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

        // ======== API ENDPOINTS

        // Retrieves all users with their facility informatio
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

        // Retrieves a single user by ID
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

        // Creates a new user (Doctor, Pharmacist, or Patient)
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

                // Build the user object first so PasswordHasher can hash against it
                var user = new User
                {
                    Name = request.Name,
                    Email = request.Email,
                    Role = request.Role,
                    IsActive = true,
                    FacilityId = request.FacilityId
                };

                // Handle password based on role
                if (request.Role == "Patient")
                {
                    // Patients don't log in, so I'm using a random placeholder hash
                    user.PasswordHash = _passwordHasher.HashPassword(user, Guid.NewGuid().ToString());
                }
                else
                {
                    // Doctors and Pharmacists need valid passwords
                    if (string.IsNullOrEmpty(request.Password) || request.Password.Length < 6)
                        return BadRequest(new { success = false, message = "Password must be at least 6 characters" });

                    user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
                }

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // AUDIT LOG: User created
                await _auditLogService.LogAsync(
                    action: "CreateUser",
                    entity: "User",
                    entityId: user.UserId,
                    details: $"User {user.Name} ({user.Email}) created with role {user.Role}",
                    facilityId: user.FacilityId
                );

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

        // Deactivates a user account
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

                // AUDIT LOG: User deactivated
                await _auditLogService.LogAsync(
                    action: "DeactivateUser",
                    entity: "User",
                    entityId: user.UserId,
                    details: $"User {user.Name} ({user.Email}) deactivated",
                    facilityId: user.FacilityId
                );

                return Ok(new { success = true, message = "User deactivated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user {Id}", id);
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        // Reactivates a deactivated user account
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

                // AUDIT LOG: User reactivated
                await _auditLogService.LogAsync(
                    action: "ReactivateUser",
                    entity: "User",
                    entityId: user.UserId,
                    details: $"User {user.Name} ({user.Email}) reactivated",
                    facilityId: user.FacilityId
                );

                return Ok(new { success = true, message = "User reactivated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reactivating user {Id}", id);
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        // Resets a user's password
        [HttpPut("api/usermanagement/users/{id}/reset-password")]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);

                if (user == null)
                    return NotFound(new { success = false, message = "User not found" });

                user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
                await _context.SaveChangesAsync();

                // AUDIT LOG: Password reset
                await _auditLogService.LogAsync(
                    action: "ResetPassword",
                    entity: "User",
                    entityId: user.UserId,
                    details: $"Password reset for user {user.Name} ({user.Email})",
                    facilityId: user.FacilityId
                );

                return Ok(new { success = true, message = "Password reset successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user {Id}", id);
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        // Retrieves all facilities for dropdowns
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

        // Updates user details (name, email, facility, isActive)
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

                // Capture old values for audit
                var oldName = user.Name;
                var oldEmail = user.Email;
                var oldFacilityId = user.FacilityId;
                var oldIsActive = user.IsActive;

                user.Name = request.Name;
                user.Email = request.Email;
                user.FacilityId = request.FacilityId;
                user.IsActive = request.IsActive;

                await _context.SaveChangesAsync();

                // AUDIT LOG: User updated
                var details = $"User {oldName} updated. ";
                if (oldName != request.Name) details += $"Name: '{oldName}' → '{request.Name}'. ";
                if (oldEmail != request.Email) details += $"Email: '{oldEmail}' → '{request.Email}'. ";
                if (oldFacilityId != request.FacilityId) details += $"FacilityId: {oldFacilityId} → {request.FacilityId}. ";
                if (oldIsActive != request.IsActive) details += $"IsActive: {oldIsActive} → {request.IsActive}. ";

                await _auditLogService.LogAsync(
                    action: "UpdateUser",
                    entity: "User",
                    entityId: user.UserId,
                    details: details.Trim(),
                    facilityId: user.FacilityId
                );

                return Ok(new { success = true, message = "User updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {Id}", id);
                return StatusCode(500, new { success = false, message = "An error occurred while updating user" });
            }
        }

        // Deletes a user (or deactivates if they have related records)
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
                    // Soft delete, deactivate instead
                    user.IsActive = false;
                    await _context.SaveChangesAsync();

                    // AUDIT LOG: User deactivated
                    await _auditLogService.LogAsync(
                        action: "DeleteUser",
                        entity: "User",
                        entityId: user.UserId,
                        details: $"User {user.Name} ({user.Email}) deactivated instead of deleted due to existing records",
                        facilityId: user.FacilityId
                    );

                    return Ok(new { success = true, message = "User has existing records. Account has been deactivated instead." });
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                // AUDIT LOG: User deleted
                await _auditLogService.LogAsync(
                    action: "DeleteUser",
                    entity: "User",
                    entityId: user.UserId,
                    details: $"User {user.Name} ({user.Email}) permanently deleted",
                    facilityId: user.FacilityId
                );

                return Ok(new { success = true, message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {Id}", id);
                return StatusCode(500, new { success = false, message = "An error occurred while deleting user" });
            }
        }
    }
}