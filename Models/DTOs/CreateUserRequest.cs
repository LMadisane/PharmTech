using System.ComponentModel.DataAnnotations;

namespace PharmTech.Models.DTOs
{
    public class CreateUserRequest
    {
        [Required]
        [MinLength(2, ErrorMessage = "Name must be at least 2 characters")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        // Password is required for Doctors and Pharmacists, but not for Patients
        public string? Password { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;

        public int? FacilityId { get; set; }
    }

    public class ResetPasswordRequest
    {
        [Required]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string NewPassword { get; set; } = string.Empty;
    }
}