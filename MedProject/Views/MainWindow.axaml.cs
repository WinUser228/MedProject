using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MedProject.ViewModels;

namespace MedProject.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        // KeyBinding на Window не получает событие, когда фокус в TextBox/DataGrid —
        // дочерние элементы обрабатывают клавиши раньше. Tunnel перехватывает до них.
        AddHandler(KeyDownEvent, OnMainWindowNavigationKeyDown, RoutingStrategies.Tunnel);
    }

    private void OnMainWindowNavigationKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyModifiers != KeyModifiers.Control)
            return;

        if (DataContext is not NavigationViewModel nav)
            return;

        switch (e.Key)
        {
            case Key.D1:
            case Key.NumPad1:
                nav.FormCommand.Execute(null);
                NavFormRadio.IsChecked = true;
                e.Handled = true;
                break;
            case Key.D2:
            case Key.NumPad2:
                nav.GuideCommand.Execute(null);
                NavGuideRadio.IsChecked = true;
                e.Handled = true;
                break;
            case Key.D3:
            case Key.NumPad3:
                nav.HelpCommand.Execute(null);
                NavHelpRadio.IsChecked = true;
                e.Handled = true;
                break;
        }
    }
}
