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
                    .FirstOrDefaultAsync(i => i.MedId == returnItem.MedId);

                if (inventory != null)
                {
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
            var medicines = await _context.Medicines
                .Where(m => m.LotNumber == lotNumber)
                .ToListAsync();

            if (!medicines.Any())
                return NotFound("No drugs found for this lot");

            foreach (var med in medicines)
            {
                var inventoryItems = await _context.InventoryItems
                    .Where(i => i.MedId == med.MedId)
                    .ToListAsync();

                foreach (var item in inventoryItems)
                {
                    item.Quantity = 0;
                }
            }

            await _context.SaveChangesAsync();

            return Ok("Recall completed");
        }
    }
}