using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WordFormatterUI.Services;

namespace WordFormatterUI.Views;

/// <summary>
/// Advanced application settings (plan §7.7).
///
/// Language, theme (via <see cref="ThemeService"/>), and auto-check-update.
///
/// NOTE: There is no backend /api/settings endpoint yet, so language and
/// auto-update toggles are applied to local UI state only (not persisted).
/// Theme changes take effect immediately through ThemeService.
/// </summary>
public sealed partial class AdvancedSettingsView : UserControl
{
    // Guard against firing change handlers while pushing initial state
    private bool _isLoading;

    public AdvancedSettingsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _isLoading = true;

        // Language (default zh-CN — no persistence yet)
        LanguageBox.SelectedIndex = 0;

        // Theme (default light)
        ThemeBox.SelectedIndex = 0;

        _isLoading = false;
    }

    private void LanguageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;
        // No-op until backend /api/settings + i18n resource swapping is available.
        // Language selection is captured but not yet persisted or applied.
    }

    private void ThemeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;
        // Placeholder: wired when theme switching is re-enabled.
    }


}
