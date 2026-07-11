using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WordFormatterUI.Services;
using WordFormatterUI.ViewModels;

namespace WordFormatterUI.Pages;

public sealed partial class FormatPage : Page
{
    public FormatViewModel Vm { get; }

    public FormatPage()
    {
        InitializeComponent();
        Vm = App.Api is not null ? new FormatViewModel(App.Api) : throw new InvalidOperationException("ApiService not initialized");
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        Vm.PropertyChanged += Vm_PropertyChanged;
        StatusText.Text = "就绪";
    }

    private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            switch (e.PropertyName)
            {
                case nameof(Vm.StatusMessage):
                    StatusText.Text = Vm.StatusMessage;
                    break;
                case nameof(Vm.IsRunning):
                    StartBtn.IsEnabled = !Vm.IsRunning;
                    CancelBtn.IsEnabled = Vm.IsRunning;
                    ProgressBar.Visibility = Vm.IsRunning ? Visibility.Visible : Visibility.Collapsed;
                    ProgressText.Visibility = Vm.IsRunning ? Visibility.Visible : Visibility.Collapsed;
                    break;
                case nameof(Vm.ProgressPercent):
                    ProgressBar.Value = Vm.ProgressPercent;
                    ProgressText.Text = $"{Vm.ProgressCurrent}/{Vm.ProgressTotal}";
                    break;
                case nameof(Vm.HasResults):
                    if (Vm.HasResults)
                    {
                        ResultsList.Visibility = Visibility.Visible;
                        SummaryPanel.Visibility = Visibility.Visible;
                        ResultsList.ItemsSource = Vm.Results.Select(r =>
                            $"{(r.Success ? "✓" : "✗")} {r.FilePath}  {(r.Success ? "" : r.Message)}").ToList();
                        SummaryText.Text = $"成功: {Vm.OkCount}  失败: {Vm.FailCount}";
                    }
                    break;
            }
        });
    }

    private async void StartFormat_Click(object sender, RoutedEventArgs e)
    {
        var api = App.Api!;
        var filesResp = await api.GetFilesAsync();
        var files = filesResp?.Files ?? new List<string>();

        if (files.Count == 0)
        {
            StatusText.Text = "请先在「文件管理」页面添加文件";
            return;
        }

        // Reset results
        ResultsList.Visibility = Visibility.Collapsed;
        SummaryPanel.Visibility = Visibility.Collapsed;
        ProgressBar.Visibility = Visibility.Visible;
        ProgressText.Visibility = Visibility.Visible;
        ProgressBar.Value = 0;

        await Vm.StartFormatCommand.ExecuteAsync(files);
    }

    private void CancelFormat_Click(object sender, RoutedEventArgs e)
    {
        Vm.CancelFormatCommand.Execute(null);
    }

    private async void PickOutputDir_Click(object sender, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FolderPicker();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        picker.FileTypeFilter.Add("*");
        var folder = await picker.PickSingleFolderAsync();
        if (folder is not null)
        {
            Vm.OutputDir = folder.Path;
            StatusText.Text = $"输出目录: {folder.Path}";
        }
    }
}