using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;
using PharmTech.Services;

namespace PharmTech.Controllers
{
    public class SupplierController : Controller
    {
        private readonly PharmTechContext _context;
        private readonly ILogger<SupplierController> _logger;
        private readonly IAuditLogService _auditLogService;

        public SupplierController(
            PharmTechContext context,
            ILogger<SupplierController> logger,
            IAuditLogService auditLogService)
        {
            _context = context;
            _logger = logger;
            _auditLogService = auditLogService;
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

        // Only Admin can create a new supplier
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

                // AUDIT LOG: Supplier created
                await _auditLogService.LogAsync(
                    action: "CreateSupplier",
                    entity: "Supplier",
                    entityId: supplier.SupplierId,
                    details: $"Supplier '{supplier.Name}' created (Contact: {supplier.ContactInfo})"
                );

                return Ok(new { success = true, message = "Supplier created successfully", data = supplier });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating supplier");
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        // Admin and pharmacist retrieve all suppliers
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

        // Retrieves a single supplier by ID
        [HttpGet("api/supplier/{id}")]
        [Authorize(Roles = "Admin,Pharmacist")]
        public async Task<IActionResult> GetSupplierApi(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);

            if (supplier == null)
                return NotFound(new { success = false, message = "Supplier not found" });

            return Ok(new { success = true, supplier });
        }

        // Only Admin updates a supplier
        [HttpPut("api/supplier/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateSupplierApi(int id, [FromBody] Supplier updated)
        {
            try
            {
                var supplier = await _context.Suppliers.FindAsync(id);

                if (supplier == null)
                    return NotFound(new { success = false, message = "Supplier not found" });

                // Capture old values for audit
                var oldName = supplier.Name;
                var oldContactInfo = supplier.ContactInfo;
                var oldAddress = supplier.Address;

                supplier.Name = updated.Name;
                supplier.ContactInfo = updated.ContactInfo;
                supplier.Address = updated.Address;

                await _context.SaveChangesAsync();

                // AUDIT LOG: Supplier updated
                var details = $"Supplier '{oldName}' updated. ";
                if (oldName != updated.Name) details += $"Name: '{oldName}' → '{updated.Name}'. ";
                if (oldContactInfo != updated.ContactInfo) details += $"Contact: '{oldContactInfo}' → '{updated.ContactInfo}'. ";
                if (oldAddress != updated.Address) details += $"Address: '{oldAddress}' → '{updated.Address}'. ";

                await _auditLogService.LogAsync(
                    action: "UpdateSupplier",
                    entity: "Supplier",
                    entityId: supplier.SupplierId,
                    details: details.Trim()
                );

                return Ok(new { success = true, message = "Supplier updated successfully", data = supplier });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating supplier");
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        // Only Admin can delete a supplier
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

                var supplierName = supplier.Name;
                _context.Suppliers.Remove(supplier);
                await _context.SaveChangesAsync();

                // AUDIT LOG: Supplier deleted
                await _auditLogService.LogAsync(
                    action: "DeleteSupplier",
                    entity: "Supplier",
                    entityId: id,
                    details: $"Supplier '{supplierName}' (ID: {id}) deleted"
                );

                return Ok(new { success = true, message = "Supplier deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting supplier");
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        // Creates a new supplier from the MVC form
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateSupplier(Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                _context.Suppliers.Add(supplier);
                await _context.SaveChangesAsync();

                // AUDIT LOG: Supplier created via form
                await _auditLogService.LogAsync(
                    action: "CreateSupplier",
                    entity: "Supplier",
                    entityId: supplier.SupplierId,
                    details: $"Supplier '{supplier.Name}' created (Contact: {supplier.ContactInfo})"
                );

                TempData["Success"] = "Supplier created successfully";
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

        // Updates an existing supplier from the MVC form
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditSupplier(Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                _context.Suppliers.Update(supplier);
                await _context.SaveChangesAsync();

                // AUDIT LOG: Supplier updated via form
                await _auditLogService.LogAsync(
                    action: "UpdateSupplier",
                    entity: "Supplier",
                    entityId: supplier.SupplierId,
                    details: $"Supplier '{supplier.Name}' updated (Contact: {supplier.ContactInfo}, Address: {supplier.Address})"
                );

                TempData["Success"] = "Supplier updated successfully";
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }
    }
}