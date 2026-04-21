using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace MedProject.Utilities;

/// <summary>
/// База для VM с валидацией через <see cref="INotifyDataErrorInfo"/> (поддержка в Avalonia 11).
/// </summary>
public abstract class ViewModelValidationBase : ViewModelBase, INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>> _propertyErrors = new();

    public bool HasErrors => _propertyErrors.Values.Any(v => v.Count > 0);

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return _propertyErrors.SelectMany(kv => kv.Value);
        return _propertyErrors.TryGetValue(propertyName, out var list) ? list : Array.Empty<string>();
    }

    protected void SetFieldError(string propertyName, string? error)
    {
        if (string.IsNullOrEmpty(error))
        {
            if (_propertyErrors.Remove(propertyName))
            {
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
                OnPropertyChanged($"Has{propertyName}Error");
                OnPropertyChanged($"{propertyName}ErrorMessage");
            }
            return;
        }

        if (!_propertyErrors.TryGetValue(propertyName, out var list))
        {
            list = new List<string>();
            _propertyErrors[propertyName] = list;
        }

        if (list.Count == 1 && list[0] == error)
            return;

        list.Clear();
        list.Add(error);
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        OnPropertyChanged($"Has{propertyName}Error");
        OnPropertyChanged($"{propertyName}ErrorMessage");
    }

    public bool HasPropertyError(string propertyName)
        => _propertyErrors.TryGetValue(propertyName, out var l) && l.Count > 0;

    public string? GetPropertyErrorMessage(string propertyName)
        => _propertyErrors.TryGetValue(propertyName, out var l) && l.Count > 0 ? l[0] : null;
}
