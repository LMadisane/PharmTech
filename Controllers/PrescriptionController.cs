using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;

[ApiController]
[Route("api/[controller]")]
public class PrescriptionController : ControllerBase
{
    private readonly PharmTechContext _context;

    public PrescriptionController(PharmTechContext context)
    {
        _context = context;
    }

    // Creating Prescriptions
    [HttpPost]
    public async Task<IActionResult> CreatePrescription([FromBody] Prescription prescription)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Generating Reference Codes
        prescription.ReferenceCode = "RX-" + DateTime.Now.Ticks;
        prescription.Status = "Pending";

        _context.Prescriptions.Add(prescription);
        await _context.SaveChangesAsync();

        return Ok(prescription);
    }

    // Getting By ID
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPrescription(int id)
    {
        var prescription = await _context.Prescriptions
            .Include(p => p.Patient)
            .Include(p => p.Medicine)
            .FirstOrDefaultAsync(p => p.PrescriptionId == id);

        if (prescription == null)
            return NotFound();

        return Ok(prescription);
    }

    // Searching By Reference Code
    [HttpGet("search")]
    public async Task<IActionResult> SearchByReference(string refCode)
    {
        var prescription = await _context.Prescriptions
            .Include(p => p.Patient)
            .Include(p => p.Medicine)
            .FirstOrDefaultAsync(p => p.ReferenceCode == refCode);

        if (prescription == null)
            return NotFound("Prescription not found");

        return Ok(prescription);
    }
}