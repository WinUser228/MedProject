using HospitalStatistics.Core.Entities;
using HospitalStatistics.Core.Services.Interfaces.Entities;
using MedProject.Utilities;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MedProject.ViewModels.FormsViewModels;

public class DepartmentsViewModel : ViewModelBase, IDisposable
{
    private readonly IDepartmentService _departmentService;
    private CancellationTokenSource _cts = new();

    public ObservableCollection<Departments> Departments { get; set; } = new();
    public ObservableCollection<Departments> FilteredDepartments { get; } = new();

    private bool _hasSearchResults;
    public bool HasSearchResults
    {
        get => _hasSearchResults;
        set => SetProperty(ref _hasSearchResults, value);
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                ApplyFilter();
        }
    }

    public DepartmentsViewModel(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
        _ = LoadDepartmentsAsync();
    }

    public async Task LoadDepartmentsAsync()
    {
        try
        {
            var token = _cts.Token;
            var result = await _departmentService.GetAllDepartmentAsync(token);
            if (token.IsCancellationRequested) return;

            Departments.Clear();
            foreach (var item in result)
                Departments.Add(item);

            ApplyFilter();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during department loading: {ex.Message}");
            HasSearchResults = false;
        }
    }

    public async Task UpdateDepartmentAsync(Departments department)
    {
        try
        {
            await _departmentService.UpdateDepartmentAsync(department.Id, department, _cts.Token);
            await LoadDepartmentsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during department update: {ex.Message}");
        }
    }

    private void ApplyFilter()
    {
        FilteredDepartments.Clear();

        var filtered = Departments.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(d =>
                d.DepartmentName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false);
        }

        foreach (var item in filtered)
            FilteredDepartments.Add(item);

        HasSearchResults = FilteredDepartments.Any();
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
