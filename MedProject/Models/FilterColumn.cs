namespace MedProject.Models;

public class FilterColumn
{
    public string PropertyName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    public override string ToString() => DisplayName;
}
