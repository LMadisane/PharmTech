using System.ComponentModel.DataAnnotations;

namespace PharmTech.Models.DTOs
{
    public class UpdateUserRequest
    {
        [Required]
        [MinLength(2, ErrorMessage = "Name must be at least 2 characters")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        public int? FacilityId { get; set; }

        public bool IsActive { get; set; }
    }
}