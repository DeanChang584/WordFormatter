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
        // Load current theme
        try
        {
            var theme = await App.Api.GetThemeAsync();
            if (theme is not null)
            {
                var targetTag = theme.Mode;
                foreach (var child in ThemeRadio.Items)
                {
                    if (child is RadioButton rb && rb.Tag?.ToString() == targetTag)
                    {
                        rb.IsChecked = true;
                        break;
                    }
                }
            }
        }
        catch { }

        // Check backend status
        await RefreshStatusAsync();
    }

    private async void ThemeRadio_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeRadio.SelectedItem is RadioButton rb && rb.Tag is string mode)
        {
            await App.Api.SetThemeAsync(mode);

            // Apply theme locally
            var root = App.MainWindow.Content as FrameworkElement;
            if (root is not null)
            {
                root.RequestedTheme = mode switch
                {
                    "light" => ElementTheme.Light,
                    "dark" => ElementTheme.Dark,
                    _ => ElementTheme.Default,
                };
            }
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