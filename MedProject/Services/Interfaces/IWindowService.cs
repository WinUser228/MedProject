using MedProject.ViewModels.AddViewModels;
using MedProject.ViewModels.EditModelView;
using System.Threading.Tasks;

namespace MedProject.Services.Interfaces;

/// <summary>
/// Service for opening application dialogs from ViewModels.
/// </summary>
public interface IWindowService
{
    Task ShowAddBedDialogAsync(AddBedViewModel vm);
    Task ShowEditBedDialogAsync(EditBedViewModel vm);
    Task ShowAddDepartmentDialogAsync(AddDepartmentViewModel vm);
    Task ShowEditDepartmentDialogAsync(EditDepartmentViewModel vm);
    Task ShowAddBedTypeDialogAsync(AddBedTypeViewModel vm);
    Task ShowEditBedTypeDialogAsync(EditBedTypeViewModel vm);
    Task ShowReportDateSelectorDialogAsync(ViewModels.ReportDateSelectorViewModel vm);
}
