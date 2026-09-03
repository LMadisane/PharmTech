using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;
using PharmTech.Models.DTOs;
using System.Security.Claims;

namespace PharmTech.Controllers
{
    public class DispensingController : Controller
    {
        private readonly PharmTechContext _context;
        private readonly ILogger<DispensingController> _logger;

        public DispensingController(PharmTechContext context, ILogger<DispensingController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ----- VIEWS

        [Authorize(Roles = "Pharmacist")]
        public IActionResult Index()
        {
            return View();
        }

        // ====== API ENDPOINTS

        [HttpGet("api/dispensing/{referenceCode}")]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> GetByReference(string referenceCode)
        {
            try
            {
                var pharmacistId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var pharmacist = await _context.Users.FindAsync(pharmacistId);

                if (pharmacist == null || !pharmacist.FacilityId.HasValue)
                    return BadRequest(new { success = false, message = "Pharmacist not assigned to any facility" });

                var prescription = await _context.Prescriptions
                    .Include(p => p.Patient)
                    .Include(p => p.Medicine)
                    .Include(p => p.Facility)
                    .FirstOrDefaultAsync(p => p.ReferenceCode == referenceCode);

                if (prescription == null)
                    return NotFound(new { success = false, message = "Prescription not found" });

                // Only allow dispensing of prescriptions from the pharmacist's facility
                if (prescription.FacilityId != pharmacist.FacilityId.Value)
                    return BadRequest(new { success = false, message = "This prescription is from a different facility. Cannot dispense." });

                if (prescription.Status == "Dispensed")
                    return BadRequest(new { success = false, message = "This prescription has already been dispensed" });

                return Ok(new
                {
                    success = true,
                    prescription = new
                    {
                        prescription.PrescriptionId,
                        prescription.ReferenceCode,
                        prescription.Status,
                        patient = new
                        {
                            prescription.Patient.UserId,
                            prescription.Patient.Name,
                            prescription.Patient.Email
                        },
                        medicine = new
                        {
                            prescription.Medicine.MedId,
                            prescription.Medicine.Name,
                            prescription.Medicine.DosageForm
                        },
                        prescription.DosagePerDay,
                        prescription.DurationDays,
                        totalQuantity = prescription.DosagePerDay * prescription.DurationDays,
                        prescription.PrescribedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error looking up prescription {ReferenceCode}", referenceCode);
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        // Dispensing a prescription
        [HttpPost("api/dispensing/dispense")]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> Dispense([FromBody] DispenseRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Invalid request data" });

                var pharmacistId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var pharmacist = await _context.Users.FindAsync(pharmacistId);

                if (pharmacist == null || !pharmacist.FacilityId.HasValue)
                    return BadRequest(new { success = false, message = "Pharmacist not assigned to any facility" });

                var prescription = await _context.Prescriptions
                    .Include(p => p.Medicine)
                    .Include(p => p.Patient)
                    .FirstOrDefaultAsync(p => p.PrescriptionId == request.PrescriptionId);

                if (prescription == null)
                    return NotFound(new { success = false, message = "Prescription not found" });

                if (prescription.Status == "Dispensed")
                    return BadRequest(new { success = false, message = "This prescription has already been dispensed" });

                if (prescription.FacilityId != pharmacist.FacilityId.Value)
                    return BadRequest(new { success = false, message = "Cannot dispense prescriptions from other facilities" });

                int requiredQty = prescription.DosagePerDay * prescription.DurationDays;

                var inventory = await _context.InventoryItems
                    .FirstOrDefaultAsync(i =>
                        i.MedId == prescription.MedId &&
                        i.FacilityId == pharmacist.FacilityId.Value);

                if (inventory == null)
                    return BadRequest(new { success = false, message = $"Medicine '{prescription.Medicine?.Name}' not found in inventory at your facility" });

                if (inventory.Quantity < requiredQty)
                    return BadRequest(new
                    {
                        success = false,
                        message = $"Insufficient stock. Available: {inventory.Quantity}, Required: {requiredQty}"
                    });

                var batches = await _context.MedicineBatches
                    .Where(b => b.MedId == prescription.MedId &&
                                b.FacilityId == pharmacist.FacilityId.Value &&
                                b.Quantity > 0)
                    .OrderBy(b => b.ExpiryDate)
                    .ToListAsync();

                if (!batches.Any())
                    return BadRequest(new { success = false, message = "No available batches found for this medicine" });

                int remainingToDeduct = requiredQty;
                foreach (var batch in batches)
                {
                    if (remainingToDeduct <= 0) break;

                    int deductFromBatch = Math.Min(batch.Quantity, remainingToDeduct);
                    batch.Quantity -= deductFromBatch;
                    remainingToDeduct -= deductFromBatch;
                }

                inventory.Quantity -= requiredQty;

                var dispenseRecord = new DispenseRecord
                {
                    PrescriptionId = prescription.PrescriptionId,
                    DispensedById = request.DispensedById,
                    FacilityId = pharmacist.FacilityId.Value,
                    QuantityDispensed = requiredQty,
                    DispensedAt = DateTime.Now
                };

                _context.DispenseRecords.Add(dispenseRecord);
                prescription.Status = "Dispensed";

                var receipt = new Receipt
                {
                    ReceiptNumber = $"DISP-{DateTime.Now:yyyyMMdd}-{prescription.PrescriptionId}",
                    ReceiptType = "Dispense",
                    GeneratedById = request.DispensedById,
                    PatientName = prescription.Patient?.Name ?? "Unknown",
                    MedicineName = prescription.Medicine?.Name ?? "Unknown",
                    Quantity = requiredQty,
                    Notes = $"Prescription: {prescription.ReferenceCode}",
                    LinkedRecordId = prescription.PrescriptionId,
                    GeneratedAt = DateTime.Now,
                    FacilityId = pharmacist.FacilityId.Value
                };

                _context.Receipts.Add(receipt);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Dispensed successfully",
                    data = new
                    {
                        remainingStock = inventory.Quantity,
                        dispenseRecord = new
                        {
                            dispenseRecord.DispenseRecordId,
                            dispenseRecord.QuantityDispensed,
                            dispenseRecord.DispensedAt
                        },
                        receipt = new
                        {
                            receipt.ReceiptId,
                            receipt.ReceiptNumber,
                            receipt.GeneratedAt
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dispensing prescription {PrescriptionId}: {Message}", request.PrescriptionId, ex.Message);
                return StatusCode(500, new { success = false, message = $"Dispensing error: {ex.Message}" });
            }
        }

        // Get dispense history for a prescription
        [HttpGet("api/dispensing/prescription/{prescriptionId}/history")]
        [Authorize(Roles = "Admin,Doctor,Pharmacist")]
        public async Task<IActionResult> GetDispenseHistory(int prescriptionId)
        {
            try
            {
                var dispenses = await _context.DispenseRecords
                    .Include(d => d.DispensedBy)
                    .Include(d => d.Facility)
                    .Where(d => d.PrescriptionId == prescriptionId)
                    .OrderByDescending(d => d.DispensedAt)
                    .Select(d => new
                    {
                        d.DispenseRecordId,
                        d.QuantityDispensed,
                        d.DispensedAt,
                        dispensedBy = d.DispensedBy != null ? d.DispensedBy.Name : "Unknown",
                        facility = d.Facility != null ? d.Facility.Name : "Unknown"
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = dispenses });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching dispense history for prescription {PrescriptionId}", prescriptionId);
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        // Get facilities for the logged-in pharmacist
        [HttpGet("api/dispensing/facilities")]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> GetFacilities()
        {
            try
            {
                var pharmacistId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var pharmacist = await _context.Users.FindAsync(pharmacistId);

                // Pharmacists can only see their own facility
                if (pharmacist != null && pharmacist.FacilityId.HasValue)
                {
                    var facility = await _context.Facilities
                        .Where(f => f.FacilityId == pharmacist.FacilityId.Value)
                        .Select(f => new { f.FacilityId, f.Name })
                        .FirstOrDefaultAsync();

                    return Ok(new { success = true, data = facility });
                }

                return Ok(new { success = true, data = (object)null });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching facilities");
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        // Check stock levels and create low stock alerts if necessary
        private async Task CheckLowStockAndAlert(int medId, int facilityId, int currentStock)
        {
            var medicine = await _context.Medicines.FindAsync(medId);
            if (medicine == null) return;

            var threshold = await _context.StockThresholds
                .FirstOrDefaultAsync(t => t.MedId == medId && t.FacilityId == facilityId);

            var minimumLevel = threshold?.MinimumStockLevel ?? medicine.BufferQty;

            if (currentStock <= minimumLevel)
            {
                var existingAlert = await _context.SystemAlerts
                    .FirstOrDefaultAsync(a =>
                        a.AlertType == "LowStock" &&
                        a.MedId == medId &&
                        a.FacilityId == facilityId &&
                        a.CreatedAt.Date == DateTime.Today);

                if (existingAlert == null)
                {
                    var facility = await _context.Facilities.FindAsync(facilityId);
                    var alert = new SystemAlert
                    {
                        AlertType = "LowStock",
                        Message = $"Low stock alert: {medicine.Name} at {(facility?.Name ?? "Facility")} - " +
                                  $"Only {currentStock} units remaining (minimum: {minimumLevel})",
                        MedId = medId,
                        FacilityId = facilityId,
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    };

                    _context.SystemAlerts.Add(alert);
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}