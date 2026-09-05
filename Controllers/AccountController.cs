using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;
using PharmTech.Services;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace PharmTech.Controllers
{
    [Route("account")]
    public class AccountController : Controller
    {
        private readonly PharmTechContext _context;
        private readonly IAuditLogService _auditLogService;

        public AccountController(PharmTechContext context, IAuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        [HttpGet("login")]
        public IActionResult Login() => View();

        [HttpPost("login")]
        public async Task<IActionResult> Login(string email, string password)
        {
            // Check user exists
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || !VerifyPassword(password, user?.PasswordHash))
            {
                // Log failed login attempt using System log (no authenticated user)
                await _auditLogService.LogSystemAsync(
                    action: "LoginFailed",
                    entity: "User",
                    details: $"Failed login attempt for email: {email}"
                );

                ModelState.AddModelError("", "Invalid credentials");
                return View();
            }

            // Block inactive accounts
            if (!user.IsActive)
            {
                await _auditLogService.LogSystemAsync(
                    action: "LoginFailed",
                    entity: "User",
                    entityId: user.UserId,
                    details: $"Login attempt for inactive account: {user.Email}"
                );

                ModelState.AddModelError("", "Your account has been deactivated. Contact your administrator.");
                return View();
            }

            // Only Admin, Doctor and Pharmacist can log in
            var allowedRoles = new[] { "Admin", "Doctor", "Pharmacist" };
            if (!allowedRoles.Contains(user.Role))
            {
                ModelState.AddModelError("", "You are not authorized to access this system.");
                return View();
            }

            // Build claims for cookie-based RBAC
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(identity);

            // Sign in with cookie
            await HttpContext.SignInAsync("Cookies", principal);

            // Store session info
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Role", user.Role);

            // Log successful login (use LogWithUserAsync to pass explicit user info)
            await _auditLogService.LogWithUserAsync(
                action: "Login",
                entity: "User",
                userId: user.UserId,
                userName: user.Name,
                userRole: user.Role,
                entityId: user.UserId,
                details: $"User {user.Email} logged in successfully",
                facilityId: user.FacilityId
            );

            return RedirectToAction("Index", "Dashboard");
        }

        private static bool VerifyPassword(string password, string? hash)
        {
            if (string.IsNullOrEmpty(hash))
                return false;

            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            var hashedPassword = Convert.ToBase64String(hashedBytes);
            return hashedPassword == hash;
        }

        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
            // Get current user info from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                var user = await _context.Users.FindAsync(userId);
                var userName = user?.Name ?? User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                var userRole = user?.Role ?? User.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";

                // Log logout using explicit user info
                await _auditLogService.LogWithUserAsync(
                    action: "Logout",
                    entity: "User",
                    userId: userId,
                    userName: userName,
                    userRole: userRole,
                    entityId: userId,
                    details: $"User {user?.Email ?? userId.ToString()} logged out",
                    facilityId: user?.FacilityId
                );
            }

            // Sign out of cookie auth
            await HttpContext.SignOutAsync("Cookies");
            HttpContext.Session.Clear();

            return RedirectToAction("Login");
        }

        [HttpGet("accessdenied")]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}