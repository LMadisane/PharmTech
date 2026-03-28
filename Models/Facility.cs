using System.ComponentModel.DataAnnotations;

namespace PharmTech.Models
{
    public class Facility
    {
        [Key]
        public int FacilityId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;
        public string ContactInfo { get; set; } = string.Empty;

        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
        public virtual ICollection<OrderRequest> OrderRequests { get; set; } = new List<OrderRequest>();
    }
}