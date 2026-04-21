using HospitalStatistics.Core.Entities;
using HospitalStatistics.Core.Services.Interfaces.Entities;
using MedProject.Services.Interfaces;
using MedProject.Utilities;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MedProject.ViewModels.EditModelView;

public class EditBedTypeViewModel : ViewModelValidationBase
{
    private bool _shouldValidate;

    private readonly IBedTypesService _bedTypesService;
    private readonly IMessageService _messageService;
    private readonly BedTypes _originalBedType;
    private readonly CancellationTokenSource _cts = new();

    public ICommand UpdateBedTypeCommand { get; }
    public event EventHandler? BedTypeUpdated;
    public Action<bool?>? CloseWindow { get; set; }
    public int Id { get; private set; }

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
        else if (BedTypeName == _originalBedType?.BedTypeName)
            error = "Новое название должно отличаться от текущего.";

        SetFieldError(nameof(BedTypeName), error);
    }

    public EditBedTypeViewModel(IBedTypesService bedTypesService, IMessageService messageService,
        BedTypes bedTypeToEdit)
    {
        _bedTypesService = bedTypesService ?? throw new ArgumentNullException(nameof(bedTypesService));
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        _originalBedType = bedTypeToEdit ?? throw new ArgumentNullException(nameof(bedTypeToEdit));
        Id = bedTypeToEdit.Id;
        BedTypeName = bedTypeToEdit.BedTypeName;
        UpdateBedTypeCommand = new AsyncRelayCommand<object>(async _ => await UpdateAsync(), _ => true);
    }

    private async Task UpdateAsync()
    {
        _shouldValidate = true;
        ValidateBedTypeName();

        if (HasErrors)
            return;

        try
        {
            var entity = new BedTypes { Id = Id, BedTypeName = BedTypeName };
            await _bedTypesService.UpdateBedTypeAsync(Id, entity, _cts.Token);
            await _messageService.ShowInfoAsync("Тип койки успешно обновлен.", "Успех");
            BedTypeUpdated?.Invoke(this, EventArgs.Empty);
            CloseWindow?.Invoke(true);
        }
        catch (OperationCanceledException) { CloseWindow?.Invoke(false); }
        catch (Exception ex)
        {
            await _messageService.ShowErrorAsync($"Ошибка при обновлении типа койки: {ex.Message}", "Ошибка");
            CloseWindow?.Invoke(false);
        }
    }
}
