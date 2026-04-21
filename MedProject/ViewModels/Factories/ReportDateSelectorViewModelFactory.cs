using MedProject.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MedProject;

public class ReportDateSelectorViewModelFactory
{
    private readonly IServiceProvider _provider;

    public ReportDateSelectorViewModelFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    public ReportDateSelectorViewModel Create(ReportType type)
    {
        return ActivatorUtilities.CreateInstance<ReportDateSelectorViewModel>(_provider, type);
    }
}

