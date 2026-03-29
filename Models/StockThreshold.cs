using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmTech.Models
{
    public class StockThreshold
    {
        [Key]
        public int ThresholdId { get; set; }

        [Required]
        public int MedId { get; set; }
        [ForeignKey("MedId")]
        public virtual Medicine? Medicine { get; set; }

        [Required]
        public int FacilityId { get; set; }
        [ForeignKey("FacilityId")]
        public virtual Facility? Facility { get; set; }

        // Minimum stock level before alert is triggered
        [Required]
        public int MinimumStockLevel { get; set; }

        // Admin who set this threshold
        [Required]
        public int SetById { get; set; }
        [ForeignKey("SetById")]
        public virtual User? SetBy { get; set; }

        public DateTime SetAt { get; set; } = DateTime.Now;
    }
}