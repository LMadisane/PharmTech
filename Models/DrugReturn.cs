using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmTech.Models
{
    public class DrugReturn
    {
        [Key]
        public int ReturnId { get; set; }

        [Required]
        public int MedId { get; set; }
        [ForeignKey("MedId")]
        public virtual Medicine? Medicine { get; set; }

        [Required]
        public int FacilityId { get; set; }
        [ForeignKey("FacilityId")]
        public virtual Facility? Facility { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public int PatientId { get; set; }
        [ForeignKey("PatientId")]
        public virtual User? Patient { get; set; }

        public int? ProcessedById { get; set; }
        [ForeignKey("ProcessedById")]
        public virtual User? ProcessedBy { get; set; }

        [Required]
        public string Reason { get; set; } = string.Empty;

        [Required]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        public bool IsRestockable { get; set; } = false;

        public DateTime ProcessedAt { get; set; } = DateTime.Now;
    }
}