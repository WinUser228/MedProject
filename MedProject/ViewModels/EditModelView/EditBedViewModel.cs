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

namespace MedProject.ViewModels.EditModelView;

public class EditBedViewModel : BedBaseViewModel, IDisposable
{
    private readonly IBedService _bedService;
    private readonly IDepartmentService _departmentService;
    private readonly IBedTypesService _bedTypesService;
    private readonly IMessageService _messageService;
    private readonly Beds _originalBed;
    private readonly CancellationTokenSource _cts = new();
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public Action<bool?> CloseWindow { get; set; }

    public int Id { get; private set; }

    public ObservableCollection<Departments> AvailableDepartments { get; set; } = new();
    public ObservableCollection<BedTypes> AvailableBedTypes { get; set; } = new();

    public ICommand SaveCommand { get; }

    public event EventHandler BedUpdated;

    public EditBedViewModel(IBedService bedService, IDepartmentService departmentService,
        IBedTypesService bedTypesService, IMessageService messageService, Beds bedToEdit)
    {
        _logger.Info("Инициализация EditBedViewModel для койки ID: {0}", bedToEdit?.Id);
        _bedService = bedService ?? throw new ArgumentNullException(nameof(bedService));
        _departmentService = departmentService ?? throw new ArgumentNullException(nameof(departmentService));
        _bedTypesService = bedTypesService ?? throw new ArgumentNullException(nameof(bedTypesService));
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        _originalBed = bedToEdit ?? throw new ArgumentNullException(nameof(bedToEdit));

        IsLoading = true;
        Id = _originalBed.Id;
        AmountInput = _originalBed.Amount.ToString();
        EffectiveDate = _originalBed.EffectiveDate.ToDateTime(TimeOnly.MinValue);
        NotUsedSince = _originalBed.NotUsedSince?.ToDateTime(TimeOnly.MinValue);
        MonthlyPlanInput = _originalBed.MonthlyPlan.ToString();

        SaveCommand = new AsyncRelayCommand<object>(async _ => await SaveAsync(), _ => CanSave());

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
                SelectedDepartment = AvailableDepartments.FirstOrDefault(d => d.Id == _originalBed.DepartmentId);
            });
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.Error(ex, "Ошибка при загрузке списка отделений.");
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
                SelectedBedType = AvailableBedTypes.FirstOrDefault(bt => bt.Id == _originalBed.BedTypeId);
            });
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.Error(ex, "Ошибка при загрузке списка типов коек.");
            await _messageService.ShowErrorAsync($"Ошибка при загрузке типов коек: {ex.Message}");
        }
    }

    protected override void RaiseCanExecuteChangedForCommands()
    {
        if (SaveCommand is AsyncRelayCommand<object> cmd)
            cmd.RaiseCanExecuteChanged();
    }

    private bool CanSave() => !IsLoading;

    private async Task SaveAsync()
    {
        if (HasValidationErrors())
        {
            TriggerValidationUIUpdate();
            return;
        }

        try
        {
            var effectiveDateOnly = DateOnly.FromDateTime(EffectiveDate!.Value);
            var allBeds = await _bedService.GetAllBedAsync(_cts.Token);

            bool duplicate = allBeds.Any(b =>
                b.Id != Id &&
                b.BedTypeId == SelectedBedType!.Id &&
                b.DepartmentId == SelectedDepartment!.Id &&
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

            var originalNotUsedSinceDate = _originalBed.NotUsedSince?.ToDateTime(TimeOnly.MinValue).Date;
            bool hasChanges = SelectedBedType?.Id != _originalBed.BedTypeId ||
                              SelectedDepartment?.Id != _originalBed.DepartmentId ||
                              Amount != _originalBed.Amount ||
                              EffectiveDate?.Date != _originalBed.EffectiveDate.ToDateTime(TimeOnly.MinValue).Date ||
                              MonthlyPlan != _originalBed.MonthlyPlan ||
                              (NotUsedSince?.Date != originalNotUsedSinceDate);

            var updated = new Beds
            {
                Id = Id,
                BedTypeId = SelectedBedType!.Id,
                DepartmentId = SelectedDepartment!.Id,
                Amount = Amount,
                EffectiveDate = effectiveDateOnly,
                MonthlyPlan = MonthlyPlan,
                NotUsedSince = NotUsedSince.HasValue ? DateOnly.FromDateTime(NotUsedSince.Value) : null
            };

            await _bedService.UpdateBedAsync(Id, updated, _cts.Token);
            string message = hasChanges ? "Койка успешно обновлена." : "Данные койки подтверждены без изменений.";
            await _messageService.ShowInfoAsync(message, "Успех");
            BedUpdated?.Invoke(this, EventArgs.Empty);
            CloseWindow?.Invoke(true);
        }
        catch (OperationCanceledException) { CloseWindow?.Invoke(false); }
        catch (Exception ex)
        {
            await _messageService.ShowErrorAsync($"Ошибка при сохранении койки: {ex.Message}", "Ошибка сохранения");
            CloseWindow?.Invoke(false);
        }
    }

    public override void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        base.Dispose();
    }
}
