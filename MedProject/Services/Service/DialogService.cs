using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MedProject.Services.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace MedProject.Services.Service;

public class DialogService : IDialogService
{
    private readonly Window _owner;

    public DialogService(Window owner)
    {
        _owner = owner;
    }

    public async Task<string?> BrowseFolderAsync(string? initialDirectory = null)
    {
        var options = new FolderPickerOpenOptions
        {
            Title = "Выберите папку",
            AllowMultiple = false
        };

        var results = await _owner.StorageProvider.OpenFolderPickerAsync(options);
        return results.FirstOrDefault()?.Path.LocalPath;
    }

    public async Task<string?> BrowseFileAsync(string? initialDirectory = null)
    {
        var options = new FilePickerOpenOptions
        {
            Title = "Выберите файл",
            AllowMultiple = false
        };

        var results = await _owner.StorageProvider.OpenFilePickerAsync(options);
        return results.FirstOrDefault()?.Path.LocalPath;
    }
}
