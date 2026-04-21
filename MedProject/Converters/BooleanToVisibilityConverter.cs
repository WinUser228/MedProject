using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace MedProject.Converters;

/// <summary>
/// Converts bool to bool for IsVisible property. IsInverted=true: true→false, false→true.
/// </summary>
public class BooleanToVisibilityConverter : IValueConverter
{
    public static readonly BooleanToVisibilityConverter Instance = new();
    public static readonly BooleanToVisibilityConverter Inverted = new() { IsInverted = true };

    public bool IsInverted { get; set; } = false;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return IsInverted ? !b : b;
        return IsInverted;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return IsInverted ? !b : b;
        return false;
    }
}
