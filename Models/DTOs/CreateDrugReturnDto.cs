using System.ComponentModel.DataAnnotations;

namespace PharmTech.Models.DTOs
{
    public class CreateDrugReturnDto
    {
        [Required]
        public int MedId { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        [MinLength(3, ErrorMessage = "Reason must be at least 3 characters")]
        public string Reason { get; set; } = string.Empty;

        public bool IsRestockable { get; set; }

        public int FacilityId { get; set; }
    }
}