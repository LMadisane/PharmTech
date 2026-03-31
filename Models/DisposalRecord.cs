using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmTech.Models
{
    public class DisposalRecord
    {
        [Key]
        public int DisposalId { get; set; }

        [Required]
        public int MedId { get; set; }

        [ForeignKey("MedId")]
        public virtual Medicine? Medicine { get; set; }

        [Required]
        public int FacilityId { get; set; }

        [ForeignKey("FacilityId")]
        public virtual Facility? Facility { get; set; }

        // Optional batch reference (if disposal came from a batch)
        public int? BatchId { get; set; }

        [ForeignKey("BatchId")]
        public virtual MedicineBatch? Batch { get; set; }

        [Required]
        public int Quantity { get; set; }

        // Why the drug is being disposed
        [Required]
        public string Reason { get; set; } = string.Empty;
        // e.g. "Expired", "Damaged", "Recalled", "Contaminated"

        // Who recorded the disposal
        public int RecordedById { get; set; }

        [ForeignKey("RecordedById")]
        public virtual User? RecordedBy { get; set; }

        public DateTime RecordedAt { get; set; } = DateTime.Now;

        // Optional notes
        public string Notes { get; set; } = string.Empty;
    }
}