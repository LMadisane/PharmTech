using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmTech.Models
{
    public class Receipt
    {
        [Key]
        public int ReceiptId { get; set; }

        [Required]
        public string ReceiptNumber { get; set; } = string.Empty; // e.g. RCP-2026-001

        [Required]
        public string ReceiptType { get; set; } = string.Empty; // Dispense, Return, Order

        // Who generated the receipt
        [Required]
        public int GeneratedById { get; set; }
        [ForeignKey("GeneratedById")]
        public virtual User? GeneratedBy { get; set; }

        // Patient and medicine info stored as strings for receipt permanence
        // (so receipt stays accurate even if records change)
        [Required]
        public string PatientName { get; set; } = string.Empty;

        [Required]
        public string MedicineName { get; set; } = string.Empty;

        [Required]
        public int Quantity { get; set; }

        public string Notes { get; set; } = string.Empty;

        // Links back to the original record
        public int? LinkedRecordId { get; set; }

        public DateTime GeneratedAt { get; set; } = DateTime.Now;
    }
}