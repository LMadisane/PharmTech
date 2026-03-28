using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;

namespace PharmTech.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderRequestController(PharmTechContext context) : ControllerBase
    {
        private readonly PharmTechContext _context = context;

        // Creating order request - Pharmacist and Admin can create
        [HttpPost]
        [Authorize(Roles = "Admin,Pharmacist")]
        public async Task<IActionResult> CreateOrder([FromBody] OrderRequest order)
        {
            order.Status = "Pending";
            order.RequestedAt = DateTime.Now;

            _context.OrderRequests.Add(order);
            await _context.SaveChangesAsync();

            return Ok(order);
        }

        // Getting all orders - Pharmacist and Admin can view
        [HttpGet]
        [Authorize(Roles = "Admin,Pharmacist")]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _context.OrderRequests
                .Include(o => o.Medicine)
                .Include(o => o.Supplier)
                .ToListAsync();

            return Ok(orders);
        }

        // Approving order - Admin only
        [HttpPut("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveOrder(int id, int approvedById)
        {
            var order = await _context.OrderRequests.FindAsync(id);

            if (order == null)
                return NotFound();

            order.Status = "Approved";
            order.ApprovedById = approvedById;

            await _context.SaveChangesAsync();

            return Ok(order);
        }

        // Rejecting order - Admin only
        [HttpPut("{id}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectOrder(int id, int approvedById)
        {
            var order = await _context.OrderRequests.FindAsync(id);

            if (order == null)
                return NotFound();

            order.Status = "Rejected";
            order.ApprovedById = approvedById;

            await _context.SaveChangesAsync();

            return Ok(order);
        }

        // Marking as fulfilled (Stock arrives) - Admin only
        [HttpPut("{id}/fulfill")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> FulfillOrder(int id)
        {
            var order = await _context.OrderRequests
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
                return NotFound();

            if (order.Status != "Approved")
                return BadRequest("Order must be approved first");

            // Add stock to inventory
            var inventory = await _context.InventoryItems
                .FirstOrDefaultAsync(i =>
                    i.MedId == order.MedId &&
                    i.FacilityId == order.FacilityId);

            if (inventory != null)
            {
                inventory.Quantity += order.Quantity;
            }
            else
            {
                // Create new inventory record if none exists
                inventory = new InventoryItem
                {
                    MedId = order.MedId,
                    FacilityId = order.FacilityId,
                    Quantity = order.Quantity
                };

                _context.InventoryItems.Add(inventory);
            }

            order.Status = "Fulfilled";
            order.FulfilledAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(order);
        }
    }
}