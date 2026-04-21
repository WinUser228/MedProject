using MedProject.Services.Interfaces;
using NLog;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MedProject.Services.Service;

public class LogCleanupService : ILogCleanupService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private const string LogsDirectory = "logs";

    public async Task<int> DeleteOldLogsAsync(int daysToKeep, CancellationToken ct = default)
    {
        if (daysToKeep < 0)
            throw new ArgumentException("Количество дней не может быть отрицательным.", nameof(daysToKeep));

        var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
        Logger.Info($"Запуск очистки логов старше {daysToKeep} дней (до {cutoffDate:yyyy-MM-dd HH:mm:ss})");
        return await DeleteLogsBeforeDateAsync(cutoffDate, ct);
    }

    public async Task<int> DeleteLogsBeforeDateAsync(DateTime cutoffDate, CancellationToken ct = default)
    {
        if (!Directory.Exists(LogsDirectory))
        {
            Logger.Debug($"Каталог '{LogsDirectory}' не существует. Очистка не выполнялась.");
            return 0;
        }

        try
        {
            var logFiles = Directory.GetFiles(LogsDirectory, "*.*", SearchOption.TopDirectoryOnly)
                .Where(IsLogFile)
                .ToList();

            if (!logFiles.Any()) return 0;

            int deletedCount = 0;
            long totalSize = 0;

            foreach (var filePath in logFiles)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var fi = new FileInfo(filePath);
                    if (fi.LastWriteTime < cutoffDate)
                    {
                        long size = fi.Length;
                        await Task.Run(() => File.Delete(filePath), ct);
                        deletedCount++;
                        totalSize += size;
                        Logger.Debug($"Удалён лог-файл: {Path.GetFileName(filePath)}");
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    Logger.Warn(ex, $"Не удалось удалить лог-файл: {Path.GetFileName(filePath)}");
                }
            }

            if (deletedCount > 0)
                Logger.Info($"Очистка завершена. Удалено файлов: {deletedCount}, освобождено: {FormatSize(totalSize)}");

            return deletedCount;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Logger.Error(ex, "Фатальная ошибка очистки логов.");
            throw;
        }
    }

    private static bool IsLogFile(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var name = Path.GetFileName(filePath).ToLowerInvariant();
        return ext is ".txt" or ".log"
            || name.StartsWith("log", StringComparison.OrdinalIgnoreCase)
            || name.Contains("archive", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatSize(long bytes)
    {
        string[] sizes = { "Б", "КБ", "МБ", "ГБ" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1) { order++; len /= 1024; }
        return $"{len:0.##} {sizes[order]}";
    }
}
