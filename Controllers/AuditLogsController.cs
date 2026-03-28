using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;

namespace PharmTech.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // Only Admins can view audit logs
    public class AuditLogsController(PharmTechContext context) : ControllerBase
    {
        private readonly PharmTechContext _context = context;

        [HttpGet]
        public async Task<IActionResult> GetLogs()
        {
            var logs = await _context.AuditLogs.ToListAsync();
            return Ok(logs);
        }
    }
}