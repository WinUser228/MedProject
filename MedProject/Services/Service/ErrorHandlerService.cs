using MedProject.Services.Interfaces;
using NLog;
using System;
using System.IO;
using System.Net.Http;

namespace MedProject.Services.Service;

public class ErrorHandlerService : IErrorHandlerService
{
    private readonly IMessageService _messageService;

    public ErrorHandlerService(IMessageService messageService)
    {
        _messageService = messageService;
    }

    private void ShowErrorAndLog(Exception ex, ILogger logger, string logMsg, string caption, string userMsg, params object[] logParams)
    {
        logger.Error(ex, logMsg, logParams);
        _ = _messageService.ShowErrorAsync(userMsg, caption);
    }

    public void HandlePrintFileException(Exception ex, string filePath, ILogger logger)
    {
        switch (ex)
        {
            case UnauthorizedAccessException ua:
                ShowErrorAndLog(ua, logger, "Нет доступа к файлу: {0}", "Ошибка доступа к файлу",
                    $"Нет доступа к сохранённому файлу. Проверьте права доступа.\n\nОшибка: {ua.Message}", filePath);
                break;
            case InvalidOperationException io:
                ShowErrorAndLog(io, logger, "Ошибка открытия файла: {0}", "Ошибка открытия",
                    $"Не удалось открыть файл.\n\nОшибка: {io.Message}", filePath);
                break;
            default:
                ShowErrorAndLog(ex, logger, "Непредвиденная ошибка при открытии файла: {0}", "Ошибка",
                    $"Произошла непредвиденная ошибка: {ex.Message}", filePath);
                break;
        }
    }

    public void HandleReportException(Exception ex, ILogger logger, string reportName, DateOnly startDate, DateOnly endDate)
    {
        string logBase = "Ошибка при формировании отчёта '{0}' за период {1} - {2}";

        switch (ex)
        {
            case HttpRequestException httpEx:
                ShowErrorAndLog(httpEx, logger, logBase, "Ошибка соединения с сервером",
                    $"Не удалось получить данные для отчёта '{reportName}'.\n\nОшибка: {httpEx.Message}",
                    reportName, startDate, endDate);
                break;
            case InvalidOperationException invOp:
                ShowErrorAndLog(invOp, logger, logBase, "Ошибка данных или операции",
                    $"Невозможно сформировать отчёт '{reportName}'. Выбранный период может не содержать данных.\n\nДеталь: {invOp.Message}",
                    reportName, startDate, endDate);
                break;
            case ArgumentException argEx:
                ShowErrorAndLog(argEx, logger, logBase, "Ошибка параметров отчёта",
                    $"Выбран некорректный период для отчёта '{reportName}' ({startDate} - {endDate}).",
                    reportName, startDate, endDate);
                break;
            default:
                ShowErrorAndLog(ex, logger, logBase, "Непредвиденная ошибка",
                    $"При формировании отчёта '{reportName}' произошла непредвиденная ошибка: {ex.Message}",
                    reportName, startDate, endDate);
                break;
        }
    }

    public void HandleException(Exception ex, ILogger logger, string userAction)
    {
        logger.Error(ex, "Фатальная ошибка при выполнении действия: {UserAction}", userAction);
        _ = _messageService.ShowErrorAsync(
            $"Критическая ошибка при выполнении действия: {userAction}\n\nДеталь: {ex.Message}",
            "Критическая ошибка");
    }

    public void HandleBackupIOException(IOException ioex, ILogger logger, string backupPath, string fullBackupPath)
    {
        string caption = "Ошибка резервного копирования";
        string userMessage = ioex.HResult == -2147024864
            ? $"Файл уже используется другим приложением: '{backupPath}'.\n\nЗакройте программу, использующую эту папку, и попробуйте снова."
            : $"Ошибка ввода-вывода при резервном копировании: {ioex.Message}";

        ShowErrorAndLog(ioex, logger, "Ошибка ввода-вывода при резервном копировании: {0}", caption, userMessage, fullBackupPath);
    }
}
