using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;

namespace PharmTech.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Pharmacist")]
    public class DisposalController : ControllerBase
    {
        private readonly PharmTechContext _context;

        public DisposalController(PharmTechContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> DisposeStock([FromBody] DisposalRecord record)
        {
            var batch = await _context.MedicineBatches
                .FirstOrDefaultAsync(b => b.BatchId == record.BatchId);

            if (batch == null)
                return BadRequest("Batch not found");

            if (batch.Quantity < record.Quantity)
                return BadRequest("Not enough stock in batch");

            // Reduce batch
            batch.Quantity -= record.Quantity;

            // Reduce inventory summary
            var inventory = await _context.InventoryItems
                .FirstOrDefaultAsync(i =>
                    i.MedId == record.MedId &&
                    i.FacilityId == record.FacilityId);

            if (inventory != null)
                inventory.Quantity -= record.Quantity;

            record.RecordedAt = DateTime.Now;

            _context.DisposalRecords.Add(record);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Stock disposed successfully" });
        }
    }
}