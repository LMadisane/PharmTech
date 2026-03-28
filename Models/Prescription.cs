using System.ComponentModel.DataAnnotations;

namespace PharmTech.Models
{
    public class Prescription
    {
        [Key]
        public int PrescriptionId { get; set; }

        [Required]
        public int PatientId { get; set; }
        public virtual User Patient { get; set; } = null!;

        [Required]
        public int MedId { get; set; }
        public virtual Medicine Medicine { get; set; } = null!;

        [Required]
        public int DosagePerDay { get; set; }

        [Required]
        public int DurationDays { get; set; }

        public DateTime PrescribedAt { get; set; } = DateTime.Now;

        [Required]
        public string ReferenceCode { get; set; } = string.Empty;

        [Required]
        public string Status { get; set; } = "Pending";

        public virtual ICollection<DispenseRecord> DispenseRecords { get; set; } = new List<DispenseRecord>();
    }
}