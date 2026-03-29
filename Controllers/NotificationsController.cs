using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;

namespace PharmTech.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Pharmacist")] // Alerts are relevant to Admin and Pharmacist
    public class NotificationsController(PharmTechContext context) : ControllerBase
    {
        private readonly PharmTechContext _context = context;

        // Get all unread alerts - used by notification bell in layout
        [HttpGet("unread")]
        public async Task<IActionResult> GetUnreadAlerts()
        {
            var alerts = await _context.SystemAlerts
                .Include(a => a.Medicine)
                .Include(a => a.Facility)
                .Where(a => !a.IsRead)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new
                {
                    a.AlertId,
                    a.AlertType,
                    a.Message,
                    medicine = a.Medicine != null ? a.Medicine.Name : null,
                    facility = a.Facility != null ? a.Facility.Name : null,
                    a.CreatedAt
                })
                .ToListAsync();

            return Ok(alerts);
        }

        // Get low stock alerts specifically - used by notifications.js
        [HttpGet("lowstock")]
        public async Task<IActionResult> GetLowStockAlerts()
        {
            var alerts = await _context.SystemAlerts
                .Include(a => a.Medicine)
                .Include(a => a.Facility)
                .Where(a => a.AlertType == "LowStock" && !a.IsRead)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new
                {
                    a.AlertId,
                    medicine = a.Medicine != null ? a.Medicine.Name : "Unknown",
                    facility = a.Facility != null ? a.Facility.Name : "Unknown",
                    a.Message,
                    quantity = a.Medicine != null ? (int?)a.Medicine.BufferQty : null,
                    a.CreatedAt
                })
                .ToListAsync();

            return Ok(alerts);
        }

        // Mark a single alert as read
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var alert = await _context.SystemAlerts.FindAsync(id);

            if (alert == null)
                return NotFound();

            alert.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Alert marked as read" });
        }

        // Mark all alerts as read
        [HttpPut("read/all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var unread = await _context.SystemAlerts
                .Where(a => !a.IsRead)
                .ToListAsync();

            unread.ForEach(a => a.IsRead = true);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"{unread.Count} alerts marked as read" });
        }
    }
}