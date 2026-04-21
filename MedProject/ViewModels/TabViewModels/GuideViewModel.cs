using HospitalStatistics.Core.Entities;
using HospitalStatistics.Core.Exceptions;
using HospitalStatistics.Core.Services.Interfaces.Entities;
using MedProject.Services.Interfaces;
using MedProject.Utilities;
using MedProject.ViewModels.AddViewModels;
using MedProject.ViewModels.EditModelView;
using MedProject.ViewModels.FormsViewModels;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MedProject.ViewModels.TabViewModels;

public enum GuideSection
{
    Departments = 0,
    Beds = 1,
    BedTypes = 2
}

public class GuideViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessageService _messageService;
    private readonly IWindowService _windowService;
    private CancellationTokenSource _cancellationTokenSource = new();
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private bool _isUpdatingTabIndex = false;
    private bool _isDisposed = false;

    #region Commands

    public ICommand AddRecordCommand { get; }
    public ICommand EditSelectedCommand => _editSelectedCommand;
    public ICommand DeleteSelectedCommand => _deleteSelectedCommand;
    private readonly AsyncRelayCommand _editSelectedCommand;
    private readonly AsyncRelayCommand _deleteSelectedCommand;
    public ICommand DeleteCommandDepartment { get; }
    public ICommand DeleteCommandBed { get; }
    public ICommand DeleteCommandBedType { get; }
    public ICommand EditCommandDepartment { get; }
    public ICommand EditCommandBed { get; }
    public ICommand EditCommandBedType { get; }

    #endregion

    #region Collections

    public ObservableCollection<Departments> Departments { get; } = new();
    public ObservableCollection<Beds> Beds { get; } = new();
    public ObservableCollection<BedTypes> BedTypes { get; } = new();
    public ObservableCollection<Departments> AvailableDepartments { get; } = new();
    public ObservableCollection<Beds> AvailableBeds { get; } = new();

    #endregion

    #region Properties

    private int _selectedTabIndex;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (_isDisposed || _isUpdatingTabIndex) return;

            if (SetProperty(ref _selectedTabIndex, value))
            {
                if (value >= 0 && value < Enum.GetValues(typeof(GuideSection)).Length)
                {
                    _isUpdatingTabIndex = true;
                    try { CurrentSection = (GuideSection)value; }
                    catch (Exception ex) { _logger.Warn(ex, "Ошибка при обновлении CurrentSection"); }
                    finally { _isUpdatingTabIndex = false; }
                }
            }
        }
    }

    private object _currentContentViewModel;
    public object CurrentContentViewModel
    {
        get => _currentContentViewModel;
        private set
        {
            if (_currentContentViewModel != value)
            {
                if (_currentContentViewModel is IDisposable old)
                    old.Dispose();
                _currentContentViewModel = value;
                OnPropertyChanged();
            }
        }
    }

    private GuideSection _currentSection;
    public GuideSection CurrentSection
    {
        get => _currentSection;
        set
        {
            if (_isDisposed) return;

            if (SetProperty(ref _currentSection, value))
            {
                try
                {
                    UpdateSelectedViewModel();
                    if (!_isUpdatingTabIndex)
                    {
                        _isUpdatingTabIndex = true;
                        try
                        {
                            _selectedTabIndex = (int)value;
                            OnPropertyChanged(nameof(SelectedTabIndex));
                        }
                        finally { _isUpdatingTabIndex = false; }
                    }
                }
                catch (Exception ex) { _logger.Warn(ex, "Ошибка при обновлении CurrentSection"); }
                NotifySelectionCommands();
            }
        }
    }

    #endregion

    #region Selected Items (for hotkeys)

    private Departments? _selectedDepartment;
    public Departments? SelectedDepartment
    {
        get => _selectedDepartment;
        set
        {
            if (SetProperty(ref _selectedDepartment, value))
                NotifySelectionCommands();
        }
    }

    private Beds? _selectedBed;
    public Beds? SelectedBed
    {
        get => _selectedBed;
        set
        {
            if (SetProperty(ref _selectedBed, value))
                NotifySelectionCommands();
        }
    }

    private BedTypes? _selectedBedType;
    public BedTypes? SelectedBedType
    {
        get => _selectedBedType;
        set
        {
            if (SetProperty(ref _selectedBedType, value))
                NotifySelectionCommands();
        }
    }

    #endregion

    #region Sub ViewModels

    public DepartmentsViewModel DepartmentsViewModel { get; }
    public BedsViewModel BedsViewModel { get; }
    public BedTypesViewModel BedTypesViewModel { get; }

    #endregion

    public GuideViewModel(IServiceProvider serviceProvider, IWindowService windowService)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
        _messageService = _serviceProvider.GetRequiredService<IMessageService>();

        var departmentService = _serviceProvider.GetRequiredService<IDepartmentService>();
        var bedService = _serviceProvider.GetRequiredService<IBedService>();
        var bedTypesService = _serviceProvider.GetRequiredService<IBedTypesService>();

        DepartmentsViewModel = new DepartmentsViewModel(departmentService);
        BedsViewModel = new BedsViewModel(bedService);
        BedTypesViewModel = new BedTypesViewModel(bedTypesService);

        _editSelectedCommand = new AsyncRelayCommand(OnEditSelectedAsync, HasSelectionForCurrentSection);
        _deleteSelectedCommand = new AsyncRelayCommand(OnDeleteSelectedAsync, HasSelectionForCurrentSection);

        CurrentSection = GuideSection.Departments;

        AddRecordCommand = new AsyncRelayCommand(OnAddRecordAsync);
        DeleteCommandDepartment = new AsyncRelayCommand<Departments>(OnDeleteDepartment, CanDeleteDepartment);
        DeleteCommandBed = new AsyncRelayCommand<Beds>(OnDeleteBed, CanDeleteBed);
        DeleteCommandBedType = new AsyncRelayCommand<BedTypes>(OnDeleteBedType, CanDeleteBedType);
        EditCommandDepartment = new AsyncRelayCommand<Departments>(OnEditDepartment);
        EditCommandBed = new AsyncRelayCommand<Beds>(OnEditBed);
        EditCommandBedType = new AsyncRelayCommand<BedTypes>(OnEditBedType);

        _ = LoadDataAsync(_cancellationTokenSource.Token);
    }

    private bool HasSelectionForCurrentSection()
        => CurrentSection switch
        {
            GuideSection.Departments => SelectedDepartment is not null,
            GuideSection.Beds => SelectedBed is not null,
            GuideSection.BedTypes => SelectedBedType is not null,
            _ => false
        };

    private void NotifySelectionCommands()
    {
        _editSelectedCommand.RaiseCanExecuteChanged();
        _deleteSelectedCommand.RaiseCanExecuteChanged();
    }

    private async Task OnEditSelectedAsync()
    {
        switch (CurrentSection)
        {
            case GuideSection.Departments:
                await OnEditDepartment(SelectedDepartment);
                break;
            case GuideSection.Beds:
                await OnEditBed(SelectedBed);
                break;
            case GuideSection.BedTypes:
                await OnEditBedType(SelectedBedType);
                break;
        }
    }

    private async Task OnDeleteSelectedAsync()
    {
        switch (CurrentSection)
        {
            case GuideSection.Departments:
                await OnDeleteDepartment(SelectedDepartment);
                break;
            case GuideSection.Beds:
                await OnDeleteBed(SelectedBed);
                break;
            case GuideSection.BedTypes:
                await OnDeleteBedType(SelectedBedType);
                break;
        }
    }

    #region Add Record

    private async Task OnAddRecordAsync()
    {
        switch (CurrentSection)
        {
            case GuideSection.Departments:
                await ShowAddDepartmentDialogAsync();
                break;
            case GuideSection.Beds:
                await ShowAddBedDialogAsync();
                break;
            case GuideSection.BedTypes:
                await ShowAddBedTypeDialogAsync();
                break;
        }
    }

    private async Task ShowAddDepartmentDialogAsync()
    {
        var vm = new AddDepartmentViewModel(
            _serviceProvider.GetRequiredService<IDepartmentService>(),
            _messageService);

        vm.DepartmentAdded += async (_, _) =>
        {
            var token = GetSafeCancellationToken();
            if (token != CancellationToken.None) await LoadDataAsync(token);
        };

        await _windowService.ShowAddDepartmentDialogAsync(vm);
    }

    private async Task ShowAddBedDialogAsync()
    {
        var vm = new AddBedViewModel(
            _serviceProvider.GetRequiredService<IBedService>(),
            _serviceProvider.GetRequiredService<IDepartmentService>(),
            _serviceProvider.GetRequiredService<IBedTypesService>(),
            _messageService);

        vm.BedAdded += async (_, _) =>
        {
            var token = GetSafeCancellationToken();
            if (token != CancellationToken.None) await LoadDataAsync(token);
        };

        await _windowService.ShowAddBedDialogAsync(vm);
    }

    private async Task ShowAddBedTypeDialogAsync()
    {
        var vm = new AddBedTypeViewModel(
            _serviceProvider.GetRequiredService<IBedTypesService>(),
            _messageService);

        vm.BedTypeAdded += async (_, _) =>
        {
            var token = GetSafeCancellationToken();
            if (token != CancellationToken.None) await LoadDataAsync(token);
        };

        await _windowService.ShowAddBedTypeDialogAsync(vm);
    }

    #endregion

    #region Edit Handlers

    private async Task OnEditDepartment(Departments departmentToEdit)
    {
        if (departmentToEdit is null)
            return;

        var vm = new EditDepartmentViewModel(
            _serviceProvider.GetRequiredService<IDepartmentService>(),
            _messageService,
            departmentToEdit);

        vm.DepartmentUpdated += async (_, _) =>
        {
            var token = GetSafeCancellationToken();
            if (token != CancellationToken.None) await LoadDataAsync(token);
        };

        await _windowService.ShowEditDepartmentDialogAsync(vm);
        if (vm is IDisposable d) d.Dispose();
    }

    private async Task OnEditBed(Beds bedToEdit)
    {
        if (bedToEdit is null)
            return;

        var vm = new EditBedViewModel(
            _serviceProvider.GetRequiredService<IBedService>(),
            _serviceProvider.GetRequiredService<IDepartmentService>(),
            _serviceProvider.GetRequiredService<IBedTypesService>(),
            _messageService,
            bedToEdit);

        vm.BedUpdated += async (_, _) =>
        {
            var token = GetSafeCancellationToken();
            if (token != CancellationToken.None) await LoadDataAsync(token);
        };

        await _windowService.ShowEditBedDialogAsync(vm);
        if (vm is IDisposable d) d.Dispose();
    }

    private async Task OnEditBedType(BedTypes bedTypeToEdit)
    {
        if (bedTypeToEdit is null)
            return;

        var vm = new EditBedTypeViewModel(
            _serviceProvider.GetRequiredService<IBedTypesService>(),
            _messageService,
            bedTypeToEdit);

        vm.BedTypeUpdated += async (_, _) =>
        {
            var token = GetSafeCancellationToken();
            if (token != CancellationToken.None) await LoadDataAsync(token);
        };

        await _windowService.ShowEditBedTypeDialogAsync(vm);
        if (vm is IDisposable d) d.Dispose();
    }

    #endregion

    #region Delete Handlers

    private bool CanDeleteDepartment(Departments d) => d != null;
    private bool CanDeleteBed(Beds b) => b != null;
    private bool CanDeleteBedType(BedTypes bt) => bt != null;

    private async Task OnDeleteDepartment(Departments department)
    {
        if (department == null) return;

        var token = GetSafeCancellationToken();
        if (token == CancellationToken.None) return;

        var bedSvc = _serviceProvider.GetRequiredService<IBedService>();
        var beds = await bedSvc.GetBedsByDepartmentIdAsync(department.Id, token);
        if (beds.Any())
        {
            await _messageService.ShowWarningAsync(
                "Нельзя удалить отделение, так как к нему привязаны койки. Удалите их или переназначьте.",
                "Удаление невозможно");
            return;
        }

        bool confirmed = await _messageService.ShowQuestionAsync(
            $"Вы уверены, что хотите удалить отделение \"{department.DepartmentName}\"?",
            "Подтверждение удаления");

        if (!confirmed) return;

        try
        {
            token = GetSafeCancellationToken();
            if (token == CancellationToken.None) return;

            await _serviceProvider.GetRequiredService<IDepartmentService>()
                .DeleteDepartmentAsync(department.Id, token);
            DepartmentsViewModel.Departments.Remove(department);
            await _messageService.ShowInfoAsync("Отделение успешно удалено.", "Успех");

            token = GetSafeCancellationToken();
            if (token != CancellationToken.None) await LoadDataAsync(token);
        }
        catch (RelatedRecordsExistException ex)
        {
            await _messageService.ShowErrorAsync(ex.Message, "Ошибка удаления");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            await _messageService.ShowErrorAsync($"Непредвиденная ошибка при удалении отделения: {ex.Message}");
            _logger.Error(ex, "Непредвиденная ошибка при удалении отделения.");
        }
    }

    private async Task OnDeleteBed(Beds bed)
    {
        if (bed == null) return;

        var token = GetSafeCancellationToken();
        if (token == CancellationToken.None) return;

        var hasMovements = await _serviceProvider.GetRequiredService<IListPatientMovementService>()
            .HasMovementsForBedAsync(bed.Id, token);
        if (hasMovements)
        {
            await _messageService.ShowWarningAsync(
                "Нельзя удалить эту запись коек, так как по ней есть данные о движении пациентов.\n" +
                "Для прекращения использования укажите дату в поле «Не используется с».",
                "Удаление невозможно");
            return;
        }

        bool confirmed = await _messageService.ShowQuestionAsync(
            $"Вы уверены, что хотите удалить койку \"{bed.BedType?.BedTypeName}\" (ID: {bed.Id})?",
            "Подтверждение удаления");

        if (!confirmed) return;

        try
        {
            token = GetSafeCancellationToken();
            if (token == CancellationToken.None) return;

            await _serviceProvider.GetRequiredService<IBedService>().DeleteBedAsync(bed.Id, token);
            BedsViewModel.Beds.Remove(bed);
            await _messageService.ShowInfoAsync("Койка успешно удалена.", "Успех");

            token = GetSafeCancellationToken();
            if (token != CancellationToken.None) await LoadDataAsync(token);
        }
        catch (RelatedRecordsExistException ex)
        {
            await _messageService.ShowErrorAsync(ex.Message, "Ошибка удаления");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            await _messageService.ShowErrorAsync($"Непредвиденная ошибка при удалении койки: {ex.Message}");
            _logger.Error(ex, "Непредвиденная ошибка при удалении койки.");
        }
    }

    private async Task OnDeleteBedType(BedTypes bedType)
    {
        if (bedType == null) return;

        bool confirmed = await _messageService.ShowQuestionAsync(
            $"Вы уверены, что хотите удалить тип койки \"{bedType.BedTypeName}\"?",
            "Подтверждение удаления");

        if (!confirmed) return;

        try
        {
            var token = GetSafeCancellationToken();
            if (token == CancellationToken.None) return;

            await _serviceProvider.GetRequiredService<IBedTypesService>()
                .DeleteBedTypeAsync(bedType.Id, token);
            BedTypesViewModel.BedTypes.Remove(bedType);
            await _messageService.ShowInfoAsync("Тип койки успешно удален.", "Успех");

            token = GetSafeCancellationToken();
            if (token != CancellationToken.None) await LoadDataAsync(token);
        }
        catch (RelatedRecordsExistException ex)
        {
            await _messageService.ShowErrorAsync(ex.Message, "Ошибка удаления");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            await _messageService.ShowErrorAsync($"Непредвиденная ошибка при удалении типа койки: {ex.Message}");
            _logger.Error(ex, "Непредвиденная ошибка при удалении типа койки.");
        }
    }

    #endregion

    #region UI Logic

    private void UpdateSelectedViewModel()
    {
        CurrentContentViewModel = CurrentSection switch
        {
            GuideSection.Departments => DepartmentsViewModel,
            GuideSection.Beds => BedsViewModel,
            GuideSection.BedTypes => BedTypesViewModel,
            _ => DepartmentsViewModel
        };
    }

    #endregion

    #region Data Loading

    private async Task LoadDataAsync(CancellationToken cancellationToken)
    {
        try
        {
            var deptSvc = _serviceProvider.GetRequiredService<IDepartmentService>();
            var bedSvc = _serviceProvider.GetRequiredService<IBedService>();
            var bedTypesSvc = _serviceProvider.GetRequiredService<IBedTypesService>();

            var departmentsTask = deptSvc.GetAllDepartmentAsync(cancellationToken);
            var bedsWithDeptsTask = bedSvc.GetAllBedsWithDepartmentsAsync(cancellationToken);
            var allBedsTask = bedSvc.GetAllBedAsync(cancellationToken);
            var bedTypesTask = bedTypesSvc.GetAllBedTypesAsync(cancellationToken);

            await Task.WhenAll(departmentsTask, bedsWithDeptsTask, allBedsTask, bedTypesTask);

            var departments = await departmentsTask;
            var beds = await bedsWithDeptsTask;
            var allBeds = await allBedsTask;
            var bedTypes = await bedTypesTask;

            UpdateCollection(Departments, departments);
            UpdateCollection(Beds, beds);
            UpdateCollection(BedTypes, bedTypes);
            UpdateCollection(AvailableDepartments, departments);
            UpdateCollection(AvailableBeds, allBeds);

            UpdateCollection(DepartmentsViewModel.Departments, departments);
            UpdateCollection(BedsViewModel.Beds, beds);
            UpdateCollection(BedTypesViewModel.BedTypes, bedTypes);

            // Иначе ItemsSource DataGrid (FilteredBeds) остаётся от старого набора Beds
            BedsViewModel.RefreshFilter();

            _logger.Info("Все данные успешно загружены.");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.Error(ex, "Произошла ошибка при загрузке данных.");
            await _messageService.ShowErrorAsync($"Произошла ошибка при загрузке данных: {ex.Message}");
        }
    }

    private static void UpdateCollection<T>(ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        if (items != null)
            foreach (var item in items)
                collection.Add(item);
    }

    #endregion

    #region Cleanup

    private CancellationToken GetSafeCancellationToken()
    {
        var cts = _cancellationTokenSource;
        if (cts == null) return CancellationToken.None;
        try
        {
            var token = cts.Token;
            if (!token.IsCancellationRequested) return token;
        }
        catch (ObjectDisposedException) { }
        return CancellationToken.None;
    }

    public override void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;

        if (DepartmentsViewModel is IDisposable d1) d1.Dispose();
        if (BedsViewModel is IDisposable d2) d2.Dispose();
        if (BedTypesViewModel is IDisposable d3) d3.Dispose();
        if (_currentContentViewModel is IDisposable d4) d4.Dispose();
        _currentContentViewModel = null;

        base.Dispose();
    }

    #endregion
}
