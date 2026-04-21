using HospitalStatistics.Core.Entities;
using HospitalStatistics.Core.Services.Interfaces.Entities;
using MedProject.Services.Interfaces;
using MedProject.Utilities;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MedProject.ViewModels.EditModelView;

public class EditDepartmentViewModel : ViewModelValidationBase
{
    private bool _shouldValidate;

    private readonly IDepartmentService _departmentService;
    private readonly IMessageService _messageService;
    private readonly Departments _originalDepartment;
    private readonly CancellationTokenSource _cts = new();

    public ICommand UpdateDepartmentCommand { get; }
    public event EventHandler? DepartmentUpdated;
    public Action<bool?>? CloseWindow { get; set; }
    public int Id { get; private set; }

    private string _departmentName = string.Empty;
    public string DepartmentName
    {
        get => _departmentName;
        set
        {
            if (SetProperty(ref _departmentName, value) && _shouldValidate)
                ValidateDepartmentName();
        }
    }

    private void ValidateDepartmentName()
    {
        if (!_shouldValidate)
        {
            SetFieldError(nameof(DepartmentName), null);
            return;
        }

        string? error = null;
        if (string.IsNullOrWhiteSpace(DepartmentName))
            error = "Название отделения не может быть пустым.";
        else if (DepartmentName.Length < 2)
            error = "Название отделения должно содержать минимум 2 символа.";
        else if (DepartmentName == _originalDepartment?.DepartmentName)
            error = "Новое название должно отличаться от текущего.";

        SetFieldError(nameof(DepartmentName), error);
    }

    public EditDepartmentViewModel(IDepartmentService departmentService, IMessageService messageService,
        Departments departmentToEdit)
    {
        _departmentService = departmentService ?? throw new ArgumentNullException(nameof(departmentService));
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        _originalDepartment = departmentToEdit ?? throw new ArgumentNullException(nameof(departmentToEdit));
        Id = departmentToEdit.Id;
        DepartmentName = departmentToEdit.DepartmentName;
        UpdateDepartmentCommand = new AsyncRelayCommand<object>(async _ => await UpdateAsync(), _ => true);
    }

    private async Task UpdateAsync()
    {
        _shouldValidate = true;
        ValidateDepartmentName();

        if (HasErrors)
            return;

        try
        {
            var entity = new Departments { Id = Id, DepartmentName = DepartmentName };
            await _departmentService.UpdateDepartmentAsync(Id, entity, _cts.Token);
            await _messageService.ShowInfoAsync("Отделение успешно обновлено.", "Успех");
            DepartmentUpdated?.Invoke(this, EventArgs.Empty);
            CloseWindow?.Invoke(true);
        }
        catch (OperationCanceledException) { CloseWindow?.Invoke(false); }
        catch (Exception ex)
        {
            await _messageService.ShowErrorAsync($"Ошибка при обновлении отделения: {ex.Message}", "Ошибка");
            CloseWindow?.Invoke(false);
        }
    }
}
