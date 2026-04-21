using Avalonia.Threading;
using HospitalStatistics.Core.Entities;
using HospitalStatistics.Core.Services.Interfaces.Entities;
using MedProject.Models;
using MedProject.Services.Interfaces;
using MedProject.Utilities;
using NLog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MedProject.ViewModels.AddViewModels;

public class AddBedViewModel : BedBaseViewModel, IDisposable
{
    private readonly IBedService _bedService;
    private readonly IDepartmentService _departmentService;
    private readonly IBedTypesService _bedTypesService;
    private readonly IMessageService _messageService;
    private readonly CancellationTokenSource _cts = new();
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public Action<bool?> CloseWindow { get; set; }

    public ObservableCollection<Departments> AvailableDepartments { get; set; } = new();
    public ObservableCollection<BedTypes> AvailableBedTypes { get; set; } = new();

    public ICommand AddBedCommand { get; }

    public event EventHandler BedAdded;

    public AddBedViewModel(IBedService bedService, IDepartmentService departmentService,
        IBedTypesService bedTypesService, IMessageService messageService)
    {
        _logger.Info("Инициализация AddBedViewModel");
        _bedService = bedService ?? throw new ArgumentNullException(nameof(bedService));
        _departmentService = departmentService ?? throw new ArgumentNullException(nameof(departmentService));
        _bedTypesService = bedTypesService ?? throw new ArgumentNullException(nameof(bedTypesService));
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));

        IsLoading = true;
        EffectiveDate = DateTime.Now;
        AmountInput = "0";
        MonthlyPlanInput = "0";

        AddBedCommand = new AsyncRelayCommand<object>(
            async _ => await AddAsync(),
            _ => CanAdd()
        );

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            await Task.WhenAll(LoadDepartmentsAsync(), LoadBedTypesAsync());
        }
        finally
        {
            IsLoading = false;
            RaiseCanExecuteChangedForCommands();
        }
    }

    private async Task LoadDepartmentsAsync()
    {
        try
        {
            var result = await _departmentService.GetAllDepartmentAsync(_cts.Token);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                AvailableDepartments.Clear();
                foreach (var dept in result)
                    AvailableDepartments.Add(dept);
            });
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.Error(ex, "Ошибка при загрузке списка отделений");
            await _messageService.ShowErrorAsync($"Ошибка при загрузке отделений: {ex.Message}");
        }
    }

    private async Task LoadBedTypesAsync()
    {
        try
        {
            var result = await _bedTypesService.GetAllBedTypesAsync(_cts.Token);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                AvailableBedTypes.Clear();
                foreach (var bedType in result)
                    AvailableBedTypes.Add(bedType);
            });
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.Error(ex, "Ошибка при загрузке списка типов коек");
            await _messageService.ShowErrorAsync($"Ошибка при загрузке типов коек: {ex.Message}");
        }
    }

    private bool CanAdd() => !IsLoading;

    private async Task AddAsync()
    {
        if (HasValidationErrors())
        {
            TriggerValidationUIUpdate();
            return;
        }

        if (SelectedBedType == null || SelectedDepartment == null)
        {
            TriggerValidationUIUpdate();
            return;
        }

        try
        {
            var allBeds = await _bedService.GetAllBedAsync(_cts.Token);
            var effectiveDateOnly = DateOnly.FromDateTime(EffectiveDate!.Value);
            bool duplicate = allBeds.Any(b =>
                b.BedTypeId == SelectedBedType.Id &&
                b.DepartmentId == SelectedDepartment.Id &&
                b.Amount == Amount &&
                b.EffectiveDate == effectiveDateOnly &&
                b.MonthlyPlan == MonthlyPlan);

            if (duplicate)
            {
                await _messageService.ShowWarningAsync(
                    "Запись с такими же значениями уже существует. Измените хотя бы одно из полей.",
                    "Дубликат");
                return;
            }

            var entity = new Beds
            {
                BedTypeId = SelectedBedType.Id,
                DepartmentId = SelectedDepartment.Id,
                Amount = Amount,
                EffectiveDate = DateOnly.FromDateTime(EffectiveDate.Value),
                MonthlyPlan = MonthlyPlan,
                NotUsedSince = NotUsedSince.HasValue ? DateOnly.FromDateTime(NotUsedSince.Value) : null
            };

            await _bedService.CreateBedAsync(entity, _cts.Token);
            await _messageService.ShowInfoAsync("Койки успешно добавлены.", "Успех");
            BedAdded?.Invoke(this, EventArgs.Empty);
            CloseWindow?.Invoke(true);
        }
        catch (OperationCanceledException) { CloseWindow?.Invoke(false); }
        catch (Exception ex)
        {
            _logger.Error(ex, "Ошибка при добавлении коек");
            await _messageService.ShowErrorAsync($"Ошибка при добавлении коек: {ex.Message}");
            CloseWindow?.Invoke(false);
        }
    }

    protected override void RaiseCanExecuteChangedForCommands()
    {
        if (AddBedCommand is AsyncRelayCommand<object> cmd)
            cmd.RaiseCanExecuteChanged();
    }

    public override void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        base.Dispose();
    }
}
