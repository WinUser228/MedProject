using HospitalStatistics.Core.Entities;
using HospitalStatistics.Core.Services.Interfaces.Entities;
using MedProject.Services.Interfaces;
using MedProject.Utilities;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MedProject.ViewModels.AddViewModels;

public class AddDepartmentViewModel : ViewModelValidationBase
{
    private bool _shouldValidate;

    public ICommand AddDepartmentCommand { get; }
    public event EventHandler? DepartmentAdded;
    public Action<bool?>? CloseWindow { get; set; }

    private readonly IDepartmentService _departmentService;
    private readonly IMessageService _messageService;
    private readonly CancellationTokenSource _cts = new();

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value) && _shouldValidate)
                ValidateName();
        }
    }

    private void ValidateName()
    {
        if (!_shouldValidate)
        {
            SetFieldError(nameof(Name), null);
            return;
        }

        string? error = null;
        if (string.IsNullOrWhiteSpace(Name))
            error = "Название отделения не может быть пустым.";
        else if (Name.Length < 2)
            error = "Название отделения должно содержать минимум 2 символа.";

        SetFieldError(nameof(Name), error);
    }

    public AddDepartmentViewModel(IDepartmentService departmentService, IMessageService messageService)
    {
        _departmentService = departmentService;
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        AddDepartmentCommand = new AsyncRelayCommand<object>(async _ => await SaveAsync(), _ => true);
    }

    private async Task SaveAsync()
    {
        _shouldValidate = true;
        ValidateName();

        if (HasErrors)
            return;

        try
        {
            var entity = new Departments { DepartmentName = Name };
            await _departmentService.CreateDepartmentAsync(entity, _cts.Token);
            await _messageService.ShowInfoAsync("Отделение успешно добавлено.", "Успех");
            DepartmentAdded?.Invoke(this, EventArgs.Empty);
            CloseWindow?.Invoke(true);
        }
        catch (OperationCanceledException) { CloseWindow?.Invoke(false); }
        catch (Exception ex)
        {
            await _messageService.ShowErrorAsync($"Ошибка при добавлении отделения: {ex.Message}", "Ошибка");
            CloseWindow?.Invoke(false);
        }
    }
}
