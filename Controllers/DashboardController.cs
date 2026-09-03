using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using System.Security.Claims;

namespace PharmTech.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly PharmTechContext _context;

        public DashboardController(PharmTechContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);

            // Determine facility filter for non‑admins
            int? facilityId = null;
            if (userRole != "Admin" && user != null && user.FacilityId.HasValue)
            {
                facilityId = user.FacilityId.Value;
            }

            // ----- Summary Cards
            // Total Medicines (global)
            ViewBag.TotalMedicines = await _context.Medicines.CountAsync();

            // Total Prescriptions, filtered by facility if non‑admin
            var prescriptionsQuery = _context.Prescriptions.AsQueryable();
            if (facilityId.HasValue)
                prescriptionsQuery = prescriptionsQuery.Where(p => p.FacilityId == facilityId.Value);
            ViewBag.TotalPrescriptions = await prescriptionsQuery.CountAsync();

            // Pending Orders, filtered by facility if non‑admin
            var ordersQuery = _context.OrderRequests.AsQueryable();
            if (facilityId.HasValue)
                ordersQuery = ordersQuery.Where(o => o.FacilityId == facilityId.Value);
            ViewBag.PendingOrders = await ordersQuery.CountAsync(o => o.Status == "Pending");

            // Pending Returns, filtered by facility if non‑admin
            var returnsQuery = _context.DrugReturns.AsQueryable();
            if (facilityId.HasValue)
                returnsQuery = returnsQuery.Where(r => r.FacilityId == facilityId.Value);
            ViewBag.PendingReturns = await returnsQuery.CountAsync(r => r.Status == "Pending");

            // Total Users, filtered by facility if non‑admin
            var usersQuery = _context.Users.AsQueryable();
            if (facilityId.HasValue)
                usersQuery = usersQuery.Where(u => u.FacilityId == facilityId.Value);
            ViewBag.TotalUsers = await usersQuery.CountAsync();

            // Total Facilities, only admin sees this
            ViewBag.TotalFacilities = await _context.Facilities.CountAsync();

            // ----- Charts
            // Prescription Status: dispensed vs pending, filtered by facility
            var statusQuery = _context.Prescriptions.AsQueryable();
            if (facilityId.HasValue)
                statusQuery = statusQuery.Where(p => p.FacilityId == facilityId.Value);

            ViewBag.DispensedPrescriptions = await statusQuery.CountAsync(p => p.Status == "Dispensed");
            ViewBag.PendingPrescriptions = await statusQuery.CountAsync(p => p.Status == "Pending");

            // Stock Levels, top 10 medicines by available stock at the facility
            var today = DateTime.Today;
            var stockQuery = _context.InventoryItems
                .Include(i => i.Medicine)
                .Where(i => i.Medicine != null);

            if (facilityId.HasValue)
                stockQuery = stockQuery.Where(i => i.FacilityId == facilityId.Value);

            var stockData = await stockQuery
                .Select(i => new
                {
                    MedicineName = i.Medicine!.Name,
                    AvailableStock = _context.MedicineBatches
                        .Where(b => b.MedId == i.MedId && b.FacilityId == i.FacilityId && b.ExpiryDate > today)
                        .Sum(b => b.Quantity)
                })
                .OrderByDescending(x => x.AvailableStock)
                .Take(10)
                .ToListAsync();

            ViewBag.StockLabels = stockData.Select(s => s.MedicineName).ToList();
            ViewBag.StockQuantities = stockData.Select(s => s.AvailableStock).ToList();

            // Drug Returns by Medicine, filtered by facility
            var returnsDataQuery = _context.DrugReturns
                .Include(r => r.Medicine)
                .Where(r => r.Medicine != null);

            if (facilityId.HasValue)
                returnsDataQuery = returnsDataQuery.Where(r => r.FacilityId == facilityId.Value);

            var returnsData = await returnsDataQuery
                .GroupBy(r => r.Medicine!.Name)
                .Select(g => new
                {
                    Medicine = g.Key,
                    TotalReturned = g.Sum(r => r.Quantity)
                })
                .OrderByDescending(x => x.TotalReturned)
                .Take(10)
                .ToListAsync();

            ViewBag.ReturnsLabels = returnsData.Select(r => r.Medicine).ToList();
            ViewBag.ReturnsQuantities = returnsData.Select(r => r.TotalReturned).ToList();

            // Unread alerts are system‑wide for now
            ViewBag.UnreadAlerts = await _context.SystemAlerts.CountAsync(a => !a.IsRead);

            return View();
        }
    }
}