using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WordFormatterUI.Services;

namespace WordFormatterUI.ViewModels;

public partial class FilesViewModel : ObservableObject
{
    private readonly ApiService _api;

    public FilesViewModel(ApiService api)
    {
        _api = api;
    }

    [ObservableProperty]
    private ObservableCollection<string> _files = new();

    [ObservableProperty]
    private int _fileCount;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private bool _hasFiles;

    [RelayCommand]
    public async Task LoadFilesAsync()
    {
        IsLoading = true;
        try
        {
            var resp = await _api.GetFilesAsync();
            if (resp is not null)
            {
                Files = new ObservableCollection<string>(resp.Files);
                FileCount = resp.Count;
                HasFiles = resp.Count > 0;
            }
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

    [RelayCommand]
    public async Task AddFilesAsync(IEnumerable<string> paths)
    {
        IsLoading = true;
        try
        {
            var resp = await _api.SelectFilesAsync(paths);
            if (resp is not null)
            {
                Files = new ObservableCollection<string>(resp.Files);
                FileCount = resp.Count;
                HasFiles = resp.Count > 0;
                StatusMessage = resp.Added.Count > 0
                    ? $"已添加 {resp.Added.Count} 个文件"
                    : "未添加新文件（可能已存在或格式不支持）";
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

    [RelayCommand]
    public async Task AddFolderAsync(string folder)
    {
        IsLoading = true;
        try
        {
            var resp = await _api.SelectFolderAsync(folder);
            if (resp is not null)
            {
                Files = new ObservableCollection<string>(resp.Files);
                FileCount = resp.Count;
                HasFiles = resp.Count > 0;
                StatusMessage = resp.Added.Count > 0
                    ? $"从文件夹添加了 {resp.Added.Count} 个文件"
                    : "文件夹中未找到新文件";
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

    [RelayCommand]
    public async Task RemoveSelectedAsync(IEnumerable<string> selectedPaths)
    {
        if (!selectedPaths.Any()) return;

        IsLoading = true;
        try
        {
            var resp = await _api.RemoveFilesAsync(selectedPaths);
            if (resp is not null)
            {
                Files = new ObservableCollection<string>(resp.Files);
                FileCount = resp.Count;
                HasFiles = resp.Count > 0;
                StatusMessage = $"已移除 {selectedPaths.Count()} 个文件";
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
                StatusMessage = "已清空所有文件";
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
}