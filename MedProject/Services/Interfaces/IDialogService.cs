using System.Threading.Tasks;

namespace MedProject.Services.Interfaces;

public interface IDialogService
{
    Task<string?> BrowseFolderAsync(string? initialDirectory = null);
    Task<string?> BrowseFileAsync(string? initialDirectory = null);
}
