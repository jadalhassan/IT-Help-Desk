using System.Globalization;
using ClosedXML.Excel;
using HelpDesk.Api.Dtos;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace HelpDesk.Api.Services;

public sealed class ReportExportService : IReportExportService
{
    private static readonly XColor Navy = XColor.FromArgb(18, 39, 66);
    private static readonly XColor Blue = XColor.FromArgb(42, 111, 151);
    private static readonly XColor Slate = XColor.FromArgb(83, 101, 120);
    private static readonly XColor Pale = XColor.FromArgb(241, 245, 249);

    public byte[] CreatePdf(TicketReportDto report, DateTime generatedAtUtc)
    {
        using var document = new PdfDocument();
        document.Info.Title = "IT Help Desk Ticket Report";
        document.Info.Author = "IT Help Desk";

        var rows = report.Tickets.ToList();
        var pageNumber = 0;
        for (var offset = 0; offset < Math.Max(1, rows.Count); offset += 18)
        {
            pageNumber++;
            var page = document.AddPage();
            page.Orientation = PdfSharpCore.PageOrientation.Landscape;
            page.Size = PdfSharpCore.PageSize.A4;

            using var graphics = XGraphics.FromPdfPage(page);
            DrawPage(graphics, page, report, rows.Skip(offset).Take(18).ToList(), generatedAtUtc, pageNumber);
        }

        using var stream = new MemoryStream();
        document.Save(stream, closeStream: false);
        return stream.ToArray();
    }

    public byte[] CreateExcel(TicketReportDto report, DateTime generatedAtUtc)
    {
        using var workbook = new XLWorkbook();
        var summary = workbook.Worksheets.Add("Summary");
        var data = workbook.Worksheets.Add("Tickets");

        summary.ShowGridLines = false;
        summary.Cell("A1").Value = "IT Help Desk Ticket Report";
        summary.Range("A1:F1").Merge().Style
            .Fill.SetBackgroundColor(XLColor.FromHtml("#122742"))
            .Font.SetFontColor(XLColor.White)
            .Font.SetBold()
            .Font.SetFontSize(18);
        summary.Row(1).Height = 30;

        summary.Cell("A3").Value = "Generated";
        summary.Cell("B3").Value = generatedAtUtc;
        summary.Cell("B3").Style.DateFormat.Format = "yyyy-mm-dd hh:mm \"UTC\"";
        summary.Cell("A4").Value = "Filters";
        summary.Cell("B4").Value = FormatFilters(report.Filters);

        var metrics = new (string Label, object? Value)[]
        {
            ("Total", report.Summary.TotalTickets),
            ("Open", report.Summary.OpenTickets),
            ("Pending", report.Summary.PendingTickets),
            ("Resolved", report.Summary.ResolvedTickets),
            ("Closed", report.Summary.ClosedTickets),
            ("Overdue", report.Summary.OverdueTickets),
            ("Avg resolution (hours)", report.Summary.AverageResolutionHours)
        };

        for (var index = 0; index < metrics.Length; index++)
        {
            var row = 7 + index;
            summary.Cell(row, 1).Value = metrics[index].Label;
            summary.Cell(row, 2).Value = XLCellValue.FromObject(metrics[index].Value ?? string.Empty);
        }

        summary.Range("A7:B13").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        summary.Range("A7:A13").Style.Font.Bold = true;
        summary.Range("A7:A13").Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F1F7");
        summary.Columns("A:B").AdjustToContents();
        summary.Column("B").Width = Math.Min(summary.Column("B").Width, 64);
        summary.SheetView.FreezeRows(1);

        var headers = new[]
        {
            "Ticket ID", "Title", "Status", "Priority", "Category", "Assigned Agent",
            "Requester", "Created", "Updated", "Resolved", "Resolution Hours"
        };
        for (var column = 0; column < headers.Length; column++)
        {
            data.Cell(1, column + 1).Value = headers[column];
        }

        for (var index = 0; index < report.Tickets.Count; index++)
        {
            var ticket = report.Tickets[index];
            var row = index + 2;
            data.Cell(row, 1).Value = ticket.Id;
            data.Cell(row, 2).Value = ticket.Title;
            data.Cell(row, 3).Value = ticket.Status;
            data.Cell(row, 4).Value = ticket.Priority;
            data.Cell(row, 5).Value = ticket.Category;
            data.Cell(row, 6).Value = ticket.AssignedAgentName;
            data.Cell(row, 7).Value = ticket.CreatorName;
            data.Cell(row, 8).Value = ticket.CreatedAtUtc;
            data.Cell(row, 9).Value = ticket.UpdatedAtUtc;
            if (ticket.ResolvedAtUtc.HasValue)
            {
                data.Cell(row, 10).Value = ticket.ResolvedAtUtc.Value;
            }
            if (ticket.ResolutionHours.HasValue)
            {
                data.Cell(row, 11).Value = ticket.ResolutionHours.Value;
            }
        }

        var lastRow = Math.Max(2, report.Tickets.Count + 1);
        var table = data.Range(1, 1, lastRow, headers.Length).CreateTable("TicketReport");
        table.Theme = XLTableTheme.TableStyleMedium2;
        data.SheetView.FreezeRows(1);
        data.Range(2, 8, lastRow, 10).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
        data.Range(2, 11, lastRow, 11).Style.NumberFormat.Format = "0.0";
        data.Columns().AdjustToContents();
        data.Column(2).Width = Math.Min(data.Column(2).Width, 48);
        data.Column(2).Style.Alignment.WrapText = true;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void DrawPage(
        XGraphics graphics,
        PdfPage page,
        TicketReportDto report,
        IReadOnlyList<TicketReportRowDto> rows,
        DateTime generatedAtUtc,
        int pageNumber)
    {
        var titleFont = new XFont("Arial", 20, XFontStyle.Bold);
        var headingFont = new XFont("Arial", 10, XFontStyle.Bold);
        var bodyFont = new XFont("Arial", 8, XFontStyle.Regular);
        var smallFont = new XFont("Arial", 7, XFontStyle.Regular);
        var whiteBrush = XBrushes.White;

        graphics.DrawRectangle(new XSolidBrush(Navy), 0, 0, page.Width, 76);
        graphics.DrawString("IT Help Desk Ticket Report", titleFont, whiteBrush, new XPoint(32, 38));
        graphics.DrawString(
            $"Generated {generatedAtUtc:yyyy-MM-dd HH:mm} UTC  |  Page {pageNumber}",
            bodyFont,
            whiteBrush,
            new XPoint(34, 58));

        var metrics = new[]
        {
            ("Total", report.Summary.TotalTickets),
            ("Open", report.Summary.OpenTickets),
            ("Pending", report.Summary.PendingTickets),
            ("Resolved", report.Summary.ResolvedTickets),
            ("Overdue", report.Summary.OverdueTickets)
        };
        var cardWidth = 118d;
        for (var index = 0; index < metrics.Length; index++)
        {
            var x = 32 + index * (cardWidth + 10);
            graphics.DrawRectangle(new XSolidBrush(Pale), x, 92, cardWidth, 48);
            graphics.DrawString(metrics[index].Item1, smallFont, new XSolidBrush(Slate), new XPoint(x + 10, 109));
            graphics.DrawString(metrics[index].Item2.ToString(CultureInfo.InvariantCulture), headingFont, new XSolidBrush(Navy), new XPoint(x + 10, 130));
        }

        graphics.DrawString($"Filters: {FormatFilters(report.Filters)}", bodyFont, new XSolidBrush(Slate), new XPoint(32, 160));

        var columns = new[]
        {
            ("ID", 34d), ("Title", 220d), ("Status", 80d), ("Priority", 62d),
            ("Category", 88d), ("Agent", 108d), ("Created", 82d)
        };
        var tableX = 32d;
        var tableY = 180d;
        var rowHeight = 19d;
        var xCursor = tableX;
        foreach (var (label, width) in columns)
        {
            graphics.DrawRectangle(new XSolidBrush(Blue), xCursor, tableY, width, 24);
            graphics.DrawString(label, headingFont, whiteBrush, new XRect(xCursor + 5, tableY + 5, width - 10, 16));
            xCursor += width;
        }

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var ticket = rows[rowIndex];
            var values = new[]
            {
                ticket.Id.ToString(CultureInfo.InvariantCulture),
                Trim(ticket.Title, 48),
                ticket.Status,
                ticket.Priority,
                ticket.Category,
                Trim(ticket.AssignedAgentName, 20),
                ticket.CreatedAtUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            };
            var y = tableY + 24 + rowIndex * rowHeight;
            xCursor = tableX;
            for (var columnIndex = 0; columnIndex < columns.Length; columnIndex++)
            {
                var width = columns[columnIndex].Item2;
                graphics.DrawRectangle(
                    rowIndex % 2 == 0 ? XBrushes.White : new XSolidBrush(Pale),
                    xCursor,
                    y,
                    width,
                    rowHeight);
                graphics.DrawRectangle(new XPen(XColor.FromArgb(220, 228, 236), 0.5), xCursor, y, width, rowHeight);
                graphics.DrawString(values[columnIndex], bodyFont, new XSolidBrush(Navy), new XRect(xCursor + 5, y + 5, width - 10, 12));
                xCursor += width;
            }
        }

        if (rows.Count == 0)
        {
            graphics.DrawString("No tickets matched the selected filters.", bodyFont, new XSolidBrush(Slate), new XPoint(38, 225));
        }
    }

    private static string FormatFilters(TicketReportFiltersDto filters)
    {
        var parts = new[]
        {
            filters.StartDate is null ? null : $"from {filters.StartDate}",
            filters.EndDate is null ? null : $"to {filters.EndDate}",
            filters.Status is null ? null : $"status {filters.Status}",
            filters.Priority is null ? null : $"priority {filters.Priority}",
            filters.Category is null ? null : $"category {filters.Category}",
            filters.AssignedAgentId is null ? null : $"agent #{filters.AssignedAgentId}",
            filters.CreatorUserId is null ? null : $"requester #{filters.CreatorUserId}",
            filters.Search is null ? null : $"search \"{filters.Search}\""
        }.Where(part => part is not null);

        return string.Join(", ", parts) is { Length: > 0 } value ? value : "none";
    }

    private static string Trim(string value, int max) => value.Length <= max ? value : value[..(max - 3)] + "...";
}
