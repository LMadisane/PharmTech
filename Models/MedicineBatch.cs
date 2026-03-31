using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmTech.Models
{
    public class MedicineBatch
    {
        [Key]
        public int BatchId { get; set; }

        [Required]
        public int MedId { get; set; }

        [ForeignKey("MedId")]
        public virtual Medicine? Medicine { get; set; }

        [Required]
        public int FacilityId { get; set; }

        [ForeignKey("FacilityId")]
        public virtual Facility? Facility { get; set; }

        // Unique batch identifier
        [Required]
        public string LotNumber { get; set; } = string.Empty;

        [Required]
        public int Quantity { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }

        // FIFO tie-breaker
        public DateTime DateReceived { get; set; } = DateTime.Now;

        // For priority dispensing (near expiry)
        public bool IsFlagged { get; set; } = false;
    }
}