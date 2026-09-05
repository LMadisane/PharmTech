using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;
using PharmTech.Models.DTOs;
using System.Security.Claims;
using PharmTech.Services;

namespace PharmTech.Controllers
{
    [Authorize(Roles = "Admin,Pharmacist,Doctor")]
    public class InventoryController : Controller
    {
        private readonly PharmTechContext _context;
        private readonly ILogger<InventoryController> _logger;
        private readonly IAuditLogService _auditLogService;

        public InventoryController(
            PharmTechContext context,
            ILogger<InventoryController> logger,
            IAuditLogService auditLogService)
        {
            _context = context;
            _logger = logger;
            _auditLogService = auditLogService;
        }

        // ========= VIEWS

        public IActionResult Index()
        {
            return View();
        }

        [Authorize(Roles = "Admin,Pharmacist")]
        public IActionResult AddStock()
        {
            return View();
        }

        [Authorize(Roles = "Admin,Pharmacist")]
        public IActionResult EditBatch(int id)
        {
            ViewBag.BatchId = id;
            return View();
        }

        // ========= API ENDPOINTS

        [HttpGet("api/inventory")]
        public async Task<IActionResult> GetInventory()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var user = await _context.Users.FindAsync(userId);
                var today = DateTime.Today;

                // Get all medicines and facilities, but filter by user facility if not admin
                var query = _context.InventoryItems
                    .Include(i => i.Medicine)
                    .Include(i => i.Facility)
                    .AsQueryable();

                if (!User.IsInRole("Admin"))
                {
                    if (user != null && user.FacilityId.HasValue)
                        query = query.Where(i => i.FacilityId == user.FacilityId.Value);
                    else
                        return Ok(new { success = true, inventory = new List<object>() });
                }

                // For each inventory item, compute available stock from non-expired batches
                var inventory = await query
                    .Select(i => new
                    {
                        i.InventoryId,
                        medicineId = i.Medicine != null ? i.Medicine.MedId : 0,
                        medicineName = i.Medicine != null ? i.Medicine.Name : "Unknown",
                        facilityId = i.Facility != null ? i.Facility.FacilityId : 0,
                        facilityName = i.Facility != null ? i.Facility.Name : "Unknown",
                        bufferQty = i.Medicine != null ? i.Medicine.BufferQty : 0,
                        availableQuantity = _context.MedicineBatches // Available stock = sum of batch quantities where expiry date > today
                            .Where(b => b.MedId == i.MedId && b.FacilityId == i.FacilityId && b.ExpiryDate > today)
                            .Sum(b => b.Quantity),
                        expiredQuantity = _context.MedicineBatches // Also get expired stock for reference
                            .Where(b => b.MedId == i.MedId && b.FacilityId == i.FacilityId && b.ExpiryDate <= today)
                            .Sum(b => b.Quantity)
                    })
                    .OrderBy(i => i.medicineName)
                    .ToListAsync();

                return Ok(new { success = true, inventory });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching inventory");
                return Ok(new { success = true, inventory = new List<object>() });
            }
        }

        [HttpGet("api/inventory/batches")]
        public async Task<IActionResult> GetBatches([FromQuery] int? facilityId, [FromQuery] int? medicineId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var user = await _context.Users.FindAsync(userId);

                var allBatches = await _context.MedicineBatches
                    .Include(b => b.Medicine)
                    .Include(b => b.Facility)
                    .ToListAsync();

                var filtered = allBatches.AsEnumerable();

                if (!User.IsInRole("Admin") && user != null && user.FacilityId.HasValue)
                {
                    filtered = filtered.Where(b => b.FacilityId == user.FacilityId.Value);
                }

                if (facilityId.HasValue)
                {
                    filtered = filtered.Where(b => b.FacilityId == facilityId.Value);
                }

                if (medicineId.HasValue)
                {
                    filtered = filtered.Where(b => b.MedId == medicineId.Value);
                }

                var batches = filtered
                    .OrderBy(b => b.ExpiryDate)
                    .Select(b => new
                    {
                        b.BatchId,
                        b.LotNumber,
                        b.Quantity,
                        b.ExpiryDate,
                        b.DateReceived,
                        b.IsFlagged,
                        medicineId = b.MedId,
                        medicineName = b.Medicine != null ? b.Medicine.Name : "Unknown",
                        facilityId = b.FacilityId,
                        facilityName = b.Facility != null ? b.Facility.Name : "Unknown",
                        daysUntilExpiry = (b.ExpiryDate - DateTime.Today).Days
                    })
                    .ToList();

                return Ok(new { success = true, batches });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching batches: {Message}", ex.Message);
                return Ok(new { success = true, batches = new List<object>() });
            }
        }

        [HttpGet("api/inventory/batches/expiring")]
        public async Task<IActionResult> GetExpiringBatches([FromQuery] int days = 90)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var user = await _context.Users.FindAsync(userId);

                var allBatches = await _context.MedicineBatches
                    .Include(b => b.Medicine)
                    .Include(b => b.Facility)
                    .ToListAsync();

                var filtered = allBatches.AsEnumerable();

                if (!User.IsInRole("Admin") && user != null && user.FacilityId.HasValue)
                {
                    filtered = filtered.Where(b => b.FacilityId == user.FacilityId.Value);
                }

                var cutoffDate = DateTime.Today.AddDays(days);
                var expiringBatches = filtered
                    .Where(b => b.ExpiryDate <= cutoffDate)
                    .OrderBy(b => b.ExpiryDate)
                    .Select(b => new
                    {
                        b.BatchId,
                        b.LotNumber,
                        b.Quantity,
                        b.ExpiryDate,
                        b.IsFlagged,
                        medicineName = b.Medicine != null ? b.Medicine.Name : "Unknown",
                        facilityName = b.Facility != null ? b.Facility.Name : "Unknown",
                        daysUntilExpiry = (b.ExpiryDate - DateTime.Today).Days
                    })
                    .ToList();

                return Ok(new { success = true, expiringBatches });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching expiring batches: {Message}", ex.Message);
                return Ok(new { success = true, expiringBatches = new List<object>() });
            }
        }

        // Adds a new batch of medicine to inventory (Admin & Pharmacist)
        [HttpPost("api/inventory/batch")]
        [Authorize(Roles = "Admin,Pharmacist")]
        public async Task<IActionResult> AddBatch([FromBody] AddBatchRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid request data" });
                }

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var user = await _context.Users.FindAsync(userId);

                if (User.IsInRole("Pharmacist") && !User.IsInRole("Admin"))
                {
                    if (user == null || !user.FacilityId.HasValue)
                    {
                        return BadRequest(new { success = false, message = "You are not assigned to any facility" });
                    }

                    if (request.FacilityId != user.FacilityId.Value)
                    {
                        return BadRequest(new { success = false, message = "You can only add stock to your assigned facility" });
                    }
                }

                var medicine = await _context.Medicines.FindAsync(request.MedicineId);
                if (medicine == null)
                {
                    return BadRequest(new { success = false, message = "Medicine not found" });
                }

                var facility = await _context.Facilities.FindAsync(request.FacilityId);
                if (facility == null)
                {
                    return BadRequest(new { success = false, message = "Facility not found" });
                }

                var existingBatch = await _context.MedicineBatches
                    .FirstOrDefaultAsync(b => b.LotNumber == request.LotNumber);

                if (existingBatch != null)
                {
                    return BadRequest(new { success = false, message = "A batch with this lot number already exists" });
                }

                var batch = new MedicineBatch
                {
                    MedId = request.MedicineId,
                    FacilityId = request.FacilityId,
                    LotNumber = request.LotNumber,
                    Quantity = request.Quantity,
                    ExpiryDate = request.ExpiryDate,
                    DateReceived = DateTime.Now,
                    IsFlagged = request.ExpiryDate <= DateTime.Now.AddDays(90)
                };

                _context.MedicineBatches.Add(batch);

                var inventory = await _context.InventoryItems
                    .FirstOrDefaultAsync(i => i.MedId == request.MedicineId && i.FacilityId == request.FacilityId);

                if (inventory != null)
                {
                    inventory.Quantity += request.Quantity;
                }
                else
                {
                    inventory = new InventoryItem
                    {
                        MedId = request.MedicineId,
                        FacilityId = request.FacilityId,
                        Quantity = request.Quantity
                    };
                    _context.InventoryItems.Add(inventory);
                }

                await _context.SaveChangesAsync();

                // AUDIT LOG: Stock added (batch created)
                await _auditLogService.LogAsync(
                    action: "AddBatch",
                    entity: "MedicineBatch",
                    entityId: batch.BatchId,
                    details: $"Added {request.Quantity} units of '{medicine.Name}' (Lot: {request.LotNumber}) to facility '{facility.Name}' (Expiry: {request.ExpiryDate:yyyy-MM-dd})",
                    facilityId: request.FacilityId
                );

                return Ok(new
                {
                    success = true,
                    message = "Stock added successfully",
                    batch = new
                    {
                        batch.BatchId,
                        batch.LotNumber,
                        batch.Quantity,
                        batch.ExpiryDate
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding batch");
                return StatusCode(500, new { success = false, message = "An error occurred while adding stock" });
            }
        }

        [HttpGet("api/inventory/medicines")]
        public async Task<IActionResult> GetMedicines()
        {
            try
            {
                var medicines = await _context.Medicines
                    .Select(m => new { m.MedId, m.Name, m.DosageForm })
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

        [HttpGet("api/inventory/facilities")]
        public async Task<IActionResult> GetFacilities()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var user = await _context.Users.FindAsync(userId);

                if (!User.IsInRole("Admin"))
                {
                    if (user != null && user.FacilityId.HasValue)
                    {
                        var facility = await _context.Facilities
                            .Where(f => f.FacilityId == user.FacilityId.Value)
                            .Select(f => new { f.FacilityId, f.Name })
                            .FirstOrDefaultAsync();

                        return Ok(new { success = true, facilities = facility != null ? new[] { facility } : Array.Empty<object>() });
                    }

                    return Ok(new { success = true, facilities = Array.Empty<object>() });
                }

                var facilities = await _context.Facilities
                    .Select(f => new { f.FacilityId, f.Name })
                    .ToListAsync();

                return Ok(new { success = true, facilities });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching facilities");
                return Ok(new { success = true, facilities = Array.Empty<object>() });
            }
        }
    }
}