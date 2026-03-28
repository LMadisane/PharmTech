using System.ComponentModel.DataAnnotations;

namespace PharmTech.Models
{
    public class Medicine
    {
        [Key]
        public int MedId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string DosageForm { get; set; } = string.Empty;

        [Required]
        public int BufferQty { get; set; }

        public DateTime ExpiryDate { get; set; }

        public string LotNumber { get; set; } = string.Empty;

        public virtual ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
        public virtual ICollection<DrugReturn> DrugReturns { get; set; } = new List<DrugReturn>();
        public virtual ICollection<OrderRequest> OrderRequests { get; set; } = new List<OrderRequest>();
        public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    }
}