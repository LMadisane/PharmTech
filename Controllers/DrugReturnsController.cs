using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;

namespace PharmTech.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Pharmacist")] // Only Admin and Pharmacist handle drug returns
    public class DrugReturnsController(PharmTechContext context) : ControllerBase
    {
        private readonly PharmTechContext _context = context;

        // Creating Return Request - (Pharmacist logs it)
        [HttpPost]
        public async Task<IActionResult> CreateReturn([FromBody] DrugReturn model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            model.Status = "Pending";
            model.ProcessedAt = DateTime.Now;

            _context.DrugReturns.Add(model);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Return request created", model });
        }

        // Get return by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetReturn(int id)
        {
            var result = await _context.DrugReturns
                .Include(r => r.Medicine)
                .Include(r => r.Patient)
                .FirstOrDefaultAsync(r => r.ReturnId == id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // Approving the return
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> ApproveReturn(int id, int processedById)
        {
            var returnItem = await _context.DrugReturns
                .FirstOrDefaultAsync(r => r.ReturnId == id);

            if (returnItem == null)
                return NotFound();

            if (returnItem.Status != "Pending")
                return BadRequest("Already processed");

            returnItem.Status = "Approved";
            returnItem.ProcessedById = processedById;

            // Only add to stock if retun is safe
            if (returnItem.IsRestockable)
            {
                var inventory = await _context.InventoryItems
                    .FirstOrDefaultAsync(i =>
                    i.MedId == returnItem.MedId &&
                    i.FacilityId == returnItem.FacilityId);

                if (inventory != null)
                {
                    var newBatch = new MedicineBatch
                    {
                        MedId = returnItem.MedId,
                        FacilityId = returnItem.FacilityId,
                        LotNumber = "RETURN-" + Guid.NewGuid().ToString().Substring(0, 6),
                        Quantity = returnItem.Quantity,
                        ExpiryDate = DateTime.Now,
                        IsFlagged = true
                    };
                    _context.MedicineBatches.Add(newBatch);
                    inventory.Quantity += returnItem.Quantity;
                }

            }
            await _context.SaveChangesAsync();

            return Ok(new { message = "Return approved", returnItem });
        }

        // Rejecting returns
        [HttpPut("{id}/reject")]
        public async Task<IActionResult> RejectReturn(int id, int processedById)
        {
            var returnItem = await _context.DrugReturns
                .FirstOrDefaultAsync(r => r.ReturnId == id);

            if (returnItem == null)
                return NotFound();

            if (returnItem.Status != "Pending")
                return BadRequest("Already processed");

            returnItem.Status = "Rejected";
            returnItem.ProcessedById = processedById;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Return rejected", returnItem });
        }

        // Recalling drugs - (Batch-based)
        [HttpPost("recall")]
        public async Task<IActionResult> RecallDrug(string lotNumber)
        {
            var batches = await _context.MedicineBatches
                .Where(b => b.LotNumber == lotNumber && b.Quantity > 0)
                .ToListAsync();

            if (!batches.Any())
                return NotFound("No batches found for this lot");

            foreach (var batch in batches)
            {
                // Move stock to disposal
                var disposal = new DisposalRecord
                {
                    MedId = batch.MedId,
                    FacilityId = batch.FacilityId,
                    BatchId = batch.BatchId,
                    Quantity = batch.Quantity,
                    Reason = "Recalled",
                    RecordedAt = DateTime.Now
                };

                _context.DisposalRecords.Add(disposal);

                // Reduce inventory summary
                var inventory = await _context.InventoryItems
                    .FirstOrDefaultAsync(i =>
                        i.MedId == batch.MedId &&
                        i.FacilityId == batch.FacilityId);

                if (inventory != null)
                    inventory.Quantity -= batch.Quantity;

                // Clear batch
                batch.Quantity = 0;
            }

            await _context.SaveChangesAsync();

            return Ok("Recall completed"); 
        }
    }
}