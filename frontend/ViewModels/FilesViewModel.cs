using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WordFormatterUI.Models.Common;
using WordFormatterUI.Models.Files;
using WordFormatterUI.Services;

namespace WordFormatterUI.ViewModels;

/// <summary>
/// File-management view model (design-document §7.1).
///
/// Holds the working file list as rich <see cref="FileItemDto"/> objects
/// (name / path / size), the current search keyword, and the recent-open
/// records. Selection state lives in the View (ListView.SelectedItems).
/// </summary>
public partial class FilesViewModel : ObservableObject
{
    private readonly ApiService _api;

    public FilesViewModel(ApiService api)
    {
        _api = api;
    }

    /// <summary>Files currently shown (may be filtered by <see cref="SearchKeyword"/>).</summary>
    [ObservableProperty]
    private ObservableCollection<FileItemDto> _files = new();

    /// <summary>Total files in the backend queue (unfiltered count).</summary>
    [ObservableProperty]
    private int _fileCount;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private bool _hasFiles;

    /// <summary>Live search keyword (empty = show all).</summary>
    [ObservableProperty]
    private string _searchKeyword = "";

    /// <summary>Recent-open records (files & folders), most recent first.</summary>
    [ObservableProperty]
    private ObservableCollection<RecentRecordDto> _recent = new();

    // ── Load ─────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task LoadFilesAsync()
    {
        IsLoading = true;
        try
        {
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Add files ────────────────────────────────────────────────────

    [RelayCommand]
    public async Task AddFilesAsync(IEnumerable<string> paths)
    {
        IsLoading = true;
        try
        {
            var resp = await _api.AddFilesAsync(paths);
            if (resp?.Success == true)
            {
                await ReloadAsync();
                var count = resp.Data?.Count ?? 0;
                StatusMessage = count > 0
                    ? $"已添加 {count} 个文件"
                    : "未添加新文件（可能已存在或格式不支持）";
            }
            else
            {
                StatusMessage = $"添加失败: {resp?.Message ?? "未知错误"}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"添加失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Add folder ───────────────────────────────────────────────────

    [RelayCommand]
    public async Task AddFolderAsync(string folder)
    {
        IsLoading = true;
        try
        {
            var resp = await _api.AddFolderAsync(folder);
            if (resp?.Success == true)
            {
                await ReloadAsync();
                var count = resp.Data?.Count ?? 0;
                StatusMessage = count > 0
                    ? $"从文件夹添加了 {count} 个文件"
                    : "文件夹中未找到新文件";
            }
            else
            {
                StatusMessage = $"添加失败: {resp?.Message ?? "未知错误"}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"添加失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Remove selected ──────────────────────────────────────────────

    [RelayCommand]
    public async Task RemoveSelectedAsync(IEnumerable<string> selectedPaths)
    {
        var paths = selectedPaths.ToList();
        if (paths.Count == 0) return;

        IsLoading = true;
        try
        {
            var resp = await _api.RemoveFilesAsync(paths);
            if (resp?.Success == true)
            {
                await ReloadAsync();
                StatusMessage = $"已移除 {paths.Count} 个文件";
            }
            else
            {
                StatusMessage = $"移除失败: {resp?.Message ?? "未知错误"}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"移除失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Clear all ────────────────────────────────────────────────────

    [RelayCommand]
    public async Task ClearAllAsync()
    {
        IsLoading = true;
        try
        {
            var ok = await _api.ClearFilesAsync();
            if (ok)
            {
                Files.Clear();
                FileCount = 0;
                HasFiles = false;
                SearchKeyword = "";
                StatusMessage = "已清空所有文件";
            }
            else
            {
                StatusMessage = "清空失败";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"清空失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Search ───────────────────────────────────────────────────────

    /// <summary>
    /// Filter the file list by <see cref="SearchKeyword"/> via the backend
    /// (case-insensitive name/path match). Empty keyword returns all files.
    /// </summary>
    [RelayCommand]
    public async Task SearchAsync()
    {
        try
        {
            var resp = await _api.SearchFilesAsync(SearchKeyword ?? "");
            if (resp?.Success == true && resp.Data is not null)
            {
                Files = new ObservableCollection<FileItemDto>(resp.Data.Files);
                // Note: FileCount reflects the total queue, not the filtered view;
                // it is refreshed on the next full reload.
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"搜索失败: {ex.Message}";
        }
    }

    // ── Recent ───────────────────────────────────────────────────────

    [RelayCommand]
    public async Task LoadRecentAsync()
    {
        try
        {
            var resp = await _api.GetRecentFilesAsync();
            if (resp?.Success == true && resp.Data is not null)
                Recent = new ObservableCollection<RecentRecordDto>(resp.Data.Recent);
        }
        catch
        {
            // Non-critical — leave Recent as-is
        }
    }

    // ── Internal helper ──────────────────────────────────────────────

    /// <summary>Reload the full file list from backend and refresh counts.</summary>
    private async Task ReloadAsync()
    {
        var resp = await _api.GetFilesAsync();
        if (resp?.Success == true && resp.Data is not null)
        {
            Files = new ObservableCollection<FileItemDto>(resp.Data.Files);
            FileCount = Files.Count;
            HasFiles = FileCount > 0;
        }
    }
}
