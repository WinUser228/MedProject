using MedProject.Services.Interfaces;
using NLog;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MedProject.Services.Service;

public class ReportWindowService : IReportWindowService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public async Task ShowPreviewAsync(byte[] pdfBytes, string reportTitle)
    {
        Logger.Debug("ShowPreviewAsync called.");

        if (pdfBytes == null || pdfBytes.Length == 0)
            throw new ArgumentException("PDF данные пустые.", nameof(pdfBytes));

        if (string.IsNullOrWhiteSpace(reportTitle))
            reportTitle = "Отчёт";

        var safeTitle = string.Concat(reportTitle.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));
        var fileName = $"{safeTitle}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        var filePath = Path.Combine(Path.GetTempPath(), fileName);

        await File.WriteAllBytesAsync(filePath, pdfBytes);
        Logger.Info($"PDF сохранён во временный файл: {filePath}");

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Не удалось открыть PDF через ShellExecute. Пробуем cmd start.");
            try
            {
                var fileUri = new Uri(filePath).AbsoluteUri;
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd",
                    Arguments = $"/c start \"\" \"{fileUri}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
            }
            catch
            {
                // На Linux/macOS попробуем xdg-open
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "xdg-open",
                        Arguments = $"\"{filePath}\"",
                        UseShellExecute = false
                    });
                }
                catch (Exception fallbackEx)
                {
                    Logger.Error(fallbackEx, "Не удалось открыть PDF ни одним способом.");
                    throw;
                }
            }
        }
    }
}
