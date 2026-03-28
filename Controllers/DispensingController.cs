using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;


namespace PharmTech.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DispensingController(PharmTechContext context) : ControllerBase
    {
        private readonly PharmTechContext _context = context;

        // Getting Prescription by reference for pharmacist lookup
        [HttpGet("{referenceCode}")]
        [Authorize(Roles = "Pharmacist")] // Only Pharmacist looks up prescriptions for dispensing
        public async Task<IActionResult> GetByReference(string referenceCode)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Medicine)
                .FirstOrDefaultAsync(p => p.ReferenceCode == referenceCode);

            if (prescription == null)
                return NotFound("Prescription not found");

            return Ok(prescription);
        }

        // Dispensing the medicine - Only Pharmacist can dispense
        [HttpPost("dispense")]
        [Authorize(Roles = "Pharmacist")] // Only Pharmacist can dispense drugs
        public async Task<IActionResult> Dispense([FromBody] DispenseRecord request)
        {
            var prescription = await _context.Prescriptions
                .Include(p => p.Medicine)
                .FirstOrDefaultAsync(p => p.PrescriptionId == request.PrescriptionId);

            if (prescription == null)
                return NotFound("Prescription not found");

            if (prescription.Status == "Dispensed")
                return BadRequest("Already dispensed");

            // Calculating the required quantity
            int requiredQty = prescription.DosagePerDay * prescription.DurationDays;

            var inventory = await _context.InventoryItems
                .FirstOrDefaultAsync(i =>
                    i.MedId == prescription.MedId &&
                    i.FacilityId == request.FacilityId);

            if (inventory == null)
                return BadRequest("Medicine not found in inventory");

            if (inventory.Quantity < requiredQty)
                return BadRequest("Insufficient stock");

            // Deducting stock
            inventory.Quantity -= requiredQty;

            // Creating a dispense record
            var record = new DispenseRecord
            {
                PrescriptionId = prescription.PrescriptionId,
                DispensedById = request.DispensedById,
                QuantityDispensed = requiredQty
            };

            _context.DispenseRecords.Add(record);

            // Updating prescription
            prescription.Status = "Dispensed";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Dispensed successfully",
                remainingStock = inventory.Quantity
            });
        }
    }
}