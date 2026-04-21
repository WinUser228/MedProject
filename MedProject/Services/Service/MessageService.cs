using Avalonia.Controls;
using MedProject.Services.Interfaces;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;

namespace MedProject.Services.Service;

public class MessageService : IMessageService
{
    private const double DialogMaxWidth = 600;

    public async System.Threading.Tasks.Task ShowInfoAsync(string message, string title = "Информация")
    {
        var box = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
        {
            ContentTitle = title,
            ContentMessage = message,
            ButtonDefinitions = ButtonEnum.Ok,
            Icon = Icon.Info,
            MaxWidth = DialogMaxWidth,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        });
        await box.ShowAsync();
    }

    public async System.Threading.Tasks.Task ShowWarningAsync(string message, string title = "Предупреждение")
    {
        var box = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
        {
            ContentTitle = title,
            ContentMessage = message,
            ButtonDefinitions = ButtonEnum.Ok,
            Icon = Icon.Warning,
            MaxWidth = DialogMaxWidth,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        });
        await box.ShowAsync();
    }

    public async System.Threading.Tasks.Task ShowErrorAsync(string message, string title = "Ошибка")
    {
        var box = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
        {
            ContentTitle = title,
            ContentMessage = message,
            ButtonDefinitions = ButtonEnum.Ok,
            Icon = Icon.Error,
            MaxWidth = DialogMaxWidth,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        });
        await box.ShowAsync();
    }

    public async System.Threading.Tasks.Task<bool> ShowQuestionAsync(string message, string title = "Вопрос")
    {
        // Standard button captions are not localized (Yes/No). Use custom buttons with Russian labels.
        var box = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
        {
            ContentTitle = title,
            ContentMessage = message,
            Icon = Icon.Question,
            MaxWidth = DialogMaxWidth,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ButtonDefinitions = new[]
            {
                new ButtonDefinition { Name = "Да", IsDefault = true },
                new ButtonDefinition { Name = "Нет", IsCancel = true },
            }
        });
        var result = await box.ShowAsync();
        return string.Equals(result, "Да", System.StringComparison.Ordinal);
    }

    public async System.Threading.Tasks.Task<bool> ShowTwoOptionQuestionAsync(
        string message,
        string title,
        string primaryButtonText,
        string secondaryButtonText)
    {
        var box = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
        {
            ContentTitle = title,
            ContentMessage = message,
            Icon = Icon.Question,
            MaxWidth = DialogMaxWidth,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ButtonDefinitions = new[]
            {
                new ButtonDefinition { Name = primaryButtonText, IsDefault = true },
                new ButtonDefinition { Name = secondaryButtonText, IsCancel = true },
            }
        });
        var result = await box.ShowAsync();
        return string.Equals(result, primaryButtonText, System.StringComparison.Ordinal);
    }

    public async System.Threading.Tasks.Task<UnsavedChangesChoice> ShowUnsavedChangesDialogAsync(
        string message,
        string title = "Несохранённые данные")
    {
        // Standard button captions are not localized (Yes/No/Cancel). Use custom buttons with Russian labels.
        var box = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
        {
            ContentTitle = title,
            ContentMessage = message,
            Icon = Icon.Warning,
            MaxWidth = DialogMaxWidth,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ButtonDefinitions = new[]
            {
                new ButtonDefinition { Name = "Да", IsDefault = true },
                new ButtonDefinition { Name = "Нет" },
                new ButtonDefinition { Name = "Отмена", IsCancel = true },
            }
        });
        var result = await box.ShowAsync();

        return result switch
        {
            "Да" => UnsavedChangesChoice.Save,
            "Нет" => UnsavedChangesChoice.Discard,
            _ => UnsavedChangesChoice.Cancel,
        };
    }
}
