using System.ComponentModel.DataAnnotations;

namespace PharmTech.Models
{
    public class Supplier
    {
        [Key]
        public int SupplierId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string ContactInfo { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        public virtual ICollection<OrderRequest> Orders { get; set; } = new List<OrderRequest>();
    }
}