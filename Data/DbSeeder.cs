using Microsoft.EntityFrameworkCore;
using PharmTech.Models;
using System.Security.Cryptography;
using System.Text;

namespace PharmTech.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(PharmTechContext db)
        {
            // Only seed if no users exist at all
            if (await db.Users.AnyAsync())
                return;

            // Hash the admin password
            var hashedPassword = HashPassword("[REDACTED]");

            // Create default Admin account
            var admin = new User
            {
                Name = "System Admin",
                Email = "admin@pharmtech.com",
                PasswordHash = hashedPassword,
                Role = "Admin",
                IsActive = true
            };

            db.Users.Add(admin);
            await db.SaveChangesAsync();
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}