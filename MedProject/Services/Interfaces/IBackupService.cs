using System.Threading.Tasks;

namespace MedProject.Services.Interfaces;

public interface IBackupService
{
    Task BrowseAndBackupAsync();

    /// <summary>
    /// Выбор файла резервной копии и замена текущей HospitalStatistics.db (с перезапуском приложения).
    /// </summary>
    Task BrowseAndRestoreAsync();
}
