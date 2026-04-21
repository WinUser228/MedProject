using HospitalStatistics.Core.Models;

namespace HospitalStatistics.Core.Services
{
    public interface IReportSevenService
    {
        Task<List<ReportSevenDTO>> GetReportAsync(DateOnly startDate, DateOnly endDate, CancellationToken ct);

        Task<List<ReportSevenPart2DTO>> GetReportPart2Async(DateOnly startDate, DateOnly endDate, CancellationToken ct);
    }

}
