using System.ComponentModel.DataAnnotations;

namespace PharmTech.Models.DTOs
{
    public class AddBatchRequest
    {
        [Required]
        public int MedicineId { get; set; }

        [Required]
        public int FacilityId { get; set; }

        [Required]
        [MinLength(3, ErrorMessage = "Lot number must be at least 3 characters")]
        public string LotNumber { get; set; } = string.Empty;

        [Required]
        [Range(1, 100000, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [Required]
        public DateTime ExpiryDate { get; set; }
    }
}