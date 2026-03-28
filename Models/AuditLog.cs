using System.ComponentModel.DataAnnotations;

namespace PharmTech.Models
{
    public class AuditLog
    {
        [Key]
        public int AuditLogId { get; set; }

        [Required]
        public string Action { get; set; } = string.Empty;

        [Required]
        public string Entity { get; set; } = string.Empty;

        public int UserId { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}