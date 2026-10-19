using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using HospitalStatistics.Core;
using HospitalStatistics.Core.Repository.Interfaces;
using HospitalStatistics.Core.Services;
using HospitalStatistics.Core.Services.Entities;
using HospitalStatistics.Core.Services.Interfaces.Entities;
using HospitalStatistics.Infrastructure;
using HospitalStatistics.Infrastructure.Data;
using HospitalStatistics.Infrastructure.Repository;
using HospitalStatistics.Infrastructure.Services.Reports;
using MedProject.Services.Interfaces;
using MedProject.Services.Service;
using MedProject.Settings;
using MedProject.Utilities;
using MedProject.ViewModels;
using MedProject.ViewModels.TabViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Linq;
using System.Threading;

namespace MedProject;

public partial class App : Application
{
    public static IServiceProvider ServiceProvider { get; private set; }
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public override void Initialize()
    {
        PdfFontSetup.Ensure();
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        DisableAvaloniaDataAnnotationValidation();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Logger.Info("=== Приложение запущено ===");

            AppPaths.EnsureUserFiles();

            var appSettings = new AppSettings();

            try
            {
                var services = new ServiceCollection();
                ConfigureServices(services);
                ServiceProvider = services.BuildServiceProvider();
                Logger.Info("DI контейнер успешно инициализирован.");

                using (var scope = ServiceProvider.CreateScope())
                {
                    var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
                    await dbInitializer.InitializeAsync();
                    Logger.Info("База данных инициализирована.");

                    try
                    {
                        var logCleanupService = scope.ServiceProvider.GetRequiredService<ILogCleanupService>();
                        int deleted = await logCleanupService.DeleteOldLogsAsync(
                            appSettings.LogRetentionDays, CancellationToken.None);
                        if (deleted > 0)
                            Logger.Info($"Очистка логов завершена. Удалено файлов: {deleted}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex, "Ошибка при очистке логов при старте приложения.");
                    }
                }

                var mainWindow = ServiceProvider.GetRequiredService<Views.MainWindow>();
                mainWindow.DataContext = ServiceProvider.GetRequiredService<NavigationViewModel>();

                desktop.MainWindow = mainWindow;
                mainWindow.Show();
                Logger.Info("Главное окно успешно запущено.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Ошибка при инициализации приложения.");
                try
                {
                    var msg = ServiceProvider?.GetService<IMessageService>();
                    if (msg != null)
                        await msg.ShowErrorAsync($"Критическая ошибка запуска: {ex.Message}");
                }
                catch { }
                desktop.Shutdown();
            }
        }

        base.OnFrameworkInitializationCompleted();

        AppDomain.CurrentDomain.UnhandledException += (s, evt) =>
        {
            var ex = evt.ExceptionObject as Exception;
            Logger.Error(ex, "Критическая ошибка на уровне домена приложения");
        };
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging();

        // Store SQLite DB in a user-writable location (important for Linux packages under /opt).
        var connectionString = $"Data Source={AppPaths.DatabasePath}";

        services.AddDbContextFactory<HospitalContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IDbInitializer, DbInitializer>();

        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IBedRepository, BedRepository>();
        services.AddScoped<IBedTypesRepository, BedTypesRepository>();
        services.AddScoped<IListPatientMovementRepository, ListPatientMovementRepository>();

        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IBedService, BedService>();
        services.AddScoped<IBedTypesService, BedTypesService>();
        services.AddScoped<IListPatientMovementService, ListPatientMovementService>();
        services.AddScoped<IReportSevenService, ReportSevenService>();

        services.AddSingleton<IErrorHandlerService, ErrorHandlerService>();
        services.AddSingleton<IMessageService, MessageService>();
        services.AddSingleton<ILogCleanupService, LogCleanupService>();
        services.AddSingleton<IBackupService, BackupService>();
        services.AddSingleton<IPdfReportGenerator, PdfReportGenerator>();
        services.AddSingleton<IReportWindowService, ReportWindowService>();
        services.AddSingleton<IWindowService, WindowService>();

        services.AddSingleton<Func<ReportType, ReportDateSelectorViewModel>>(sp =>
            reportType => ActivatorUtilities.CreateInstance<ReportDateSelectorViewModel>(sp, reportType));

        services.AddScoped<FormViewModel>();
        services.AddScoped<HelpViewModel>();
        services.AddScoped<GuideViewModel>();
        services.AddSingleton<NavigationViewModel>();

        services.AddSingleton<Views.MainWindow>();
    }

    private static void DisableAvaloniaDataAnnotationValidation()
    {
        var toRemove = BindingPlugins.DataValidators
            .OfType<DataAnnotationsValidationPlugin>().ToArray();
        foreach (var plugin in toRemove)
            BindingPlugins.DataValidators.Remove(plugin);
    }
}
