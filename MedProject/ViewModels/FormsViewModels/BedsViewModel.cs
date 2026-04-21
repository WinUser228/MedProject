using HospitalStatistics.Core.Entities;
using HospitalStatistics.Core.Services.Interfaces.Entities;
using MedProject.Models;
using MedProject.Utilities;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MedProject.ViewModels.FormsViewModels;

public class BedsViewModel : ViewModelBase
{
    private readonly IBedService _bedService;
    private CancellationTokenSource _cts = new();

    public ObservableCollection<Beds> Beds { get; set; } = new();
    public ObservableCollection<Beds> FilteredBeds { get; } = new();

    private bool _showOnlyCurrentYear = true;
    public bool ShowOnlyCurrentYear
    {
        get => _showOnlyCurrentYear;
        set
        {
            if (SetProperty(ref _showOnlyCurrentYear, value))
                ApplyFilter();
        }
    }

    private bool _showOnlyActiveToday = true;
    public bool ShowOnlyActiveToday
    {
        get => _showOnlyActiveToday;
        set
        {
            if (SetProperty(ref _showOnlyActiveToday, value))
                ApplyFilter();
        }
    }

    private bool _hasSearchResults;
    public bool HasSearchResults
    {
        get => _hasSearchResults;
        set => SetProperty(ref _hasSearchResults, value);
    }

    private string _searchText;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                ApplyFilter();
        }
    }

    public ObservableCollection<FilterColumn> AvailableFilterColumns { get; }

    private FilterColumn _selectedFilterColumn;
    public FilterColumn SelectedFilterColumn
    {
        get => _selectedFilterColumn;
        set
        {
            if (SetProperty(ref _selectedFilterColumn, value))
                ApplyFilter();
        }
    }

    public ObservableCollection<FilterColumn> AvailableDepartments { get; set; } = new();

    public BedsViewModel(IBedService bedService)
    {
        _bedService = bedService;

        AvailableFilterColumns = new ObservableCollection<FilterColumn>
        {
            new FilterColumn { PropertyName = "BedType.BedTypeName", DisplayName = "Тип койки" },
            new FilterColumn { PropertyName = "Amount", DisplayName = "Количество" },
            new FilterColumn { PropertyName = "Department.DepartmentName", DisplayName = "Отделение" },
            new FilterColumn { PropertyName = "EffectiveDate", DisplayName = "Дата начала действия" },
            new FilterColumn { PropertyName = "NotUsedSince", DisplayName = "Не используется с" },
            new FilterColumn { PropertyName = "MonthlyPlan", DisplayName = "Месячный план" }
        };
        SelectedFilterColumn = AvailableFilterColumns.FirstOrDefault();

        _ = LoadBedsAsync();
    }

    public async Task LoadBedsAsync()
    {
        var result = await _bedService.GetAllBedsWithDepartmentsAsync(_cts.Token);
        Beds.Clear();
        foreach (var item in result)
            Beds.Add(item);

        ApplyFilter();
    }

    /// <summary>Обновляет <see cref="FilteredBeds"/> после замены элементов в <see cref="Beds"/> извне (например полная перезагрузка в справочнике).</summary>
    public void RefreshFilter() => ApplyFilter();

    public async Task UpdateBedAsync(Beds bed)
    {
        await _bedService.UpdateBedAsync(bed.Id, bed, _cts.Token);

        var existingBed = Beds.FirstOrDefault(b => b.Id == bed.Id);
        if (existingBed != null)
        {
            var updatedBed = await _bedService.GetBedByIdAsync(bed.Id, _cts.Token);
            if (updatedBed != null)
            {
                int index = Beds.IndexOf(existingBed);
                Beds[index] = updatedBed;
            }
        }

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        FilteredBeds.Clear();

        foreach (var bed in Beds)
        {
            if (MatchesFilter(bed))
                FilteredBeds.Add(bed);
        }

        HasSearchResults = FilteredBeds.Any();
    }

    private bool MatchesFilter(Beds bed)
    {
        if (bed == null) return false;

        var today = DateOnly.FromDateTime(DateTime.Today);

        if (ShowOnlyCurrentYear && bed.EffectiveDate.Year != today.Year)
            return false;

        if (ShowOnlyActiveToday &&
            !(bed.EffectiveDate <= today && (bed.NotUsedSince == null || bed.NotUsedSince > today)))
            return false;

        if (string.IsNullOrWhiteSpace(SearchText) || SelectedFilterColumn == null)
            return true;

        object propertyValue = GetPropertyValue(bed, SelectedFilterColumn.PropertyName);
        return propertyValue != null &&
               propertyValue.ToString().Contains(SearchText, StringComparison.OrdinalIgnoreCase);
    }

    private static object GetPropertyValue(object obj, string propertyPath)
    {
        if (obj == null || string.IsNullOrWhiteSpace(propertyPath))
            return null;

        object current = obj;
        foreach (var part in propertyPath.Split('.'))
        {
            if (current == null) return null;
            PropertyInfo propInfo = current.GetType().GetProperty(part,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (propInfo == null) return null;
            current = propInfo.GetValue(current);
        }
        return current;
    }
}
