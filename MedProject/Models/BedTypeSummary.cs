using MedProject.Utilities;

namespace MedProject.Models;

public class BedTypeSummary : ViewModelBase
{
    private string _bedTypeName = string.Empty;
    public string BedTypeName
    {
        get => _bedTypeName;
        set => SetProperty(ref _bedTypeName, value);
    }

    private int _totalAmount;
    public int TotalAmount
    {
        get => _totalAmount;
        set => SetProperty(ref _totalAmount, value);
    }
}
