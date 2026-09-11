using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmTech.Models
{
    public class AuditLog
    {
        [Key]
        public int AuditLogId { get; set; }

        // Change from 'int' to 'int?' (nullable)
        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Required]
        [MaxLength(100)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string UserRole { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Entity { get; set; } = string.Empty;

        public int? EntityId { get; set; }

        [MaxLength(500)]
        public string Details { get; set; } = string.Empty;

        public string? PreviousValue { get; set; }

        public string? NewValue { get; set; }

        [MaxLength(45)]
        public string? IPAddress { get; set; }

        [MaxLength(255)]
        public string? UserAgent { get; set; }

        public int? FacilityId { get; set; }

        [ForeignKey("FacilityId")]
        public virtual Facility? Facility { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}