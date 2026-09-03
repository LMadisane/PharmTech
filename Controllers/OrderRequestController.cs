using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;
using System.Security.Claims;

namespace PharmTech.Controllers
{
    [Authorize(Roles = "Admin,Pharmacist")]
    public class OrderRequestController : Controller
    {
        private readonly PharmTechContext _context;
        private readonly ILogger<OrderRequestController> _logger;

        public OrderRequestController(PharmTechContext context, ILogger<OrderRequestController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ========= VIEWS

        // Pharmacist views own orders, Admin: view all
        public async Task<IActionResult> Index()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            IQueryable<OrderRequest> query = _context.OrderRequests
                .Include(o => o.Medicine)
                .Include(o => o.Supplier)
                .Include(o => o.RequestedBy)
                .Include(o => o.ApprovedBy)
                .AsQueryable();

            if (userRole == "Pharmacist")
            {
                query = query.Where(o => o.RequestedById == userId);
            }

            var orders = await query
                .OrderByDescending(o => o.RequestedAt)
                .ToListAsync();

            return View(orders);
        }

        // Pharmacist: create new order request
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> CreateRequest()
        {
            ViewBag.Medicines = await _context.Medicines
                .OrderBy(m => m.Name)
                .ToListAsync();
            ViewBag.Suppliers = await _context.Suppliers
                .OrderBy(s => s.Name)
                .ToListAsync();
            return View();
        }

        // Pharmacist: create new order request
        [HttpPost]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> CreateRequest(OrderRequest order)
        {
            // Set system-assigned values before validation
            order.Status = "Pending";
            order.RequestedAt = DateTime.Now;

            // Clear ModelState errors for system-assigned fields
            ModelState.Remove("Status");
            ModelState.Remove("RequestedAt");
            ModelState.Remove("RequestedById");
            ModelState.Remove("FacilityId");

            if (!ModelState.IsValid)
            {
                ViewBag.Medicines = await _context.Medicines.OrderBy(m => m.Name).ToListAsync();
                ViewBag.Suppliers = await _context.Suppliers.OrderBy(s => s.Name).ToListAsync();
                return View(order);
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);

            order.RequestedById = userId;

            if (user?.FacilityId == null)
            {
                ModelState.AddModelError("", "You are not assigned to any facility. Please contact admin.");
                ViewBag.Medicines = await _context.Medicines.OrderBy(m => m.Name).ToListAsync();
                ViewBag.Suppliers = await _context.Suppliers.OrderBy(s => s.Name).ToListAsync();
                return View(order);
            }

            order.FacilityId = user.FacilityId.Value;

            _context.OrderRequests.Add(order);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Order request submitted successfully!";
            return RedirectToAction(nameof(Index));
        }

        // Admin: view all pending requests
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminIndex()
        {
            var orders = await _context.OrderRequests
                .Include(o => o.Medicine)
                .Include(o => o.Supplier)
                .Include(o => o.RequestedBy)
                .Where(o => o.Status == "Pending")
                .OrderByDescending(o => o.RequestedAt)
                .ToListAsync();
            return View(orders);
        }

        // Admin: view all orders
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AllOrders()
        {
            var orders = await _context.OrderRequests
                .Include(o => o.Medicine)
                .Include(o => o.Supplier)
                .Include(o => o.RequestedBy)
                .Include(o => o.ApprovedBy)
                .OrderByDescending(o => o.RequestedAt)
                .ToListAsync();
            return View(orders);
        }

        // ======== API ENDPOINTS

        [HttpGet("api/orderrequest")]
        public async Task<IActionResult> GetOrders()
        {
            try
            {
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var query = _context.OrderRequests
                    .Include(o => o.Medicine)
                    .Include(o => o.Supplier)
                    .Include(o => o.RequestedBy)
                    .Include(o => o.ApprovedBy)
                    .AsQueryable();

                if (userRole == "Pharmacist")
                    query = query.Where(o => o.RequestedById == userId);

                var orders = await query
                    .OrderByDescending(o => o.RequestedAt)
                    .Select(o => new
                    {
                        o.OrderId,
                        o.MedId,
                        o.Quantity,
                        o.Status,
                        o.RequestedAt,
                        o.FulfilledAt,
                        medicineName = o.Medicine != null ? o.Medicine.Name : null,
                        supplierName = o.Supplier != null ? o.Supplier.Name : null,
                        requestedByName = o.RequestedBy != null ? o.RequestedBy.Name : null,
                        approvedByName = o.ApprovedBy != null ? o.ApprovedBy.Name : null
                    })
                    .ToListAsync();

                return Ok(new { success = true, orders });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching orders");
                return Ok(new { success = true, orders = new List<object>() });
            }
        }

        [HttpPost("api/orderrequest")]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> CreateOrder([FromBody] OrderRequest order)
        {
            try
            {
                // Set system-assigned values first
                order.Status = "Pending";
                order.RequestedAt = DateTime.Now;

                // Clear system-assigned fields from ModelState before validating
                ModelState.Remove("Status");
                ModelState.Remove("RequestedAt");
                ModelState.Remove("RequestedById");
                ModelState.Remove("FacilityId");

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(new { success = false, message = string.Join(", ", errors) });
                }

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var user = await _context.Users.FindAsync(userId);

                order.RequestedById = userId;

                if (user?.FacilityId != null)
                    order.FacilityId = user.FacilityId.Value;

                _context.OrderRequests.Add(order);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Order request created",
                    orderId = order.OrderId,
                    status = order.Status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        [HttpPut("api/orderrequest/{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveOrder(int id)
        {
            try
            {
                var order = await _context.OrderRequests.FindAsync(id);
                if (order == null)
                    return NotFound(new { success = false, message = "Order not found" });

                if (order.Status != "Pending")
                    return BadRequest(new { success = false, message = "Order already processed" });

                var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                order.Status = "Approved";
                order.ApprovedById = adminId;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Order approved" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving order");
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        [HttpPut("api/orderrequest/{id}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectOrder(int id)
        {
            try
            {
                var order = await _context.OrderRequests.FindAsync(id);
                if (order == null)
                    return NotFound(new { success = false, message = "Order not found" });

                if (order.Status != "Pending")
                    return BadRequest(new { success = false, message = "Order already processed" });

                var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                order.Status = "Rejected";
                order.ApprovedById = adminId;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Order rejected" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting order");
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        [HttpPut("api/orderrequest/{id}/fulfill")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> FulfillOrder(int id)
        {
            try
            {
                var order = await _context.OrderRequests
                    .Include(o => o.Medicine)
                    .FirstOrDefaultAsync(o => o.OrderId == id);

                if (order == null)
                    return NotFound(new { success = false, message = "Order not found" });

                if (order.Status != "Approved")
                    return BadRequest(new { success = false, message = "Order must be approved first" });

                var inventory = await _context.InventoryItems
                    .FirstOrDefaultAsync(i => i.MedId == order.MedId && i.FacilityId == order.FacilityId);

                if (inventory != null)
                {
                    inventory.Quantity += order.Quantity;
                }
                else
                {
                    inventory = new InventoryItem
                    {
                        MedId = order.MedId,
                        FacilityId = order.FacilityId,
                        Quantity = order.Quantity
                    };
                    _context.InventoryItems.Add(inventory);
                }

                var batch = new MedicineBatch
                {
                    MedId = order.MedId,
                    FacilityId = order.FacilityId,
                    LotNumber = "ORDER-" + order.OrderId + "-" + DateTime.Now.ToString("yyyyMMdd"),
                    Quantity = order.Quantity,
                    ExpiryDate = DateTime.Now.AddMonths(24),
                    DateReceived = DateTime.Now,
                    IsFlagged = false
                };
                _context.MedicineBatches.Add(batch);

                order.Status = "Fulfilled";
                order.FulfilledAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Order fulfilled, stock added" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fulfilling order");
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }
    }
}