using System.ComponentModel.DataAnnotations;

namespace PharmTech.Models.DTOs
{
    public class PrescriptionRequest
    {
        [Required]
        public int PatientId { get; set; }

        [Required]
        public int MedId { get; set; }

        [Required]
        [Range(1, 10, ErrorMessage = "Dosage must be between 1 and 10 per day")]
        public int DosagePerDay { get; set; }

        [Required]
        [Range(1, 90, ErrorMessage = "Duration must be between 1 and 90 days")]
        public int DurationDays { get; set; }
    }
}