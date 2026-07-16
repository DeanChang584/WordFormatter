using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WordFormatterUI.Models.Profile;

namespace WordFormatterUI.ViewModels;

/// <summary>
/// Per-level heading style ViewModel (design-document §9.4).
/// Reads/writes heading level 1-6 from/to the shared <see cref="ProfileConfigDto"/>.
/// </summary>
public partial class HeadingStyleViewModel : ObservableObject
{
    private ProfileConfigDto? _sharedProfile = new();
    private bool _isLoading;

    [ObservableProperty] private int _currentHeadingLevel = 1;
    [ObservableProperty] private string _headingFontCn = "黑体";
    [ObservableProperty] private string _headingFontEn = "Times New Roman";
    [ObservableProperty] private double _headingFontSize = 22.0;
    [ObservableProperty] private string _headingFontStyle = "bold";
    [ObservableProperty] private string _headingAlignment = "left";
    [ObservableProperty] private double _headingLineSpacing = 1.5;
    [ObservableProperty] private string _headingLineSpacingMode = "multiple";
    [ObservableProperty] private string _headingLineSpacingUnit = "pt";
    [ObservableProperty] private double _headingSpaceBefore = 0.0;
    [ObservableProperty] private double _headingSpaceAfter = 0.0;
    [ObservableProperty] private string _headingSpaceBeforeUnit = "行";
    [ObservableProperty] private string _headingSpaceAfterUnit = "行";
    [ObservableProperty] private string _headingIndentType = "none";
    [ObservableProperty] private double _headingIndentValue = 0.0;
    [ObservableProperty] private string _headingIndentUnit = "字符";

    // ── DTO sync ──────────────────────────────────────────────────────

    public void SetSharedProfile(ProfileConfigDto profile)
    {
        _sharedProfile = profile;
    }

    /// <summary>Set reference AND reload values from DTO (use only on init/template switch).</summary>
    public void LoadSharedProfile(ProfileConfigDto profile)
    {
        _sharedProfile = profile;
        LoadFromSharedProfile();
    }

    public void LoadFromSharedProfile()
    {
        if (_sharedProfile is null) return;
        _isLoading = true;

        var key = CurrentHeadingLevel.ToString();
        if (_sharedProfile.Heading.TryGetValue(key, out var h))
        {
            HeadingFontCn = h.FontCn; HeadingFontEn = h.FontEn;
            HeadingFontSize = h.FontSize; HeadingFontStyle = h.FontStyle;
            HeadingAlignment = h.Alignment;
            HeadingLineSpacing = h.LineSpacing;
            HeadingLineSpacingMode = h.LineSpacingMode;
            HeadingLineSpacingUnit = h.LineSpacingUnit;
            HeadingSpaceBefore = h.SpaceBefore; HeadingSpaceAfter = h.SpaceAfter;
            HeadingSpaceBeforeUnit = h.SpaceBeforeUnit; HeadingSpaceAfterUnit = h.SpaceAfterUnit;
            HeadingIndentType = h.IndentType; HeadingIndentValue = h.IndentValue; HeadingIndentUnit = h.IndentUnit;
        }
        _isLoading = false;
    }

    private void WriteToSharedProfile()
    {
        if (_sharedProfile is null || _isLoading) return;

        var key = CurrentHeadingLevel.ToString();
        if (!_sharedProfile.Heading.TryGetValue(key, out var h))
        {
            h = new HeadingStyleConfigDto { Level = CurrentHeadingLevel };
            _sharedProfile.Heading[key] = h;
        }
        h.FontCn = HeadingFontCn; h.FontEn = HeadingFontEn;
        h.FontSize = HeadingFontSize; h.FontStyle = HeadingFontStyle;
        h.Alignment = HeadingAlignment;
        h.LineSpacing = HeadingLineSpacing;
        h.LineSpacingMode = HeadingLineSpacingMode;
        h.LineSpacingUnit = HeadingLineSpacingUnit;
        h.SpaceBefore = HeadingSpaceBefore; h.SpaceAfter = HeadingSpaceAfter;
        h.SpaceBeforeUnit = HeadingSpaceBeforeUnit; h.SpaceAfterUnit = HeadingSpaceAfterUnit;
        h.IndentType = HeadingIndentType; h.IndentValue = HeadingIndentValue; h.IndentUnit = HeadingIndentUnit;
    }

    // ── Property change handlers ─────────────────────────────────────

    partial void OnCurrentHeadingLevelChanged(int value)
    {
        // NOTE: Individual property changes already call WriteToSharedProfile()
        // immediately, so the current level's data is already persisted.
        // We only need to load the new level's data here.
        // DO NOT call SaveCurrentLevel() — it would corrupt the target level
        // because CurrentHeadingLevel has already changed to the new value.
        LoadFromSharedProfile();
    }

    partial void OnHeadingFontCnChanged(string value)     { IsDirty = true; WriteToSharedProfile(); }
    partial void OnHeadingFontEnChanged(string value)     { IsDirty = true; WriteToSharedProfile(); }
    partial void OnHeadingFontSizeChanged(double value)   { IsDirty = true; WriteToSharedProfile(); }
    partial void OnHeadingFontStyleChanged(string value)  { IsDirty = true; WriteToSharedProfile(); }
    partial void OnHeadingAlignmentChanged(string value)  { IsDirty = true; WriteToSharedProfile(); }
    partial void OnHeadingLineSpacingChanged(double value){ IsDirty = true; WriteToSharedProfile(); }
    partial void OnHeadingLineSpacingModeChanged(string value){ IsDirty = true; WriteToSharedProfile(); }
    partial void OnHeadingLineSpacingUnitChanged(string value){ IsDirty = true; WriteToSharedProfile(); }
    partial void OnHeadingSpaceBeforeChanged(double value){ IsDirty = true; WriteToSharedProfile(); }
    partial void OnHeadingSpaceAfterChanged(double value) { IsDirty = true; WriteToSharedProfile(); }
    partial void OnHeadingSpaceBeforeUnitChanged(string value){ IsDirty = true; WriteToSharedProfile(); }
    partial void OnHeadingSpaceAfterUnitChanged(string value) { IsDirty = true; WriteToSharedProfile(); }
    partial void OnHeadingIndentTypeChanged(string value)
    {
        IsDirty = true;
        // When switching to firstLine or hanging indent, set default value to 2 if currently 0
        if ((value == "firstLine" || value == "hanging") && HeadingIndentValue == 0.0)
        {
            HeadingIndentValue = 2.0;
        }
        WriteToSharedProfile();
    }
    partial void OnHeadingIndentValueChanged(double value)   { IsDirty = true; WriteToSharedProfile(); }
    partial void OnHeadingIndentUnitChanged(string value)    { IsDirty = true; WriteToSharedProfile(); }

    [ObservableProperty] private bool _isDirty;

    /// <summary>
    /// Reset the current heading level to its factory defaults.
    /// </summary>
    [RelayCommand]
    private void Reset()
    {
        // Reset to defaults for each level based on current selection
        switch (CurrentHeadingLevel)
        {
            case 1:
                HeadingFontCn = "黑体"; HeadingFontEn = "Times New Roman"; HeadingFontSize = 22.0;
                HeadingFontStyle = "bold"; HeadingAlignment = "left";
                HeadingLineSpacing = 1.5; HeadingLineSpacingMode = "multiple"; HeadingLineSpacingUnit = "pt";
                HeadingSpaceBefore = 1.0; HeadingSpaceAfter = 1.0; HeadingSpaceBeforeUnit = "行"; HeadingSpaceAfterUnit = "行";
                HeadingIndentType = "none"; HeadingIndentValue = 0.0; HeadingIndentUnit = "字符";
                break;
            case 2:
                HeadingFontCn = "黑体"; HeadingFontEn = "Times New Roman"; HeadingFontSize = 18.0;
                HeadingFontStyle = "bold"; HeadingAlignment = "left";
                HeadingLineSpacing = 1.5; HeadingLineSpacingMode = "multiple"; HeadingLineSpacingUnit = "pt";
                HeadingSpaceBefore = 1.0; HeadingSpaceAfter = 1.0; HeadingSpaceBeforeUnit = "行"; HeadingSpaceAfterUnit = "行";
                HeadingIndentType = "none"; HeadingIndentValue = 0.0; HeadingIndentUnit = "字符";
                break;
            case 3:
                HeadingFontCn = "黑体"; HeadingFontEn = "Times New Roman"; HeadingFontSize = 16.0;
                HeadingFontStyle = "bold"; HeadingAlignment = "left";
                HeadingLineSpacing = 1.5; HeadingLineSpacingMode = "multiple"; HeadingLineSpacingUnit = "pt";
                HeadingSpaceBefore = 0.5; HeadingSpaceAfter = 0.5; HeadingSpaceBeforeUnit = "行"; HeadingSpaceAfterUnit = "行";
                HeadingIndentType = "none"; HeadingIndentValue = 0.0; HeadingIndentUnit = "字符";
                break;
            case 4:
                HeadingFontCn = "黑体"; HeadingFontEn = "Times New Roman"; HeadingFontSize = 14.0;
                HeadingFontStyle = "bold"; HeadingAlignment = "left";
                HeadingLineSpacing = 1.5; HeadingLineSpacingMode = "multiple"; HeadingLineSpacingUnit = "pt";
                HeadingSpaceBefore = 0.5; HeadingSpaceAfter = 0.5; HeadingSpaceBeforeUnit = "行"; HeadingSpaceAfterUnit = "行";
                HeadingIndentType = "none"; HeadingIndentValue = 0.0; HeadingIndentUnit = "字符";
                break;
            case 5:
                HeadingFontCn = "黑体"; HeadingFontEn = "Times New Roman"; HeadingFontSize = 12.0;
                HeadingFontStyle = "bold"; HeadingAlignment = "left";
                HeadingLineSpacing = 1.5; HeadingLineSpacingMode = "multiple"; HeadingLineSpacingUnit = "pt";
                HeadingSpaceBefore = 0.25; HeadingSpaceAfter = 0.25; HeadingSpaceBeforeUnit = "行"; HeadingSpaceAfterUnit = "行";
                HeadingIndentType = "none"; HeadingIndentValue = 0.0; HeadingIndentUnit = "字符";
                break;
            case 6:
                HeadingFontCn = "黑体"; HeadingFontEn = "Times New Roman"; HeadingFontSize = 10.5;
                HeadingFontStyle = "bold"; HeadingAlignment = "left";
                HeadingLineSpacing = 1.5; HeadingLineSpacingMode = "multiple"; HeadingLineSpacingUnit = "pt";
                HeadingSpaceBefore = 0.25; HeadingSpaceAfter = 0.25; HeadingSpaceBeforeUnit = "行"; HeadingSpaceAfterUnit = "行";
                HeadingIndentType = "none"; HeadingIndentValue = 0.0; HeadingIndentUnit = "字符";
                break;
        }
        IsDirty = false;
        WriteToSharedProfile();
    }
}