using System.ComponentModel.DataAnnotations;

namespace PharmTech.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty; // Admin, Doctor, Pharmacist

        public bool IsActive { get; set; } = true; // Admin can deactivate accounts

        public int? FacilityId { get; set; }
        public virtual Facility? Facility { get; set; }

        public virtual ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
        public virtual ICollection<DrugReturn> DrugReturnsAsPatient { get; set; } = new List<DrugReturn>();
        public virtual ICollection<DrugReturn> DrugReturnsAsProcessor { get; set; } = new List<DrugReturn>();
        public virtual ICollection<OrderRequest> OrderRequestsAsRequester { get; set; } = new List<OrderRequest>();
        public virtual ICollection<OrderRequest> OrderRequestsAsApprover { get; set; } = new List<OrderRequest>();
    }
}