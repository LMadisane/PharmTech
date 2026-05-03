using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;
using PharmTech.Models.DTOs;
using System.Security.Claims;

namespace PharmTech.Controllers
{
    public class DispensingViewController : Controller
    {
        private readonly PharmTechContext _context;
        private readonly ILogger<DispensingViewController> _logger;

        public DispensingViewController(PharmTechContext context, ILogger<DispensingViewController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==================== VIEWS ====================

        [Authorize(Roles = "Pharmacist")]
        public IActionResult Index()
        {
            return View();
        }

        // ==================== API ENDPOINTS ====================

        [HttpGet("api/dispensing/{referenceCode}")]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> GetByReference(string referenceCode)
        {
            try
            {
                var prescription = await _context.Prescriptions
                    .Include(p => p.Patient)
                    .Include(p => p.Medicine)
                    .FirstOrDefaultAsync(p => p.ReferenceCode == referenceCode);

                if (prescription == null)
                    return NotFound(new { success = false, message = "Prescription not found" });

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

        [HttpPost("api/dispensing/dispense")]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> Dispense([FromBody] DispenseRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Invalid request data" });

                var prescription = await _context.Prescriptions
                    .Include(p => p.Medicine)
                    .Include(p => p.Patient)
                    .FirstOrDefaultAsync(p => p.PrescriptionId == request.PrescriptionId);

                if (prescription == null)
                    return NotFound(new { success = false, message = "Prescription not found" });

                if (prescription.Status == "Dispensed")
                    return BadRequest(new { success = false, message = "This prescription has already been dispensed" });

                int requiredQty = prescription.DosagePerDay * prescription.DurationDays;

                var inventory = await _context.InventoryItems
                    .FirstOrDefaultAsync(i =>
                        i.MedId == prescription.MedId &&
                        i.FacilityId == request.FacilityId);

                if (inventory == null)
                    return BadRequest(new { success = false, message = "Medicine not found in inventory at this facility" });

                if (inventory.Quantity < requiredQty)
                    return BadRequest(new
                    {
                        success = false,
                        message = "Insufficient stock",
                        available = inventory.Quantity,
                        required = requiredQty
                    });

                var batches = await _context.MedicineBatches
                    .Where(b => b.MedId == prescription.MedId &&
                                b.FacilityId == request.FacilityId &&
                                b.Quantity > 0)
                    .OrderBy(b => b.ExpiryDate)
                    .ToListAsync();

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
                    FacilityId = request.FacilityId,
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
                    GeneratedAt = DateTime.Now
                };

                _context.Receipts.Add(receipt);
                await _context.SaveChangesAsync();

                await CheckLowStockAndAlert(prescription.MedId, request.FacilityId, inventory.Quantity);

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
                _logger.LogError(ex, "Error dispensing prescription {PrescriptionId}", request.PrescriptionId);
                return StatusCode(500, new { success = false, message = "An error occurred while dispensing" });
            }
        }

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

        [HttpGet("api/dispensing/facilities")]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> GetFacilities()
        {
            try
            {
                var facilities = await _context.Facilities
                    .Select(f => new { f.FacilityId, f.Name })
                    .ToListAsync();

                return Ok(new { success = true, data = facilities });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching facilities");
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

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