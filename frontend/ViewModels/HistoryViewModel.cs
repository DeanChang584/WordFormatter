using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WordFormatterUI.Models.History;
using WordFormatterUI.Services;

namespace WordFormatterUI.ViewModels;

/// <summary>
/// History records ViewModel (design-document §14).
/// Loads, lists, and clears task history. Provides per-record reuse.
/// </summary>
public partial class HistoryViewModel : ObservableObject
{
    private readonly ApiService _api;

    public HistoryViewModel(ApiService api)
    {
        _api = api;
    }

    [ObservableProperty]
    private ObservableCollection<HistoryRecordDto> _records = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasRecords;

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var resp = await _api.GetHistoryAsync();
            if (resp?.Success == true && resp.Data is not null)
            {
                Records = new ObservableCollection<HistoryRecordDto>(resp.Data.History);
                HasRecords = Records.Count > 0;
            }
        }
        catch { }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    public async Task ClearAsync()
    {
        var ok = await _api.ClearHistoryAsync();
        if (ok)
        {
            Records.Clear();
            HasRecords = false;
        }
    }

    [RelayCommand]
    private async Task<HistoryRecordDto?> ReuseAsync(string recordId)
    {
        try
        {
            var resp = await _api.GetHistoryDetailAsync(recordId);
            return resp?.Success == true ? resp.Data : null;
        }
        catch { return null; }
    }
}
