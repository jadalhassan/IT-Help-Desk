using HelpDesk.Api.Dtos;

namespace HelpDesk.Api.Services;

public interface IReportExportService
{
    byte[] CreatePdf(TicketReportDto report, DateTime generatedAtUtc);
    byte[] CreateExcel(TicketReportDto report, DateTime generatedAtUtc);
}
