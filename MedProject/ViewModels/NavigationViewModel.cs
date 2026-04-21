using MedProject.Utilities;
using MedProject.ViewModels.TabViewModels;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MedProject.ViewModels;

public class NavigationViewModel : ViewModelBase
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly IServiceProvider _provider;

    private object? _currentView;
    public object? CurrentView
    {
        get => _currentView;
        set
        {
            _currentView = value;
            Logger.Info("Смена текущего представления на: {0}", value?.GetType().Name);
            OnPropertyChanged();
        }
    }

    public ICommand HelpCommand { get; }
    public ICommand FormCommand { get; }
    public ICommand GuideCommand { get; }

    public NavigationViewModel(IServiceProvider provider)
    {
        Logger.Info("Инициализация NavigationViewModel");
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));

        HelpCommand = new RelayCommand(_ => CurrentView = _provider.GetRequiredService<HelpViewModel>());
        FormCommand = new RelayCommand(_ =>
        {
            var form = _provider.GetRequiredService<FormViewModel>();
            CurrentView = form;
            _ = ReloadFormDepartmentsFireAndForgetAsync(form);
        });
        GuideCommand = new RelayCommand(_ => CurrentView = _provider.GetRequiredService<GuideViewModel>());

        CurrentView = _provider.GetRequiredService<FormViewModel>();
        Logger.Info("Начальное представление: FormViewModel");
    }

    private static async Task ReloadFormDepartmentsFireAndForgetAsync(FormViewModel form)
    {
        try
        {
            await form.ReloadDepartmentsAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Не удалось обновить список отделений при переходе на форму ввода.");
        }
    }
}
