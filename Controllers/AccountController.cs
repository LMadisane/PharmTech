using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;
using System.Security.Claims;

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
            // Check user exists and password is correct
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

            return RedirectToAction("Index", "Dashboard"); // landing page
        }

        private static bool VerifyPassword(string password, string hash)
        {
            // Replace with real hash check (BCrypt / SHA)
            return password == hash; // placeholder
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