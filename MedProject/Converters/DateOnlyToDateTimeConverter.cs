using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace MedProject.Converters;

public class DateOnlyToDateTimeConverter : IValueConverter
{
    public static readonly DateOnlyToDateTimeConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateOnly dateOnly)
            return dateOnly.ToDateTime(TimeOnly.MinValue);
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
            return DateOnly.FromDateTime(dateTime);
        if (value is DateTimeOffset offset)
            return DateOnly.FromDateTime(offset.DateTime);
        return default(DateOnly);
    }
}
