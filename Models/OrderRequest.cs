using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmTech.Models
{
    public class OrderRequest
    {
        [Key]
        public int OrderId { get; set; }

        [Required]
        public int MedId { get; set; }
        [ForeignKey("MedId")]
        public virtual Medicine? Medicine { get; set; }

        [Required]
        public int FacilityId { get; set; }
        [ForeignKey("FacilityId")]
        public virtual Facility? Facility { get; set; }

        [Required]
        public int RequestedById { get; set; }
        [ForeignKey("RequestedById")]
        public virtual User? RequestedBy { get; set; }

        public int? ApprovedById { get; set; }
        [ForeignKey("ApprovedById")]
        public virtual User? ApprovedBy { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Fulfilled
        public DateTime RequestedAt { get; set; } = DateTime.Now;

        public int? SupplierId { get; set; }
        [ForeignKey("SupplierId")]
        public virtual Supplier? Supplier { get; set; }

        public DateTime? FulfilledAt { get; set; }
    }
}