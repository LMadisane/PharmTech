using System.ComponentModel.DataAnnotations;

namespace PharmTech.Models.DTOs
{
    public class CreateFacilityRequest
    {
        [Required]
        [MinLength(2, ErrorMessage = "Facility name must be at least 2 characters")]
        public string Name { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string ContactInfo { get; set; } = string.Empty;
    }

    public class UpdateFacilityRequest
    {
        [Required]
        [MinLength(2, ErrorMessage = "Facility name must be at least 2 characters")]
        public string Name { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string ContactInfo { get; set; } = string.Empty;
    }
}