using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using HelpDesk.Api.Dtos;

namespace HelpDesk.Api.Services;

public class ReportExportService : IReportExportService
{
    public byte[] CreatePdf(TicketReportDto report, DateTime generatedAtUtc)
    {
        var lines = new List<string>
        {
            "IT Help Desk Ticket Report",
            $"Generated: {generatedAtUtc:yyyy-MM-dd HH:mm} UTC",
            $"Filters: {FormatFilters(report.Filters)}",
            $"Total: {report.Summary.TotalTickets} | Open: {report.Summary.OpenTickets} | Pending: {report.Summary.PendingTickets} | Resolved: {report.Summary.ResolvedTickets} | Closed: {report.Summary.ClosedTickets}",
            "",
            "ID | Title | Status | Priority | Category | Agent | Created | Resolved"
        };

        lines.AddRange(report.Tickets.Select(ticket =>
            $"{ticket.Id} | {Trim(ticket.Title, 34)} | {ticket.Status} | {ticket.Priority} | {ticket.Category} | {Trim(ticket.AssignedAgentName, 18)} | {ticket.CreatedAtUtc:yyyy-MM-dd} | {(ticket.ResolvedAtUtc.HasValue ? ticket.ResolvedAtUtc.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "-")}"));

        return SimplePdf.Create(lines);
    }

    public byte[] CreateExcel(TicketReportDto report, DateTime generatedAtUtc)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(archive, "[Content_Types].xml", ContentTypesXml());
            AddEntry(archive, "_rels/.rels", RelationshipsXml());
            AddEntry(archive, "xl/workbook.xml", WorkbookXml());
            AddEntry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationshipsXml());
            AddEntry(archive, "xl/styles.xml", StylesXml());
            AddEntry(archive, "xl/worksheets/sheet1.xml", WorksheetXml(report, generatedAtUtc));
        }

        return stream.ToArray();
    }

    private static string WorksheetXml(TicketReportDto report, DateTime generatedAtUtc)
    {
        var rows = new List<IReadOnlyList<object?>>
        {
            new object?[] { "IT Help Desk Ticket Report" },
            new object?[] { "Generated", $"{generatedAtUtc:yyyy-MM-dd HH:mm} UTC" },
            new object?[] { "Filters", FormatFilters(report.Filters) },
            new object?[] { "Total Tickets", report.Summary.TotalTickets, "Open", report.Summary.OpenTickets, "Pending", report.Summary.PendingTickets, "Resolved", report.Summary.ResolvedTickets, "Closed", report.Summary.ClosedTickets },
            Array.Empty<object?>(),
            new object?[] { "Ticket ID", "Title", "Status", "Priority", "Category", "Assigned Agent", "Created By", "Created Date", "Updated Date", "Resolved Date", "Resolution Hours" }
        };

        rows.AddRange(report.Tickets.Select(ticket => new object?[]
        {
            ticket.Id,
            ticket.Title,
            ticket.Status,
            ticket.Priority,
            ticket.Category,
            ticket.AssignedAgentName,
            ticket.CreatorName,
            ticket.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
            ticket.UpdatedAtUtc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
            ticket.ResolvedAtUtc?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
            ticket.ResolutionHours.HasValue ? Math.Round(ticket.ResolutionHours.Value, 2) : null
        }));

        var sheetData = new StringBuilder();
        for (var r = 0; r < rows.Count; r++)
        {
            sheetData.Append($"<row r=\"{r + 1}\">");
            var row = rows[r];
            for (var c = 0; c < row.Count; c++)
            {
                sheetData.Append(Cell(c + 1, r + 1, row[c], r is 0 or 5 ? 1 : 0));
            }
            sheetData.Append("</row>");
        }

        return $"""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
              <cols>
                <col min="1" max="1" width="12" customWidth="1"/>
                <col min="2" max="2" width="42" customWidth="1"/>
                <col min="3" max="7" width="18" customWidth="1"/>
                <col min="8" max="11" width="20" customWidth="1"/>
              </cols>
              <sheetData>{sheetData}</sheetData>
            </worksheet>
            """;
    }

    private static string Cell(int column, int row, object? value, int style)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var reference = $"{ColumnName(column)}{row}";
        if (value is int or long or double or decimal)
        {
            return $"<c r=\"{reference}\" s=\"{style}\"><v>{Convert.ToString(value, CultureInfo.InvariantCulture)}</v></c>";
        }

        return $"<c r=\"{reference}\" s=\"{style}\" t=\"inlineStr\"><is><t>{Escape(value.ToString() ?? string.Empty)}</t></is></c>";
    }

    private static string ColumnName(int column)
    {
        var name = string.Empty;
        while (column > 0)
        {
            column--;
            name = (char)('A' + column % 26) + name;
            column /= 26;
        }
        return name;
    }

    private static void AddEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content.Trim());
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
            filters.CreatorUserId is null ? null : $"creator #{filters.CreatorUserId}",
            filters.Search is null ? null : $"search \"{filters.Search}\""
        }.Where(part => part is not null);

        return string.Join(", ", parts) is { Length: > 0 } value ? value : "none";
    }

    private static string Trim(string value, int max) => value.Length <= max ? value : value[..(max - 3)] + "...";

    private static string Escape(string value) => System.Security.SecurityElement.Escape(value) ?? string.Empty;

    private static string ContentTypesXml() => """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
          <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
          <Default Extension="xml" ContentType="application/xml"/>
          <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
          <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
          <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
        </Types>
        """;

    private static string RelationshipsXml() => """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
        </Relationships>
        """;

    private static string WorkbookXml() => """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
          <sheets><sheet name="Ticket Report" sheetId="1" r:id="rId1"/></sheets>
        </workbook>
        """;

    private static string WorkbookRelationshipsXml() => """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
          <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
        </Relationships>
        """;

    private static string StylesXml() => """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
          <fonts count="2"><font/><font><b/></font></fonts>
          <fills count="1"><fill><patternFill patternType="none"/></fill></fills>
          <borders count="1"><border/></borders>
          <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
          <cellXfs count="2"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/><xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0" applyFont="1"/></cellXfs>
        </styleSheet>
        """;

    private static class SimplePdf
    {
        public static byte[] Create(IReadOnlyList<string> lines)
        {
            using var output = new MemoryStream();
            var objects = new List<string>();
            var pageObjectIds = new List<int>();
            var contentObjectIds = new List<int>();
            var pages = lines.Chunk(42).ToList();

            objects.Add("<< /Type /Catalog /Pages 2 0 R >>");
            objects.Add(string.Empty);

            foreach (var pageLines in pages)
            {
                var contentId = objects.Count + 1;
                contentObjectIds.Add(contentId);
                objects.Add(ContentStream(pageLines));
                var pageId = objects.Count + 1;
                pageObjectIds.Add(pageId);
                objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 {pages.Count * 2 + 3} 0 R >> >> /Contents {contentId} 0 R >>");
            }

            objects.Add($"<< /Type /Pages /Kids [{string.Join(' ', pageObjectIds.Select(id => $"{id} 0 R"))}] /Count {pageObjectIds.Count} >>");
            var pagesObject = objects.Count;
            objects[1] = objects[^1];
            objects.RemoveAt(objects.Count - 1);
            objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

            var writer = new StreamWriter(output, Encoding.ASCII, leaveOpen: true);
            writer.Write("%PDF-1.4\n");
            var offsets = new List<long> { 0 };
            for (var i = 0; i < objects.Count; i++)
            {
                offsets.Add(output.Position);
                writer.Write($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
                writer.Flush();
            }

            var xref = output.Position;
            writer.Write($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
            foreach (var offset in offsets.Skip(1))
            {
                writer.Write($"{offset:0000000000} 00000 n \n");
            }
            writer.Write($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
            writer.Flush();
            return output.ToArray();
        }

        private static string ContentStream(IEnumerable<string> lines)
        {
            var text = new StringBuilder("BT /F1 10 Tf 36 552 Td 13 TL\n");
            foreach (var line in lines)
            {
                text.Append('(').Append(EscapePdf(line)).Append(") Tj T*\n");
            }
            text.Append("ET");
            var content = text.ToString();
            return $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream";
        }

        private static string EscapePdf(string value) => value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
    }
}
