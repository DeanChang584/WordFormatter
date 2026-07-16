using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using WordFormatterUI.Models.Profile;

namespace WordFormatterUI.ViewModels;

public partial class TableSettingsViewModel : ObservableObject
{
    private ProfileConfigDto? _sharedProfile = new(); // never null — always writeable
    private bool _isLoading;

    // ── 表格对齐与宽度 ──
    [ObservableProperty] private string _tableAlignment = "center";
    [ObservableProperty] private string _widthMode = "auto";
    [ObservableProperty] private double _widthValue;
    [ObservableProperty] private string _widthUnit = "cm";
    [ObservableProperty] private bool _autoFitColumns = true;

    // ── 表头字体 ──
    [ObservableProperty] private string _headerFontCn = "宋体";
    [ObservableProperty] private string _headerFontEn = "Times New Roman";
    [ObservableProperty] private double _headerSize = 10.5;
    [ObservableProperty] private bool _headerBold = true;
    [ObservableProperty] private bool _headerTextCenter = true;
    [ObservableProperty] private string _headerBgColor = "";

    // ── 边框 ──
    [ObservableProperty] private string _borderStyle = "all";
    [ObservableProperty] private string _borderColor = "#000000";
    [ObservableProperty] private double _borderWidth = 0.5;

    // ── 单元格对齐 ──
    [ObservableProperty] private string _cellAlignH = "left";
    [ObservableProperty] private string _cellAlignV = "middle";

    // ── 单元格边距 ──
    [ObservableProperty] private double _cellMargin = 0.19;
    [ObservableProperty] private string _cellMarginUnit = "cm";

    // ── 特殊格式（缩进） ──
    [ObservableProperty] private string _indentType = "none";
    [ObservableProperty] private double _indentValue = 0.0;
    [ObservableProperty] private string _indentUnit = "字符";

    // ── 全局字形（应用到所有单元格） ──
    [ObservableProperty] private bool _fontBold = false;
    [ObservableProperty] private bool _fontItalic;
    [ObservableProperty] private bool _fontUnderline;

    // ── 行高 ──
    [ObservableProperty] private string _rowHeightMode = "auto";
    [ObservableProperty] private double _rowHeight = 0.8;
    [ObservableProperty] private string _rowHeightUnit = "cm";

    // ── 跨页选项 ──
    [ObservableProperty] private bool _autoSplit = true;
    [ObservableProperty] private bool _repeatHeader;

    // ── 选择列表 ──

    public static List<string> AlignmentOptions => new() { "left", "center", "right" };
    public static List<string> AlignmentDisplayNames => new() { "左对齐", "居中对齐", "右对齐" };
    public static List<string> WidthModeOptions => new() { "auto", "fixed" };
    public static List<string> WidthModeDisplayNames => new() { "自动调整", "指定宽度" };
    public static List<string> BorderStyleOptions => new() { "all", "none", "horizontal", "grid" };
    public static List<string> BorderStyleDisplayNames => new() { "全部框线", "无边框", "仅横向框线", "网格线" };
    public static List<string> BorderColorOptions => new()
    {
        "#000000", "#C00000", "#FF0000", "#FFC000", "#FFFF00",
        "#92D050", "#00B050", "#00B0F0", "#0070C0", "#002060", "#7030A0"
    };
    public static List<string> BorderColorDisplayNames => new()
    {
        "黑色", "深红", "红色", "橙色", "黄色",
        "浅绿", "绿色", "青色", "蓝色", "深蓝", "紫色"
    };
    public static List<string> HeaderBgColorOptions => new()
    {
        "", "#000000", "#C00000", "#FF0000", "#FFC000", "#FFFF00",
        "#92D050", "#00B050", "#00B0F0", "#0070C0", "#002060", "#7030A0"
    };
    public static List<string> HeaderBgColorDisplayNames => new()
    {
        "无", "黑色", "深红", "红色", "橙色", "黄色",
        "浅绿", "绿色", "青色", "蓝色", "深蓝", "紫色"
    };
    public static List<string> WidthUnitOptions => new() { "cm", "mm", "%" };
    public static List<string> WidthUnitDisplayNames => new() { "厘米", "毫米", "百分比" };
    public static List<string> CellAlignHOptions => new() { "left", "center", "right" };
    public static List<string> CellAlignHDisplayNames => new() { "左对齐", "水平居中", "右对齐" };
    public static List<string> CellAlignVOptions => new() { "top", "middle", "bottom" };
    public static List<string> CellAlignVDisplayNames => new() { "顶端对齐", "垂直居中", "底端对齐" };
    public static List<string> RowHeightModeOptions => new() { "auto", "fixed", "at_least" };
    public static List<string> RowHeightModeDisplayNames => new() { "自动", "固定高度", "最小值" };

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
        if (_sharedProfile is null || _isLoading) return;
        _isLoading = true;
        var t = _sharedProfile.Table;

        TableAlignment = t.TableAlignment;
        WidthMode = t.WidthMode;
        WidthValue = t.WidthValue;
        WidthUnit = t.WidthUnit;
        AutoFitColumns = t.AutoFitColumns;

        HeaderFontCn = t.HeaderFontCn;
        HeaderFontEn = t.HeaderFontEn;
        HeaderSize = t.HeaderSize;
        HeaderBold = t.HeaderBold;
        HeaderTextCenter = t.HeaderTextCenter;
        HeaderBgColor = t.HeaderBgColor;

        BorderStyle = t.BorderStyle;
        BorderColor = t.BorderColor;
        BorderWidth = t.BorderWidth;

        CellAlignH = t.CellAlignH;
        CellAlignV = t.CellAlignV;

        CellMargin = t.CellMargin;
        CellMarginUnit = t.CellMarginUnit;

        IndentType = t.IndentType;
        IndentValue = t.IndentValue;
        IndentUnit = t.IndentUnit;

        FontBold = t.FontBold;
        FontItalic = t.FontItalic;
        FontUnderline = t.FontUnderline;

        RowHeightMode = t.RowHeightMode;
        RowHeight = t.RowHeight;
        RowHeightUnit = t.RowHeightUnit;

        AutoSplit = t.AutoSplit;
        RepeatHeader = t.RepeatHeader;

        _isLoading = false;
    }

    private void WriteToSharedProfile()
    {
        if (_sharedProfile is null || _isLoading) return;
        var t = _sharedProfile.Table;

        t.TableAlignment = TableAlignment;
        t.WidthMode = WidthMode;
        t.WidthValue = WidthValue;
        t.WidthUnit = WidthUnit;
        t.AutoFitColumns = AutoFitColumns;

        t.HeaderFontCn = HeaderFontCn;
        t.HeaderFontEn = HeaderFontEn;
        t.HeaderSize = HeaderSize;
        t.HeaderBold = HeaderBold;
        t.HeaderTextCenter = HeaderTextCenter;
        t.HeaderBgColor = HeaderBgColor;

        t.BorderStyle = BorderStyle;
        t.BorderColor = BorderColor;
        t.BorderWidth = BorderWidth;

        t.CellAlignH = CellAlignH;
        t.CellAlignV = CellAlignV;

        t.CellMargin = CellMargin;
        t.CellMarginUnit = CellMarginUnit;

        t.IndentType = IndentType;
        t.IndentValue = IndentValue;
        t.IndentUnit = IndentUnit;

        t.FontBold = FontBold;
        t.FontItalic = FontItalic;
        t.FontUnderline = FontUnderline;

        t.RowHeightMode = RowHeightMode;
        t.RowHeight = RowHeight;
        t.RowHeightUnit = RowHeightUnit;

        t.AutoSplit = AutoSplit;
        t.RepeatHeader = RepeatHeader;
    }

    // ── Change handlers ──

    partial void OnTableAlignmentChanged(string value)  { IsDirty = true; WriteToSharedProfile(); }
    partial void OnWidthModeChanged(string value)       { IsDirty = true; WriteToSharedProfile(); }
    partial void OnWidthValueChanged(double value)      { IsDirty = true; WriteToSharedProfile(); }
    partial void OnWidthUnitChanged(string value)       { IsDirty = true; WriteToSharedProfile(); }
    partial void OnAutoFitColumnsChanged(bool value)    { IsDirty = true; WriteToSharedProfile(); }
    partial void OnHeaderFontCnChanged(string value)    { IsDirty = true; WriteToSharedProfile(); }
    partial void OnHeaderFontEnChanged(string value)    { IsDirty = true; WriteToSharedProfile(); }
    partial void OnHeaderSizeChanged(double value)      { IsDirty = true; WriteToSharedProfile(); }
    partial void OnHeaderBoldChanged(bool value)        { IsDirty = true; WriteToSharedProfile(); }
    partial void OnHeaderTextCenterChanged(bool value)   { IsDirty = true; WriteToSharedProfile(); }
    partial void OnHeaderBgColorChanged(string value)   { IsDirty = true; WriteToSharedProfile(); }
    partial void OnBorderStyleChanged(string value)     { IsDirty = true; WriteToSharedProfile(); }
    partial void OnBorderColorChanged(string value)     { IsDirty = true; WriteToSharedProfile(); }
    partial void OnBorderWidthChanged(double value)     { IsDirty = true; WriteToSharedProfile(); }
    partial void OnCellAlignHChanged(string value)      { IsDirty = true; WriteToSharedProfile(); }
    partial void OnCellAlignVChanged(string value)      { IsDirty = true; WriteToSharedProfile(); }
    partial void OnCellMarginChanged(double value)      { IsDirty = true; WriteToSharedProfile(); }
    partial void OnCellMarginUnitChanged(string value)  { IsDirty = true; WriteToSharedProfile(); }
    partial void OnIndentTypeChanged(string value)      { IsDirty = true; WriteToSharedProfile(); }
    partial void OnIndentValueChanged(double value)     { IsDirty = true; WriteToSharedProfile(); }
    partial void OnIndentUnitChanged(string value)      { IsDirty = true; WriteToSharedProfile(); }
    partial void OnFontBoldChanged(bool value)
    {
        IsDirty = true;
        // 字形加粗时，自动勾选表头字体加粗
        if (value) HeaderBold = true;
        WriteToSharedProfile();
    }
    partial void OnFontItalicChanged(bool value)        { IsDirty = true; WriteToSharedProfile(); }
    partial void OnFontUnderlineChanged(bool value)     { IsDirty = true; WriteToSharedProfile(); }
    partial void OnRowHeightModeChanged(string value)
    {
        IsDirty = true;
        WriteToSharedProfile();

        // 切换到"固定高度"或"最小值"时，若当前值为0则设为默认值0.8厘米
        if ((value == "fixed" || value == "at_least") && RowHeight == 0.0)
        {
            RowHeight = 0.8;
        }
    }
    partial void OnRowHeightChanged(double value)       { IsDirty = true; WriteToSharedProfile(); }
    partial void OnRowHeightUnitChanged(string value)   { IsDirty = true; WriteToSharedProfile(); }
    partial void OnAutoSplitChanged(bool value)         { IsDirty = true; WriteToSharedProfile(); }
    partial void OnRepeatHeaderChanged(bool value)      { IsDirty = true; WriteToSharedProfile(); }

    [ObservableProperty] private bool _isDirty;

    public void ResetDefaults()
    {
        _isLoading = true;

        TableAlignment = "center";
        WidthMode = "auto";
        WidthValue = 0.0;
        WidthUnit = "cm";
        AutoFitColumns = true;

        HeaderFontCn = "宋体";
        HeaderFontEn = "Times New Roman";
        HeaderSize = 10.5;
        HeaderBold = true;
        HeaderTextCenter = true;
        HeaderBgColor = "";

        BorderStyle = "all";
        BorderColor = "#000000";
        BorderWidth = 0.5;

        CellAlignH = "left";
        CellAlignV = "middle";

        CellMargin = 0.19;
        CellMarginUnit = "cm";

        IndentType = "none";
        IndentValue = 0.0;
        IndentUnit = "字符";

        FontBold = false;
        FontItalic = false;
        FontUnderline = false;

        RowHeightMode = "auto";
        RowHeight = 0.8;
        RowHeightUnit = "cm";

        AutoSplit = true;
        RepeatHeader = false;

        _isLoading = false;
        IsDirty = false;
        WriteToSharedProfile();
    }
}