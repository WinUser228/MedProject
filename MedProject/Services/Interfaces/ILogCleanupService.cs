using System;
using System.Threading;
using System.Threading.Tasks;

namespace MedProject.Services.Interfaces;

public interface ILogCleanupService
{
    Task<int> DeleteOldLogsAsync(int daysToKeep, CancellationToken ct = default);
    Task<int> DeleteLogsBeforeDateAsync(DateTime cutoffDate, CancellationToken ct = default);
}
