using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WordFormatterUI.Services;

namespace WordFormatterUI.ViewModels;

public partial class FormatViewModel : ObservableObject
{
    private readonly ApiService _api;

    public FormatViewModel(ApiService api)
    {
        _api = api;
    }

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private int _progressCurrent;

    [ObservableProperty]
    private int _progressTotal;

    [ObservableProperty]
    private double _progressPercent;

    [ObservableProperty]
    private string _taskId = "";

    [ObservableProperty]
    private ObservableCollection<FormatResultItem> _results = new();

    [ObservableProperty]
    private int _okCount;

    [ObservableProperty]
    private int _failCount;

    [ObservableProperty]
    private bool _hasResults;

    [ObservableProperty]
    private string _outputDir = "";

    private CancellationTokenSource? _pollCts;

    [RelayCommand]
    public async Task StartFormatAsync(IEnumerable<string> files)
    {
        if (!files.Any())
        {
            StatusMessage = "请先添加文件";
            return;
        }

        IsLoading = true;
        IsRunning = true;
        StatusMessage = "正在启动排版任务…";
        Results.Clear();
        HasResults = false;

        try
        {
            var req = new FormatStartRequest
            {
                Files = files.ToList(),
                OutputDir = OutputDir,
            };

            var resp = await _api.StartFormatAsync(req);
            if (resp is null || string.IsNullOrEmpty(resp.TaskId))
            {
                StatusMessage = "启动失败";
                IsRunning = false;
                IsLoading = false;
                return;
            }

            TaskId = resp.TaskId;
            StatusMessage = $"任务 {TaskId} 已启动";

            // Poll for progress
            _pollCts?.Cancel();
            _pollCts = new CancellationTokenSource();
            await PollProgressAsync(_pollCts.Token);
        }
        catch (Exception ex)
        {
            StatusMessage = $"启动失败: {ex.Message}";
            IsRunning = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task PollProgressAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(500, ct);

                var progress = await _api.GetFormatProgressAsync(TaskId);
                if (progress is null) continue;

                ProgressCurrent = progress.Current;
                ProgressTotal = progress.Total;
                ProgressPercent = progress.Total > 0
                    ? (double)progress.Current / progress.Total * 100
                    : 0;

                StatusMessage = $"排版中… {progress.Current}/{progress.Total}";

                if (progress.Status == "finished" || progress.Status == "error")
                {
                    await LoadResultAsync();
                    IsRunning = false;
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Retry on transient errors
            }
        }
    }

    private async Task LoadResultAsync()
    {
        try
        {
            var result = await _api.GetFormatResultAsync(TaskId);
            if (result is not null)
            {
                Results = new ObservableCollection<FormatResultItem>(result.Results);
                OkCount = result.OkCount;
                FailCount = result.FailCount;
                HasResults = true;

                StatusMessage = FailCount == 0
                    ? $"全部完成 ✓  共 {OkCount} 个文件"
                    : $"完成: {OkCount} 成功，{FailCount} 失败";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"获取结果失败: {ex.Message}";
        }
    }

    [RelayCommand]
    public void CancelFormat()
    {
        _pollCts?.Cancel();
        IsRunning = false;
        StatusMessage = "已取消轮询（后端任务仍在运行）";
    }
}