using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;
using PharmTech.Models.DTOs;
using System.Security.Claims;

namespace PharmTech.Controllers
{
    [Authorize(Roles = "Admin,Doctor,Pharmacist")]
    public class PrescriptionViewController : Controller
    {
        private readonly PharmTechContext _context;
        private readonly ILogger<PrescriptionViewController> _logger;

        public PrescriptionViewController(PharmTechContext context, ILogger<PrescriptionViewController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==================== VIEWS ====================

        [Authorize(Roles = "Admin,Doctor,Pharmacist")]
        public IActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = "Doctor")]
        public IActionResult CreatePrescription()
        {
            return View();
        }

        [Authorize(Roles = "Admin,Doctor,Pharmacist")]
        public IActionResult Details(int id)
        {
            ViewBag.PrescriptionId = id;
            return View();
        }

        // ==================== API ENDPOINTS ====================

        // GET: api/prescription
        [HttpGet("api/prescription")]
        public async Task<IActionResult> GetPrescriptions(
            [FromQuery] string? status,
            [FromQuery] string? patientName,
            [FromQuery] string? medicineName,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            try
            {
                var query = _context.Prescriptions
                    .Include(p => p.Patient)
                    .Include(p => p.Medicine)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(p => p.Status == status);

                if (!string.IsNullOrEmpty(patientName))
                    query = query.Where(p => p.Patient.Name.Contains(patientName));

                if (!string.IsNullOrEmpty(medicineName))
                    query = query.Where(p => p.Medicine.Name.Contains(medicineName));

                if (from.HasValue)
                    query = query.Where(p => p.PrescribedAt >= from.Value);

                if (to.HasValue)
                    query = query.Where(p => p.PrescribedAt <= to.Value);

                var prescriptions = await query
                    .OrderByDescending(p => p.PrescribedAt)
                    .Select(p => new
                    {
                        p.PrescriptionId,
                        p.ReferenceCode,
                        p.Status,
                        p.PrescribedAt,
                        p.DosagePerDay,
                        p.DurationDays,
                        totalQuantity = p.DosagePerDay * p.DurationDays,
                        patient = new { p.Patient.UserId, p.Patient.Name, p.Patient.Email },
                        medicine = new { p.Medicine.MedId, p.Medicine.Name, p.Medicine.DosageForm }
                    })
                    .ToListAsync();

                return Ok(new { success = true, prescriptions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching prescriptions");
                return Ok(new { success = true, prescriptions = new List<object>() });
            }
        }

        // GET: api/prescription/{id}
        [HttpGet("api/prescription/{id}")]
        public async Task<IActionResult> GetPrescription(int id)
        {
            try
            {
                var prescription = await _context.Prescriptions
                    .Include(p => p.Patient)
                    .Include(p => p.Medicine)
                    .FirstOrDefaultAsync(p => p.PrescriptionId == id);

                if (prescription == null)
                    return Ok(new { success = false, message = "Prescription not found" });

                return Ok(new
                {
                    success = true,
                    prescription = new
                    {
                        prescription.PrescriptionId,
                        prescription.ReferenceCode,
                        prescription.Status,
                        prescription.PrescribedAt,
                        prescription.DosagePerDay,
                        prescription.DurationDays,
                        totalQuantity = prescription.DosagePerDay * prescription.DurationDays,
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
                            prescription.Medicine.DosageForm,
                            prescription.Medicine.BufferQty
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching prescription {Id}", id);
                return Ok(new { success = false, message = "An error occurred" });
            }
        }

        // POST: api/prescription
        [HttpPost("api/prescription")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> CreatePrescription([FromBody] PrescriptionRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Invalid request data" });

                var patient = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == request.PatientId && u.Role == "Patient");

                if (patient == null)
                    return BadRequest(new { success = false, message = "Patient not found" });

                var medicine = await _context.Medicines
                    .FirstOrDefaultAsync(m => m.MedId == request.MedId);

                if (medicine == null)
                    return BadRequest(new { success = false, message = "Medicine not found" });

                var referenceCode = $"RX-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";

                var prescription = new Prescription
                {
                    PatientId = request.PatientId,
                    MedId = request.MedId,
                    DosagePerDay = request.DosagePerDay,
                    DurationDays = request.DurationDays,
                    ReferenceCode = referenceCode,
                    Status = "Pending",
                    PrescribedAt = DateTime.Now
                };

                _context.Prescriptions.Add(prescription);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Prescription created successfully",
                    prescription = new
                    {
                        prescription.PrescriptionId,
                        prescription.ReferenceCode,
                        prescription.Status,
                        prescription.PrescribedAt,
                        prescription.DosagePerDay,
                        prescription.DurationDays,
                        totalQuantity = prescription.DosagePerDay * prescription.DurationDays
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating prescription");
                return StatusCode(500, new { success = false, message = "An error occurred while creating prescription" });
            }
        }

        // GET: api/prescription/search/{referenceCode}
        [HttpGet("api/prescription/search/{referenceCode}")]
        public async Task<IActionResult> SearchByReference(string referenceCode)
        {
            try
            {
                var prescription = await _context.Prescriptions
                    .Include(p => p.Patient)
                    .Include(p => p.Medicine)
                    .FirstOrDefaultAsync(p => p.ReferenceCode == referenceCode);

                if (prescription == null)
                    return Ok(new { success = false, message = "Prescription not found" });

                return Ok(new
                {
                    success = true,
                    prescription = new
                    {
                        prescription.PrescriptionId,
                        prescription.ReferenceCode,
                        prescription.Status,
                        prescription.PrescribedAt,
                        prescription.DosagePerDay,
                        prescription.DurationDays,
                        totalQuantity = prescription.DosagePerDay * prescription.DurationDays,
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
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching prescription {ReferenceCode}", referenceCode);
                return Ok(new { success = false, message = "An error occurred" });
            }
        }

        // GET: api/prescription/patients
        [HttpGet("api/prescription/patients")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> GetPatients()
        {
            try
            {
                var patients = await _context.Users
                    .Where(u => u.Role == "Patient" && u.IsActive)
                    .Select(u => new { u.UserId, u.Name, u.Email })
                    .ToListAsync();

                return Ok(new { success = true, patients });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching patients");
                return Ok(new { success = true, patients = new List<object>() });
            }
        }

        // GET: api/prescription/medicines
        [HttpGet("api/prescription/medicines")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> GetMedicines()
        {
            try
            {
                var medicines = await _context.Medicines
                    .Select(m => new { m.MedId, m.Name, m.DosageForm, m.BufferQty })
                    .ToListAsync();

                return Ok(new { success = true, medicines });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching medicines");
                return Ok(new { success = true, medicines = new List<object>() });
            }
        }

        // PUT: api/prescription/{id}/cancel
        [HttpPut("api/prescription/{id}/cancel")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> CancelPrescription(int id)
        {
            try
            {
                var prescription = await _context.Prescriptions
                    .FirstOrDefaultAsync(p => p.PrescriptionId == id);

                if (prescription == null)
                    return NotFound(new { success = false, message = "Prescription not found" });

                if (prescription.Status == "Dispensed")
                    return BadRequest(new { success = false, message = "Cannot cancel a dispensed prescription" });

                prescription.Status = "Cancelled";
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Prescription cancelled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling prescription {Id}", id);
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }
    }
}