using System;
using System.Windows.Input;

namespace MedProject.Utilities;

public class RelayCommand : ICommand
{
    private readonly Action<object?>? _executeWithParam;
    private readonly Action? _executeNoParam;
    private readonly Func<object?, bool>? _canExecuteWithParam;
    private readonly Func<bool>? _canExecuteNoParam;

    public event EventHandler? CanExecuteChanged;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _executeWithParam = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecuteWithParam = canExecute;
    }

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _executeNoParam = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecuteNoParam = canExecute;
    }

    public bool CanExecute(object? parameter)
    {
        if (_canExecuteWithParam != null) return _canExecuteWithParam(parameter);
        if (_canExecuteNoParam != null) return _canExecuteNoParam();
        return true;
    }

    public void Execute(object? parameter)
    {
        _executeWithParam?.Invoke(parameter);
        _executeNoParam?.Invoke();
    }

    public void RaiseCanExecuteChanged()
        => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
