using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;
using PharmTech.Services;
using System.Security.Claims;

namespace PharmTech.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RefillRequestController : ControllerBase
    {
        private readonly PharmTechContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<RefillRequestController> _logger;

        public RefillRequestController(
            PharmTechContext context,
            IAuditLogService auditLogService,
            ILogger<RefillRequestController> logger)
        {
            _context = context;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        // Pharmacist creates a refill request on behalf of a patient
        [HttpPost]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> CreateRefillRequest([FromBody] RefillRequest request)
        {
            try
            {
                // Verify the original prescription exists
                var prescription = await _context.Prescriptions
                    .Include(p => p.Patient)
                    .Include(p => p.Medicine)
                    .FirstOrDefaultAsync(p => p.PrescriptionId == request.OriginalPrescriptionId);

                if (prescription == null)
                    return NotFound("Original prescription not found");

                // Verify the doctor exists and is actually a Doctor
                var doctor = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == request.DoctorId && u.Role == "Doctor");

                if (doctor == null)
                    return BadRequest("Invalid doctor specified");

                request.Status = "Pending";
                request.RequestedAt = DateTime.Now;

                _context.RefillRequests.Add(request);
                await _context.SaveChangesAsync();

                // AUDIT LOG: Refill request created
                await _auditLogService.LogAsync(
                    action: "CreateRefillRequest",
                    entity: "RefillRequest",
                    entityId: request.RefillRequestId,
                    details: $"Refill request created for prescription {prescription.ReferenceCode} ({prescription.Medicine?.Name}) to doctor {doctor.Name}",
                    facilityId: prescription.FacilityId
                );

                return Ok(new { message = "Refill request submitted", request });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating refill request");
                return StatusCode(500, new { message = "An error occurred while creating the refill request" });
            }
        }

        // Retrieves all refill requests
        [HttpGet]
        [Authorize(Roles = "Admin,Doctor,Pharmacist")]
        public async Task<IActionResult> GetRefillRequests()
        {
            var requests = await _context.RefillRequests
                .Include(r => r.OriginalPrescription)
                    .ThenInclude(p => p!.Medicine)
                .Include(r => r.RequestedBy)
                .Include(r => r.Doctor)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            return Ok(requests);
        }

        // Retrieves a single refill request by ID
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Doctor,Pharmacist")]
        public async Task<IActionResult> GetRefillRequest(int id)
        {
            var request = await _context.RefillRequests
                .Include(r => r.OriginalPrescription)
                    .ThenInclude(p => p!.Medicine)
                .Include(r => r.RequestedBy)
                .Include(r => r.Doctor)
                .FirstOrDefaultAsync(r => r.RefillRequestId == id);

            if (request == null)
                return NotFound();

            return Ok(request);
        }

        // Doctor approves a refill request by creating a new prescription
        [HttpPut("{id}/approve")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> ApproveRefill(int id)
        {
            try
            {
                var request = await _context.RefillRequests
                    .Include(r => r.OriginalPrescription)
                        .ThenInclude(p => p!.Medicine)
                    .Include(r => r.RequestedBy)
                    .FirstOrDefaultAsync(r => r.RefillRequestId == id);

                if (request == null)
                    return NotFound();

                if (request.Status != "Pending")
                    return BadRequest("This request has already been reviewed");

                // Create a new prescription based on the original
                var original = request.OriginalPrescription!;
                var newPrescription = new Prescription
                {
                    PatientId = original.PatientId,
                    MedId = original.MedId,
                    DosagePerDay = original.DosagePerDay,
                    DurationDays = original.DurationDays,
                    ReferenceCode = "RX-" + DateTime.Now.Ticks,
                    Status = "Pending",
                    PrescribedAt = DateTime.Now,
                    FacilityId = original.FacilityId
                };

                _context.Prescriptions.Add(newPrescription);

                // Update refill request status
                request.Status = "Approved";
                request.ReviewedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                // AUDIT LOG: Refill request approved
                await _auditLogService.LogAsync(
                    action: "ApproveRefill",
                    entity: "RefillRequest",
                    entityId: request.RefillRequestId,
                    details: $"Refill request approved. New prescription {newPrescription.ReferenceCode} created for '{original.Medicine?.Name}'",
                    facilityId: original.FacilityId
                );

                return Ok(new
                {
                    message = "Refill approved and new prescription created",
                    newPrescription,
                    request
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving refill request {Id}", id);
                return StatusCode(500, new { message = "An error occurred while approving the refill request" });
            }
        }

        // Doctor rejects a refill request
        [HttpPut("{id}/reject")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> RejectRefill(int id, [FromBody] string notes)
        {
            try
            {
                var request = await _context.RefillRequests
                    .Include(r => r.OriginalPrescription)
                        .ThenInclude(p => p!.Medicine)
                    .FirstOrDefaultAsync(r => r.RefillRequestId == id);

                if (request == null)
                    return NotFound();

                if (request.Status != "Pending")
                    return BadRequest("This request has already been reviewed");

                request.Status = "Rejected";
                request.Notes = notes;
                request.ReviewedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                // AUDIT LOG: Refill request rejected
                await _auditLogService.LogAsync(
                    action: "RejectRefill",
                    entity: "RefillRequest",
                    entityId: request.RefillRequestId,
                    details: $"Refill request rejected for '{request.OriginalPrescription?.Medicine?.Name ?? "Unknown"}'. Notes: {notes}",
                    facilityId: request.OriginalPrescription?.FacilityId
                );

                return Ok(new { message = "Refill request rejected", request });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting refill request {Id}", id);
                return StatusCode(500, new { message = "An error occurred while rejecting the refill request" });
            }
        }
    }
}