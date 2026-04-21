using HospitalStatistics.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MedProject.Services.Interfaces;

public interface IPdfReportGenerator
{
    Task<byte[]> GenerateReportSevenPdfBytesAsync(
        List<ReportSevenDTO> data, DateOnly startDate, DateOnly endDate, CancellationToken ct);

    Task<byte[]> GenerateReportSevenEmptyPdfBytesAsync(
        DateOnly startDate, DateOnly endDate, CancellationToken ct);

    Task<byte[]> GenerateReportSevenPart2PdfBytesAsync(
        List<ReportSevenPart2DTO> data, DateOnly startDate, DateOnly endDate, CancellationToken ct);

    Task<byte[]> GenerateReportSevenPart2EmptyPdfBytesAsync(
        DateOnly startDate, DateOnly endDate, CancellationToken ct);
}
