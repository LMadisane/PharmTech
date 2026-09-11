using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;

namespace PharmTech.Services
{
    // Hosted service that runs once a day and checks for low stock and expiring medicines
    public class LowStockBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
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
                    await CheckExpiryDatesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during background stock/expiry check");
                }

                // Wait until next day 
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }

            _logger.LogInformation("LowStockBackgroundService stopping.");
        }

        private async Task CheckLowStockAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PharmTechContext>();

            // Load inventory items with related medicine and facility
            var inventoryItems = await db.InventoryItems
                .Include(ii => ii.Medicine)
                .Include(ii => ii.Facility)
                .Where(ii => ii.Medicine != null)
                .ToListAsync(cancellationToken);

            // Load custom thresholds set by Admin
            var thresholds = await db.StockThresholds
                .ToListAsync(cancellationToken);

            foreach (var item in inventoryItems)
            {
                // Check if a custom threshold exists for this medicine/facility combo
                var threshold = thresholds.FirstOrDefault(t =>
                    t.MedId == item.MedId && t.FacilityId == item.FacilityId);

                // Fall back to BufferQty on the medicine if no custom threshold is set
                var minimumLevel = threshold?.MinimumStockLevel ?? item.Medicine.BufferQty;

                if (item.Quantity <= minimumLevel)
                {
                    var message = $"Low stock: {item.Medicine.Name} at " +
                                  $"{item.Facility?.Name ?? "Unknown"} — " +
                                  $"Quantity: {item.Quantity}, Minimum: {minimumLevel}";

                    _logger.LogInformation(message);

                    // Avoid duplicate alerts - only create if one doesn't already exist today
                    var alreadyAlerted = await db.SystemAlerts.AnyAsync(a =>
                        a.AlertType == "LowStock" &&
                        a.MedId == item.MedId &&
                        a.FacilityId == item.FacilityId &&
                        a.CreatedAt.Date == DateTime.Today,
                        cancellationToken);

                    if (!alreadyAlerted)
                    {
                        db.SystemAlerts.Add(new SystemAlert
                        {
                            AlertType = "LowStock",
                            Message = message,
                            MedId = item.MedId,
                            FacilityId = item.FacilityId,
                            IsRead = false,
                            CreatedAt = DateTime.Now
                        });
                    }
                }
            }

            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Low stock check completed at {time}.", DateTimeOffset.Now);
        }

        private async Task CheckExpiryDatesAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PharmTechContext>();

            // Check for medicines expiring in 30, 60, or 90 days, AND already expired
            var warningThresholds = new[] { 30, 60, 90 };

            var batches = await db.MedicineBatches
                .Include(b => b.Medicine)
                .Where(b => b.Quantity > 0)  // Check ALL batches
                .ToListAsync(cancellationToken);

            foreach (var batch in batches)
            {
                var daysUntilExpiry = (batch.ExpiryDate - DateTime.Today).Days;
                string alertType = "ExpiryWarning";
                string message;

                // Handle expired batches
                if (daysUntilExpiry < 0)
                {
                    alertType = "ExpiredStock";
                    message = $"EXPIRED STOCK: {batch.Medicine!.Name} (Lot: {batch.LotNumber}) expired on " +
                              $"{batch.ExpiryDate:dd MMM yyyy}. Quantity: {batch.Quantity}. Immediate disposal required.";
                }
                else
                {
                    // Check each threshold (30, 60, 90 days)
                    bool alerted = false;
                    foreach (var days in warningThresholds)
                    {
                        if (daysUntilExpiry <= days && !alerted)
                        {
                            message = $"Expiry warning: {batch.Medicine!.Name} " +
                                      $"(Lot: {batch.LotNumber}) expires in " +
                                      $"{daysUntilExpiry} day(s) on " +
                                      $"{batch.ExpiryDate:dd MMM yyyy}";
                            alerted = true;

                            var alreadyAlerted = await db.SystemAlerts.AnyAsync(a =>
                                a.AlertType == "ExpiryWarning" &&
                                a.MedId == batch.MedId &&
                                a.CreatedAt.Date == DateTime.Today,
                                cancellationToken);

                            if (!alreadyAlerted)
                            {
                                db.SystemAlerts.Add(new SystemAlert
                                {
                                    AlertType = alertType,
                                    Message = message,
                                    MedId = batch.MedId,
                                    FacilityId = batch.FacilityId,
                                    IsRead = false,
                                    CreatedAt = DateTime.Now
                                });
                            }
                        }
                    }
                }

                // Handle expired batches separately (if not already handled above)
                if (daysUntilExpiry < 0)
                {
                    var alreadyAlerted = await db.SystemAlerts.AnyAsync(a =>
                        a.AlertType == "ExpiredStock" &&
                        a.MedId == batch.MedId &&
                        a.CreatedAt.Date == DateTime.Today,
                        cancellationToken);

                    if (!alreadyAlerted)
                    {
                        db.SystemAlerts.Add(new SystemAlert
                        {
                            AlertType = "ExpiredStock",
                            Message = $"EXPIRED STOCK: {batch.Medicine!.Name} (Lot: {batch.LotNumber}) expired on " +
                                      $"{batch.ExpiryDate:dd MMM yyyy}. Quantity: {batch.Quantity}. Immediate disposal required.",
                            MedId = batch.MedId,
                            FacilityId = batch.FacilityId,
                            IsRead = false,
                            CreatedAt = DateTime.Now
                        });
                    }
                }
            }
            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Expiry date check completed at {time}.", DateTimeOffset.Now);
        }
    }
}