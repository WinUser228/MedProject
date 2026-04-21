using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace MedProject.Converters;

/// <summary>
/// Returns true (visible) for non-empty strings, false (collapsed) for null/empty.
/// </summary>
public class StringToVisibilityConverter : IValueConverter
{
    public static readonly StringToVisibilityConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string s && !string.IsNullOrWhiteSpace(s);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
