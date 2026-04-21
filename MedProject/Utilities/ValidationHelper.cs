using System;
using System.Globalization;

namespace MedProject.Utilities;

public static class ValidationHelper
{
    public static string ValidateIntInput(string input, string propertyName, out int parsedValue,
        bool allowNegative = false, bool isOptional = false)
    {
        parsedValue = 0;

        if (string.IsNullOrWhiteSpace(input))
        {
            if (isOptional) return string.Empty;
            return $"{propertyName} не может быть пустым.";
        }

        if (!long.TryParse(input, out long longValue))
            return $"{propertyName} должен быть числом.";
        if (longValue > int.MaxValue || longValue < int.MinValue)
            return $"{propertyName} не может быть больше {int.MaxValue} и меньше {int.MinValue}.";

        parsedValue = (int)longValue;

        if (!allowNegative && parsedValue < 0)
            return $"{propertyName} не может быть отрицательным.";

        return string.Empty;
    }

    public static string ValidateId(int id, string displayName)
        => id <= 0 ? $"Выберите {displayName.ToLower()}." : string.Empty;

    public static string ValidateDateNotDefault(DateOnly date, string displayName)
        => date == default ? $"{displayName} не выбрана." : string.Empty;

    public static string ValidateStringIsNotNullOrEmpty(string input, string propertyName)
        => string.IsNullOrWhiteSpace(input) ? $"{propertyName} не может быть пустым." : string.Empty;

    public static string ValidateDateInput(string input, string propertyName, out DateOnly parsedDate, bool isOptional = false)
    {
        parsedDate = default;

        if (string.IsNullOrWhiteSpace(input))
        {
            if (isOptional) return string.Empty;
            return $"{propertyName} не может быть пустой.";
        }

        var formats = new[] { "dd.MM.yyyy", "dd-MM-yyyy", "yyyy-MM-dd", "dd MM yyyy", "dd/MM/yyyy", "yyyy/MM/dd" };

        if (DateOnly.TryParseExact(input, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
            return string.Empty;

        if (DateOnly.TryParse(input, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsedDate)
            || DateOnly.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
            return string.Empty;

        return $"{propertyName} должен быть датой в формате дд.мм.гггг.";
    }
}
