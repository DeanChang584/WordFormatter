using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WordFormatterUI.Services;
using WordFormatterUI.ViewModels;

namespace WordFormatterUI.Pages;

public sealed partial class FilesPage : Page
{
    public FilesViewModel Vm { get; }

    public FilesPage()
    {
        InitializeComponent();
        Vm = App.Api is not null ? new FilesViewModel(App.Api) : throw new InvalidOperationException("ApiService not initialized");
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await Vm.LoadFilesCommand.ExecuteAsync(null);
        UpdateButtonStates();
    }

    private async void AddFiles_Click(object sender, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        picker.FileTypeFilter.Add(".docx");
        picker.FileTypeFilter.Add(".doc");

        var files = await picker.PickMultipleFilesAsync();
        if (files.Count > 0)
        {
            var paths = files.Select(f => f.Path).ToList();
            await Vm.AddFilesCommand.ExecuteAsync(paths);
        }
        UpdateButtonStates();
        ShowStatus(Vm.StatusMessage);
    }

    private async void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FolderPicker();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        picker.FileTypeFilter.Add("*");

        var folder = await picker.PickSingleFolderAsync();
        if (folder is not null)
        {
            await Vm.AddFolderCommand.ExecuteAsync(folder.Path);
        }
        UpdateButtonStates();
        ShowStatus(Vm.StatusMessage);
    }

    private async void RemoveSelected_Click(object sender, RoutedEventArgs e)
    {
        var selected = FileListView.SelectedItems.Cast<string>().ToList();
        if (selected.Count > 0)
        {
            await Vm.RemoveSelectedCommand.ExecuteAsync(selected);
        }
        UpdateButtonStates();
        ShowStatus(Vm.StatusMessage);
    }

    private async void ClearAll_Click(object sender, RoutedEventArgs e)
    {
        await Vm.ClearAllCommand.ExecuteAsync(null);
        UpdateButtonStates();
        ShowStatus(Vm.StatusMessage);
    }

    private void FileListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RemoveBtn.IsEnabled = FileListView.SelectedItems.Count > 0;
    }

    private void UpdateButtonStates()
    {
        ClearBtn.IsEnabled = Vm.HasFiles;
        RemoveBtn.IsEnabled = FileListView.SelectedItems.Count > 0;

        // Refresh the ListView source
        FileListView.ItemsSource = null;
        FileListView.ItemsSource = Vm.Files;
    }

    private void ShowStatus(string message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            StatusInfoBar.Message = message;
            StatusInfoBar.IsOpen = true;
        }
    }
}