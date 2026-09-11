using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;
using PharmTech.Models.DTOs;
using PharmTech.Services;
using System.Security.Claims;

namespace PharmTech.Controllers
{
    [Authorize(Roles = "Admin,Doctor,Pharmacist")]
    public class PrescriptionController : Controller
    {
        private readonly PharmTechContext _context;
        private readonly ILogger<PrescriptionController> _logger;
        private readonly IAuditLogService _auditLogService;

        public PrescriptionController(
            PharmTechContext context,
            ILogger<PrescriptionController> logger,
            IAuditLogService auditLogService)
        {
            _context = context;
            _logger = logger;
            _auditLogService = auditLogService;
        }

        // ======== VIEWS

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

        // ------- API ENDPOINTS
        
        // Retrieves prescriptions with optional filtering
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
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var user = await _context.Users.FindAsync(userId);

                var query = _context.Prescriptions
                    .Include(p => p.Patient)
                    .Include(p => p.Medicine)
                    .Include(p => p.Facility)
                    .AsQueryable();

                if (userRole != "Admin" && user != null && user.FacilityId.HasValue)
                    query = query.Where(p => p.FacilityId == user.FacilityId.Value);

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
                        medicine = new { p.Medicine.MedId, p.Medicine.Name, p.Medicine.DosageForm },
                        facility = new { p.Facility.FacilityId, p.Facility.Name }
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

        // Retrieves a single prescription by ID
        [HttpGet("api/prescription/{id}")]
        public async Task<IActionResult> GetPrescription(int id)
        {
            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var user = await _context.Users.FindAsync(userId);

                var prescription = await _context.Prescriptions
                    .Include(p => p.Patient)
                    .Include(p => p.Medicine)
                    .Include(p => p.Facility)
                    .FirstOrDefaultAsync(p => p.PrescriptionId == id);

                if (prescription == null)
                    return Ok(new { success = false, message = "Prescription not found" });

                if (userRole != "Admin" && user != null && user.FacilityId.HasValue)
                {
                    if (prescription.FacilityId != user.FacilityId.Value)
                        return Ok(new { success = false, message = "You don't have access to this prescription" });
                }

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
                        },
                        facility = new
                        {
                            prescription.Facility.FacilityId,
                            prescription.Facility.Name
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
        
        // Doctor creates a new prescription
        [HttpPost("api/prescription")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> CreatePrescription([FromBody] PrescriptionRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(new { success = false, message = string.Join(", ", errors) });
                }

                // Get the logged in doctor
                var doctorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var doctor = await _context.Users.FindAsync(doctorId);

                if (doctor == null)
                    return BadRequest(new { success = false, message = "Doctor account not found" });

                if (!doctor.FacilityId.HasValue)
                    return BadRequest(new { success = false, message = "You are not assigned to any facility. Contact your administrator." });

                // Patients are just Users referenced by ID
                var patient = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == request.PatientId);

                if (patient == null)
                    return BadRequest(new { success = false, message = $"Patient with ID {request.PatientId} not found" });

                // Verify medicine exists
                var medicine = await _context.Medicines
                    .FirstOrDefaultAsync(m => m.MedId == request.MedId);

                if (medicine == null)
                    return BadRequest(new { success = false, message = $"Medicine with ID {request.MedId} not found" });

                // Verify facility exists
                var facility = await _context.Facilities
                    .FirstOrDefaultAsync(f => f.FacilityId == doctor.FacilityId.Value);

                if (facility == null)
                    return BadRequest(new { success = false, message = "Your assigned facility was not found in the system" });

                // Generate unique reference code
                var referenceCode = $"RX-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";

                var prescription = new Prescription
                {
                    PatientId = request.PatientId,
                    MedId = request.MedId,
                    DosagePerDay = request.DosagePerDay,
                    DurationDays = request.DurationDays,
                    ReferenceCode = referenceCode,
                    Status = "Pending",
                    PrescribedAt = DateTime.Now,
                    FacilityId = doctor.FacilityId.Value
                };

                _context.Prescriptions.Add(prescription);
                await _context.SaveChangesAsync();

                // AUDIT LOG: Prescription created
                await _auditLogService.LogAsync(
                    action: "CreatePrescription",
                    entity: "Prescription",
                    entityId: prescription.PrescriptionId,
                    details: $"Prescription {referenceCode} created for patient '{patient.Name}' - {request.DosagePerDay}x/day for {request.DurationDays} days of '{medicine.Name}'",
                    facilityId: doctor.FacilityId.Value
                );

                _logger.LogInformation(
                    "Prescription {ReferenceCode} created by Doctor {DoctorId} for Patient {PatientId}",
                    referenceCode, doctorId, request.PatientId);

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
                        totalQuantity = prescription.DosagePerDay * prescription.DurationDays,
                        patientName = patient.Name,
                        medicineName = medicine.Name,
                        facilityName = facility.Name
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating prescription: {Message} | Inner: {Inner}",
                    ex.Message, ex.InnerException?.Message);

                return StatusCode(500, new
                {
                    success = false,
                    message = $"An error occurred while creating prescription: {ex.Message}",
                    detail = ex.InnerException?.Message
                });
            }
        }

        // Retrieves all active patients for the prescription form
        [HttpGet("api/prescription/patients")]
        [Authorize(Roles = "Doctor,Pharmacist")]
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

        [HttpPost("api/prescription/test")]
        public async Task<IActionResult> TestCreate()
        {
            try
            {
                var doctorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var doctor = await _context.Users.FindAsync(doctorId);

                return Ok(new
                {
                    doctorId = doctorId,
                    doctorExists = doctor != null,
                    doctorFacilityId = doctor?.FacilityId,
                    doctorName = doctor?.Name
                });
            }
            catch (Exception ex)
            {
                return Ok(new { error = ex.Message });
            }
        }

        [HttpGet("api/prescription/medicines")]
        [Authorize(Roles = "Doctor")]
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

        // Doctor cancels a pending prescription
        [HttpPut("api/prescription/{id}/cancel")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> CancelPrescription(int id)
        {
            try
            {
                var doctorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var doctor = await _context.Users.FindAsync(doctorId);

                var prescription = await _context.Prescriptions
                    .FirstOrDefaultAsync(p => p.PrescriptionId == id);

                if (prescription == null)
                    return NotFound(new { success = false, message = "Prescription not found" });

                if (doctor != null && doctor.FacilityId.HasValue && prescription.FacilityId != doctor.FacilityId.Value)
                    return BadRequest(new { success = false, message = "You cannot cancel prescriptions from other facilities" });

                if (prescription.Status == "Dispensed")
                    return BadRequest(new { success = false, message = "Cannot cancel a dispensed prescription" });

                // Capture previous status for audit
                var previousStatus = prescription.Status;
                prescription.Status = "Cancelled";
                await _context.SaveChangesAsync();

                // AUDIT LOG: Prescription cancelled
                await _auditLogService.LogAsync(
                    action: "CancelPrescription",
                    entity: "Prescription",
                    entityId: prescription.PrescriptionId,
                    details: $"Prescription {prescription.ReferenceCode} cancelled (was: {previousStatus})",
                    previousValue: previousStatus,
                    newValue: "Cancelled",
                    facilityId: prescription.FacilityId
                );

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