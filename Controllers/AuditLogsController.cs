using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;
using System.Security.Claims;

namespace PharmTech.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AuditLogsController : Controller
    {
        private readonly PharmTechContext _context;
        private readonly ILogger<AuditLogsController> _logger;

        public AuditLogsController(PharmTechContext context, ILogger<AuditLogsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ======= VIEWS

        public async Task<IActionResult> Index()
        {
            var recentLogs = await _context.AuditLogs
                .Include(a => a.User)
                .Include(a => a.Facility)
                .OrderByDescending(a => a.Timestamp)
                .Take(100)
                .ToListAsync();

            return View(recentLogs);
        }

        // ====== API ENDPOINTS

        [HttpGet("api/auditlogs")]
        public async Task<IActionResult> GetLogs(
            [FromQuery] string? user,
            [FromQuery] string? action,
            [FromQuery] string? entity,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int? limit = 200)
        {
            try
            {
                var query = _context.AuditLogs
                    .Include(a => a.User)
                    .Include(a => a.Facility)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(user))
                    query = query.Where(l => l.UserName.Contains(user) || l.UserId.ToString().Contains(user));

                if (!string.IsNullOrEmpty(action))
                    query = query.Where(l => l.Action.Contains(action));

                if (!string.IsNullOrEmpty(entity))
                    query = query.Where(l => l.Entity.Contains(entity));

                if (from.HasValue)
                    query = query.Where(l => l.Timestamp >= from.Value);

                if (to.HasValue)
                    query = query.Where(l => l.Timestamp <= to.Value);

                var logs = await query
                    .OrderByDescending(l => l.Timestamp)
                    .Take(limit ?? 200)
                    .Select(l => new
                    {
                        l.AuditLogId,
                        l.UserId,
                        l.UserName,
                        l.UserRole,
                        l.Action,
                        l.Entity,
                        l.EntityId,
                        l.Details,
                        l.PreviousValue,
                        l.NewValue,
                        l.IPAddress,
                        l.UserAgent,
                        l.FacilityId,
                        facilityName = l.Facility != null ? l.Facility.Name : null,
                        l.Timestamp
                    })
                    .ToListAsync();

                return Ok(new { success = true, logs });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching audit logs");
                return Ok(new { success = true, logs = new List<object>() });
            }
        }

        [HttpGet("api/auditlogs/actions")]
        public async Task<IActionResult> GetActionTypes()
        {
            try
            {
                var actions = await _context.AuditLogs
                    .Select(l => l.Action)
                    .Distinct()
                    .OrderBy(a => a)
                    .ToListAsync();

                return Ok(new { success = true, actions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching action types");
                return Ok(new { success = true, actions = new List<string>() });
            }
        }

        [HttpGet("api/auditlogs/entities")]
        public async Task<IActionResult> GetEntityTypes()
        {
            try
            {
                var entities = await _context.AuditLogs
                    .Select(l => l.Entity)
                    .Distinct()
                    .OrderBy(e => e)
                    .ToListAsync();

                return Ok(new { success = true, entities });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching entity types");
                return Ok(new { success = true, entities = new List<string>() });
            }
        }
    }
}