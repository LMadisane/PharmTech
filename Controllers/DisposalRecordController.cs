using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;
using System.Security.Claims;

namespace PharmTech.Controllers
{
    [Authorize(Roles = "Admin,Pharmacist")]
    public class DisposalRecordController : Controller
    {
        private readonly PharmTechContext _context;
        private readonly ILogger<DisposalRecordController> _logger;

        public DisposalRecordController(PharmTechContext context, ILogger<DisposalRecordController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ========= VIEWS

        // Displays the disposal records listing page
        public IActionResult Index()
        {
            return View();
        }

        // Displays the manual disposal creation form
        [Authorize(Roles = "Pharmacist")]
        public IActionResult Create()
        {
            return View("CreateDisposal");
        }

        // ========= API ENDPOINTS

        // Retrieves all disposal records filtered by user role/facility
        [HttpGet("api/disposal")]
        [Authorize(Roles = "Admin,Doctor,Pharmacist")]
        public async Task<IActionResult> GetDisposals()
        {
            try
            {
                // Get current user's role and facility
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var user = await _context.Users.FindAsync(userId);

                // Build base query with related data
                var query = _context.DisposalRecords
                    .Include(d => d.Medicine)
                    .Include(d => d.Facility)
                    .Include(d => d.RecordedBy)
                    .AsQueryable();

                // Non‑Admins see only their facility's disposals
                if (userRole != "Admin" && user != null && user.FacilityId.HasValue)
                {
                    query = query.Where(d => d.FacilityId == user.FacilityId.Value);
                }

                // Project to anonymous type for cleaner JSON
                var disposals = await query
                    .OrderByDescending(d => d.RecordedAt)
                    .Select(d => new
                    {
                        d.DisposalId,
                        d.MedId,
                        d.FacilityId,
                        d.Quantity,
                        d.Reason,
                        d.Notes,
                        d.RecordedAt,
                        MedicineName = d.Medicine != null ? d.Medicine.Name : "Unknown",
                        FacilityName = d.Facility != null ? d.Facility.Name : "Unknown",
                        RecordedByName = d.RecordedBy != null ? d.RecordedBy.Name : "Unknown"
                    })
                    .ToListAsync();

                return Ok(new { success = true, disposals });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching disposal records");
                return Ok(new { success = true, disposals = new List<object>() });
            }
        }

        // Creates a new disposal record and updates stock levels
        [HttpPost("api/disposal")]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> DisposeStock([FromBody] DisposalRecord record)
        {
            try
            {
                // Validate the medicine exists
                var medicine = await _context.Medicines.FindAsync(record.MedId);
                if (medicine == null)
                {
                    return BadRequest(new { success = false, message = "Medicine not found" });
                }

                // If a batch is specified, validate it exists and has enough stock
                if (record.BatchId.HasValue)
                {
                    var batch = await _context.MedicineBatches
                        .FirstOrDefaultAsync(b => b.BatchId == record.BatchId.Value);

                    if (batch == null)
                    {
                        return BadRequest(new { success = false, message = "Batch not found" });
                    }

                    if (batch.Quantity < record.Quantity)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = $"Not enough stock in batch. Available: {batch.Quantity}, Requested: {record.Quantity}"
                        });
                    }

                    // Reduce batch quantity
                    batch.Quantity -= record.Quantity;
                }

                // Reduce facility-level inventory summary
                var inventory = await _context.InventoryItems
                    .FirstOrDefaultAsync(i =>
                        i.MedId == record.MedId &&
                        i.FacilityId == record.FacilityId);

                if (inventory != null)
                {
                    if (inventory.Quantity < record.Quantity)
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = $"Not enough total stock at facility. Available: {inventory.Quantity}, Requested: {record.Quantity}"
                        });
                    }
                    inventory.Quantity -= record.Quantity;
                }
                else
                {
                    return BadRequest(new { success = false, message = "Inventory record not found for this medicine at the facility" });
                }

                // Set audit fields
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                record.RecordedById = userId;
                record.RecordedAt = DateTime.Now;

                // Add disposal record
                _context.DisposalRecords.Add(record);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Disposal recorded: Medicine {MedicineName}, Quantity {Quantity}, Reason {Reason}, Facility {FacilityId}",
                    medicine.Name, record.Quantity, record.Reason, record.FacilityId);

                return Ok(new
                {
                    success = true,
                    message = "Stock disposed successfully",
                    data = new
                    {
                        record.DisposalId,
                        record.Quantity,
                        record.Reason,
                        record.RecordedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing stock for Medicine {MedId}", record.MedId);
                return StatusCode(500, new { success = false, message = "An error occurred while disposing stock" });
            }
        }
    }
}