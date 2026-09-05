using PharmTech.Services;

namespace PharmTech.Helpers
{
    public static class AuditLogHelper
    {
        public static async Task LogAsync(
            this IAuditLogService service,
            string action,
            string entity,
            int? entityId = null,
            string? details = null,
            string? previousValue = null,
            string? newValue = null,
            int? facilityId = null)
        {
            await service.LogAsync(action, entity, entityId, details, previousValue, newValue, facilityId);
        }
    }
}