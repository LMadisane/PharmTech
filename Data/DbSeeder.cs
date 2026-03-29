using Microsoft.EntityFrameworkCore;
using PharmTech.Models;

namespace PharmTech.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(PharmTechContext db)
        {
            // Only seed if no users exist at all
            if (await db.Users.AnyAsync())
                return;

            // Create default Admin account
            var admin = new User
            {
                Name = "System Admin",
                Email = "admin@pharmtech.com",
                PasswordHash = "[REDACTED]",
                Role = "Admin",
                IsActive = true
            };

            db.Users.Add(admin);
            await db.SaveChangesAsync();
        }
    }
}