using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;
using PharmTech.Models.DTOs;

namespace PharmTech.Controllers
{
    [Authorize(Roles = "Admin")]
    public class FacilityController : Controller
    {
        private readonly PharmTechContext _context;
        private readonly ILogger<FacilityController> _logger;

        public FacilityController(PharmTechContext context, ILogger<FacilityController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==================== VIEWS ====================

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult CreateFacility()
        {
            return View();
        }

        public IActionResult EditFacility(int id)
        {
            ViewBag.FacilityId = id;
            return View();
        }

        // ==================== API ENDPOINTS ====================

        // GET: api/facility
        [HttpGet("api/facility")]
        public async Task<IActionResult> GetFacilities()
        {
            try
            {
                var facilities = await _context.Facilities
                    .Select(f => new
                    {
                        f.FacilityId,
                        f.Name,
                        f.Address,
                        f.ContactInfo
                    })
                    .OrderBy(f => f.Name)
                    .ToListAsync();

                return Ok(new { success = true, facilities });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching facilities");
                return Ok(new { success = true, facilities = new List<object>() });
            }
        }

        // GET: api/facility/{id}
        [HttpGet("api/facility/{id}")]
        public async Task<IActionResult> GetFacility(int id)
        {
            try
            {
                var facility = await _context.Facilities
                    .FirstOrDefaultAsync(f => f.FacilityId == id);

                if (facility == null)
                {
                    return Ok(new { success = false, message = "Facility not found" });
                }

                return Ok(new
                {
                    success = true,
                    facility = new
                    {
                        facility.FacilityId,
                        facility.Name,
                        facility.Address,
                        facility.ContactInfo
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching facility {Id}", id);
                return Ok(new { success = false, message = "An error occurred" });
            }
        }

        // POST: api/facility
        [HttpPost("api/facility")]
        public async Task<IActionResult> CreateFacility([FromBody] CreateFacilityRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { success = false, message = "Invalid request data" });

                // Check if facility with same name already exists
                var existingFacility = await _context.Facilities
                    .FirstOrDefaultAsync(f => f.Name == request.Name);

                if (existingFacility != null)
                    return BadRequest(new { success = false, message = "A facility with this name already exists" });

                var facility = new Facility
                {
                    Name = request.Name,
                    Address = request.Address,
                    ContactInfo = request.ContactInfo
                };

                _context.Facilities.Add(facility);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Facility created successfully",
                    facility = new
                    {
                        facility.FacilityId,
                        facility.Name,
                        facility.Address,
                        facility.ContactInfo
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating facility");
                return StatusCode(500, new { success = false, message = "An error occurred while creating facility" });
            }
        }

        // PUT: api/facility/{id}
        [HttpPut("api/facility/{id}")]
        public async Task<IActionResult> UpdateFacility(int id, [FromBody] UpdateFacilityRequest request)
        {
            try
            {
                var facility = await _context.Facilities.FindAsync(id);

                if (facility == null)
                    return NotFound(new { success = false, message = "Facility not found" });

                // Check if another facility has the same name (excluding this one)
                var existingFacility = await _context.Facilities
                    .FirstOrDefaultAsync(f => f.Name == request.Name && f.FacilityId != id);

                if (existingFacility != null)
                    return BadRequest(new { success = false, message = "Another facility with this name already exists" });

                facility.Name = request.Name;
                facility.Address = request.Address;
                facility.ContactInfo = request.ContactInfo;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Facility updated successfully",
                    facility = new
                    {
                        facility.FacilityId,
                        facility.Name,
                        facility.Address,
                        facility.ContactInfo
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating facility {Id}", id);
                return StatusCode(500, new { success = false, message = "An error occurred while updating facility" });
            }
        }

        // DELETE: api/facility/{id}
        [HttpDelete("api/facility/{id}")]
        public async Task<IActionResult> DeleteFacility(int id)
        {
            try
            {
                var facility = await _context.Facilities
                    .Include(f => f.Users)
                    .Include(f => f.InventoryItems)
                    .FirstOrDefaultAsync(f => f.FacilityId == id);

                if (facility == null)
                    return NotFound(new { success = false, message = "Facility not found" });

                // Check if facility has any associated users or inventory
                if (facility.Users.Any() || facility.InventoryItems.Any())
                {
                    return BadRequest(new { success = false, message = "Cannot delete facility with associated users or inventory" });
                }

                _context.Facilities.Remove(facility);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Facility deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting facility {Id}", id);
                return StatusCode(500, new { success = false, message = "An error occurred while deleting facility" });
            }
        }
    }
}