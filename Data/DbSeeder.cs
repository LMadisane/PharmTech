using Microsoft.AspNetCore.Identity;
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

            var passwordHasher = new PasswordHasher<User>();  // Use proper password hasher

            // Create default Admin account
            var admin = new User
            {
                Name = "System Admin",
                Email = "admin@pharmtech.com",
                Role = "Admin",
                IsActive = true
            };
            admin.PasswordHash = passwordHasher.HashPassword(admin, "[REDACTED]");
            db.Users.Add(admin);

            // Create Demo Facility 1
            var facility1 = new Facility
            {
                Name = "University Teaching Hospital - Main Campus",
                Address = "Nationalist Rd, Lusaka",
                ContactInfo = "+260 211 123 456"
            };
            db.Facilities.Add(facility1);
            await db.SaveChangesAsync();  // Saving to get FacilityId

            // Create Demo Facility 2
            var facility2 = new Facility
            {
                Name = "Levy Mwanawasa Medical University - Pharmacy",
                Address = "Lusaka East, Lusaka",
                ContactInfo = "+260 211 789 012"
            };
            db.Facilities.Add(facility2);
            await db.SaveChangesAsync();  // Saving in order to get FacilityId

            
            // 4. Create Doctor accounts
            var doctor1 = new User
            {
                Name = "Dr. Charis Madisane",
                Email = "charismadisane@pharmtech.com",
                Role = "Doctor",
                IsActive = true,
                FacilityId = facility1.FacilityId
            };
            doctor1.PasswordHash = passwordHasher.HashPassword(doctor1, "[REDACTED]");
            db.Users.Add(doctor1);

            var doctor2 = new User
            {
                Name = "Dr. Ernest Mwanza",
                Email = "ernest.mwanza@pharmtech.com",
                Role = "Doctor",
                IsActive = true,
                FacilityId = facility2.FacilityId
            };
            doctor2.PasswordHash = passwordHasher.HashPassword(doctor2, "[REDACTED]");
            db.Users.Add(doctor2);

            // Creating Pharmacist accounts
            var pharmacist1 = new User
            {
                Name = "Chebwa Hampongo",
                Email = "chebwa.hampongo@pharmtech.com",
                Role = "Pharmacist",
                IsActive = true,
                FacilityId = facility1.FacilityId
            };
            pharmacist1.PasswordHash = passwordHasher.HashPassword(pharmacist1, "[REDACTED]");
            db.Users.Add(pharmacist1);

            var pharmacist2 = new User
            {
                Name = "Priscilla Ndhlovu",
                Email = "priscillandhl@pharmtech.com",
                Role = "Pharmacist",
                IsActive = true,
                FacilityId = facility2.FacilityId
            };
            pharmacist2.PasswordHash = passwordHasher.HashPassword(pharmacist2, "[REDACTED]");
            db.Users.Add(pharmacist2);

            // Creating some sample patients
            var patient1 = new User
            {
                Name = "John Banda",
                Email = "john.banda@patient.com",
                Role = "Patient",
                IsActive = true,
                FacilityId = facility1.FacilityId
            };
            patient1.PasswordHash = passwordHasher.HashPassword(patient1, Guid.NewGuid().ToString());
            db.Users.Add(patient1);

            var patient2 = new User
            {
                Name = "Mary Phiri",
                Email = "mary.phiri@patient.com",
                Role = "Patient",
                IsActive = true,
                FacilityId = facility2.FacilityId
            };
            patient2.PasswordHash = passwordHasher.HashPassword(patient2, Guid.NewGuid().ToString());
            db.Users.Add(patient2);

            await db.SaveChangesAsync();
        }
    }
}