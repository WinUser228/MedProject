using Avalonia.Threading;
using MedProject.Utilities;

namespace MedProject.ViewModels;

public class SplashScreenViewModel : ViewModelBase
{
    private double _progress;
    public double Progress
    {
        get => _progress;
        set { _progress = value; OnPropertyChanged(); }
    }

    private string _statusMessage = "Инициализация приложения...";
    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public void UpdateProgress(double progress, string? message)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            Progress = System.Math.Max(0, System.Math.Min(100, progress));
            if (message != null) StatusMessage = message;
        });
    }
}
