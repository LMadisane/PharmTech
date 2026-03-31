using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;

namespace PharmTech.Controllers
{
    [Authorize] // All authenticated roles can see the dashboard
    public class DashboardController(PharmTechContext context) : Controller
    {
        private readonly PharmTechContext _context = context;

        public async Task<IActionResult> Index()
        {
            // Total counts for summary cards
            ViewBag.TotalMedicines = await _context.Medicines.CountAsync();
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalPrescriptions = await _context.Prescriptions.CountAsync();
            ViewBag.PendingOrders = await _context.OrderRequests
                .CountAsync(o => o.Status == "Pending");
            ViewBag.PendingReturns = await _context.DrugReturns
                .CountAsync(r => r.Status == "Pending");
            ViewBag.TotalFacilities = await _context.Facilities.CountAsync();

            // Prescription status for doughnut chart
            ViewBag.DispensedPrescriptions = await _context.Prescriptions
                .CountAsync(p => p.Status == "Dispensed");
            ViewBag.PendingPrescriptions = await _context.Prescriptions
                .CountAsync(p => p.Status == "Pending");

            // Stock levels for bar chart - top 10 medicines by quantity
            var stockData = await _context.InventoryItems
                .Include(i => i.Medicine)
                .GroupBy(i => i.Medicine.Name)
                .Select(g => new
                {
                    Medicine = g.Key,
                    TotalQuantity = g.Sum(i => i.Quantity)
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(10)
                .ToListAsync();

            ViewBag.StockLabels = stockData.Select(s => s.Medicine).ToList();
            ViewBag.StockQuantities = stockData.Select(s => s.TotalQuantity).ToList();

            // Drug returns by medicine for returns chart
            var returnsData = await _context.DrugReturns
                .Include(r => r.Medicine)
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

            // Unread system alerts count for the dashboard
            ViewBag.UnreadAlerts = await _context.SystemAlerts
                .CountAsync(a => !a.IsRead);

            return View();
        }
    }
}