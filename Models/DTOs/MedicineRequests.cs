using System.ComponentModel.DataAnnotations;

namespace PharmTech.Models.DTOs
{
    public class CreateMedicineRequest
    {
        [Required]
        [MinLength(2, ErrorMessage = "Medicine name must be at least 2 characters")]
        public string Name { get; set; } = string.Empty;

        public string DosageForm { get; set; } = string.Empty;

        [Required]
        [Range(1, 10000, ErrorMessage = "Buffer quantity must be between 1 and 10,000")]
        public int BufferQty { get; set; }
    }

    public class UpdateMedicineRequest
    {
        [Required]
        [MinLength(2, ErrorMessage = "Medicine name must be at least 2 characters")]
        public string Name { get; set; } = string.Empty;

        public string DosageForm { get; set; } = string.Empty;

        [Required]
        [Range(1, 10000, ErrorMessage = "Buffer quantity must be between 1 and 10,000")]
        public int BufferQty { get; set; }
    }
}