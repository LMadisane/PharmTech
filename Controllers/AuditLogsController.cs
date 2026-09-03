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

        // ------ VIEWS CONTROLLER 

        public async Task<IActionResult> Index()
        {
            // Get recent logs for initial display
            var recentLogs = await _context.AuditLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(100)
                .ToListAsync();

            return View(recentLogs);
        }

        // ------ API ENDPOINTS 

        // Getting auditlogs
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
                var query = _context.AuditLogs.AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(user))
                    query = query.Where(l => l.UserId.ToString() == user || l.UserId.ToString().Contains(user));

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
                    .Take(limit.Value)
                    .Select(l => new
                    {
                        l.AuditLogId,
                        l.UserId,
                        l.Action,
                        l.Entity,
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

        // Getting auditlogs by id
        [HttpGet("api/auditlogs/{id}")]
        public async Task<IActionResult> GetLog(int id)
        {
            try
            {
                var log = await _context.AuditLogs.FindAsync(id);
                if (log == null)
                    return NotFound(new { success = false, message = "Log entry not found" });

                return Ok(new { success = true, log });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching audit log {Id}", id);
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        // Get distinct action types for filtering
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

        // Getting distinct entity types for filtering
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

        // Deleting a specific log entry by id
        [HttpDelete("api/auditlogs/{id}")]
        public async Task<IActionResult> DeleteLog(int id)
        {
            try
            {
                var log = await _context.AuditLogs.FindAsync(id);
                if (log == null)
                    return NotFound(new { success = false, message = "Log entry not found" });

                _context.AuditLogs.Remove(log);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Log entry deleted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting audit log {Id}", id);
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        // Deleting all log entries older than a specified date
        [HttpDelete("api/auditlogs/clear")]
        public async Task<IActionResult> ClearLogs([FromQuery] DateTime? olderThan)
        {
            try
            {
                var query = _context.AuditLogs.AsQueryable();

                if (olderThan.HasValue)
                    query = query.Where(l => l.Timestamp < olderThan.Value);

                var count = await query.CountAsync();
                _context.AuditLogs.RemoveRange(query);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = $"{count} log entries cleared" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing audit logs");
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }
    }
}