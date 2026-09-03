using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;

namespace PharmTech.Controllers
{
    public class SupplierController : Controller
    {
        private readonly PharmTechContext _context;
        private readonly ILogger<SupplierController> _logger;

        public SupplierController(PharmTechContext context, ILogger<SupplierController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ======== VIEWS

        [Authorize(Roles = "Admin,Pharmacist")]
        public async Task<IActionResult> Index()
        {
            var suppliers = await _context.Suppliers
                .OrderBy(s => s.Name)
                .ToListAsync();
            return View(suppliers);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult CreateSupplier()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditSupplier(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
                return NotFound();
            return View(supplier);
        }

        // ========= API ENDPOINTS

        // Create a new supplier (Admin only)
        [HttpPost("api/supplier")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateSupplierApi([FromBody] Supplier supplier)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Invalid request data" });

                _context.Suppliers.Add(supplier);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Supplier created successfully", data = supplier });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating supplier");
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        // Getting all suppliers as Admin, Pharmacist
        [HttpGet("api/supplier")]
        [Authorize(Roles = "Admin,Pharmacist")]
        public async Task<IActionResult> GetSuppliersApi()
        {
            try
            {
                var suppliers = await _context.Suppliers
                    .Select(s => new { s.SupplierId, s.Name, s.ContactInfo, s.Address })
                    .OrderBy(s => s.Name)
                    .ToListAsync();
                return Ok(new { success = true, suppliers });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching suppliers");
                return Ok(new { success = true, suppliers = new List<object>() });
            }
        }

        // Getting a specific supplier by ID as Admin, Pharmacist
        [HttpGet("api/supplier/{id}")]
        [Authorize(Roles = "Admin,Pharmacist")]
        public async Task<IActionResult> GetSupplierApi(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);

            if (supplier == null)
                return NotFound(new { success = false, message = "Supplier not found" });

            return Ok(new { success = true, supplier });
        }

        // Putting an update to a specific supplier by ID as Admin
        [HttpPut("api/supplier/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateSupplierApi(int id, [FromBody] Supplier updated)
        {
            try
            {
                var supplier = await _context.Suppliers.FindAsync(id);

                if (supplier == null)
                    return NotFound(new { success = false, message = "Supplier not found" });

                supplier.Name = updated.Name;
                supplier.ContactInfo = updated.ContactInfo;
                supplier.Address = updated.Address;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Supplier updated successfully", data = supplier });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating supplier");
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        // Deleting a specific supplier by ID as Admin, with check for associated order requests
        [HttpDelete("api/supplier/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteSupplierApi(int id)
        {
            try
            {
                var supplier = await _context.Suppliers.FindAsync(id);

                if (supplier == null)
                    return NotFound(new { success = false, message = "Supplier not found" });

                // Check if supplier has any associated order requests
                var hasOrders = await _context.OrderRequests.AnyAsync(o => o.SupplierId == id);
                if (hasOrders)
                {
                    return BadRequest(new { success = false, message = "Cannot delete supplier with associated order requests" });
                }

                _context.Suppliers.Remove(supplier);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Supplier deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting supplier");
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        // Form post actions for creating and editing suppliers for Admin only

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateSupplier(Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                _context.Suppliers.Add(supplier);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Supplier created successfully";
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditSupplier(Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                _context.Suppliers.Update(supplier);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Supplier updated successfully";
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }
    }
}