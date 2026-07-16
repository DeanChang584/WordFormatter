using Microsoft.UI.Xaml;

namespace WordFormatterUI.Services;

/// <summary>
/// Local theme management — simplified to always use Light theme.
/// Theme switching was removed to keep a consistent light appearance.
/// </summary>
public static class ThemeService
{
    public static string CurrentMode => "light";

    public static void Apply(string _)
    {
        try
        {
            if (App.MainWindow is { Content: FrameworkElement root })
            {
                root.RequestedTheme = ElementTheme.Light;
            }
        }
        catch
        {
            // COM exceptions may occur during shutdown or rapid startup;
            // safe to ignore.
        }
    }
}