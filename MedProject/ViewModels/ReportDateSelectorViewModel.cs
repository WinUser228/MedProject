using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HospitalStatistics.Core.Models;
using HospitalStatistics.Core.Services;
using MedProject.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MedProject.ViewModels;

public partial class ReportDateSelectorViewModel : ObservableObject
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly ReportType _reportType;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMessageService _messageService;
    private readonly IErrorHandlerService _errorHandlerService;
    private readonly IPdfReportGenerator _pdfGenerator;
    private readonly IReportWindowService _windowService;

    [ObservableProperty]
    private DateTime? reportStartDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

    [ObservableProperty]
    private DateTime? reportEndDate = DateTime.Today;

    private string _reportErrorMessage = string.Empty;
    public string ReportErrorMessage
    {
        get => _reportErrorMessage;
        set => SetProperty(ref _reportErrorMessage, value);
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GenerateReportCommand))]
    private bool isBusy;

    public event Action<bool>? RequestClose;

    public ReportDateSelectorViewModel(
        ReportType reportType,
        IServiceScopeFactory scopeFactory,
        IMessageService messageService,
        IErrorHandlerService errorHandlerService,
        IPdfReportGenerator pdfGenerator,
        IReportWindowService windowService)
    {
        _reportType = reportType;
        _scopeFactory = scopeFactory;
        _messageService = messageService;
        _errorHandlerService = errorHandlerService;
        _pdfGenerator = pdfGenerator;
        _windowService = windowService;
    }

    private bool ValidateDates()
    {
        var errors = new List<string>();

        if (!ReportStartDate.HasValue)
            errors.Add("Не выбрана начальная дата периода отчёта");

        if (!ReportEndDate.HasValue)
            errors.Add("Не выбрана конечная дата периода отчёта");

        if (ReportStartDate.HasValue && ReportEndDate.HasValue)
        {
            if (ReportStartDate.Value.Date > ReportEndDate.Value.Date)
                errors.Add("Начальная дата не может быть позже конечной даты");

            if (ReportEndDate.Value.Date > DateTime.Today)
                errors.Add("Конечная дата не может быть в будущем");
        }

        if (errors.Any())
        {
            ReportErrorMessage = string.Join(Environment.NewLine, errors);
            return false;
        }

        ReportErrorMessage = string.Empty;
        return true;
    }

    private bool CanGenerateReport => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanGenerateReport))]
    private async Task GenerateReportAsync(CancellationToken ct)
    {
        if (!ValidateDates()) return;

        var start = DateOnly.FromDateTime(ReportStartDate!.Value);
        var end = DateOnly.FromDateTime(ReportEndDate!.Value);

        try
        {
            IsBusy = true;

            var reportShown = _reportType switch
            {
                ReportType.ReportSeven => await GenerateReportSevenAsync(start, end, ct),
                ReportType.ReportSevenPart2 => await GenerateReportSevenPart2Async(start, end, ct),
                _ => throw new InvalidOperationException("Unknown report type."),
            };

            // Окно закрываем только после успешного показа PDF; при «нет данных» форма выбора дат остаётся открытой
            if (reportShown)
                RequestClose?.Invoke(true);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Ошибка формирования отчёта.");
            string reportName = _reportType == ReportType.ReportSeven ? "Отчёт 7" : "Отчёт 7 часть 2";
            _errorHandlerService.HandleReportException(ex, Logger, reportName, start, end);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<bool> GenerateReportSevenAsync(DateOnly start, DateOnly end, CancellationToken ct)
    {
        const string reportName = "Отчёт 7";
        await using var scope = _scopeFactory.CreateAsyncScope();
        var reportService = scope.ServiceProvider.GetRequiredService<IReportSevenService>();
        var data = await reportService.GetReportAsync(start, end, ct);
        if (IsReportSevenNoData(data))
        {
            var generateEmpty = await _messageService.ShowTwoOptionQuestionAsync(
                "За выбранный период нет данных.\nСформировать пустой PDF?",
                reportName,
                primaryButtonText: "Сформировать",
                secondaryButtonText: "Изменить даты");

            if (!generateEmpty)
                return false;

            var emptyPdfBytes = await _pdfGenerator.GenerateReportSevenEmptyPdfBytesAsync(start, end, ct);
            await _windowService.ShowPreviewAsync(emptyPdfBytes, reportName);
            return true;
        }
        var pdfBytes = await _pdfGenerator.GenerateReportSevenPdfBytesAsync(data, start, end, ct);
        await _windowService.ShowPreviewAsync(pdfBytes, reportName);
        return true;
    }

    private async Task<bool> GenerateReportSevenPart2Async(DateOnly start, DateOnly end, CancellationToken ct)
    {
        const string reportName = "Отчёт 7 часть 2";
        await using var scope = _scopeFactory.CreateAsyncScope();
        var reportService = scope.ServiceProvider.GetRequiredService<IReportSevenService>();
        var data = await reportService.GetReportPart2Async(start, end, ct);
        if (data == null || data.Count == 0)
        {
            var generateEmpty = await _messageService.ShowTwoOptionQuestionAsync(
                "За выбранный период нет данных.\nСформировать пустой PDF?",
                reportName,
                primaryButtonText: "Сформировать",
                secondaryButtonText: "Изменить даты");

            if (!generateEmpty)
                return false;

            var emptyPdfBytes = await _pdfGenerator.GenerateReportSevenPart2EmptyPdfBytesAsync(start, end, ct);
            await _windowService.ShowPreviewAsync(emptyPdfBytes, reportName);
            return true;
        }
        var pdfBytes = await _pdfGenerator.GenerateReportSevenPart2PdfBytesAsync(data, start, end, ct);
        await _windowService.ShowPreviewAsync(pdfBytes, reportName);
        return true;
    }

    [RelayCommand]
    private void CloseDialog() => RequestClose?.Invoke(false);

    private static bool IsReportSevenNoData(IReadOnlyList<ReportSevenDTO>? data)
    {
        if (data == null || data.Count == 0)
            return true;

        if (data.Count != 1)
            return false;

        var d = data[0];
        return string.IsNullOrWhiteSpace(d.DepartmentName) &&
               string.IsNullOrWhiteSpace(d.BedType) &&
               d.BedCount == 0 &&
               d.PatientsAtStartOfDay == 0 &&
               d.TotalAdmitted == 0 &&
               d.FromVillage == 0 &&
               d.ChildrenUnder14 == 0 &&
               d.TransferredFromOtherDepartments == 0 &&
               d.TransferredToOtherDepartments == 0 &&
               d.TotalDischarged == 0 &&
               d.DischargedByTransfer == 0 &&
               d.Died == 0 &&
               d.TotalPatients == 0 &&
               d.FromVillagePatient == 0 &&
               d.MothersWithPatients == 0 &&
               d.MonthlyPlanPercent == 0.0 &&
               d.BedDaysTotal == 0 &&
               d.BedDaysFromVillage == 0 &&
               d.BedDaysMothers == 0;
    }
}
