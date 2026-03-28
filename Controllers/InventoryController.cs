using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;

namespace PharmTech.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Pharmacist")] // Only Admin and Pharmacist manage inventory
    public class InventoryController(PharmTechContext context) : ControllerBase
    {
        private readonly PharmTechContext _context = context;

        //Making predictions
        [HttpGet("predict")]
        public async Task<IActionResult> PredictStock(int medId, int facilityId)
        {
            var inventory = await _context.InventoryItems
                .FirstOrDefaultAsync(i => i.MedId == medId && i.FacilityId == facilityId);

            if (inventory == null)
                return NotFound("Inventory not found");

            var dispenseRecords = await _context.DispenseRecords
                .Where(d => d.Prescription.MedId == medId)
                .Include(d => d.Prescription)
                .ToListAsync();

            if (!dispenseRecords.Any())
                return Ok("Not enough data for prediction");

            var totalDispensed = dispenseRecords.Sum(d => d.QuantityDispensed);

            var days = (DateTime.Now - dispenseRecords.Min(d => d.DispensedAt)).Days;
            if (days == 0) days = 1;

            var dailyUsage = totalDispensed / days;

            if (dailyUsage == 0)
                return Ok("No usage data");

            var daysRemaining = inventory.Quantity / dailyUsage;

            var depletionDate = DateTime.Now.AddDays(daysRemaining);

            return Ok(new
            {
                currentStock = inventory.Quantity,
                dailyUsage,
                daysRemaining,
                estimatedDepletionDate = depletionDate
            });
        }
    }
}