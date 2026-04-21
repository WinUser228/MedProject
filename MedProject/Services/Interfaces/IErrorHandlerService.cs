using NLog;
using System;
using System.IO;

namespace MedProject.Services.Interfaces;

public interface IErrorHandlerService
{
    void HandlePrintFileException(Exception ex, string filePath, ILogger logger);
    void HandleReportException(Exception ex, ILogger logger, string reportName, DateOnly startDate, DateOnly endDate);
    void HandleException(Exception ex, ILogger logger, string userAction);
    void HandleBackupIOException(IOException ioex, ILogger logger, string backupPath, string fullBackupPath);
}
