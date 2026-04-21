using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using MedProject.Services.Interfaces;
using MedProject.Utilities;
using Microsoft.Data.Sqlite;
using NLog;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MedProject.Services.Service;

public class BackupService : IBackupService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private const string DatabaseFileName = "HospitalStatistics.db";

    private readonly IMessageService _messageService;
    private readonly IErrorHandlerService _errorHandlerService;

    public BackupService(IMessageService messageService, IErrorHandlerService errorHandlerService)
    {
        _messageService = messageService;
        _errorHandlerService = errorHandlerService;
    }

    public async Task BrowseAndBackupAsync()
    {
        var mainWindow = GetMainWindow();
        if (mainWindow == null)
        {
            Logger.Error("BackupService: не удалось получить главное окно для открытия диалога.");
            await _messageService.ShowErrorAsync("Не удалось открыть диалог выбора папки.", "Ошибка резервного копирования");
            return;
        }

        var folderResults = await mainWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Выберите папку для сохранения резервной копии",
            AllowMultiple = false
        });

        var destinationFolder = folderResults.Count > 0 ? folderResults[0].Path.LocalPath : null;
        if (string.IsNullOrWhiteSpace(destinationFolder))
            return;

        await CreateBackupAsync(destinationFolder);
    }

    public async Task BrowseAndRestoreAsync()
    {
        var mainWindow = GetMainWindow();
        if (mainWindow == null)
        {
            Logger.Error("BackupService: не удалось получить главное окно для восстановления.");
            await _messageService.ShowErrorAsync("Не удалось открыть диалог выбора файла.", "Восстановление БД");
            return;
        }

        var confirmed = await _messageService.ShowQuestionAsync(
            "Текущая база данных будет полностью заменена выбранной резервной копией.\n\n" +
            "Несохранённые в других программах данные не затрагиваются, но все текущие данные в этой базе будут заменены.\n\n" +
            "Продолжить?",
            "Восстановление БД");
        if (!confirmed)
            return;

        var results = await mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Выберите файл резервной копии (.db)",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("База SQLite")
                {
                    Patterns = ["*.db"]
                },
                FilePickerFileTypes.All
            ]
        });

        if (results.Count == 0)
            return;

        var sourcePath = results[0].Path.LocalPath;
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            await _messageService.ShowErrorAsync("Файл не найден.", "Восстановление БД");
            return;
        }

        if (!IsSqliteDatabaseFile(sourcePath))
        {
            await _messageService.ShowErrorAsync(
                "Выбранный файл не похож на базу данных SQLite.\nВыберите файл резервной копии (.db).",
                "Восстановление БД");
            return;
        }

        var targetPath = AppPaths.DatabasePath;
        try
        {
            var fullSource = Path.GetFullPath(sourcePath);
            var fullTarget = Path.GetFullPath(targetPath);
            if (string.Equals(fullSource, fullTarget, StringComparison.OrdinalIgnoreCase))
            {
                await _messageService.ShowWarningAsync(
                    "Выбран текущий рабочий файл базы данных. Укажите файл резервной копии из другого места.",
                    "Восстановление БД");
                return;
            }
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "BackupService: сравнение путей при восстановлении");
        }

        try
        {
            await Task.Run(() =>
            {
                SqliteConnection.ClearAllPools();
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                File.Copy(sourcePath, targetPath, overwrite: true);
            });

            Logger.Info($"BackupService: база восстановлена из {sourcePath} → {targetPath}");
            await _messageService.ShowInfoAsync(
                "База данных восстановлена из резервной копии.\n\nПриложение будет перезапущено.",
                "Восстановление БД");

            TryRestartCurrentApplication();
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.Shutdown(0);
        }
        catch (IOException ioex)
        {
            _errorHandlerService.HandleBackupIOException(ioex, Logger, Path.GetFileName(sourcePath), targetPath);
        }
        catch (UnauthorizedAccessException uaex)
        {
            Logger.Error(uaex, "BackupService: нет доступа при восстановлении БД");
            await _messageService.ShowErrorAsync(
                $"Нет доступа к файлу базы данных.\nЗакройте другие программы, использующие этот файл, и повторите.\n\n{uaex.Message}",
                "Восстановление БД");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "BackupService: ошибка восстановления БД");
            await _messageService.ShowErrorAsync(
                $"Не удалось восстановить базу данных.\n\nОшибка: {ex.Message}",
                "Восстановление БД");
        }
    }

    private static bool IsSqliteDatabaseFile(string path)
    {
        try
        {
            using var fs = File.OpenRead(path);
            Span<byte> header = stackalloc byte[16];
            if (fs.Read(header) < 16)
                return false;
            ReadOnlySpan<byte> sig = "SQLite format 3\0"u8;
            return header.SequenceEqual(sig);
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "BackupService: не удалось прочитать заголовок файла БД");
            return false;
        }
    }

    private static void TryRestartCurrentApplication()
    {
        try
        {
            var exe = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exe) || !File.Exists(exe))
                return;

            var cwd = Path.GetDirectoryName(exe) ?? AppContext.BaseDirectory;

            // On Linux/macOS (and sometimes Windows when running from CLI), ProcessPath can be "dotnet".
            // In that case we must restart with the original command line arguments (dotnet <app>.dll ...).
            var cmd = Environment.GetCommandLineArgs();
            var isDotNetHost = string.Equals(Path.GetFileName(exe), "dotnet", StringComparison.OrdinalIgnoreCase)
                               || string.Equals(Path.GetFileNameWithoutExtension(exe), "dotnet", StringComparison.OrdinalIgnoreCase);

            if (isDotNetHost && cmd.Length > 1)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = string.Join(" ", cmd.Skip(1).Select(QuoteArg)),
                    UseShellExecute = false,
                    WorkingDirectory = cwd,
                });
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                UseShellExecute = true,
                WorkingDirectory = cwd,
            });
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "BackupService: не удалось запустить новый процесс приложения");
        }
    }

    private static string QuoteArg(string arg)
    {
        if (string.IsNullOrEmpty(arg))
            return "\"\"";

        // Minimal safe quoting for ProcessStartInfo.Arguments.
        if (arg.IndexOfAny([' ', '\t', '\n', '\r', '"']) < 0)
            return arg;

        return "\"" + arg.Replace("\"", "\\\"") + "\"";
    }

    private async Task CreateBackupAsync(string destinationFolder)
    {
        var sourcePath = AppPaths.DatabasePath;
        if (!File.Exists(sourcePath))
        {
            Logger.Error($"BackupService: файл базы данных не найден: {sourcePath}");
            await _messageService.ShowErrorAsync(
                $"Файл базы данных не найден:\n{sourcePath}",
                "Ошибка резервного копирования");
            return;
        }

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm");
        var backupFileName = $"HospitalStatistics_backup_{timestamp}.db";
        var backupPath = Path.Combine(destinationFolder, backupFileName);

        Logger.Info($"BackupService: начало резервного копирования → {backupPath}");

        try
        {
            var sourceConnectionString = $"Data Source={sourcePath}";
            var destConnectionString = $"Data Source={backupPath}";

            await Task.Run(() =>
            {
                using var source = new SqliteConnection(sourceConnectionString);
                using var destination = new SqliteConnection(destConnectionString);
                source.Open();
                destination.Open();
                source.BackupDatabase(destination);
            });

            Logger.Info($"BackupService: резервная копия успешно создана: {backupPath}");
            await _messageService.ShowInfoAsync(
                $"Резервная копия успешно создана:\n{backupPath}",
                "Резервное копирование");
        }
        catch (IOException ioex)
        {
            _errorHandlerService.HandleBackupIOException(ioex, Logger, backupFileName, backupPath);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"BackupService: непредвиденная ошибка при создании резервной копии: {backupPath}");
            await _messageService.ShowErrorAsync(
                $"Не удалось создать резервную копию.\n\nОшибка: {ex.Message}",
                "Ошибка резервного копирования");
        }
    }

    private static Avalonia.Controls.Window? GetMainWindow()
    {
        return Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
    }
}
