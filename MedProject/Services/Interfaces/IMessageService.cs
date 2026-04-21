using System.Threading.Tasks;

namespace MedProject.Services.Interfaces;

public enum UnsavedChangesChoice
{
    Save,
    Discard,
    Cancel
}

public interface IMessageService
{
    Task ShowInfoAsync(string message, string title = "Информация");
    Task ShowWarningAsync(string message, string title = "Предупреждение");
    Task ShowErrorAsync(string message, string title = "Ошибка");
    Task<bool> ShowQuestionAsync(string message, string title = "Вопрос");
    Task<bool> ShowTwoOptionQuestionAsync(
        string message,
        string title,
        string primaryButtonText,
        string secondaryButtonText);
    Task<UnsavedChangesChoice> ShowUnsavedChangesDialogAsync(string message, string title = "Несохранённые данные");
}
