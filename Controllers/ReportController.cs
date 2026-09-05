using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PharmTech.Controllers
{
    [Authorize(Roles = "Admin,Pharmacist")]
    public class ReportController : Controller
    {
        private readonly PharmTechContext _context;

        public ReportController(PharmTechContext context)
        {
            _context = context;
        }

        // ======== VIEWS
        
        // Displays the reports page with download buttons
        public IActionResult Reports()
        {
            return View();
        }

        // ======== API ENDPOINTS

        // Generates and downloads a PDF summary of today's operations
        [HttpGet("api/report/daily")]
        public async Task<IActionResult> DailyReport()
        {
            var today = DateTime.Today;

            // Gather today's data
            var dispenses = await _context.DispenseRecords
                .Include(d => d.Prescription)
                    .ThenInclude(p => p!.Medicine)
                .Include(d => d.DispensedBy)
                .Where(d => d.DispensedAt.Date == today)
                .ToListAsync();

            var returns = await _context.DrugReturns
                .Include(r => r.Medicine)
                .Where(r => r.ProcessedAt.Date == today)
                .ToListAsync();

            var orders = await _context.OrderRequests
                .Include(o => o.Medicine)
                .Where(o => o.RequestedAt.Date == today)
                .ToListAsync();

            QuestPDF.Settings.License = LicenseType.Community;

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Content().Column(col =>
                    {
                        // Header
                        col.Item().Text("💊 PharmTech — Daily Summary Report")
                            .Bold().FontSize(18).FontColor(Colors.Blue.Darken3);

                        col.Item().Text($"Date: {today:dd MMM yyyy}")
                            .FontSize(10).FontColor(Colors.Grey.Medium);

                        col.Item().PaddingVertical(8)
                            .LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Summary counts
                        col.Item().Text("Summary").Bold().FontSize(13);
                        col.Item().Text($"Total Dispenses Today: {dispenses.Count}");
                        col.Item().Text($"Total Returns Today: {returns.Count}");
                        col.Item().Text($"Total Orders Today: {orders.Count}");

                        col.Item().PaddingVertical(8)
                            .LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Dispenses section
                        col.Item().Text("Dispenses").Bold().FontSize(12);
                        if (dispenses.Any())
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Blue.Darken3)
                                        .Padding(4).Text("Medicine").Bold().FontColor(Colors.White);
                                    header.Cell().Background(Colors.Blue.Darken3)
                                        .Padding(4).Text("Qty").Bold().FontColor(Colors.White);
                                    header.Cell().Background(Colors.Blue.Darken3)
                                        .Padding(4).Text("Dispensed By").Bold().FontColor(Colors.White);
                                });

                                foreach (var d in dispenses)
                                {
                                    table.Cell().Padding(4)
                                        .Text(d.Prescription?.Medicine?.Name ?? "N/A");
                                    table.Cell().Padding(4)
                                        .Text(d.QuantityDispensed.ToString());
                                    table.Cell().Padding(4)
                                        .Text(d.DispensedBy?.Name ?? "N/A");
                                }
                            });
                        }
                        else
                        {
                            col.Item().Text("No dispenses today.")
                                .FontColor(Colors.Grey.Medium).Italic();
                        }

                        col.Item().PaddingVertical(8)
                            .LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Returns section
                        col.Item().Text("Returns").Bold().FontSize(12);
                        if (returns.Any())
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1.5f);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Blue.Darken3)
                                        .Padding(4).Text("Medicine").Bold().FontColor(Colors.White);
                                    header.Cell().Background(Colors.Blue.Darken3)
                                        .Padding(4).Text("Qty").Bold().FontColor(Colors.White);
                                    header.Cell().Background(Colors.Blue.Darken3)
                                        .Padding(4).Text("Status").Bold().FontColor(Colors.White);
                                });

                                foreach (var r in returns)
                                {
                                    table.Cell().Padding(4).Text(r.Medicine?.Name ?? "N/A");
                                    table.Cell().Padding(4).Text(r.Quantity.ToString());
                                    table.Cell().Padding(4).Text(r.Status);
                                }
                            });
                        }
                        else
                        {
                            col.Item().Text("No returns today.")
                                .FontColor(Colors.Grey.Medium).Italic();
                        }

                        col.Item().PaddingVertical(10)
                            .LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Footer
                        col.Item().Text("© 2026 PharmTech — Louis Madisane — Confidential")
                            .FontSize(8).FontColor(Colors.Grey.Lighten1).Italic();
                    });
                });
            }).GeneratePdf();

            return File(pdf, "application/pdf",
                $"DailySummary-{today:yyyyMMdd}.pdf");
        }

        // Getting weekly reports
        [HttpGet("api/report/weekly")]
        public async Task<IActionResult> WeeklyReport()
        {
            var today = DateTime.Today;
            var weekStart = today.AddDays(-7);

            // Gather this week's data
            var dispenses = await _context.DispenseRecords
                .Include(d => d.Prescription)
                    .ThenInclude(p => p!.Medicine)
                .Include(d => d.DispensedBy)
                .Where(d => d.DispensedAt.Date >= weekStart && d.DispensedAt.Date <= today)
                .ToListAsync();

            var returns = await _context.DrugReturns
                .Include(r => r.Medicine)
                .Where(r => r.ProcessedAt.Date >= weekStart && r.ProcessedAt.Date <= today)
                .ToListAsync();

            var orders = await _context.OrderRequests
                .Include(o => o.Medicine)
                .Where(o => o.RequestedAt.Date >= weekStart && o.RequestedAt.Date <= today)
                .ToListAsync();

            QuestPDF.Settings.License = LicenseType.Community;

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Content().Column(col =>
                    {
                        // Header
                        col.Item().Text("💊 PharmTech — Weekly Summary Report")
                            .Bold().FontSize(18).FontColor(Colors.Blue.Darken3);

                        col.Item().Text($"Period: {weekStart:dd MMM yyyy} — {today:dd MMM yyyy}")
                            .FontSize(10).FontColor(Colors.Grey.Medium);

                        col.Item().PaddingVertical(8)
                            .LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Summary counts
                        col.Item().Text("Summary").Bold().FontSize(13);
                        col.Item().Text($"Total Dispenses This Week: {dispenses.Count}");
                        col.Item().Text($"Total Returns This Week: {returns.Count}");
                        col.Item().Text($"Total Orders This Week: {orders.Count}");

                        col.Item().PaddingVertical(8)
                            .LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Dispenses
                        col.Item().Text("Dispenses").Bold().FontSize(12);
                        if (dispenses.Any())
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Blue.Darken3)
                                        .Padding(4).Text("Medicine").Bold().FontColor(Colors.White);
                                    header.Cell().Background(Colors.Blue.Darken3)
                                        .Padding(4).Text("Qty").Bold().FontColor(Colors.White);
                                    header.Cell().Background(Colors.Blue.Darken3)
                                        .Padding(4).Text("Dispensed By").Bold().FontColor(Colors.White);
                                    header.Cell().Background(Colors.Blue.Darken3)
                                        .Padding(4).Text("Date").Bold().FontColor(Colors.White);
                                });

                                foreach (var d in dispenses)
                                {
                                    table.Cell().Padding(4)
                                        .Text(d.Prescription?.Medicine?.Name ?? "N/A");
                                    table.Cell().Padding(4)
                                        .Text(d.QuantityDispensed.ToString());
                                    table.Cell().Padding(4)
                                        .Text(d.DispensedBy?.Name ?? "N/A");
                                    table.Cell().Padding(4)
                                        .Text(d.DispensedAt.ToString("dd MMM yyyy"));
                                }
                            });
                        }
                        else
                        {
                            col.Item().Text("No dispenses this week.")
                                .FontColor(Colors.Grey.Medium).Italic();
                        }

                        col.Item().PaddingVertical(10)
                            .LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Footer
                        col.Item().Text("© 2026 PharmTech — Louis Madisane — Confidential")
                            .FontSize(8).FontColor(Colors.Grey.Lighten1).Italic();
                    });
                });
            }).GeneratePdf();

            return File(pdf, "application/pdf",
                $"WeeklySummary-{today:yyyyMMdd}.pdf");
        }
    }
}