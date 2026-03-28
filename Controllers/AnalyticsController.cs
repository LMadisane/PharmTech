using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;

namespace PharmTech.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Doctor,Pharmacist")] // All authenticated roles can view analytics
    public class AnalyticsController(PharmTechContext context) : ControllerBase
    {
        private readonly PharmTechContext _context = context;

        [HttpGet("stock")]
        public async Task<IActionResult> GetStockAnalytics()
        {
            var data = await _context.InventoryItems
                .Include(i => i.Medicine)
                .Select(i => new {
                    medicine = i.Medicine.Name,
                    quantity = i.Quantity
                })
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("returns")]
        public async Task<IActionResult> GetReturnsAnalytics()
        {
            var data = await _context.DrugReturns
                .GroupBy(r => r.MedId)
                .Select(g => new {
                    MedId = g.Key,
                    TotalReturned = g.Sum(x => x.Quantity)
                })
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("prescriptions")]
        public async Task<IActionResult> GetPrescriptionAnalytics()
        {
            var total = await _context.Prescriptions.CountAsync();
            var dispensed = await _context.Prescriptions
                .CountAsync(p => p.Status == "Dispensed");

            return Ok(new
            {
                totalPrescriptions = total,
                dispensed,
                pending = total - dispensed
            });
        }
    }
}