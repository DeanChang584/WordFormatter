using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WordFormatterUI.Models.Profile;

namespace WordFormatterUI.ViewModels;

/// <summary>
/// Page layout settings ViewModel (design-document §9.1).
/// Reads/writes to the shared <see cref="ProfileConfigDto"/> owned by MainViewModel.
/// </summary>
public partial class PageSettingsViewModel : ObservableObject
{
    private ProfileConfigDto? _sharedProfile = new();
    private bool _isLoading;

    [ObservableProperty] private string _paperSize = "A4";
    [ObservableProperty] private string _orientation = "portrait";
    [ObservableProperty] private double _marginTop = 25.4;
    [ObservableProperty] private double _marginBottom = 25.4;
    [ObservableProperty] private double _marginLeft = 31.7;
    [ObservableProperty] private double _marginRight = 31.7;
    [ObservableProperty] private bool _pageNumber = true;

    // ── 文档网格 ──────────────────────────────────────────────────────

    /// <summary>
    /// 文档网格模式选项（ComboBox 绑定）
    /// </summary>
    public List<string> GridModeOptions { get; } = new()
    {
        "无网格",
        "只指定行网格",
        "指定行和字符网格",
    };

    [ObservableProperty] private string _documentGridMode = "无网格";

    [ObservableProperty] private int _linesPerPage = 30;

    [ObservableProperty] private int _charsPerLine = 35;

    [ObservableProperty] private bool _adjustRightIndent = true;

    [ObservableProperty] private bool _alignToGrid = true;

    /// <summary>
    /// 是否显示网格相关设置（mode != "无网格"）
    /// </summary>
    public bool ShowGridSettings => DocumentGridMode != "无网格";

    /// <summary>
    /// 是否显示每行字符设置（mode == "指定行和字符网格"）
    /// </summary>
    public bool ShowCharSettings => DocumentGridMode == "指定行和字符网格";

    // 模式值映射（中文 → 英文）
    private static string MapGridModeToValue(string chinese) => chinese switch
    {
        "无网格" => "none",
        "只指定行网格" => "lines",
        "指定行和字符网格" => "both",
        _ => "none",
    };

    private static string MapGridModeToDisplay(string value) => value switch
    {
        "none" => "无网格",
        "lines" => "只指定行网格",
        "both" => "指定行和字符网格",
        _ => "无网格",
    };

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
        var p = _sharedProfile.Page;
        PaperSize = p.PaperSize;
        Orientation = p.Orientation;
        MarginTop = p.MarginTop;
        MarginBottom = p.MarginBottom;
        MarginLeft = p.MarginLeft;
        MarginRight = p.MarginRight;
        PageNumber = p.PageNumber;

        // 文档网格
        var dg = p.DocumentGrid;
        DocumentGridMode = MapGridModeToDisplay(dg.Mode);
        LinesPerPage = dg.LinesPerPage;
        CharsPerLine = dg.CharsPerLine;
        AdjustRightIndent = dg.AdjustRightIndent;
        AlignToGrid = dg.AlignToGrid;

        _isLoading = false;
    }

    private void WriteToSharedProfile()
    {
        if (_sharedProfile is null || _isLoading) return;
        var p = _sharedProfile.Page;
        p.PaperSize = PaperSize;
        p.Orientation = Orientation;
        p.MarginTop = MarginTop;
        p.MarginBottom = MarginBottom;
        p.MarginLeft = MarginLeft;
        p.MarginRight = MarginRight;
        p.PageNumber = PageNumber;

        // 文档网格
        p.DocumentGrid.Mode = MapGridModeToValue(DocumentGridMode);
        p.DocumentGrid.LinesPerPage = LinesPerPage;
        p.DocumentGrid.CharsPerLine = CharsPerLine;
        p.DocumentGrid.AdjustRightIndent = AdjustRightIndent;
        p.DocumentGrid.AlignToGrid = AlignToGrid;
    }

    // ── Property change handlers ─────────────────────────────────────

    partial void OnPaperSizeChanged(string value)     { IsDirty = true; WriteToSharedProfile(); }
    partial void OnOrientationChanged(string value)   { IsDirty = true; WriteToSharedProfile(); }
    partial void OnMarginTopChanged(double value)     { IsDirty = true; WriteToSharedProfile(); }
    partial void OnMarginBottomChanged(double value)  { IsDirty = true; WriteToSharedProfile(); }
    partial void OnMarginLeftChanged(double value)    { IsDirty = true; WriteToSharedProfile(); }
    partial void OnMarginRightChanged(double value)   { IsDirty = true; WriteToSharedProfile(); }
    partial void OnPageNumberChanged(bool value)      { IsDirty = true; WriteToSharedProfile(); }

    // 文档网格
    partial void OnDocumentGridModeChanged(string value)
    {
        IsDirty = true;
        OnPropertyChanged(nameof(ShowGridSettings));
        OnPropertyChanged(nameof(ShowCharSettings));
        WriteToSharedProfile();
    }

    partial void OnLinesPerPageChanged(int value)     { IsDirty = true; WriteToSharedProfile(); }
    partial void OnCharsPerLineChanged(int value)     { IsDirty = true; WriteToSharedProfile(); }
    partial void OnAdjustRightIndentChanged(bool value) { IsDirty = true; WriteToSharedProfile(); }
    partial void OnAlignToGridChanged(bool value)    { IsDirty = true; WriteToSharedProfile(); }

    [ObservableProperty] private bool _isDirty;

    [RelayCommand]
    private void Reset()
    {
        PaperSize = "A4"; Orientation = "portrait";
        MarginTop = 25.4; MarginBottom = 25.4;
        MarginLeft = 31.7; MarginRight = 31.7;
        PageNumber = true;

        // 文档网格默认
        DocumentGridMode = "无网格";
        LinesPerPage = 30;
        CharsPerLine = 35;
        AdjustRightIndent = true;
        AlignToGrid = true;

        IsDirty = false;
        WriteToSharedProfile();
    }

    [RelayCommand]
    private async Task SaveAsTemplateAsync()
    {
        await Task.CompletedTask;
    }
}