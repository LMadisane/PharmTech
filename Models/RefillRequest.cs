using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmTech.Models
{
    public class RefillRequest
    {
        [Key]
        public int RefillRequestId { get; set; }

        // The original prescription being refilled
        [Required]
        public int OriginalPrescriptionId { get; set; }
        [ForeignKey("OriginalPrescriptionId")]
        public virtual Prescription? OriginalPrescription { get; set; }

        // Pharmacist requesting the refill on behalf of patient
        [Required]
        public int RequestedById { get; set; }
        [ForeignKey("RequestedById")]
        public virtual User? RequestedBy { get; set; }

        // Doctor who needs to approve the refill
        [Required]
        public int DoctorId { get; set; }
        [ForeignKey("DoctorId")]
        public virtual User? Doctor { get; set; }

        [Required]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        public string Notes { get; set; } = string.Empty;

        public DateTime RequestedAt { get; set; } = DateTime.Now;

        public DateTime? ReviewedAt { get; set; }
    }
}