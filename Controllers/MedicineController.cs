using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;
using PharmTech.Models.DTOs;

namespace PharmTech.Controllers
{
    public class MedicineController : Controller
    {
        private readonly PharmTechContext _context;
        private readonly ILogger<MedicineController> _logger;

        public MedicineController(PharmTechContext context, ILogger<MedicineController> logger)
        {
            _context = context;
            _logger = logger;
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

        // Allow Admins and Pharmacists to view medicines
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

        // Allow Admins and Pharmacists to view medicines
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

        // Create a new medicine. Admin only
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

        // Updating a medicine. Admin only
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

                medicine.Name = request.Name;
                medicine.DosageForm = request.DosageForm;
                medicine.BufferQty = request.BufferQty;

                await _context.SaveChangesAsync();

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

        // Deleting a medicine. Admin only
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

                _context.Medicines.Remove(medicine);
                await _context.SaveChangesAsync();

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