using HospitalStatistics.Core.Entities;
using HospitalStatistics.Core.Services.Interfaces.Entities;
using MedProject.Services.Interfaces;
using MedProject.Utilities;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MedProject.ViewModels.AddViewModels;

public class AddBedTypeViewModel : ViewModelValidationBase
{
    private bool _shouldValidate;

    public ICommand AddBedTypeCommand { get; }
    public event EventHandler? BedTypeAdded;
    public Action<bool?>? CloseWindow { get; set; }

    private readonly IBedTypesService _bedTypesService;
    private readonly IMessageService _messageService;
    private readonly CancellationTokenSource _cts = new();

    private string _bedTypeName = string.Empty;
    public string BedTypeName
    {
        get => _bedTypeName;
        set
        {
            if (SetProperty(ref _bedTypeName, value) && _shouldValidate)
                ValidateBedTypeName();
        }
    }

    private void ValidateBedTypeName()
    {
        if (!_shouldValidate)
        {
            SetFieldError(nameof(BedTypeName), null);
            return;
        }

        string? error = null;
        if (string.IsNullOrWhiteSpace(BedTypeName))
            error = "Название типа койки не может быть пустым.";
        else if (BedTypeName.Length < 2)
            error = "Название типа койки должно содержать минимум 2 символа.";

        SetFieldError(nameof(BedTypeName), error);
    }

    public AddBedTypeViewModel(IBedTypesService bedTypesService, IMessageService messageService)
    {
        _bedTypesService = bedTypesService ?? throw new ArgumentNullException(nameof(bedTypesService));
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        AddBedTypeCommand = new AsyncRelayCommand<object>(async _ => await SaveAsync(), _ => true);
    }

    private async Task SaveAsync()
    {
        _shouldValidate = true;
        ValidateBedTypeName();

        if (HasErrors)
            return;

        try
        {
            var entity = new BedTypes { BedTypeName = BedTypeName };
            await _bedTypesService.CreateBedTypeAsync(entity, _cts.Token);
            await _messageService.ShowInfoAsync("Тип койки успешно добавлен.", "Успех");
            BedTypeAdded?.Invoke(this, EventArgs.Empty);
            CloseWindow?.Invoke(true);
        }
        catch (OperationCanceledException) { CloseWindow?.Invoke(false); }
        catch (Exception ex)
        {
            await _messageService.ShowErrorAsync($"Ошибка при добавлении типа койки: {ex.Message}", "Ошибка");
            CloseWindow?.Invoke(false);
        }
    }
}
