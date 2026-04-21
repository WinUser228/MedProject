using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace MedProject.Converters;

/// <summary>
/// Converts between DateTime? (ViewModel) and DateTimeOffset? (Avalonia DatePicker.SelectedDate).
/// </summary>
public class DateTimeToDateTimeOffsetConverter : IValueConverter
{
    public static readonly DateTimeToDateTimeOffsetConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime dt)
            return (DateTimeOffset?)new DateTimeOffset(dt, TimeSpan.Zero);
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTimeOffset dto)
            return (DateTime?)dto.DateTime;
        return null;
    }
}
