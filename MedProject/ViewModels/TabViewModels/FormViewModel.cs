using HospitalStatistics.Core.Entities;
using HospitalStatistics.Core.Services;
using HospitalStatistics.Core.Services.Interfaces.Entities;
using MedProject.Mapper;
using MedProject.Services.Interfaces;
using MedProject.Utilities;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MedProject.ViewModels.TabViewModels;

public class FormViewModel : ViewModelBase
{
    #region Collections

    public ObservableCollection<ReportEntry> ReportEntries { get; } = new();
    public ObservableCollection<Departments> Departments { get; } = new();

    #endregion

    #region Selected Values

    private Departments _selectedDepartment;
    public Departments SelectedDepartment
    {
        get => _selectedDepartment;
        set
        {
            if (_suppressDepartmentSelection)
            {
                SetProperty(ref _selectedDepartment, value);
                return;
            }

            // При очистке/перезаполнении ItemsSource ComboBox (TwoWay) шлёт SelectedItem=null.
            // Иначе вызывается ChangeDepartmentAsync(null) и теряется восстановление отделения после смены даты.
            if (_departmentListRefreshing && value is null)
                return;

            _ = ChangeDepartmentAsync(value);
        }
    }

    private DateOnly? _reportDate = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? ReportDate
    {
        get => _reportDate;
        set
        {
            if (_suppressReportDateSelection)
            {
                SetProperty(ref _reportDate, value);
                return;
            }

            _ = ChangeReportDateAsync(value);
        }
    }

    public bool CanEdit => true;
    public bool CanSave => true;

    #endregion

    #region Services

    private readonly IReportSevenService _reportService;
    private readonly IDepartmentService _departmentService;
    private readonly IBedService _bedService;
    private readonly IListPatientMovementService _listPatientMovementService;
    private readonly IErrorHandlerService _errorHandlerService;
    private readonly IMessageService _messageService;
    private readonly IWindowService _windowService;
    private readonly Func<ReportType, ReportDateSelectorViewModel> _reportViewModelFactory;
    private readonly CancellationToken _ct = new();
    private Dictionary<string, List<int>> _bedTypeToBedIdsMap = new();

    #endregion

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private bool _isDirty;
    public bool IsDirty
    {
        get => _isDirty;
        private set => SetProperty(ref _isDirty, value);
    }

    private bool _isLoading;
    private bool _suppressDepartmentSelection;
    private bool _suppressReportDateSelection;
    /// <summary>Пока перестраиваем список отделений — игнорируем ложный null из ComboBox при Clear().</summary>
    private bool _departmentListRefreshing;

    private readonly SemaphoreSlim _departmentLoadLock = new(1, 1);

    public ICommand SaveReportEntriesCommand { get; }
    public ICommand OpenReportSevenCommand { get; }
    public ICommand OpenReportSevenPart2Command { get; }

    public FormViewModel(
        IReportSevenService reportService,
        IDepartmentService departmentService,
        IBedService bedService,
        IListPatientMovementService movementService,
        IErrorHandlerService errorHandlerService,
        IMessageService messageService,
        IWindowService windowService,
        Func<ReportType, ReportDateSelectorViewModel> reportViewModelFactory)
    {
        Logger.Info("Инициализация FormViewModel...");

        _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
        _departmentService = departmentService ?? throw new ArgumentNullException(nameof(departmentService));
        _bedService = bedService ?? throw new ArgumentNullException(nameof(bedService));
        _listPatientMovementService = movementService ?? throw new ArgumentNullException(nameof(movementService));
        _errorHandlerService = errorHandlerService ?? throw new ArgumentNullException(nameof(errorHandlerService));
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        _windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
        _reportViewModelFactory = reportViewModelFactory ?? throw new ArgumentNullException(nameof(reportViewModelFactory));

        SaveReportEntriesCommand = new Utilities.RelayCommand(
            async () => await SaveListPatientMovementAsync(),
            () => IsReportValid && ReportEntries.Any());

        OpenReportSevenCommand = new Utilities.RelayCommand(async () => await OpenReportAsync(ReportType.ReportSeven));
        OpenReportSevenPart2Command = new Utilities.RelayCommand(async () => await OpenReportAsync(ReportType.ReportSevenPart2));

        ReportEntries.CollectionChanged += (s, e) =>
        {
            if (e.OldItems != null)
                foreach (ReportEntry item in e.OldItems)
                {
                    item.PropertyChanged -= ReportEntry_PropertyChanged;
                    item.ErrorsChanged -= ReportEntry_ErrorsChanged;
                }

            if (e.NewItems != null)
                foreach (ReportEntry item in e.NewItems)
                {
                    item.PropertyChanged += ReportEntry_PropertyChanged;
                    item.ErrorsChanged += ReportEntry_ErrorsChanged;
                }

            IsCrossRowValid(true);
            OnPropertyChanged(nameof(HasErrors));
            ((Utilities.RelayCommand)SaveReportEntriesCommand).RaiseCanExecuteChanged();
        };

        Logger.Info("FormViewModel инициализирован. Загружаю отделения...");
        _ = LoadDepartmentsAsync();
    }

    /// <summary>Перезагружает список отделений из БД (например после правок в справочниках).</summary>
    public Task ReloadDepartmentsAsync() => LoadDepartmentsAsync();

    private async Task OpenReportAsync(ReportType reportType)
    {
        var reportViewModel = _reportViewModelFactory(reportType);
        await _windowService.ShowReportDateSelectorDialogAsync(reportViewModel);
    }

    private bool _isCrossRowValidationRunning = false;

    private void ReportEntry_ErrorsChanged(object? sender, DataErrorsChangedEventArgs e)
    {
        // Ошибки валидации не всегда сопровождаются PropertyChanged на поле,
        // поэтому обновляем состояние сохранения отдельно.
        if (!_isLoading)
            IsDirty = true;

        OnPropertyChanged(nameof(HasErrors));
        ((Utilities.RelayCommand)SaveReportEntriesCommand).RaiseCanExecuteChanged();
    }

    private void ReportEntry_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        bool isValidationField =
            e.PropertyName == nameof(ReportEntry.PatientsAtStartOfDay) ||
            e.PropertyName == nameof(ReportEntry.TotalAdmitted) ||
            e.PropertyName == nameof(ReportEntry.TotalDischarged) ||
            e.PropertyName == nameof(ReportEntry.Died) ||
            e.PropertyName == nameof(ReportEntry.FromVillage) ||
            e.PropertyName == nameof(ReportEntry.ChildrenUnder14) ||
            e.PropertyName == nameof(ReportEntry.TransferredFromOtherDepartments) ||
            e.PropertyName == nameof(ReportEntry.TransferredToOtherDepartments) ||
            e.PropertyName == nameof(ReportEntry.DischargedByTransfer) ||
            e.PropertyName == nameof(ReportEntry.FromVillagePatient) ||
            e.PropertyName == nameof(ReportEntry.MothersWithPatients) ||
            e.PropertyName == nameof(ReportEntry.MothersDischarged);

        if (!_isLoading && isValidationField)
            IsDirty = true;

        if (e.PropertyName == nameof(ReportEntry.PatientsAtStartOfDay) ||
            e.PropertyName == nameof(ReportEntry.TotalAdmitted) ||
            e.PropertyName == nameof(ReportEntry.TotalDischarged) ||
            e.PropertyName == nameof(ReportEntry.TransferredFromOtherDepartments) ||
            e.PropertyName == nameof(ReportEntry.TransferredToOtherDepartments) ||
            e.PropertyName == nameof(ReportEntry.Died))
        {
            IsCrossRowValid(true);
        }

        if (isValidationField)
        {
            OnPropertyChanged(nameof(HasErrors));
            ((Utilities.RelayCommand)SaveReportEntriesCommand).RaiseCanExecuteChanged();
        }
    }

    public bool HasErrors => ReportEntries.Any(entry => entry.HasErrors);

    public bool IsReportValid => !HasErrors && IsCrossRowValid(false);

    #region Data Loading

    private async Task TryLoadBedsAsync()
    {
        if (SelectedDepartment != null && ReportDate.HasValue)
            await LoadBedsByDepartmentAsync();
    }

    private async Task ChangeDepartmentAsync(Departments newDepartment)
    {
        if (ReferenceEquals(newDepartment, _selectedDepartment) || Equals(newDepartment, _selectedDepartment))
            return;

        var previous = _selectedDepartment;

        if (IsDirty)
        {
            var choice = await _messageService.ShowUnsavedChangesDialogAsync(
                "Данные в таблице не сохранены. Сохранить перед переходом на другое отделение?",
                "Несохранённые данные");

            if (choice == UnsavedChangesChoice.Cancel)
            {
                // Откатим выбор в ComboBox
                _suppressDepartmentSelection = true;
                try { SetProperty(ref _selectedDepartment, previous); }
                finally { _suppressDepartmentSelection = false; }
                return;
            }

            if (choice == UnsavedChangesChoice.Save)
            {
                var saved = await SaveListPatientMovementInternalAsync(showSuccessMessage: true);
                if (!saved)
                {
                    // Если сохранить не получилось (валидация/ошибка) — не уходим.
                    _suppressDepartmentSelection = true;
                    try { SetProperty(ref _selectedDepartment, previous); }
                    finally { _suppressDepartmentSelection = false; }
                    return;
                }
            }
            else
            {
                // Discard
                IsDirty = false;
            }
        }

        _suppressDepartmentSelection = true;
        try { SetProperty(ref _selectedDepartment, newDepartment); }
        finally { _suppressDepartmentSelection = false; }

        await TryLoadBedsAsync();
    }

    private async Task ChangeReportDateAsync(DateOnly? newDate)
    {
        if (Equals(newDate, _reportDate))
            return;

        var previous = _reportDate;

        if (IsDirty)
        {
            var choice = await _messageService.ShowUnsavedChangesDialogAsync(
                "Данные в таблице не сохранены. Сохранить перед переходом на другую дату?",
                "Несохранённые данные");

            if (choice == UnsavedChangesChoice.Cancel)
            {
                _suppressReportDateSelection = true;
                try { SetProperty(ref _reportDate, previous); }
                finally { _suppressReportDateSelection = false; }
                return;
            }

            if (choice == UnsavedChangesChoice.Save)
            {
                var saved = await SaveListPatientMovementInternalAsync(showSuccessMessage: true);
                if (!saved)
                {
                    _suppressReportDateSelection = true;
                    try { SetProperty(ref _reportDate, previous); }
                    finally { _suppressReportDateSelection = false; }
                    return;
                }
            }
            else
            {
                // Discard
                IsDirty = false;
            }
        }

        _suppressReportDateSelection = true;
        try { SetProperty(ref _reportDate, newDate); }
        finally { _suppressReportDateSelection = false; }

        OnPropertyChanged(nameof(CanEdit));
        OnPropertyChanged(nameof(CanSave));
        // Перезагрузка отделений синхронно восстанавливает SelectedDepartment и затем загружает койки
        await LoadDepartmentsAsync();
    }

    private async Task LoadDepartmentsAsync()
    {
        await _departmentLoadLock.WaitAsync();
        try
        {
            Logger.Info("Загружаю отделения...");
            try
            {
                var reportDate = ReportDate ?? DateOnly.FromDateTime(DateTime.Today);
                var allBeds = await _bedService.GetAllBedsWithDepartmentsAsync(_ct);

                var activeDeptIds = allBeds
                    .Where(b => b.EffectiveDate <= reportDate
                             && (b.NotUsedSince == null || b.NotUsedSince > reportDate))
                    .Select(b => b.DepartmentId)
                    .ToHashSet();

                var departments = await _departmentService.GetAllDepartmentAsync(_ct);
                var active = departments?.Where(d => activeDeptIds.Contains(d.Id)).ToList()
                             ?? new List<Departments>();

                var prevId = _selectedDepartment?.Id;

                _departmentListRefreshing = true;
                try
                {
                    Departments.Clear();
                    foreach (var dept in active)
                        Departments.Add(dept);

                    var selected = Departments.FirstOrDefault(d => d.Id == prevId) ?? Departments.FirstOrDefault();
                    // Avalonia ComboBox: после Clear()/Add() часто не показывает текст выбранного пункта,
                    // хотя _selectedDepartment указывает на элемент из списка. Сброс в null и повторный выбор
                    // заставляет контрол синхронизировать отображение с ItemsSource.
                    _suppressDepartmentSelection = true;
                    try
                    {
                        _selectedDepartment = null;
                        OnPropertyChanged(nameof(SelectedDepartment));
                        _selectedDepartment = selected;
                        OnPropertyChanged(nameof(SelectedDepartment));
                    }
                    finally
                    {
                        _suppressDepartmentSelection = false;
                    }
                }
                finally
                {
                    _departmentListRefreshing = false;
                }

                await LoadBedsByDepartmentAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ошибка при загрузке отделений.");
                await _messageService.ShowErrorAsync($"Ошибка при загрузке отделений: {ex.Message}");
            }
        }
        finally
        {
            _departmentLoadLock.Release();
        }
    }

    private async Task LoadBedsByDepartmentAsync()
    {
        _isLoading = true;
        foreach (var entry in ReportEntries)
            entry.PropertyChanged -= ReportEntry_PropertyChanged;

        ReportEntries.Clear();
        _bedTypeToBedIdsMap.Clear();

        if (SelectedDepartment == null || !ReportDate.HasValue)
        {
            OnPropertyChanged(nameof(HasErrors));
            ((Utilities.RelayCommand)SaveReportEntriesCommand).RaiseCanExecuteChanged();
            IsDirty = false;
            _isLoading = false;
            return;
        }

        try
        {
            var allBeds = await _bedService.GetBedsByDepartmentIdAsync(SelectedDepartment.Id, _ct);
            var reportDate = ReportDate.Value;

            var activeBeds = allBeds.Where(bed =>
                bed.EffectiveDate <= reportDate &&
                (bed.NotUsedSince == null || bed.NotUsedSince > reportDate)).ToList();

            var bedGroupsByType = activeBeds
                .Where(b => b.BedType != null && !string.IsNullOrEmpty(b.BedType.BedTypeName))
                .GroupBy(b => b.BedType.BedTypeName)
                .ToList();

            var bedIds = activeBeds.Select(b => b.Id).ToList();
            var existingMovements = await _listPatientMovementService
                .GetListPatientMovementsByDateAndBedIdsAsync(reportDate, bedIds, _ct);

            int codeCounter = 1;
            var prevDay = reportDate.AddDays(-1);
            var bedTypeToBedIds = new Dictionary<string, List<int>>();

            foreach (var bedGroup in bedGroupsByType.OrderBy(g => g.Key))
            {
                var bedTypeName = bedGroup.Key;
                var bedsOfThisType = bedGroup.ToList();

                var activeBedForDate = bedsOfThisType
                    .OrderByDescending(b => b.EffectiveDate)
                    .ThenByDescending(b => b.Id)
                    .FirstOrDefault();

                if (activeBedForDate == null) continue;

                var koikiCount = activeBedForDate.Amount;
                var activeBedId = activeBedForDate.Id;
                var bedIdsOfThisType = bedsOfThisType.Select(b => b.Id).ToList();
                bedTypeToBedIds[bedTypeName] = bedIdsOfThisType;

                var movementsForThisType = existingMovements
                    .Where(m => bedIdsOfThisType.Contains(m.BedId)).ToList();

                // При сохранении строка формы копируется на каждый BedId этого типа — в БД несколько одинаковых записей.
                // Суммировать поля по всем койкам нельзя (получится N-кратное завышение, например ×2 при двух койках).
                var primaryMovement = movementsForThisType.FirstOrDefault(m => m.BedId == activeBedForDate.Id)
                    ?? movementsForThisType.FirstOrDefault();

                ReportEntry entry;
                if (primaryMovement != null)
                {
                    entry = ReportEntryMapper.ToReportEntry(primaryMovement);
                    entry.BedId = activeBedId;
                    entry.ReportDate = reportDate;
                }
                else
                {
                    entry = new ReportEntry { BedId = activeBedId, ReportDate = reportDate };
                }

                var prevMovement = await _listPatientMovementService
                    .GetListPatientMovementByBedAndDateAsync(activeBedForDate.Id, prevDay, _ct);
                int? previousDayTotalPatients = prevMovement?.TotalPatients;

                if (primaryMovement is null && previousDayTotalPatients.HasValue)
                    entry.PatientsAtStartOfDay = previousDayTotalPatients;

                entry.PreviousDayTotalPatients = previousDayTotalPatients;
                entry.Name = bedTypeName;
                entry.Code = codeCounter++;
                entry.KoikiCount = koikiCount;

                ReportEntries.Add(entry);
                entry.PropertyChanged += ReportEntry_PropertyChanged;
                entry.ErrorsChanged += ReportEntry_ErrorsChanged;
            }

            _bedTypeToBedIdsMap = bedTypeToBedIds;

            foreach (var entry in ReportEntries)
                entry.ValidateAll();

            IsCrossRowValid(true);
            OnPropertyChanged(nameof(HasErrors));
            ((Utilities.RelayCommand)SaveReportEntriesCommand).RaiseCanExecuteChanged();
            IsDirty = false;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Ошибка при загрузке коек.");
            await _messageService.ShowErrorAsync($"Ошибка при загрузке данных: {ex.Message}");
            OnPropertyChanged(nameof(HasErrors));
            ((Utilities.RelayCommand)SaveReportEntriesCommand).RaiseCanExecuteChanged();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private bool IsCrossRowValid(bool updateErrors)
    {
        if (_isCrossRowValidationRunning) return true;

        _isCrossRowValidationRunning = true;
        bool isValid = true;

        try
        {
            if (updateErrors)
                foreach (var entry in ReportEntries.Where(e => e.CrossRowError != null))
                    entry.CrossRowError = null;

            foreach (var currentEntry in ReportEntries)
            {
                int? patientsAtStart = currentEntry.PatientsAtStartOfDay;
                int? prevTotal = currentEntry.PreviousDayTotalPatients;

                if (patientsAtStart.HasValue && prevTotal.HasValue &&
                    patientsAtStart.Value != prevTotal.Value)
                {
                    isValid = false;
                    if (updateErrors)
                        currentEntry.CrossRowError =
                            $"Значение 'Состояло' ({patientsAtStart.Value}) не совпадает со значением 'Состоит Всего' предыдущих суток для койки '{currentEntry.Name}' ({prevTotal.Value}).";
                }
            }
        }
        finally
        {
            _isCrossRowValidationRunning = false;
        }

        return isValid;
    }

    #endregion

    #region Save

    public async Task SaveListPatientMovementAsync()
    {
        await SaveListPatientMovementInternalAsync(showSuccessMessage: true);
    }

    private async Task<bool> SaveListPatientMovementInternalAsync(bool showSuccessMessage)
    {
        Logger.Info("Сохранение перемещений пациентов. Количество записей: {0}", ReportEntries.Count);

        try
        {
            if (!ReportEntries.Any())
            {
                await _messageService.ShowInfoAsync("Нет данных для сохранения.");
                return false;
            }

            if (!IsReportValid)
            {
                IsCrossRowValid(true);
                await _messageService.ShowWarningAsync(
                    "Пожалуйста, исправьте все ошибки в таблице перед сохранением.",
                    "Ошибка валидации");
                return false;
            }

            var dtoList = new List<ListPatientMovements>();
            foreach (var entry in ReportEntries)
            {
                var bedTypeName = entry.Name;
                if (_bedTypeToBedIdsMap.TryGetValue(bedTypeName, out var bedIds))
                {
                    foreach (var bedId in bedIds)
                    {
                        var dto = ReportEntryMapper.ToMovement(entry);
                        dto.BedId = bedId;
                        dtoList.Add(dto);
                    }
                }
                else
                {
                    dtoList.Add(ReportEntryMapper.ToMovement(entry));
                }
            }

            await _listPatientMovementService.SaveListPatientMovementsAsync(dtoList, _ct);
            IsDirty = false;
            if (showSuccessMessage)
                await _messageService.ShowInfoAsync("Данные успешно сохранены.", "Сохранено");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Ошибка при сохранении перемещений пациентов.");
            await _messageService.ShowErrorAsync($"Ошибка сохранения данных: {ex.Message}");
            return false;
        }
    }

    #endregion
}
