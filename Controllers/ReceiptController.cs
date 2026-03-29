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
    [ApiController]
    [Route("api/[controller]")]
    public class ReceiptController(PharmTechContext context) : ControllerBase
    {
        private readonly PharmTechContext _context = context;

        // Generate a receipt after dispensing - Pharmacist only
        [HttpPost("generate")]
        [Authorize(Roles = "Pharmacist")]
        public async Task<IActionResult> GenerateReceipt([FromBody] Receipt receipt)
        {
            // Auto-generate receipt number
            var count = await _context.Receipts.CountAsync();
            receipt.ReceiptNumber = $"RCP-{DateTime.Now.Year}-{(count + 1):D4}";
            receipt.GeneratedAt = DateTime.Now;

            _context.Receipts.Add(receipt);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Receipt generated", receipt });
        }

        // Get a single receipt by ID - All roles
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Doctor,Pharmacist")]
        public async Task<IActionResult> GetReceipt(int id)
        {
            var receipt = await _context.Receipts
                .Include(r => r.GeneratedBy)
                .FirstOrDefaultAsync(r => r.ReceiptId == id);

            if (receipt == null)
                return NotFound("Receipt not found");

            return Ok(receipt);
        }

        // Get all receipts with filters - All roles
        [HttpGet]
        [Authorize(Roles = "Admin,Doctor,Pharmacist")]
        public async Task<IActionResult> GetReceipts(
            [FromQuery] string? patientName,
            [FromQuery] string? medicineName,
            [FromQuery] string? receiptType,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var query = _context.Receipts
                .Include(r => r.GeneratedBy)
                .AsQueryable();

            // Apply filters
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

            return Ok(receipts);
        }

        // Download a single receipt as PDF - All roles
        [HttpGet("{id}/download")]
        [Authorize(Roles = "Admin,Doctor,Pharmacist")]
        public async Task<IActionResult> DownloadReceipt(int id)
        {
            var receipt = await _context.Receipts
                .Include(r => r.GeneratedBy)
                .FirstOrDefaultAsync(r => r.ReceiptId == id);

            if (receipt == null)
                return NotFound("Receipt not found");

            // Generate PDF using QuestPDF
            var pdf = GenerateSingleReceiptPdf(receipt);

            return File(pdf, "application/pdf", $"Receipt-{receipt.ReceiptNumber}.pdf");
        }

        // Download accumulated transaction history as PDF - Admin only
        [HttpGet("download/history")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DownloadTransactionHistory(
            [FromQuery] string? patientName,
            [FromQuery] string? medicineName,
            [FromQuery] string? receiptType,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var query = _context.Receipts
                .Include(r => r.GeneratedBy)
                .AsQueryable();

            // Apply filters
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
                return NotFound("No receipts found for the selected filters");

            // Generate bulk PDF using QuestPDF
            var pdf = GenerateTransactionHistoryPdf(receipts, from, to);

            return File(pdf, "application/pdf", $"TransactionHistory-{DateTime.Now:yyyyMMdd}.pdf");
        }

        // Generate a single receipt PDF
        private byte[] GenerateSingleReceiptPdf(Receipt receipt)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Content().Column(col =>
                    {
                        // Header
                        col.Item().Text("💊 PharmTech")
                            .Bold().FontSize(20).FontColor(Colors.Blue.Darken3);

                        col.Item().Text("Pharmacy Management System")
                            .FontSize(10).FontColor(Colors.Grey.Medium);

                        col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Receipt Info
                        col.Item().Text($"Receipt No: {receipt.ReceiptNumber}").Bold();
                        col.Item().Text($"Type: {receipt.ReceiptType}");
                        col.Item().Text($"Date: {receipt.GeneratedAt:dd MMM yyyy HH:mm}");
                        col.Item().Text($"Generated By: {receipt.GeneratedBy?.Name ?? "N/A"}");

                        col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Transaction Details
                        col.Item().Text("Transaction Details").Bold().FontSize(13);
                        col.Item().Text($"Patient: {receipt.PatientName}");
                        col.Item().Text($"Medicine: {receipt.MedicineName}");
                        col.Item().Text($"Quantity: {receipt.Quantity}");

                        if (!string.IsNullOrEmpty(receipt.Notes))
                            col.Item().Text($"Notes: {receipt.Notes}");

                        col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Footer
                        col.Item().Text("Thank you. This is an official PharmTech receipt.")
                            .FontSize(9).FontColor(Colors.Grey.Medium).Italic();

                        col.Item().Text("© 2026 PharmTech — Louis Madisane")
                            .FontSize(8).FontColor(Colors.Grey.Lighten1);
                    });
                });
            }).GeneratePdf();
        }

        // Generate bulk transaction history PDF
        private byte[] GenerateTransactionHistoryPdf(
            List<Receipt> receipts,
            DateTime? from,
            DateTime? to)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Content().Column(col =>
                    {
                        // Header
                        col.Item().Text("💊 PharmTech — Transaction History")
                            .Bold().FontSize(18).FontColor(Colors.Blue.Darken3);

                        col.Item().Text("Pharmacy Management System")
                            .FontSize(10).FontColor(Colors.Grey.Medium);

                        // Date range
                        var rangeText = from.HasValue && to.HasValue
                            ? $"Period: {from:dd MMM yyyy} — {to:dd MMM yyyy}"
                            : $"Generated: {DateTime.Now:dd MMM yyyy HH:mm}";

                        col.Item().Text(rangeText).FontSize(9).FontColor(Colors.Grey.Medium);

                        col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Summary
                        col.Item().Text($"Total Transactions: {receipts.Count}").Bold();

                        col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Table
                        col.Item().Table(table =>
                        {
                            // Define columns
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2); // Receipt No
                                columns.RelativeColumn(1.5f); // Type
                                columns.RelativeColumn(2); // Patient
                                columns.RelativeColumn(2); // Medicine
                                columns.RelativeColumn(1); // Qty
                                columns.RelativeColumn(2); // Date
                            });

                            // Table header
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

                            // Table rows
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

                        // Footer
                        col.Item().Text("© 2026 PharmTech — Louis Madisane — Confidential")
                            .FontSize(8).FontColor(Colors.Grey.Lighten1).Italic();
                    });
                });
            }).GeneratePdf();
        }
    }
}