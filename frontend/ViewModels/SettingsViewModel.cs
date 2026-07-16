using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WordFormatterUI.ViewModels;

/// <summary>
/// Global settings ViewModel (design-document §17).
/// Manages language, theme, and auto-update preferences.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty] private string _language = "zh-CN";
    [ObservableProperty] private string _theme = "light";
    [ObservableProperty] private bool _autoCheckUpdate = true;

    partial void OnLanguageChanged(string value) => IsDirty = true;
    partial void OnThemeChanged(string value) => IsDirty = true;
    partial void OnAutoCheckUpdateChanged(bool value) => IsDirty = true;

    [ObservableProperty] private bool _isDirty;

    [RelayCommand]
    private async Task SaveAsync()
    {
        // Placeholder — wired when backend /api/settings exists
        IsDirty = false;
        await Task.CompletedTask;
    }
}
