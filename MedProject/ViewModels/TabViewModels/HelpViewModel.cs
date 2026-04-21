using MedProject.Services.Interfaces;
using MedProject.Utilities;
using NLog;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Input;

namespace MedProject.ViewModels.TabViewModels;

public class HelpViewModel : ViewModelBase
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private const string HelpContentDirectory = "HelpContent";

    /// <summary>Ключ .md и условие показа кнопок резервного копирования.</summary>
    public const string BackupRestoreContentKey = "BACKUP_RESTORE";

    private readonly IBackupService _backupService;

    public ICommand BackupCommand { get; }
    public ICommand RestoreCommand { get; }

    public ObservableCollection<HelpTopicViewModel> HelpTopics { get; } = new();

    private HelpTopicViewModel? _selectedTopic;
    public HelpTopicViewModel? SelectedTopic
    {
        get => _selectedTopic;
        set
        {
            if (!SetProperty(ref _selectedTopic, value))
                return;
            OnPropertyChanged(nameof(ShowBackupRestoreButtons));
            LoadHelpContent(_selectedTopic);
        }
    }

    /// <summary>Показывать кнопки «Создать резервную копию» / «Восстановить БД» только в теме про бекапы.</summary>
    public bool ShowBackupRestoreButtons => SelectedTopic?.ContentKey == BackupRestoreContentKey;

    private string _currentHelpText = string.Empty;
    public string CurrentHelpText
    {
        get => _currentHelpText;
        set => SetProperty(ref _currentHelpText, value);
    }

    public HelpViewModel(IBackupService backupService)
    {
        _backupService = backupService;
        BackupCommand = new AsyncRelayCommand(async () => await _backupService.BrowseAndBackupAsync());
        RestoreCommand = new AsyncRelayCommand(async () => await _backupService.BrowseAndRestoreAsync());

        Logger.Info("HelpViewModel: инициализация тем справочника.");
        LoadHelpTopics();
        SelectedTopic = HelpTopics.FirstOrDefault();
        OnPropertyChanged(nameof(ShowBackupRestoreButtons));
    }

    private void LoadHelpTopics()
    {
        HelpTopics.Clear();
        HelpTopics.Add(new HelpTopicViewModel("Быстрый старт", "START_GUIDE"));
        HelpTopics.Add(new HelpTopicViewModel("Горячие клавиши", "HOTKEYS"));
        HelpTopics.Add(new HelpTopicViewModel("Ввод данных", "FORM"));
        HelpTopics.Add(new HelpTopicViewModel("Справочники", "GUIDES"));
        HelpTopics.Add(new HelpTopicViewModel("Отчёт №1: Движение больных", "REPORT_ONE"));
        HelpTopics.Add(new HelpTopicViewModel("Отчёт №2: Работа коек", "BEDS_WORK"));
        HelpTopics.Add(new HelpTopicViewModel("Логирование", "LOGGING_ARCHIVE"));
        HelpTopics.Add(new HelpTopicViewModel("Резервное копирование БД", BackupRestoreContentKey));
    }

    private void LoadHelpContent(HelpTopicViewModel? topic)
    {
        if (topic == null)
        {
            CurrentHelpText = "Выберите тему справочника в списке.";
            return;
        }

        var fileName = $"{topic.ContentKey}.md";
        var filePath = ResolveHelpMarkdownPath(fileName);
        Logger.Debug($"HelpViewModel: путь к файлу справочника: {filePath}");

        try
        {
            if (filePath == null || !File.Exists(filePath))
            {
                Logger.Warn($"HelpViewModel: файл справочника не найден: {fileName} (искали рядом с процессом и сборкой)");
                CurrentHelpText =
                    $"Файл справочника для раздела \"{topic.Title}\" ({fileName}) не найден.\n\n" +
                    "Убедитесь, что папка HelpContent с .md скопирована в каталог приложения (рядом с exe) при сборке или публикации.";
                return;
            }

            CurrentHelpText = File.ReadAllText(filePath, Encoding.UTF8);
            Logger.Debug($"HelpViewModel: контент загружен для ключа: {topic.ContentKey}");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"HelpViewModel: ошибка загрузки/конвертации справочника для {topic.Title}");
            CurrentHelpText = $"Непредвиденная ошибка загрузки: {ex.Message}";
        }
    }

    /// <summary>
    /// Ищет .md рядом с exe: AppContext.BaseDirectory, затем каталог основной сборки (удобно при отладке и при разных сценариях запуска).
    /// </summary>
    private static string? ResolveHelpMarkdownPath(string fileName)
    {
        string[] roots =
        [
            AppContext.BaseDirectory,
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "",
        ];

        foreach (var root in roots)
        {
            if (string.IsNullOrWhiteSpace(root)) continue;
            try
            {
                var fullRoot = Path.GetFullPath(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                var path = Path.Combine(fullRoot, HelpContentDirectory, fileName);
                if (File.Exists(path)) return path;
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "HelpViewModel: пропуск каталога при поиске справки");
            }
        }

        return null;
    }
}

public class HelpTopicViewModel
{
    public string Title { get; }
    public string ContentKey { get; }

    public HelpTopicViewModel(string title, string contentKey)
    {
        Title = title;
        ContentKey = contentKey;
    }

    public override string ToString() => Title;
}
