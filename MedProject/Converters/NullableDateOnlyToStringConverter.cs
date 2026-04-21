using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace MedProject.Converters;

public sealed class NullableDateOnlyToStringConverter : IValueConverter
{
    public static readonly NullableDateOnlyToStringConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
            return string.Empty;

        if (value is DateOnly d)
        {
            if (d == DateOnly.MinValue)
                return string.Empty;

            return d.ToString("dd.MM.yyyy", culture);
        }

        return value?.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

