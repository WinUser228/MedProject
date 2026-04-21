using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace MedProject.Converters;

public class StringToNullableIntConverter : IValueConverter
{
    public static readonly StringToNullableIntConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Avalonia may pass BindingNotification into converters on validation errors.
        // Never render that object as text (it would show exception text inside the cell).
        if (value is BindingNotification bn)
        {
            if (bn.ErrorType == BindingErrorType.Error)
                return string.Empty;

            value = bn.Value;
        }

        return value?.ToString() ?? string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var str = value as string;
        if (string.IsNullOrWhiteSpace(str)) return null;
        if (int.TryParse(str, out int val)) return val;
        return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
    }
}
