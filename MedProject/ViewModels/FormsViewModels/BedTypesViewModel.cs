using HospitalStatistics.Core.Entities;
using HospitalStatistics.Core.Services.Interfaces.Entities;
using MedProject.Utilities;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MedProject.ViewModels.FormsViewModels;

public class BedTypesViewModel : ViewModelBase, IDisposable
{
    private readonly IBedTypesService _bedTypesService;
    private CancellationTokenSource _cts = new();

    public ObservableCollection<BedTypes> BedTypes { get; set; } = new();
    public ObservableCollection<BedTypes> FilteredBedTypes { get; } = new();

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

    public BedTypesViewModel(IBedTypesService bedTypesService)
    {
        _bedTypesService = bedTypesService ?? throw new ArgumentNullException(nameof(bedTypesService));
        _ = LoadBedTypesAsync();
    }

    public async Task LoadBedTypesAsync()
    {
        try
        {
            var token = _cts.Token;
            var result = await _bedTypesService.GetAllBedTypesAsync(token);
            if (token.IsCancellationRequested) return;

            BedTypes.Clear();
            foreach (var item in result)
                BedTypes.Add(item);

            ApplyFilter();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during bedtypes loading: {ex.Message}");
            HasSearchResults = false;
        }
    }

    public async Task UpdateBedTypeAsync(BedTypes bedType)
    {
        try
        {
            await _bedTypesService.UpdateBedTypeAsync(bedType.Id, bedType, _cts.Token);
            await LoadBedTypesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error during bed type update: {ex.Message}");
        }
    }

    private void ApplyFilter()
    {
        FilteredBedTypes.Clear();

        var filtered = BedTypes.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(b =>
                b.BedTypeName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false);
        }

        foreach (var item in filtered)
            FilteredBedTypes.Add(item);

        HasSearchResults = FilteredBedTypes.Any();
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
