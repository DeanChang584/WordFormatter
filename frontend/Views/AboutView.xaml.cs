using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WordFormatterUI.Views;

/// <summary>
/// About page — software name, version, GitHub link, copyright (plan §7.7).
///
/// The version string is centralised here so it can later be sourced from
/// assembly metadata or the backend /api/health version field.
/// </summary>
public sealed partial class AboutView : UserControl
{
    /// <summary>Displayed application version (matches shared/version.py VERSION).</summary>
    public const string Version = "2.0";

    public AboutView()
    {
        InitializeComponent();
        VersionText.Text = $"版本 v{Version}";
    }

    private void EmailLink_Click(object sender, RoutedEventArgs e)
    {
        var mailto = "mailto:zhangyx584@163.com";
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = mailto,
            UseShellExecute = true,
        });
    }
}
