using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmTech.Models
{
    public class SystemAlert
    {
        [Key]
        public int AlertId { get; set; }

        [Required]
        public string AlertType { get; set; } = string.Empty; // LowStock, ExpiryWarning

        [Required]
        public string Message { get; set; } = string.Empty;

        public int? MedId { get; set; }
        [ForeignKey("MedId")]
        public virtual Medicine? Medicine { get; set; }

        public int? FacilityId { get; set; }
        [ForeignKey("FacilityId")]
        public virtual Facility? Facility { get; set; }

        // Whether the alert has been acknowledged
        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}