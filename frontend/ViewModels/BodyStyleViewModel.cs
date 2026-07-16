using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WordFormatterUI.Models.Profile;

namespace WordFormatterUI.ViewModels;

/// <summary>
/// Body / paragraph style ViewModel (design-document §9.3).
/// Reads/writes to the shared <see cref="ProfileConfigDto"/> owned by MainViewModel.
/// </summary>
public partial class BodyStyleViewModel : ObservableObject
{
    private ProfileConfigDto? _sharedProfile = new();
    private bool _isLoading;

    [ObservableProperty] private string _bodyFontCn = "宋体";
    [ObservableProperty] private string _bodyFontEn = "Times New Roman";
    [ObservableProperty] private double _bodyFontSize = 12.0;
    [ObservableProperty] private string _bodyFontStyle = "normal";
    [ObservableProperty] private string _bodyAlignment = "justify";
    [ObservableProperty] private double _lineSpacing = 1.5;
    [ObservableProperty] private string _lineSpacingMode = "multiple";
    [ObservableProperty] private string _lineSpacingUnit = "pt";
    [ObservableProperty] private string _indentType = "firstLine";
    [ObservableProperty] private double _indentValue = 2.0;
    [ObservableProperty] private string _indentUnit = "字符";
    [ObservableProperty] private double _spaceBefore;
    [ObservableProperty] private double _spaceAfter;
    [ObservableProperty] private string _spaceBeforeUnit = "行";
    [ObservableProperty] private string _spaceAfterUnit = "行";

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
        var b = _sharedProfile.Body;
        BodyFontCn = b.FontCn; BodyFontEn = b.FontEn;
        BodyFontSize = b.FontSize; BodyFontStyle = b.FontStyle;
        BodyAlignment = b.Alignment;
        LineSpacing = b.LineSpacing; LineSpacingMode = b.LineSpacingMode;
        LineSpacingUnit = b.LineSpacingUnit;
        IndentType = b.IndentType; IndentValue = b.IndentValue; IndentUnit = b.IndentUnit;
        SpaceBefore = b.SpaceBefore; SpaceAfter = b.SpaceAfter;
        SpaceBeforeUnit = b.SpaceBeforeUnit; SpaceAfterUnit = b.SpaceAfterUnit;
        _isLoading = false;
    }

    private void WriteToSharedProfile()
    {
        if (_sharedProfile is null || _isLoading) return;
        var b = _sharedProfile.Body;
        b.FontCn = BodyFontCn; b.FontEn = BodyFontEn;
        b.FontSize = BodyFontSize; b.FontStyle = BodyFontStyle;
        b.Alignment = BodyAlignment;
        b.LineSpacing = LineSpacing; b.LineSpacingMode = LineSpacingMode;
        b.LineSpacingUnit = LineSpacingUnit;
        b.IndentType = IndentType; b.IndentValue = IndentValue; b.IndentUnit = IndentUnit;
        b.SpaceBefore = SpaceBefore; b.SpaceAfter = SpaceAfter;
        b.SpaceBeforeUnit = SpaceBeforeUnit; b.SpaceAfterUnit = SpaceAfterUnit;
    }

    // ── Property change handlers ─────────────────────────────────────

    partial void OnBodyFontCnChanged(string value)       { IsDirty = true; WriteToSharedProfile(); }
    partial void OnBodyFontEnChanged(string value)       { IsDirty = true; WriteToSharedProfile(); }
    partial void OnBodyFontSizeChanged(double value)     { IsDirty = true; WriteToSharedProfile(); }
    partial void OnBodyFontStyleChanged(string value)    { IsDirty = true; WriteToSharedProfile(); }
    partial void OnBodyAlignmentChanged(string value)    { IsDirty = true; WriteToSharedProfile(); }
    partial void OnLineSpacingChanged(double value)      { IsDirty = true; WriteToSharedProfile(); }
    partial void OnLineSpacingModeChanged(string value)  { IsDirty = true; WriteToSharedProfile(); }
    partial void OnLineSpacingUnitChanged(string value)  { IsDirty = true; WriteToSharedProfile(); }
    partial void OnIndentTypeChanged(string value)       { IsDirty = true; WriteToSharedProfile(); }
    partial void OnIndentValueChanged(double value)      { IsDirty = true; WriteToSharedProfile(); }
    partial void OnIndentUnitChanged(string value)       { IsDirty = true; WriteToSharedProfile(); }
    partial void OnSpaceBeforeChanged(double value)      { IsDirty = true; WriteToSharedProfile(); }
    partial void OnSpaceAfterChanged(double value)       { IsDirty = true; WriteToSharedProfile(); }
    partial void OnSpaceBeforeUnitChanged(string value)  { IsDirty = true; WriteToSharedProfile(); }
    partial void OnSpaceAfterUnitChanged(string value)   { IsDirty = true; WriteToSharedProfile(); }

    [ObservableProperty] private bool _isDirty;

    [RelayCommand]
    private void Reset()
    {
        BodyFontCn = "宋体"; BodyFontEn = "Times New Roman"; BodyFontSize = 12.0;
        BodyFontStyle = "normal"; BodyAlignment = "justify";
        LineSpacing = 1.5; LineSpacingMode = "multiple"; LineSpacingUnit = "pt";
        IndentType = "firstLine"; IndentValue = 2.0; IndentUnit = "字符";
        SpaceBefore = 0; SpaceAfter = 0; SpaceBeforeUnit = "行"; SpaceAfterUnit = "行";
        IsDirty = false;
        WriteToSharedProfile();
    }
}
