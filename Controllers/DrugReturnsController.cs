using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;
using PharmTech.Models.DTOs;
using System.Security.Claims;

namespace PharmTech.Controllers
{
    [Authorize(Roles = "Admin,Pharmacist")]
    public class DrugReturnsController : Controller
    {
        private readonly PharmTechContext _context;
        private readonly ILogger<DrugReturnsController> _logger;

        public DrugReturnsController(PharmTechContext context, ILogger<DrugReturnsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ======= VIEWS

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [ActionName("CreateReturn")]
        public IActionResult CreateReturn()
        {
            return View();
        }

        // ======= API ENDPOINTS

        // Getting all returns with optional filtering based on user role and facility
        [HttpGet("api/drugreturns")]
        public async Task<IActionResult> GetAllReturns()
        {
            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var user = await _context.Users.FindAsync(userId);

                var query = _context.DrugReturns
                    .Include(r => r.Medicine)
                    .Include(r => r.Patient)
                    .AsQueryable();

                if (userRole != "Admin" && user != null && user.FacilityId.HasValue)
                {
                    query = query.Where(r => r.FacilityId == user.FacilityId.Value);
                }

                var returns = await query
                    .OrderByDescending(r => r.ProcessedAt)
                    .Select(r => new
                    {
                        r.ReturnId,
                        r.MedId,
                        r.FacilityId,
                        r.Quantity,
                        r.Reason,
                        r.Status,
                        r.IsRestockable,
                        r.ProcessedAt,
                        medicineName = r.Medicine != null ? r.Medicine.Name : "Unknown",
                        patientName = r.Patient != null ? r.Patient.Name : "Unknown"
                    })
                    .ToListAsync();

                return Ok(new { success = true, data = returns });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching returns");
                return Ok(new { success = true, data = new List<object>() });
            }
        }

        // Geting a specific return by ID
        [HttpGet("api/drugreturns/{id}")]
        public async Task<IActionResult> GetReturn(int id)
        {
            var result = await _context.DrugReturns
                .Include(r => r.Medicine)
                .Include(r => r.Patient)
                .FirstOrDefaultAsync(r => r.ReturnId == id);

            if (result == null)
                return NotFound(new { success = false, message = "Return not found" });

            return Ok(new { success = true, data = result });
        }

        // Create a return request only if it's still pending
        [HttpPost("api/drugreturns")]
        public async Task<IActionResult> CreateReturnRequest([FromBody] CreateDrugReturnDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value?.Errors.Any() == true)
                        .Select(x => new { x.Key, Errors = x.Value?.Errors.Select(e => e.ErrorMessage) })
                        .ToList();

                    _logger.LogWarning("Model validation failed: {@Errors}", errors);
                    return BadRequest(new { success = false, message = "Invalid request data", errors });
                }

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var user = await _context.Users.FindAsync(userId);

                var medicine = await _context.Medicines.FindAsync(dto.MedId);
                if (medicine == null)
                    return BadRequest(new { success = false, message = "Medicine not found" });

                var patient = await _context.Users.FindAsync(dto.PatientId);
                if (patient == null)
                    return BadRequest(new { success = false, message = "Patient not found" });

                var model = new DrugReturn
                {
                    MedId = dto.MedId,
                    PatientId = dto.PatientId,
                    Quantity = dto.Quantity,
                    Reason = dto.Reason,
                    IsRestockable = dto.IsRestockable,
                    Status = "Pending",
                    ProcessedAt = DateTime.Now,
                    FacilityId = user?.FacilityId ?? 0
                };

                _context.DrugReturns.Add(model);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Return request created successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating return");
                return StatusCode(500, new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        // Putting a return request to approved status and optionally restocking the medicine
        [HttpPut("api/drugreturns/{id}/approve")]
        public async Task<IActionResult> ApproveReturn(int id, [FromQuery] int processedById)
        {
            try
            {
                var returnItem = await _context.DrugReturns
                    .Include(r => r.Medicine)
                    .Include(r => r.Patient)
                    .FirstOrDefaultAsync(r => r.ReturnId == id);

                if (returnItem == null)
                    return NotFound(new { success = false, message = "Return not found" });

                if (returnItem.Status != "Pending")
                    return BadRequest(new { success = false, message = "Already processed" });

                returnItem.Status = "Approved";
                returnItem.ProcessedById = processedById;

                if (returnItem.IsRestockable)
                {
                    var inventory = await _context.InventoryItems
                        .FirstOrDefaultAsync(i =>
                            i.MedId == returnItem.MedId &&
                            i.FacilityId == returnItem.FacilityId);

                    if (inventory != null)
                    {
                        var newBatch = new MedicineBatch
                        {
                            MedId = returnItem.MedId,
                            FacilityId = returnItem.FacilityId,
                            LotNumber = "RETURN-" + Guid.NewGuid().ToString().Substring(0, 6).ToUpper(),
                            Quantity = returnItem.Quantity,
                            ExpiryDate = DateTime.Now.AddMonths(6),
                            IsFlagged = true
                        };
                        _context.MedicineBatches.Add(newBatch);
                        inventory.Quantity += returnItem.Quantity;
                    }
                }

                var receipt = new Receipt
                {
                    ReceiptNumber = $"RET-{DateTime.Now:yyyyMMdd}-{returnItem.ReturnId}",
                    ReceiptType = "Return",
                    GeneratedById = processedById,
                    PatientName = returnItem.Patient?.Name ?? "Unknown",
                    MedicineName = returnItem.Medicine?.Name ?? "Unknown",
                    Quantity = returnItem.Quantity,
                    Notes = $"Return reason: {returnItem.Reason} | Restocked: {(returnItem.IsRestockable ? "Yes" : "No")}",
                    LinkedRecordId = returnItem.ReturnId,
                    GeneratedAt = DateTime.Now,
                    FacilityId = returnItem.FacilityId
                };

                _context.Receipts.Add(receipt);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Return approved successfully",
                    data = new
                    {
                        returnItem.ReturnId,
                        returnItem.Status,
                        returnItem.IsRestockable
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving return");
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        // Updating a return request to rejected status
        [HttpPut("api/drugreturns/{id}/reject")]
        public async Task<IActionResult> RejectReturn(int id, [FromQuery] int processedById)
        {
            try
            {
                var returnItem = await _context.DrugReturns
                    .FirstOrDefaultAsync(r => r.ReturnId == id);

                if (returnItem == null)
                    return NotFound(new { success = false, message = "Return not found" });

                if (returnItem.Status != "Pending")
                    return BadRequest(new { success = false, message = "Already processed" });

                returnItem.Status = "Rejected";
                returnItem.ProcessedById = processedById;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Return rejected successfully",
                    data = new
                    {
                        returnItem.ReturnId,
                        returnItem.Status
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting return");
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        // Creating a recall for a specific lot number, marking all batches with that lot number as recalled and reducing inventory accordingly
        [HttpPost("api/drugreturns/recall")]
        public async Task<IActionResult> RecallDrug([FromQuery] string lotNumber)
        {
            try
            {
                var batches = await _context.MedicineBatches
                    .Where(b => b.LotNumber == lotNumber && b.Quantity > 0)
                    .ToListAsync();

                if (!batches.Any())
                    return NotFound(new { success = false, message = "No batches found for this lot" });

                foreach (var batch in batches)
                {
                    var disposal = new DisposalRecord
                    {
                        MedId = batch.MedId,
                        FacilityId = batch.FacilityId,
                        BatchId = batch.BatchId,
                        Quantity = batch.Quantity,
                        Reason = "Recalled",
                        RecordedAt = DateTime.Now
                    };

                    _context.DisposalRecords.Add(disposal);

                    var inventory = await _context.InventoryItems
                        .FirstOrDefaultAsync(i =>
                            i.MedId == batch.MedId &&
                            i.FacilityId == batch.FacilityId);

                    if (inventory != null)
                        inventory.Quantity -= batch.Quantity;

                    batch.Quantity = 0;
                }

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Recall completed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalling drug");
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }
    }
}