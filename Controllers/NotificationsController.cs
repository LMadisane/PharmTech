using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using System.Security.Claims;

namespace PharmTech.Controllers
{
    [Authorize(Roles = "Admin,Pharmacist")]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly PharmTechContext _context;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(PharmTechContext context, ILogger<NotificationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get unread system alerts (filtered by user's facility for non-admins)
        /// </summary>
        [HttpGet("unread")]
        public async Task<IActionResult> GetUnreadAlerts()
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            var isAdmin = User.IsInRole("Admin");

            var query = _context.SystemAlerts
                .Include(a => a.Medicine)
                .Include(a => a.Facility)
                .Where(a => !a.IsRead);

            if (!isAdmin && user?.FacilityId.HasValue == true)
            {
                query = query.Where(a => a.FacilityId == user.FacilityId.Value);
            }

            var alerts = await query
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new
                {
                    a.AlertId,
                    a.AlertType,
                    a.Message,
                    MedicineName = a.Medicine != null ? a.Medicine.Name : null,
                    FacilityName = a.Facility != null ? a.Facility.Name : null,
                    a.CreatedAt
                })
                .ToListAsync();

            return Ok(alerts);
        }

        /// <summary>
        /// Get only low stock alerts
        /// </summary>
        [HttpGet("lowstock")]
        public async Task<IActionResult> GetLowStockAlerts()
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            var isAdmin = User.IsInRole("Admin");

            var query = _context.SystemAlerts
                .Include(a => a.Medicine)
                .Include(a => a.Facility)
                .Where(a => a.AlertType == "LowStock" && !a.IsRead);

            if (!isAdmin && user?.FacilityId.HasValue == true)
            {
                query = query.Where(a => a.FacilityId == user.FacilityId.Value);
            }

            var alerts = await query
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new
                {
                    a.AlertId,
                    MedicineName = a.Medicine != null ? a.Medicine.Name : "Unknown",
                    FacilityName = a.Facility != null ? a.Facility.Name : "Unknown",
                    a.Message,
                    a.CreatedAt
                })
                .ToListAsync();

            return Ok(alerts);
        }

        /// <summary>
        /// Mark a single alert as read
        /// </summary>
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var alert = await _context.SystemAlerts.FindAsync(id);
            if (alert == null) return NotFound();

            alert.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Alert marked as read" });
        }

        /// <summary>
        /// Mark all (visible) alerts as read for the current user/facility
        /// </summary>
        [HttpPut("read/all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            var isAdmin = User.IsInRole("Admin");

            var query = _context.SystemAlerts.Where(a => !a.IsRead);

            if (!isAdmin && user?.FacilityId.HasValue == true)
            {
                query = query.Where(a => a.FacilityId == user.FacilityId.Value);
            }

            var unread = await query.ToListAsync();

            foreach (var alert in unread)
                alert.IsRead = true;

            await _context.SaveChangesAsync();

            return Ok(new { message = $"{unread.Count} alerts marked as read" });
        }

        private int GetCurrentUserId()
        {
            var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(value, out int id) ? id : 0;
        }
    }
}