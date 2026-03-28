using System.ComponentModel.DataAnnotations;

public class DispenseRecord
{
    [Key]
    public int DispenseRecordId { get; set; }

    // Linking to Prescription
    [Required]
    public int PrescriptionId { get; set; }
    public virtual Prescription Prescription { get; set; } = null!;

    // Who dispensed?
    [Required]
    public int DispensedById { get; set; }
    public virtual User DispensedBy { get; set; } = null!;

    // Facility where it was dispensed
    [Required]
    public int FacilityId { get; set; }
    public virtual Facility Facility { get; set; } = null!;

    // Dispensed details
    [Required]
    public int QuantityDispensed { get; set; }

    [Required]
    public DateTime DispensedAt { get; set; } = DateTime.Now;
}