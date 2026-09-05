namespace PharmTech.Services
{
    public interface IAuditLogService
    {
        // Automatically gets user from HttpContext
        Task LogAsync(
            string action,
            string entity,
            int? entityId = null,
            string? details = null,
            string? previousValue = null,
            string? newValue = null,
            int? facilityId = null);

        // Using explicit user info
        Task LogWithUserAsync(
            string action,
            string entity,
            int userId,
            string userName,
            string userRole,
            int? entityId = null,
            string? details = null,
            string? previousValue = null,
            string? newValue = null,
            int? facilityId = null);

        // Failed login
        Task LogSystemAsync(
            string action,
            string entity,
            string? details = null,
            int? entityId = null,
            int? facilityId = null);
    }
}