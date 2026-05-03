using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace PharmTech.Controllers
{
    [Route("account")]
    public class AccountController(PharmTechContext context) : Controller
    {
        private readonly PharmTechContext _context = context;

        [HttpGet("login")]
        public IActionResult Login() => View();

        [HttpPost("login")]
        public async Task<IActionResult> Login(string email, string password)
        {
            // Check user exists
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || !VerifyPassword(password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Invalid credentials");
                return View();
            }

            // Block inactive accounts
            if (!user.IsActive)
            {
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

            return RedirectToAction("Index", "Dashboard");
        }

        private static bool VerifyPassword(string password, string hash)
        {
            // Hash the entered password using SHA256 and compare
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            var hashedPassword = Convert.ToBase64String(hashedBytes);
            return hashedPassword == hash;
        }

        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
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