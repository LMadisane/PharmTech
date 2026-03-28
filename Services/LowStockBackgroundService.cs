using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;

namespace PharmTech.Services
{
    // Hosted service that runs once a day and notifies via SignalR when inventory is below buffer
    public class LowStockBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        // no SignalR hub context - service will only log and potentially create alerts in DB
        private readonly ILogger<LowStockBackgroundService> _logger;

        public LowStockBackgroundService(IServiceScopeFactory scopeFactory,
                                         ILogger<LowStockBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("LowStockBackgroundService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckLowStockAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while checking low stock");
                }

                // Wait until next day (24 hours)
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }

            _logger.LogInformation("LowStockBackgroundService stopping.");
        }

        private async Task CheckLowStockAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PharmTechContext>();

            // Load inventory items with related medicine and facility
            var lowItems = await db.InventoryItems
                .Include(ii => ii.Medicine)
                .Include(ii => ii.Facility)
                .Where(ii => ii.Medicine != null && ii.Quantity <= ii.Medicine.BufferQty)
                .ToListAsync(cancellationToken);

            if (lowItems.Count == 0)
            {
                _logger.LogInformation("No low stock items found at {time}.", DateTimeOffset.Now);
                return;
            }

            foreach (var item in lowItems)
            {
                var message = $"Low stock: {item.Medicine.Name} at {item.Facility?.Name ?? "Unknown"} - Quantity: {item.Quantity}, Buffer: {item.Medicine.BufferQty}";
                _logger.LogInformation(message);
                // Future: create database alerts or send emails here
            }
        }
    }
}
