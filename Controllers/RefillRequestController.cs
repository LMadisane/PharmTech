using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;

namespace PharmTech.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RefillRequestController(PharmTechContext context) : ControllerBase
    {
        private readonly PharmTechContext _context = context;

        // Pharmacist creates a refill request on behalf of a patient
        [HttpPost]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> CreateRefillRequest([FromBody] RefillRequest request)
        {
            // Verify the original prescription exists
            var prescription = await _context.Prescriptions
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

            return Ok(new { message = "Refill request submitted", request });
        }

        // Get all refill requests - Doctor sees pending ones, Admin sees all
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

        // Get a single refill request by ID
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

        // Doctor approves a refill request - creates a new prescription
        [HttpPut("{id}/approve")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> ApproveRefill(int id)
        {
            var request = await _context.RefillRequests
                .Include(r => r.OriginalPrescription)
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
                PrescribedAt = DateTime.Now
            };

            _context.Prescriptions.Add(newPrescription);

            // Update refill request status
            request.Status = "Approved";
            request.ReviewedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Refill approved and new prescription created",
                newPrescription,
                request
            });
        }

        // Doctor rejects a refill request
        [HttpPut("{id}/reject")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> RejectRefill(int id, [FromBody] string notes)
        {
            var request = await _context.RefillRequests
                .FirstOrDefaultAsync(r => r.RefillRequestId == id);

            if (request == null)
                return NotFound();

            if (request.Status != "Pending")
                return BadRequest("This request has already been reviewed");

            request.Status = "Rejected";
            request.Notes = notes;
            request.ReviewedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Refill request rejected", request });
        }
    }
}