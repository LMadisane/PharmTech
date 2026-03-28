using System.ComponentModel.DataAnnotations;

namespace PharmTech.Models
{
    public class InventoryItem
    {
        [Key]
        public int InventoryId { get; set; }

        [Required]
        public int FacilityId { get; set; }
        public virtual Facility Facility { get; set; } = null!;

        [Required]
        public int MedId { get; set; }
        public virtual Medicine Medicine { get; set; } = null!;

        [Required]
        public int Quantity { get; set; }
    }
}