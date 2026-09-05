using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;
using PharmTech.Models.DTOs;
using PharmTech.Services;

namespace PharmTech.Controllers
{
    public class MedicineController : Controller
    {
        private readonly PharmTechContext _context;
        private readonly ILogger<MedicineController> _logger;
        private readonly IAuditLogService _auditLogService;

        public MedicineController(
            PharmTechContext context,
            ILogger<MedicineController> logger,
            IAuditLogService auditLogService)
        {
            _context = context;
            _logger = logger;
            _auditLogService = auditLogService;
        }

        // ======== VIEWS

        [Authorize(Roles = "Admin")]
        public IActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        public IActionResult CreateMedicine()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        public IActionResult EditMedicine(int id)
        {
            ViewBag.MedicineId = id;
            return View();
        }

        // ========= API ENDPOINTS

        // Retrieves all medicines (Admin & Pharmacist)
        [HttpGet("api/medicine")]
        [Authorize(Roles = "Admin,Pharmacist")]
        public async Task<IActionResult> GetMedicines()
        {
            try
            {
                var medicines = await _context.Medicines
                    .Select(m => new { m.MedId, m.Name, m.DosageForm, m.BufferQty })
                    .OrderBy(m => m.Name)
                    .ToListAsync();

                return Ok(new { success = true, medicines });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching medicines");
                return Ok(new { success = true, medicines = new List<object>() });
            }
        }

        // Retrieves a single medicine by ID
        [HttpGet("api/medicine/{id}")]
        [Authorize(Roles = "Admin,Pharmacist")]
        public async Task<IActionResult> GetMedicine(int id)
        {
            try
            {
                var medicine = await _context.Medicines
                    .FirstOrDefaultAsync(m => m.MedId == id);

                if (medicine == null)
                {
                    return Ok(new { success = false, message = "Medicine not found" });
                }

                return Ok(new
                {
                    success = true,
                    medicine = new
                    {
                        medicine.MedId,
                        medicine.Name,
                        medicine.DosageForm,
                        medicine.BufferQty
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching medicine {Id}", id);
                return Ok(new { success = false, message = "An error occurred" });
            }
        }

        // Creates a new medicine (Admin only)
        [HttpPost("api/medicine")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateMedicine([FromBody] CreateMedicineRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Invalid request data" });

                var existingMedicine = await _context.Medicines
                    .FirstOrDefaultAsync(m => m.Name == request.Name);

                if (existingMedicine != null)
                    return BadRequest(new { success = false, message = "A medicine with this name already exists" });

                var medicine = new Medicine
                {
                    Name = request.Name,
                    DosageForm = request.DosageForm,
                    BufferQty = request.BufferQty
                };

                _context.Medicines.Add(medicine);
                await _context.SaveChangesAsync();

                // AUDIT LOG: Medicine created
                await _auditLogService.LogAsync(
                    action: "CreateMedicine",
                    entity: "Medicine",
                    entityId: medicine.MedId,
                    details: $"Medicine '{medicine.Name}' (Dosage: {medicine.DosageForm}, Buffer: {medicine.BufferQty}) created"
                );

                return Ok(new
                {
                    success = true,
                    message = "Medicine created successfully",
                    medicine = new
                    {
                        medicine.MedId,
                        medicine.Name,
                        medicine.DosageForm,
                        medicine.BufferQty
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating medicine");
                return StatusCode(500, new { success = false, message = "An error occurred while creating medicine" });
            }
        }

        // Updates an existing medicine (Admin only)
        [HttpPut("api/medicine/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateMedicine(int id, [FromBody] UpdateMedicineRequest request)
        {
            try
            {
                var medicine = await _context.Medicines.FindAsync(id);

                if (medicine == null)
                    return NotFound(new { success = false, message = "Medicine not found" });

                var existingMedicine = await _context.Medicines
                    .FirstOrDefaultAsync(m => m.Name == request.Name && m.MedId != id);

                if (existingMedicine != null)
                    return BadRequest(new { success = false, message = "Another medicine with this name already exists" });

                // Capture old values for audit
                var oldName = medicine.Name;
                var oldDosageForm = medicine.DosageForm;
                var oldBufferQty = medicine.BufferQty;

                medicine.Name = request.Name;
                medicine.DosageForm = request.DosageForm;
                medicine.BufferQty = request.BufferQty;

                await _context.SaveChangesAsync();

                // AUDIT LOG: Medicine updated
                var details = $"Medicine ID {id} updated. ";
                if (oldName != request.Name) details += $"Name: '{oldName}' → '{request.Name}'. ";
                if (oldDosageForm != request.DosageForm) details += $"DosageForm: '{oldDosageForm}' → '{request.DosageForm}'. ";
                if (oldBufferQty != request.BufferQty) details += $"BufferQty: {oldBufferQty} → {request.BufferQty}. ";

                await _auditLogService.LogAsync(
                    action: "UpdateMedicine",
                    entity: "Medicine",
                    entityId: medicine.MedId,
                    details: details.Trim()
                );

                return Ok(new
                {
                    success = true,
                    message = "Medicine updated successfully",
                    medicine = new
                    {
                        medicine.MedId,
                        medicine.Name,
                        medicine.DosageForm,
                        medicine.BufferQty
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating medicine {Id}", id);
                return StatusCode(500, new { success = false, message = "An error occurred while updating medicine" });
            }
        }

        // Deletes a medicine (Admin only)
        [HttpDelete("api/medicine/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteMedicine(int id)
        {
            try
            {
                var medicine = await _context.Medicines
                    .Include(m => m.InventoryItems)
                    .Include(m => m.Prescriptions)
                    .FirstOrDefaultAsync(m => m.MedId == id);

                if (medicine == null)
                    return NotFound(new { success = false, message = "Medicine not found" });

                if (medicine.InventoryItems.Any() || medicine.Prescriptions.Any())
                {
                    return BadRequest(new { success = false, message = "Cannot delete medicine with existing inventory or prescriptions" });
                }

                var medicineName = medicine.Name;
                _context.Medicines.Remove(medicine);
                await _context.SaveChangesAsync();

                // AUDIT LOG: Medicine deleted
                await _auditLogService.LogAsync(
                    action: "DeleteMedicine",
                    entity: "Medicine",
                    entityId: id,
                    details: $"Medicine '{medicineName}' (ID: {id}) deleted"
                );

                return Ok(new { success = true, message = "Medicine deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting medicine {Id}", id);
                return StatusCode(500, new { success = false, message = "An error occurred while deleting medicine" });
            }
        }
    }
}