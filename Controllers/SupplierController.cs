using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;

namespace PharmTech.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SupplierController(PharmTechContext context) : ControllerBase
    {
        private readonly PharmTechContext _context = context;

        // Create supplier - Admin only
        [HttpPost]
        [Authorize(Roles = "Admin")] // Only Admin can create suppliers
        public async Task<IActionResult> CreateSupplier([FromBody] Supplier supplier)
        {
            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();

            return Ok(supplier);
        }

        // Get all suppliers - Admin and Pharmacist can view
        [HttpGet]
        [Authorize(Roles = "Admin,Pharmacist")]
        public async Task<IActionResult> GetSuppliers()
        {
            var suppliers = await _context.Suppliers.ToListAsync();
            return Ok(suppliers);
        }

        // Get one supplier - Admin and Pharmacist can view
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Pharmacist")]
        public async Task<IActionResult> GetSupplier(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);

            if (supplier == null)
                return NotFound();

            return Ok(supplier);
        }

        // Updating supplier - Admin only
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")] // Only Admin can update suppliers
        public async Task<IActionResult> UpdateSupplier(int id, [FromBody] Supplier updated)
        {
            var supplier = await _context.Suppliers.FindAsync(id);

            if (supplier == null)
                return NotFound();

            supplier.Name = updated.Name;
            supplier.ContactInfo = updated.ContactInfo;
            supplier.Address = updated.Address;

            await _context.SaveChangesAsync();

            return Ok(supplier);
        }

        // Deleting supplier - Admin only
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // Only Admin can delete suppliers
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);

            if (supplier == null)
                return NotFound();

            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();

            return Ok("Deleted successfully");
        }
    }
}