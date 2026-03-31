using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmTech.Data;
using PharmTech.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PharmTech.Controllers
{
    [Authorize(Roles = "Admin,Doctor,Pharmacist")]
    public class ReceiptController : Controller
    {
        private readonly PharmTechContext _context;
        private readonly ILogger<ReceiptController> _logger;

        public ReceiptController(PharmTechContext context, ILogger<ReceiptController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==================== VIEW ====================

        public IActionResult Index()
        {
            return View();
        }

        // ==================== API ENDPOINTS ====================

        // GET: api/receipt
        [HttpGet("api/receipt")]
        public async Task<IActionResult> GetReceipts(
            [FromQuery] string? patientName,
            [FromQuery] string? medicineName,
            [FromQuery] string? receiptType,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            try
            {
                var query = _context.Receipts
                    .Include(r => r.GeneratedBy)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(patientName))
                    query = query.Where(r => r.PatientName.Contains(patientName));

                if (!string.IsNullOrEmpty(medicineName))
                    query = query.Where(r => r.MedicineName.Contains(medicineName));

                if (!string.IsNullOrEmpty(receiptType))
                    query = query.Where(r => r.ReceiptType == receiptType);

                if (from.HasValue)
                    query = query.Where(r => r.GeneratedAt >= from.Value);

                if (to.HasValue)
                    query = query.Where(r => r.GeneratedAt <= to.Value);

                var receipts = await query
                    .OrderByDescending(r => r.GeneratedAt)
                    .Select(r => new
                    {
                        r.ReceiptId,
                        r.ReceiptNumber,
                        r.ReceiptType,
                        r.PatientName,
                        r.MedicineName,
                        r.Quantity,
                        r.GeneratedAt,
                        generatedBy = r.GeneratedBy != null ? new { r.GeneratedBy.Name } : null
                    })
                    .ToListAsync();

                return Ok(new { success = true, receipts = receipts });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching receipts");
                return Ok(new { success = true, receipts = new List<object>() });
            }
        }

        // GET: api/receipt/{id}
        [HttpGet("api/receipt/{id}")]
        public async Task<IActionResult> GetReceipt(int id)
        {
            try
            {
                var receipt = await _context.Receipts
                    .Include(r => r.GeneratedBy)
                    .FirstOrDefaultAsync(r => r.ReceiptId == id);

                if (receipt == null)
                    return Ok(new { success = false, message = "Receipt not found" });

                return Ok(new
                {
                    success = true,
                    receipt = new
                    {
                        receipt.ReceiptId,
                        receipt.ReceiptNumber,
                        receipt.ReceiptType,
                        receipt.PatientName,
                        receipt.MedicineName,
                        receipt.Quantity,
                        receipt.Notes,
                        receipt.GeneratedAt,
                        receipt.LinkedRecordId,
                        generatedBy = receipt.GeneratedBy != null ? new { receipt.GeneratedBy.Name, receipt.GeneratedBy.Email } : null
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching receipt {Id}", id);
                return Ok(new { success = false, message = "An error occurred" });
            }
        }

        // GET: api/receipt/{id}/download
        [HttpGet("api/receipt/{id}/download")]
        public async Task<IActionResult> DownloadReceipt(int id)
        {
            try
            {
                var receipt = await _context.Receipts
                    .Include(r => r.GeneratedBy)
                    .FirstOrDefaultAsync(r => r.ReceiptId == id);

                if (receipt == null)
                    return NotFound();

                QuestPDF.Settings.License = LicenseType.Community;

                var pdf = GenerateSingleReceiptPdf(receipt);
                return File(pdf, "application/pdf", $"Receipt-{receipt.ReceiptNumber}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading receipt {Id}", id);
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        // GET: api/receipt/download/history
        [HttpGet("api/receipt/download/history")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DownloadTransactionHistory(
            [FromQuery] string? patientName,
            [FromQuery] string? medicineName,
            [FromQuery] string? receiptType,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            try
            {
                var query = _context.Receipts
                    .Include(r => r.GeneratedBy)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(patientName))
                    query = query.Where(r => r.PatientName.Contains(patientName));

                if (!string.IsNullOrEmpty(medicineName))
                    query = query.Where(r => r.MedicineName.Contains(medicineName));

                if (!string.IsNullOrEmpty(receiptType))
                    query = query.Where(r => r.ReceiptType == receiptType);

                if (from.HasValue)
                    query = query.Where(r => r.GeneratedAt >= from.Value);

                if (to.HasValue)
                    query = query.Where(r => r.GeneratedAt <= to.Value);

                var receipts = await query
                    .OrderByDescending(r => r.GeneratedAt)
                    .ToListAsync();

                if (!receipts.Any())
                    return NotFound();

                QuestPDF.Settings.License = LicenseType.Community;

                var pdf = GenerateTransactionHistoryPdf(receipts, from, to);
                return File(pdf, "application/pdf", $"TransactionHistory-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading transaction history");
                return StatusCode(500, new { success = false, message = "An error occurred" });
            }
        }

        // ==================== PDF GENERATION ====================

        private byte[] GenerateSingleReceiptPdf(Receipt receipt)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Content().Column(col =>
                    {
                        col.Item().Text("💊 PharmTech")
                            .Bold().FontSize(20).FontColor(Colors.Blue.Darken3);

                        col.Item().Text("Pharmacy Management System")
                            .FontSize(10).FontColor(Colors.Grey.Medium);

                        col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        col.Item().Text($"Receipt No: {receipt.ReceiptNumber}").Bold();
                        col.Item().Text($"Type: {receipt.ReceiptType}");
                        col.Item().Text($"Date: {receipt.GeneratedAt:dd MMM yyyy HH:mm}");
                        col.Item().Text($"Generated By: {receipt.GeneratedBy?.Name ?? "N/A"}");

                        col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        col.Item().Text("Transaction Details").Bold().FontSize(13);
                        col.Item().Text($"Patient: {receipt.PatientName}");
                        col.Item().Text($"Medicine: {receipt.MedicineName}");
                        col.Item().Text($"Quantity: {receipt.Quantity}");

                        if (!string.IsNullOrEmpty(receipt.Notes))
                            col.Item().Text($"Notes: {receipt.Notes}");

                        col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        col.Item().Text("Thank you. This is an official PharmTech receipt.")
                            .FontSize(9).FontColor(Colors.Grey.Medium).Italic();

                        col.Item().Text("© 2026 PharmTech — Louis Madisane")
                            .FontSize(8).FontColor(Colors.Grey.Lighten1);
                    });
                });
            }).GeneratePdf();
        }

        private byte[] GenerateTransactionHistoryPdf(List<Receipt> receipts, DateTime? from, DateTime? to)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Content().Column(col =>
                    {
                        col.Item().Text("💊 PharmTech — Transaction History")
                            .Bold().FontSize(18).FontColor(Colors.Blue.Darken3);

                        col.Item().Text("Pharmacy Management System")
                            .FontSize(10).FontColor(Colors.Grey.Medium);

                        var rangeText = from.HasValue && to.HasValue
                            ? $"Period: {from:dd MMM yyyy} — {to:dd MMM yyyy}"
                            : $"Generated: {DateTime.Now:dd MMM yyyy HH:mm}";

                        col.Item().Text(rangeText).FontSize(9).FontColor(Colors.Grey.Medium);

                        col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        col.Item().Text($"Total Transactions: {receipts.Count}").Bold();

                        col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Blue.Darken3)
                                    .Padding(5).Text("Receipt No").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Blue.Darken3)
                                    .Padding(5).Text("Type").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Blue.Darken3)
                                    .Padding(5).Text("Patient").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Blue.Darken3)
                                    .Padding(5).Text("Medicine").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Blue.Darken3)
                                    .Padding(5).Text("Qty").Bold().FontColor(Colors.White);
                                header.Cell().Background(Colors.Blue.Darken3)
                                    .Padding(5).Text("Date").Bold().FontColor(Colors.White);
                            });

                            foreach (var (r, index) in receipts.Select((r, i) => (r, i)))
                            {
                                var bg = index % 2 == 0 ? Colors.White : Colors.Grey.Lighten3;

                                table.Cell().Background(bg).Padding(4).Text(r.ReceiptNumber);
                                table.Cell().Background(bg).Padding(4).Text(r.ReceiptType);
                                table.Cell().Background(bg).Padding(4).Text(r.PatientName);
                                table.Cell().Background(bg).Padding(4).Text(r.MedicineName);
                                table.Cell().Background(bg).Padding(4).Text(r.Quantity.ToString());
                                table.Cell().Background(bg).Padding(4).Text(r.GeneratedAt.ToString("dd MMM yyyy"));
                            }
                        });

                        col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        col.Item().Text("© 2026 PharmTech — Louis Madisane — Confidential")
                            .FontSize(8).FontColor(Colors.Grey.Lighten1).Italic();
                    });
                });
            }).GeneratePdf();
        }
    }
}