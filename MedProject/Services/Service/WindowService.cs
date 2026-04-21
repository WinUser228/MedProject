using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using MedProject.Services.Interfaces;
using MedProject.ViewModels;
using MedProject.ViewModels.AddViewModels;
using MedProject.ViewModels.EditModelView;
using MedProject.Views.ViewEnterForm;
using System;
using System.Threading.Tasks;

namespace MedProject.Services.Service;

/// <summary>
/// Avalonia implementation of IWindowService.
/// Opens dialogs as child windows of the current main window.
/// </summary>
public class WindowService : IWindowService
{
    private Window GetOwner()
    {
        if (Application.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow;
        return null;
    }

    public Task ShowAddDepartmentDialogAsync(AddDepartmentViewModel vm)
        => ShowFormDialogAsync(vm, result => vm.CloseWindow?.Invoke(result));

    public Task ShowEditDepartmentDialogAsync(EditDepartmentViewModel vm)
        => ShowFormDialogAsync(vm, result => vm.CloseWindow?.Invoke(result));

    public Task ShowAddBedDialogAsync(AddBedViewModel vm)
        => ShowFormDialogAsync(vm, result => vm.CloseWindow?.Invoke(result));

    public Task ShowEditBedDialogAsync(EditBedViewModel vm)
        => ShowFormDialogAsync(vm, result => vm.CloseWindow?.Invoke(result));

    public Task ShowAddBedTypeDialogAsync(AddBedTypeViewModel vm)
        => ShowFormDialogAsync(vm, result => vm.CloseWindow?.Invoke(result));

    public Task ShowEditBedTypeDialogAsync(EditBedTypeViewModel vm)
        => ShowFormDialogAsync(vm, result => vm.CloseWindow?.Invoke(result));

    public Task ShowReportDateSelectorDialogAsync(ReportDateSelectorViewModel vm)
    {
        var tcs = new TaskCompletionSource<bool>();

        var window = new Views.ReportDateSelectorView
        {
            DataContext = vm
        };

        vm.RequestClose += success =>
        {
            window.Close();
            tcs.TrySetResult(success);
        };

        var owner = GetOwner();
        if (owner != null)
            return window.ShowDialog(owner).ContinueWith(_ => tcs.TrySetResult(false));

        window.Show();
        return tcs.Task;
    }

    private async Task ShowFormDialogAsync(object viewModel, Action<bool?> closeCallback)
    {
        var tcs = new TaskCompletionSource<bool>();
        var window = new WindowEnterFormView { DataContext = viewModel };

        closeCallback = result =>
        {
            window.Close();
            tcs.TrySetResult(result == true);
        };

        if (viewModel is AddDepartmentViewModel addDeptVm)
            addDeptVm.CloseWindow = result => { window.Close(); tcs.TrySetResult(result == true); };
        else if (viewModel is EditDepartmentViewModel editDeptVm)
            editDeptVm.CloseWindow = result => { window.Close(); tcs.TrySetResult(result == true); };
        else if (viewModel is AddBedViewModel addBedVm)
            addBedVm.CloseWindow = result => { window.Close(); tcs.TrySetResult(result == true); };
        else if (viewModel is EditBedViewModel editBedVm)
            editBedVm.CloseWindow = result => { window.Close(); tcs.TrySetResult(result == true); };
        else if (viewModel is AddBedTypeViewModel addBedTypeVm)
            addBedTypeVm.CloseWindow = result => { window.Close(); tcs.TrySetResult(result == true); };
        else if (viewModel is EditBedTypeViewModel editBedTypeVm)
            editBedTypeVm.CloseWindow = result => { window.Close(); tcs.TrySetResult(result == true); };

        window.Closed += (_, _) => tcs.TrySetResult(false);

        var owner = GetOwner();
        if (owner != null)
            await window.ShowDialog(owner);
        else
            window.Show();

        await tcs.Task;
    }
}
