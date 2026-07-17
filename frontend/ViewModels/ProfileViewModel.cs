using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WordFormatterUI.Models.Profile;
using WordFormatterUI.Services;

namespace WordFormatterUI.ViewModels;

/// <summary>
/// Aggregates all formatting configuration sections (page, body, heading,
/// headerFooter, picture, table) into a single ViewModel.
///
/// TRANSITIONAL — This is a Phase 5 implementation only. Phase 7 (Step 9.2)
/// will split this into independent ViewModels per navigation section:
///   PageSettingsViewModel, BodyStyleViewModel, HeadingStyleViewModel,
///   HeaderFooterViewModel, PictureSettingsViewModel, TableSettingsViewModel.
///
/// Do not add further responsibilities here.
/// </summary>
public partial class ProfileViewModel : ObservableObject
{
    private readonly ApiService _api;

    public ProfileViewModel(ApiService api)
    {
        _api = api;
    }

    [ObservableProperty]
    private ProfileConfigDto? _profile;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private bool _isDirty;

    // ── Page fields ──
    [ObservableProperty] private string _paperSize = "A4";
    [ObservableProperty] private string _orientation = "portrait";
    [ObservableProperty] private double _marginTop = 25.4;
    [ObservableProperty] private double _marginBottom = 25.4;
    [ObservableProperty] private double _marginLeft = 31.7;
    [ObservableProperty] private double _marginRight = 31.7;
    [ObservableProperty] private bool _pageNumber = true;

    // ── Body fields ──
    [ObservableProperty] private string _bodyFontCn = "宋体";
    [ObservableProperty] private string _bodyFontEn = "Times New Roman";
    [ObservableProperty] private double _bodyFontSize = 12.0;
    [ObservableProperty] private string _bodyFontStyle = "normal";
    [ObservableProperty] private string _bodyAlignment = "justify";

    // ── Body paragraph fields ──
    [ObservableProperty] private double _lineSpacing = 1.5;
    [ObservableProperty] private string _lineSpacingMode = "multiple";
    [ObservableProperty] private string _indentType = "firstLine";
    [ObservableProperty] private double _indentValue = 2.0;
    [ObservableProperty] private double _spaceBefore;
    [ObservableProperty] private double _spaceAfter;

    // ── Heading (current level) ──
    [ObservableProperty] private int _currentHeadingLevel = 1;
    [ObservableProperty] private string _headingFontCn = "黑体";
    [ObservableProperty] private string _headingFontEn = "Arial";
    [ObservableProperty] private double _headingFontSize = 16.0;
    [ObservableProperty] private string _headingFontStyle = "bold";
    [ObservableProperty] private string _headingAlignment = "left";
    [ObservableProperty] private double _headingLineSpacing = 1.5;
    [ObservableProperty] private double _headingSpaceBefore = 12.0;
    [ObservableProperty] private double _headingSpaceAfter = 6.0;

    // ── HeaderFooter fields ──
    [ObservableProperty] private string _hfFontCn = "宋体";
    [ObservableProperty] private string _hfFontEn = "Times New Roman";
    [ObservableProperty] private double _hfFontSize = 10.5;
    [ObservableProperty] private string _hfAlignment = "center";
    [ObservableProperty] private double _hfHeaderDistance = 15.0;
    [ObservableProperty] private double _hfFooterDistance = 15.0;

    // ── Picture fields ──
    [ObservableProperty] private double _pictureWidth = 12.0;
    [ObservableProperty] private string _pictureAlignment = "center";
    [ObservableProperty] private bool _pictureKeepAspectRatio = true;

    // ── Table fields ──
    [ObservableProperty] private string _tableAlignment = "center";
    [ObservableProperty] private string _tableWidthMode = "auto";
    [ObservableProperty] private double _tableWidthValue = 0.0;
    [ObservableProperty] private string _tableWidthUnit = "cm";
    [ObservableProperty] private bool _tableAutoFitColumns = true;
    [ObservableProperty] private string _tableHeaderFontCn = "黑体";
    [ObservableProperty] private string _tableHeaderFontEn = "Arial";
    [ObservableProperty] private double _tableHeaderSize = 10.0;
    [ObservableProperty] private bool _tableHeaderBold = true;
    [ObservableProperty] private string _tableHeaderBgColor = "#D9E2F3";
    [ObservableProperty] private string _tableBorderStyle = "all";
    [ObservableProperty] private string _tableBorderColor = "#000000";
    [ObservableProperty] private double _tableBorderWidth = 0.5;
    [ObservableProperty] private string _tableCellAlignH = "center";
    [ObservableProperty] private string _tableCellAlignV = "middle";
    [ObservableProperty] private double _tableCellMarginH = 0.19;
    [ObservableProperty] private string _tableCellMarginHUnit = "cm";
    [ObservableProperty] private double _tableCellMarginV = 0.0;
    [ObservableProperty] private string _tableCellMarginVUnit = "cm";
    [ObservableProperty] private string _tableRowHeightMode = "auto";
    [ObservableProperty] private double _tableRowHeight = 0.0;
    [ObservableProperty] private string _tableRowHeightUnit = "cm";
    [ObservableProperty] private bool _tableAutoSplit = true;
    [ObservableProperty] private bool _tableRepeatHeader = true;

    // ── Dirty tracking (fires on any property change) ──

    partial void OnPaperSizeChanged(string value) => IsDirty = true;
    partial void OnOrientationChanged(string value) => IsDirty = true;
    partial void OnMarginTopChanged(double value) => IsDirty = true;
    partial void OnMarginBottomChanged(double value) => IsDirty = true;
    partial void OnMarginLeftChanged(double value) => IsDirty = true;
    partial void OnMarginRightChanged(double value) => IsDirty = true;
    partial void OnPageNumberChanged(bool value) => IsDirty = true;
    partial void OnBodyFontCnChanged(string value) => IsDirty = true;
    partial void OnBodyFontEnChanged(string value) => IsDirty = true;
    partial void OnBodyFontSizeChanged(double value) => IsDirty = true;
    partial void OnBodyFontStyleChanged(string value) => IsDirty = true;
    partial void OnBodyAlignmentChanged(string value) => IsDirty = true;
    partial void OnLineSpacingChanged(double value) => IsDirty = true;
    partial void OnLineSpacingModeChanged(string value) => IsDirty = true;
    partial void OnIndentTypeChanged(string value) => IsDirty = true;
    partial void OnIndentValueChanged(double value) => IsDirty = true;
    partial void OnSpaceBeforeChanged(double value) => IsDirty = true;
    partial void OnSpaceAfterChanged(double value) => IsDirty = true;
    partial void OnCurrentHeadingLevelChanged(int value) => LoadHeadingFields(value);
    partial void OnHeadingFontCnChanged(string value) => SaveHeadingField();
    partial void OnHeadingFontEnChanged(string value) => SaveHeadingField();
    partial void OnHeadingFontSizeChanged(double value) => SaveHeadingField();
    partial void OnHeadingFontStyleChanged(string value) => SaveHeadingField();
    partial void OnHeadingAlignmentChanged(string value) => SaveHeadingField();
    partial void OnHeadingLineSpacingChanged(double value) => SaveHeadingField();
    partial void OnHeadingSpaceBeforeChanged(double value) => SaveHeadingField();
    partial void OnHeadingSpaceAfterChanged(double value) => SaveHeadingField();
    partial void OnHfFontCnChanged(string value) => IsDirty = true;
    partial void OnHfFontEnChanged(string value) => IsDirty = true;
    partial void OnHfFontSizeChanged(double value) => IsDirty = true;
    partial void OnHfAlignmentChanged(string value) => IsDirty = true;
    partial void OnHfHeaderDistanceChanged(double value) => IsDirty = true;
    partial void OnHfFooterDistanceChanged(double value) => IsDirty = true;
    partial void OnPictureWidthChanged(double value) => IsDirty = true;
    partial void OnPictureAlignmentChanged(string value) => IsDirty = true;
    partial void OnPictureKeepAspectRatioChanged(bool value) => IsDirty = true;
    partial void OnTableAlignmentChanged(string value) => IsDirty = true;
    partial void OnTableWidthModeChanged(string value) => IsDirty = true;
    partial void OnTableWidthValueChanged(double value) => IsDirty = true;
    partial void OnTableWidthUnitChanged(string value) => IsDirty = true;
    partial void OnTableAutoFitColumnsChanged(bool value) => IsDirty = true;
    partial void OnTableHeaderFontCnChanged(string value) => IsDirty = true;
    partial void OnTableHeaderFontEnChanged(string value) => IsDirty = true;
    partial void OnTableHeaderSizeChanged(double value) => IsDirty = true;
    partial void OnTableHeaderBoldChanged(bool value) => IsDirty = true;
    partial void OnTableHeaderBgColorChanged(string value) => IsDirty = true;
    partial void OnTableBorderStyleChanged(string value) => IsDirty = true;
    partial void OnTableBorderColorChanged(string value) => IsDirty = true;
    partial void OnTableBorderWidthChanged(double value) => IsDirty = true;
    partial void OnTableCellAlignHChanged(string value) => IsDirty = true;
    partial void OnTableCellAlignVChanged(string value) => IsDirty = true;
    partial void OnTableCellMarginHChanged(double value) => IsDirty = true;
    partial void OnTableCellMarginHUnitChanged(string value) => IsDirty = true;
    partial void OnTableCellMarginVChanged(double value) => IsDirty = true;
    partial void OnTableCellMarginVUnitChanged(string value) => IsDirty = true;
    partial void OnTableRowHeightModeChanged(string value) => IsDirty = true;
    partial void OnTableRowHeightChanged(double value) => IsDirty = true;
    partial void OnTableRowHeightUnitChanged(string value) => IsDirty = true;
    partial void OnTableAutoSplitChanged(bool value) => IsDirty = true;
    partial void OnTableRepeatHeaderChanged(bool value) => IsDirty = true;

    // ── Heading level switching ──

    private bool _isLoadingHeading;

    /// <summary>
    /// Load heading fields for the specified level from the in-memory Profile.
    /// </summary>
    private void LoadHeadingFields(int level)
    {
        if (Profile is null || _isLoadingHeading) return;
        _isLoadingHeading = true;

        var key = level.ToString();
        if (Profile.Heading.TryGetValue(key, out var h))
        {
            HeadingFontCn = h.FontCn;
            HeadingFontEn = h.FontEn;
            HeadingFontSize = h.FontSize;
            HeadingFontStyle = h.FontStyle;
            HeadingAlignment = h.Alignment;
            HeadingLineSpacing = h.LineSpacing;
            HeadingSpaceBefore = h.SpaceBefore;
            HeadingSpaceAfter = h.SpaceAfter;
        }

        _isLoadingHeading = false;
    }

    /// <summary>
    /// Save the current heading fields back to the Profile's heading dict.
    /// </summary>
    private void SaveHeadingField()
    {
        if (Profile is null || _isLoadingHeading) return;

        var key = CurrentHeadingLevel.ToString();
        if (Profile.Heading.TryGetValue(key, out var h))
        {
            h.FontCn = HeadingFontCn;
            h.FontEn = HeadingFontEn;
            h.FontSize = HeadingFontSize;
            h.FontStyle = HeadingFontStyle;
            h.Alignment = HeadingAlignment;
            h.LineSpacing = HeadingLineSpacing;
            h.SpaceBefore = HeadingSpaceBefore;
            h.SpaceAfter = HeadingSpaceAfter;
        }

        IsDirty = true;
    }

    // ── Commands ──

    [RelayCommand]
    public async Task LoadProfileAsync()
    {
        IsLoading = true;
        try
        {
            var dto = await _api.GetProfileAsync();
            if (dto is not null)
            {
                Profile = dto;
                MapProfileToFields(dto);
                IsDirty = false;
                StatusMessage = "";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载配置失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SaveProfileAsync()
    {
        if (Profile is null) return;

        IsLoading = true;
        try
        {
            MapFieldsToProfile(Profile);

            var ok = await _api.UpdateProfileAsync(Profile);
            if (ok)
            {
                IsDirty = false;
                StatusMessage = "配置已保存";
            }
            else
            {
                StatusMessage = "保存失败";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"保存失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ResetProfileAsync()
    {
        IsLoading = true;
        try
        {
            var ok = await _api.ResetProfileAsync();
            if (ok)
            {
                await LoadProfileAsync();
                StatusMessage = "已恢复默认配置";
            }
            else
            {
                StatusMessage = "重置失败";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"重置失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Template application ────────────────────────────────────────

    /// <summary>
    /// Apply a template's profile to the current configuration.
    /// Called when the user selects a template from the format control.
    /// </summary>
    public void ApplyTemplateProfile(ProfileConfigDto profile)
    {
        if (profile is null) return;

        Profile = profile;
        MapProfileToFields(profile);
        IsDirty = false;
        StatusMessage = "已加载模板配置";
    }

    // ── Mapping helpers ──

    private void MapProfileToFields(ProfileConfigDto p)
    {
        // Page
        PaperSize = p.Page.PaperSize;
        Orientation = p.Page.Orientation;
        MarginTop = p.Page.MarginTop;
        MarginBottom = p.Page.MarginBottom;
        MarginLeft = p.Page.MarginLeft;
        MarginRight = p.Page.MarginRight;
        PageNumber = p.Page.PageNumber;

        // Body
        BodyFontCn = p.Body.FontCn;
        BodyFontEn = p.Body.FontEn;
        BodyFontSize = p.Body.FontSize;
        BodyFontStyle = p.Body.FontStyle;
        BodyAlignment = p.Body.Alignment;
        LineSpacing = p.Body.LineSpacing;
        LineSpacingMode = p.Body.LineSpacingMode;
        IndentType = p.Body.IndentType;
        IndentValue = p.Body.IndentValue;
        SpaceBefore = p.Body.SpaceBefore;
        SpaceAfter = p.Body.SpaceAfter;

        // Heading (current level)
        LoadHeadingFields(CurrentHeadingLevel);

        // HeaderFooter
        HfFontCn = p.HeaderFooter.FontCn;
        HfFontEn = p.HeaderFooter.FontEn;
        HfFontSize = p.HeaderFooter.FontSize;
        HfAlignment = p.HeaderFooter.Alignment;
        HfHeaderDistance = p.HeaderFooter.HeaderDistance;
        HfFooterDistance = p.HeaderFooter.FooterDistance;

        // Picture
        PictureWidth = p.Picture.Width;
        PictureAlignment = p.Picture.Alignment;
        PictureKeepAspectRatio = p.Picture.KeepAspectRatio;

        // Table
        TableAlignment = p.Table.TableAlignment;
        TableWidthMode = p.Table.WidthMode;
        TableWidthValue = p.Table.WidthValue;
        TableWidthUnit = p.Table.WidthUnit;
        TableAutoFitColumns = p.Table.AutoFitColumns;
        TableHeaderFontCn = p.Table.HeaderFontCn;
        TableHeaderFontEn = p.Table.HeaderFontEn;
        TableHeaderSize = p.Table.HeaderSize;
        TableHeaderBold = p.Table.HeaderBold;
        TableHeaderBgColor = p.Table.HeaderBgColor;
        TableBorderStyle = p.Table.BorderStyle;
        TableBorderColor = p.Table.BorderColor;
        TableBorderWidth = p.Table.BorderWidth;
        TableCellAlignH = p.Table.CellAlignH;
        TableCellAlignV = p.Table.CellAlignV;
        TableCellMarginH = p.Table.CellMarginH;
        TableCellMarginHUnit = p.Table.CellMarginHUnit;
        TableCellMarginV = p.Table.CellMarginV;
        TableCellMarginVUnit = p.Table.CellMarginVUnit;
        TableRowHeightMode = p.Table.RowHeightMode;
        TableRowHeight = p.Table.RowHeight;
        TableRowHeightUnit = p.Table.RowHeightUnit;
        TableAutoSplit = p.Table.AutoSplit;
        TableRepeatHeader = p.Table.RepeatHeader;
    }

    private void MapFieldsToProfile(ProfileConfigDto p)
    {
        // Page
        p.Page.PaperSize = PaperSize;
        p.Page.Orientation = Orientation;
        p.Page.MarginTop = MarginTop;
        p.Page.MarginBottom = MarginBottom;
        p.Page.MarginLeft = MarginLeft;
        p.Page.MarginRight = MarginRight;
        p.Page.PageNumber = PageNumber;

        // Body
        p.Body.FontCn = BodyFontCn;
        p.Body.FontEn = BodyFontEn;
        p.Body.FontSize = BodyFontSize;
        p.Body.FontStyle = BodyFontStyle;
        p.Body.Alignment = BodyAlignment;
        p.Body.LineSpacing = LineSpacing;
        p.Body.LineSpacingMode = LineSpacingMode;
        p.Body.IndentType = IndentType;
        p.Body.IndentValue = IndentValue;
        p.Body.SpaceBefore = SpaceBefore;
        p.Body.SpaceAfter = SpaceAfter;

        // Heading (current level — already saved in SaveHeadingField)
        SaveHeadingField();

        // HeaderFooter
        p.HeaderFooter.FontCn = HfFontCn;
        p.HeaderFooter.FontEn = HfFontEn;
        p.HeaderFooter.FontSize = HfFontSize;
        p.HeaderFooter.Alignment = HfAlignment;
        p.HeaderFooter.HeaderDistance = HfHeaderDistance;
        p.HeaderFooter.FooterDistance = HfFooterDistance;

        // Picture
        p.Picture.Width = PictureWidth;
        p.Picture.Alignment = PictureAlignment;
        p.Picture.KeepAspectRatio = PictureKeepAspectRatio;

        // Table
        p.Table.TableAlignment = TableAlignment;
        p.Table.WidthMode = TableWidthMode;
        p.Table.WidthValue = TableWidthValue;
        p.Table.WidthUnit = TableWidthUnit;
        p.Table.AutoFitColumns = TableAutoFitColumns;
        p.Table.HeaderFontCn = TableHeaderFontCn;
        p.Table.HeaderFontEn = TableHeaderFontEn;
        p.Table.HeaderSize = TableHeaderSize;
        p.Table.HeaderBold = TableHeaderBold;
        p.Table.HeaderBgColor = TableHeaderBgColor;
        p.Table.BorderStyle = TableBorderStyle;
        p.Table.BorderColor = TableBorderColor;
        p.Table.BorderWidth = TableBorderWidth;
        p.Table.CellAlignH = TableCellAlignH;
        p.Table.CellAlignV = TableCellAlignV;
        p.Table.CellMarginH = TableCellMarginH;
        p.Table.CellMarginHUnit = TableCellMarginHUnit;
        p.Table.CellMarginV = TableCellMarginV;
        p.Table.CellMarginVUnit = TableCellMarginVUnit;
        p.Table.RowHeightMode = TableRowHeightMode;
        p.Table.RowHeight = TableRowHeight;
        p.Table.RowHeightUnit = TableRowHeightUnit;
        p.Table.AutoSplit = TableAutoSplit;
        p.Table.RepeatHeader = TableRepeatHeader;
    }
}
