using System.ComponentModel.DataAnnotations;

namespace PharmTech.Models.DTOs
{
    public class DispenseRequest
    {
        [Required]
        public int PrescriptionId { get; set; }

        [Required]
        public int FacilityId { get; set; }

        [Required]
        public int DispensedById { get; set; }
    }
}