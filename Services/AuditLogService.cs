using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;
using System.Security.Claims;

namespace PharmTech.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly PharmTechContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(
            PharmTechContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuditLogService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        // Gets user from HttpContext
        public async Task LogAsync(
            string action,
            string entity,
            int? entityId = null,
            string? details = null,
            string? previousValue = null,
            string? newValue = null,
            int? facilityId = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                var user = httpContext?.User;

                int userId = 0;
                string userName = "System";
                string userRole = "System";

                if (user?.Identity?.IsAuthenticated == true)
                {
                    userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                    userName = user.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                    userRole = user.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";
                }

                var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
                var userAgent = httpContext?.Request?.Headers["User-Agent"].ToString() ?? "Unknown";

                var auditLog = new AuditLog
                {
                    UserId = userId,
                    UserName = userName,
                    UserRole = userRole,
                    Action = action,
                    Entity = entity,
                    EntityId = entityId,
                    Details = details ?? string.Empty,
                    PreviousValue = previousValue,
                    NewValue = newValue,
                    IPAddress = ipAddress,
                    UserAgent = userAgent,
                    FacilityId = facilityId,
                    Timestamp = DateTime.Now
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"AuditLog: Logged action '{action}' for entity '{entity}'");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"AuditLog: Error creating log entry for action '{action}'");
            }
        }

        // Using explicit user info
        public async Task LogWithUserAsync(
            string action,
            string entity,
            int userId,
            string userName,
            string userRole,
            int? entityId = null,
            string? details = null,
            string? previousValue = null,
            string? newValue = null,
            int? facilityId = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
                var userAgent = httpContext?.Request?.Headers["User-Agent"].ToString() ?? "Unknown";

                var auditLog = new AuditLog
                {
                    UserId = userId,
                    UserName = userName,
                    UserRole = userRole,
                    Action = action,
                    Entity = entity,
                    EntityId = entityId,
                    Details = details ?? string.Empty,
                    PreviousValue = previousValue,
                    NewValue = newValue,
                    IPAddress = ipAddress,
                    UserAgent = userAgent,
                    FacilityId = facilityId,
                    Timestamp = DateTime.Now
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"AuditLog: Logged action '{action}' for entity '{entity}' with user {userName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"AuditLog: Error creating log entry for action '{action}'");
            }
        }

        // Failed login
        public async Task LogSystemAsync(
            string action,
            string entity,
            string? details = null,
            int? entityId = null,
            int? facilityId = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
                var userAgent = httpContext?.Request?.Headers["User-Agent"].ToString() ?? "Unknown";

                var auditLog = new AuditLog
                {
                    UserId = 0,
                    UserName = "System",
                    UserRole = "System",
                    Action = action,
                    Entity = entity,
                    EntityId = entityId,
                    Details = details ?? string.Empty,
                    IPAddress = ipAddress,
                    UserAgent = userAgent,
                    FacilityId = facilityId,
                    Timestamp = DateTime.Now
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"AuditLog: System log created for action '{action}'");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"AuditLog: Error creating system log entry for action '{action}'");
            }
        }
    }
}