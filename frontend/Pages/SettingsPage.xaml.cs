using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WordFormatterUI.Services;

namespace WordFormatterUI.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        // Load current theme from local cache
        var currentTheme = ThemeService.CurrentMode;
        foreach (var child in ThemeRadio.Items)
        {
            if (child is RadioButton rb && rb.Tag?.ToString() == currentTheme)
            {
                rb.IsChecked = true;
                break;
            }
        }

        // Check backend status
        await RefreshStatusAsync();
    }

    private void ThemeRadio_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeRadio.SelectedItem is RadioButton rb && rb.Tag is string mode)
        {
            ThemeService.Apply(mode);
        }
    }

    private async void RefreshStatus_Click(object sender, RoutedEventArgs e)
    {
        await RefreshStatusAsync();
    }

    private async Task RefreshStatusAsync()
    {
        StatusText.Text = "检测中…";
        StatusText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];

        var ok = await App.Api.HealthAsync();
        if (ok)
        {
            StatusText.Text = "✓ 已连接";
            StatusText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorSuccessBrush"];
        }
        else
        {
            StatusText.Text = "✗ 无法连接";
            StatusText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
        }
    }
}