using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WordFormatterUI.Models;
using WordFormatterUI.Models.Profile;
using WordFormatterUI.Services;
using WordFormatterUI.Utilities;
using WordFormatterUI.ViewModels;

namespace WordFormatterUI.Views;

public sealed partial class TableSettingsView : UserControl
{
    private TableSettingsViewModel? _vm;
    private ProfileConfigDto? _sharedProfile;
    private double _rowHeightMm = 8.0;   // canonical value in mm (0.8 cm → 8 mm)
    private double _cellMarginHMm = 1.9;  // canonical value in mm (0.19 cm → 1.9 mm)
    private double _cellMarginVMm = 0.0;
    private bool _isUnitSwitching;
    private bool _isLoadingFields;
    private bool _isUpdatingModeFromValue;

    public TableSettingsView()
    {
        InitializeComponent();
    }

    private TableSettingsViewModel? GetVm()
    {
        if (DataContext is TableSettingsViewModel vm)
            return vm;
        return null;
    }

    /// <summary>
    /// Called from the parent navigation to share the profile and initialise.
    /// </summary>
    public void SetSharedProfile(ProfileConfigDto profile)
    {
        var vm = GetVm();
        if (vm is not null) vm.SetSharedProfile(profile);
    }

    /// <summary>
    /// Populate a ComboBox with items where the Tag holds the actual value
    /// and the display text is shown to the user.
    /// </summary>
    private void PopulateComboBox(ComboBox box, List<string> items, string selected)
    {
        box.Items.Clear();
        foreach (var item in items)
        {
            box.Items.Add(new ComboBoxItem { Tag = item, Content = item });
        }
        var match = box.Items.OfType<ComboBoxItem>().FirstOrDefault(i => (string)i.Tag == selected);
        if (match is not null) box.SelectedItem = match;
    }

    /// <summary>
    /// Populate a ComboBox with value/display-name pairs.
    /// </summary>
    private void PopulateComboBoxWithDisplayNames(
        ComboBox box, List<string> values, List<string> displayNames, string selected)
    {
        box.Items.Clear();
        for (int i = 0; i < values.Count; i++)
        {
            box.Items.Add(new ComboBoxItem { Tag = values[i], Content = displayNames[i] });
        }
        var match = box.Items.OfType<ComboBoxItem>().FirstOrDefault(i => (string)i.Tag == selected);
        if (match is not null) box.SelectedItem = match;
    }

    /// <summary>
    /// Populate a Chinese font ComboBox (matches BodyStyleView ordering).
    /// </summary>
    private void PopulateFontCnBox(ComboBox box, string selected)
    {
        var fonts = new List<string>
        {
            "等线", "宋体", "新宋体", "华文宋体", "华文中宋", "思源宋体",
            "仿宋", "仿宋_GB2312", "楷体", "楷体_GB2312", "黑体",
            "微软雅黑", "思源黑体", "方正小标宋简体", "方正小标宋_GBK"
        };
        box.Items.Clear();
        foreach (var f in fonts)
            box.Items.Add(new ComboBoxItem { Tag = f, Content = f });
        var match = box.Items.OfType<ComboBoxItem>().FirstOrDefault(i => (string)i.Tag == selected);
        if (match is not null) box.SelectedItem = match;
    }

    /// <summary>
    /// Populate a Western font ComboBox (matches BodyStyleView ordering).
    /// </summary>
    private void PopulateFontEnBox(ComboBox box, string selected)
    {
        var fonts = new List<string>
        {
            "Arial", "Calibri", "Cambria", "Century Gothic", "Consolas",
            "Courier New", "Georgia", "Helvetica", "Noto Sans Latin",
            "Noto Serif Latin", "Tahoma", "Times New Roman", "Verdana"
        };
        box.Items.Clear();
        foreach (var f in fonts)
            box.Items.Add(new ComboBoxItem { Tag = f, Content = f });
        var match = box.Items.OfType<ComboBoxItem>().FirstOrDefault(i => (string)i.Tag == selected);
        if (match is not null) box.SelectedItem = match;
    }

    /// <summary>
    /// Populate a font size ComboBox using FontSizeDefinition.DefaultSet,
    /// matching the order used in BodyStyleView (初号 → 小六).
    /// </summary>
    private void PopulateFontSizeBox(ComboBox box, string selectedName)
    {
        box.Items.Clear();
        foreach (var def in FontSizeDefinition.DefaultSet)
            box.Items.Add(new ComboBoxItem { Tag = def.Name, Content = def.Name });
        var match = box.Items.OfType<ComboBoxItem>().FirstOrDefault(i => (string)i.Tag == selectedName);
        if (match is not null) box.SelectedItem = match;
    }

    /// <summary>
    /// Refresh UI from the ViewModel.
    /// </summary>
    public void RefreshFromProfile()
    {
        var vm = GetVm();
        // DO NOT call LoadFromSharedProfile — resets user changes

        _isLoadingFields = true;

        WidthValueBox.Value = vm.WidthValue;

        PopulateComboBoxWithDisplayNames(
            AlignmentBox,
            TableSettingsViewModel.AlignmentOptions,
            TableSettingsViewModel.AlignmentDisplayNames,
            vm.TableAlignment);

        PopulateComboBoxWithDisplayNames(
            WidthModeBox,
            TableSettingsViewModel.WidthModeOptions,
            TableSettingsViewModel.WidthModeDisplayNames,
            vm.WidthMode);

        PopulateComboBoxWithDisplayNames(
            WidthUnitBox,
            TableSettingsViewModel.WidthUnitOptions,
            TableSettingsViewModel.WidthUnitDisplayNames,
            vm.WidthUnit);

        AutoFitCheck.IsChecked = vm.AutoFitColumns;
        UpdateWidthControlsVisibility();

        PopulateFontCnBox(HeaderFontCnBox, vm.HeaderFontCn);
        PopulateFontEnBox(HeaderFontEnBox, vm.HeaderFontEn);

        // Font size ComboBox (使用 FontSizeDefinition.DefaultSet，与页面设置一致)
        var converter = new FontSizeConverter();
        var sizeName = converter.Format(vm.HeaderSize);
        PopulateFontSizeBox(HeaderSizeBox, sizeName);

        HeaderBoldCheck.IsChecked = vm.HeaderBold;
        HeaderTextCenterCheck.IsChecked = vm.HeaderTextCenter;

        // Header background color ComboBox
        PopulateComboBoxWithDisplayNames(
            HeaderBgColorBox,
            TableSettingsViewModel.HeaderBgColorOptions,
            TableSettingsViewModel.HeaderBgColorDisplayNames,
            vm.HeaderBgColor);

        // Body background color ComboBox
        PopulateComboBoxWithDisplayNames(
            BodyBgColorBox,
            TableSettingsViewModel.BodyBgColorOptions,
            TableSettingsViewModel.BodyBgColorDisplayNames,
            vm.BodyBgColor);

        PopulateComboBoxWithDisplayNames(
            BorderStyleBox,
            TableSettingsViewModel.BorderStyleOptions,
            TableSettingsViewModel.BorderStyleDisplayNames,
            vm.BorderStyle);

        // Border color ComboBox
        PopulateComboBoxWithDisplayNames(
            BorderColorBox,
            TableSettingsViewModel.BorderColorOptions,
            TableSettingsViewModel.BorderColorDisplayNames,
            vm.BorderColor);

        PopulateComboBoxWithDisplayNames(
            CellAlignHBox,
            TableSettingsViewModel.CellAlignHOptions,
            TableSettingsViewModel.CellAlignHDisplayNames,
            vm.CellAlignH);

        PopulateComboBoxWithDisplayNames(
            CellAlignVBox,
            TableSettingsViewModel.CellAlignVOptions,
            TableSettingsViewModel.CellAlignVDisplayNames,
            vm.CellAlignV);

        PopulateComboBoxWithDisplayNames(
            RowHeightModeBox,
            TableSettingsViewModel.RowHeightModeOptions,
            TableSettingsViewModel.RowHeightModeDisplayNames,
            vm.RowHeightMode);
        UpdateRowHeightControlsVisibility();

        // Indent ComboBox
        var indentValues = new List<string> { "none", "first_line", "hanging" };
        var indentNames = new List<string> { "无缩进", "首行缩进", "悬挂缩进" };
        PopulateComboBoxWithDisplayNames(IndentTypeBox, indentValues, indentNames, vm.IndentType);
        IndentValueBox.Value = vm.IndentValue;

        // Indent unit (字符/厘米)
        var indentUnitIdx = vm.IndentUnit == "厘米" ? 1 : 0;
        IndentUnitBox.SelectedIndex = indentUnitIdx;

        // Show/hide indent value/unit based on selected type
        IndentOptionsPanel.Visibility = vm.IndentType == "none" ? Visibility.Collapsed : Visibility.Visible;

        FontBoldCheck.IsChecked = vm.FontBold;
        FontItalicCheck.IsChecked = vm.FontItalic;
        FontUnderlineCheck.IsChecked = vm.FontUnderline;

        // Line spacing ComboBox items (lazy init)
        if (TableLineSpacingModeBox.Items.Count == 0)
        {
            TableLineSpacingModeBox.Items.Add(new ComboBoxItem { Tag = "1.0", Content = "单倍行距" });
            TableLineSpacingModeBox.Items.Add(new ComboBoxItem { Tag = "1.5", Content = "1.5 倍行距" });
            TableLineSpacingModeBox.Items.Add(new ComboBoxItem { Tag = "2.0", Content = "2 倍行距" });
            TableLineSpacingModeBox.Items.Add(new ComboBoxItem { Tag = "multi", Content = "多倍行距" });
            TableLineSpacingModeBox.Items.Add(new ComboBoxItem { Tag = "fixed", Content = "固定值" });
            TableLineSpacingModeBox.Items.Add(new ComboBoxItem { Tag = "at_least", Content = "最小值" });
        }
        TableLineSpacingBox.Value = vm.TableLineSpacing;
        // Map mode + value to ComboBox index: 0=1x, 1=1.5x, 2=2x, 3=multi, 4=fixed, 5=at_least
        if (vm.TableLineSpacingMode == "fixed")
            TableLineSpacingModeBox.SelectedIndex = 4;
        else if (vm.TableLineSpacingMode == "at_least")
            TableLineSpacingModeBox.SelectedIndex = 5;
        else if (Math.Abs(vm.TableLineSpacing - 1.0) < 0.01)
            TableLineSpacingModeBox.SelectedIndex = 0;
        else if (Math.Abs(vm.TableLineSpacing - 2.0) < 0.01)
            TableLineSpacingModeBox.SelectedIndex = 2;
        else if (Math.Abs(vm.TableLineSpacing - 1.5) < 0.01)
            TableLineSpacingModeBox.SelectedIndex = 1;
        else
            TableLineSpacingModeBox.SelectedIndex = 3; // 多倍行距（其他值）

        // Initialize line spacing unit ComboBox
        TableLineSpacingUnitBox.SelectedIndex = vm.TableLineSpacingUnit switch
        {
            "cm" => 1,
            "mm" => 2,
            _ => 0, // pt
        };
        UpdateLineSpacingUnitVisibility();
        ApplyLineSpacingConfig();
        // 在初始化完成后重新同步值框，确保 UI 与 VM 一致
        TableLineSpacingBox.Value = vm.TableLineSpacing;

        AutoSplitCheck.IsChecked = vm.AutoSplit;
        RepeatHeaderCheck.IsChecked = vm.RepeatHeader;

        // Apply distance configs for RowHeightBox and CellMarginBox
        ApplyConfigForDistance(RowHeightBox, RowHeightUnitBox);
        ApplyCellMarginConfig(CellMarginHBox, CellMarginHUnitBox);
        ApplyCellMarginConfig(CellMarginVBox, CellMarginVUnitBox);

        // Initialize NumericTextBox values from ViewModel
        BorderWidthBox.Value = vm.BorderWidth;

        var rowHeightUnitLabel = GetUnitLabel(RowHeightUnitBox);
        RowHeightBox.Value = rowHeightUnitLabel == "厘米" ? _rowHeightMm / 10.0 : _rowHeightMm;

        var cellMarginHUnitLabel = GetUnitLabel(CellMarginHUnitBox);
        CellMarginHBox.Value = cellMarginHUnitLabel == "厘米" ? _cellMarginHMm / 10.0 : _cellMarginHMm;
        var cellMarginVUnitLabel = GetUnitLabel(CellMarginVUnitBox);
        CellMarginVBox.Value = cellMarginVUnitLabel == "厘米" ? _cellMarginVMm / 10.0 : _cellMarginVMm;

        _isLoadingFields = false;
    }

    /// <summary>
    /// Called from MainWindow.ProfileRefreshed. Alias for <see cref="RefreshFromProfile"/>.
    /// </summary>
    public void RefreshUI() => RefreshFromProfile();

    public void ResetDefaults()
    {
        GetVm().ResetDefaults();
        RefreshFromProfile();
    }

    private void UpdateWidthControlsVisibility()
    {
        var isFixed = WidthModeBox.SelectedItem is ComboBoxItem item && (string)item.Tag == "fixed";
        WidthValueBox.Visibility = isFixed ? Visibility.Visible : Visibility.Collapsed;
        WidthUnitBox.Visibility = isFixed ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateRowHeightControlsVisibility()
    {
        var isAuto = RowHeightModeBox.SelectedItem is ComboBoxItem item && (string)item.Tag == "auto";
        RowHeightBox.Visibility = isAuto ? Visibility.Collapsed : Visibility.Visible;
        RowHeightUnitBox.Visibility = isAuto ? Visibility.Collapsed : Visibility.Visible;
    }

    // ── Event handlers (two-way binding helpers) ──

    private void AlignmentBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AlignmentBox.SelectedItem is ComboBoxItem item)
            GetVm().TableAlignment = (string)item.Tag;
    }

    private void WidthModeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (WidthModeBox.SelectedItem is ComboBoxItem item)
        {
            var vm = GetVm();
            var mode = (string)item.Tag;
            if (mode == "fixed" && vm.WidthMode != "fixed")
            {
                // 切换到指定宽度时，默认百分比 80%
                vm.WidthUnit = "%";
                vm.WidthValue = 80;
                WidthValueBox.Value = 80;

                // 同步更新单位 ComboBox 的显示
                var percentItem = WidthUnitBox.Items
                    .OfType<ComboBoxItem>()
                    .FirstOrDefault(i => (string)i.Tag == "%");
                if (percentItem is not null) WidthUnitBox.SelectedItem = percentItem;
            }
            vm.WidthMode = mode;
        }
        UpdateWidthControlsVisibility();
    }

    private void WidthUnitBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (WidthUnitBox.SelectedItem is not ComboBoxItem item) return;
        var newUnit = (string)item.Tag;
        var vm = GetVm();
        var oldUnit = vm.WidthUnit;
        var oldValue = vm.WidthValue;

        if (oldUnit != newUnit)
        {
            // 单位切换时转换数值
            double newValue = oldValue;
            if (oldUnit == "cm" && newUnit == "mm")
                newValue = oldValue * 10;
            else if (oldUnit == "mm" && newUnit == "cm")
                newValue = oldValue / 10;
            else if (newUnit == "%")
                newValue = 80;
            else if (oldUnit == "%" && newUnit == "cm")
                newValue = 14;
            else if (oldUnit == "%" && newUnit == "mm")
                newValue = 140;
            vm.WidthValue = newValue;
            WidthValueBox.Value = newValue;
        }

        vm.WidthUnit = newUnit;
    }

    private void WidthValueBox_ValueChanged(object sender, double value)
    {
        GetVm().WidthValue = value;
    }

    private void AutoFitCheck_Changed(object sender, RoutedEventArgs e)
    {
        GetVm().AutoFitColumns = AutoFitCheck.IsChecked == true;
    }

    private void HeaderFontCnBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (HeaderFontCnBox.SelectedItem is ComboBoxItem item)
            GetVm().HeaderFontCn = (string)item.Tag;
    }

    private void HeaderFontEnBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (HeaderFontEnBox.SelectedItem is ComboBoxItem item)
            GetVm().HeaderFontEn = (string)item.Tag;
    }

    private void HeaderSizeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (HeaderSizeBox.SelectedItem is ComboBoxItem item)
        {
            var tag = (string)item.Tag;
            var converter = new FontSizeConverter();
            var pt = converter.Parse(tag);
            if (pt.HasValue)
                GetVm().HeaderSize = pt.Value;
        }
    }

    private void HeaderBoldCheck_Changed(object sender, RoutedEventArgs e)
    {
        GetVm().HeaderBold = HeaderBoldCheck.IsChecked == true;
    }

    private void HeaderTextCenterCheck_Changed(object sender, RoutedEventArgs e)
    {
        GetVm().HeaderTextCenter = HeaderTextCenterCheck.IsChecked == true;
    }

    private void HeaderBgColorBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (HeaderBgColorBox.SelectedItem is ComboBoxItem item)
            GetVm().HeaderBgColor = (string)item.Tag;
    }

    private void BodyBgColorBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BodyBgColorBox.SelectedItem is ComboBoxItem item)
            GetVm().BodyBgColor = (string)item.Tag;
    }

    private void BorderStyleBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BorderStyleBox.SelectedItem is ComboBoxItem item)
            GetVm().BorderStyle = (string)item.Tag;
    }

    private void BorderColorBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BorderColorBox.SelectedItem is ComboBoxItem item)
            GetVm().BorderColor = (string)item.Tag;
    }

    private void BorderWidthBox_ValueChanged(object sender, double value)
    {
        GetVm().BorderWidth = value;
    }

    private void CellAlignHBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CellAlignHBox.SelectedItem is ComboBoxItem item)
            GetVm().CellAlignH = (string)item.Tag;
    }

    private void CellAlignVBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CellAlignVBox.SelectedItem is ComboBoxItem item)
            GetVm().CellAlignV = (string)item.Tag;
    }

    private void CellMarginHBox_ValueChanged(object sender, double value)
    {
        if (_isUnitSwitching) return;
        var unitLabel = GetUnitLabel(CellMarginHUnitBox);
        _cellMarginHMm = unitLabel == "厘米" ? value * 10.0 : value;
        GetVm().CellMarginH = _cellMarginHMm / 10.0; // ViewModel stores in cm
    }

    private void CellMarginVBox_ValueChanged(object sender, double value)
    {
        if (_isUnitSwitching) return;
        var unitLabel = GetUnitLabel(CellMarginVUnitBox);
        _cellMarginVMm = unitLabel == "厘米" ? value * 10.0 : value;
        GetVm().CellMarginV = _cellMarginVMm / 10.0; // ViewModel stores in cm
    }

    private void RowHeightModeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RowHeightModeBox.SelectedItem is ComboBoxItem item)
            GetVm().RowHeightMode = (string)item.Tag;
        UpdateRowHeightControlsVisibility();
    }

    private void RowHeightUnitBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUnitSwitching) return;
        _isUnitSwitching = true;

        var unitLabel = GetUnitLabel(RowHeightUnitBox);
        ApplyConfigForDistance(RowHeightBox, RowHeightUnitBox);

        // Convert stored mm value to the new unit
        RowHeightBox.Value = unitLabel == "厘米" ? _rowHeightMm / 10.0 : _rowHeightMm;

        _isUnitSwitching = false;
    }

    private void CellMarginHUnitBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUnitSwitching) return;
        _isUnitSwitching = true;

        var unitLabel = GetUnitLabel(CellMarginHUnitBox);
        ApplyCellMarginConfig(CellMarginHBox, CellMarginHUnitBox);

        // Convert stored mm value to the new unit
        CellMarginHBox.Value = unitLabel == "厘米" ? _cellMarginHMm / 10.0 : _cellMarginHMm;

        _isUnitSwitching = false;
    }

    private void CellMarginVUnitBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUnitSwitching) return;
        _isUnitSwitching = true;

        var unitLabel = GetUnitLabel(CellMarginVUnitBox);
        ApplyCellMarginConfig(CellMarginVBox, CellMarginVUnitBox);

        // Convert stored mm value to the new unit
        CellMarginVBox.Value = unitLabel == "厘米" ? _cellMarginVMm / 10.0 : _cellMarginVMm;

        _isUnitSwitching = false;
    }

    private void RowHeightBox_ValueChanged(object sender, double value)
    {
        if (_isUnitSwitching) return;
        var unitLabel = GetUnitLabel(RowHeightUnitBox);
        _rowHeightMm = unitLabel == "厘米" ? value * 10.0 : value;
        GetVm().RowHeight = _rowHeightMm / 10.0; // ViewModel stores in cm
    }

    // ── Unit helpers ────────────────────────────────────────────────

    private static string GetUnitLabel(ComboBox combo)
    {
        if (combo.SelectedItem is ComboBoxItem item && item.Content is string s)
            return s;
        return "厘米"; // fallback
    }

    private void ApplyConfigForDistance(Controls.NumericTextBox nb, ComboBox unitCombo)
    {
        var unitLabel = GetUnitLabel(unitCombo);
        var unit = unitLabel switch
        {
            "厘米" => "cm",
            _ => "mm",
        };
        nb.ApplyConfig(NumericUnitConfigProvider.GetConfig(unit));
    }

    /// <summary>
    /// Apply CellMargin-specific config: step is always 0.1 mm
    /// (0.01 cm), and decimal places adapt to the current unit.
    /// </summary>
    private void ApplyCellMarginConfig(Controls.NumericTextBox nb, ComboBox unitCombo)
    {
        var unitLabel = GetUnitLabel(unitCombo);
        var config = NumericUnitConfigProvider.GetConfig(unitLabel);
        if (unitLabel == "毫米")
        {
            config = config with { Step = 0.1, DecimalPlaces = 1 };
        }
        else // 厘米
        {
            config = config with { Step = 0.01, DecimalPlaces = 2 };
        }
        nb.ApplyConfig(config);
    }

    private void IndentTypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IndentTypeBox.SelectedItem is ComboBoxItem item)
        {
            GetVm().IndentType = (string)item.Tag;

            // 非"无"缩进时显示数值输入，否则隐藏
            var isIndent = (string)item.Tag != "none";
            IndentOptionsPanel.Visibility = isIndent ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void IndentValueBox_ValueChanged(object sender, double value)
    {
        GetVm().IndentValue = value;
    }

    private void IndentUnitBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var vm = GetVm();
        if (vm is null) return;
        if (IndentUnitBox.SelectedItem is ComboBoxItem item)
            vm.IndentUnit = (string)item.Content;
    }

    private void FontBoldCheck_Changed(object sender, RoutedEventArgs e)
    {
        GetVm().FontBold = FontBoldCheck.IsChecked == true;
    }

    private void FontItalicCheck_Changed(object sender, RoutedEventArgs e)
    {
        GetVm().FontItalic = FontItalicCheck.IsChecked == true;
    }

    private void FontUnderlineCheck_Changed(object sender, RoutedEventArgs e)
    {
        GetVm().FontUnderline = FontUnderlineCheck.IsChecked == true;
    }

    private void TableLineSpacingModeBox_Changed(object sender, SelectionChangedEventArgs e)
    {
        var vm = GetVm();
        if (vm == null || _isLoadingFields) return;

        // If triggered by value-box auto-sync, only update mode
        if (_isUpdatingModeFromValue)
        {
            var autoMode = TableLineSpacingModeBox.SelectedIndex switch
            {
                4 => "fixed",
                5 => "at_least",
                _ => "multiple",
            };
            vm.TableLineSpacingMode = autoMode;
            return;
        }

        var idx = TableLineSpacingModeBox.SelectedIndex;
        var (mode, val) = idx switch
        {
            0 => ("multiple", 1.0),
            1 => ("multiple", 1.5),
            2 => ("multiple", 2.0),
            3 => ("multiple", vm.TableLineSpacing),
            4 => ("fixed", 6.0),
            _ => ("at_least", 6.0),
        };
        vm.TableLineSpacingMode = mode;
        vm.TableLineSpacing = val;
        TableLineSpacingBox.Value = val;

        UpdateLineSpacingUnitVisibility();
        ApplyLineSpacingConfig();
    }

    private void TableLineSpacingBox_ValueChanged(object sender, double value)
    {
        var vm = GetVm();
        if (vm == null || _isLoadingFields) return;
        vm.TableLineSpacing = value;

        // Auto-sync ComboBox to match value (only when mode is "multiple", idx 0-3)
        if (vm.TableLineSpacingMode == "multiple")
        {
            int newIdx;
            if (Math.Abs(value - 1.0) < 0.01)
                newIdx = 0;
            else if (Math.Abs(value - 1.5) < 0.01)
                newIdx = 1;
            else if (Math.Abs(value - 2.0) < 0.01)
                newIdx = 2;
            else
                newIdx = 3;

            if (newIdx != TableLineSpacingModeBox.SelectedIndex)
            {
                _isUpdatingModeFromValue = true;
                TableLineSpacingModeBox.SelectedIndex = newIdx;
                _isUpdatingModeFromValue = false;
            }
        }
    }

    private void TableLineSpacingUnitBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var vm = GetVm();
        if (vm == null) return;

        var newUnit = TableLineSpacingUnitBox.SelectedIndex switch
        {
            1 => "cm",
            2 => "mm",
            _ => "pt",
        };

        // 先做数值换算（old → new），再更新 unit
        var oldUnit = vm.TableLineSpacingUnit;
        if (!string.IsNullOrEmpty(oldUnit) && oldUnit != newUnit
            && NumericUnitConfigProvider.IsLengthUnit(oldUnit)
            && NumericUnitConfigProvider.IsLengthUnit(newUnit))
        {
            var converted = NumericUnitConfigProvider.ConvertLength(
                TableLineSpacingBox.Value, oldUnit, newUnit);
            TableLineSpacingBox.Value = Math.Round(converted, 1);
        }

        vm.TableLineSpacingUnit = newUnit;
        ApplyLineSpacingConfig();
        UpdateLineSpacingUnitVisibility();
    }

    private void UpdateLineSpacingUnitVisibility()
    {
        // 单位下拉框仅在 fixed/at_least 模式下可见
        var isDistanceMode = TableLineSpacingModeBox.SelectedItem is ComboBoxItem item &&
                             ((string)item.Tag == "fixed" || (string)item.Tag == "at_least");
        TableLineSpacingUnitBox.Visibility = isDistanceMode ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ApplyLineSpacingConfig()
    {
        var vm = GetVm();
        if (vm == null) return;

        // 参照正文样式 BodyStyleView.ApplyConfigForLineSpacing() 的实现方式，
        // 使用 NumericUnitConfigProvider 中央配置中心获取步进值
        var isDistanceMode = vm.TableLineSpacingMode == "fixed" || vm.TableLineSpacingMode == "at_least";
        var unit = isDistanceMode ? vm.TableLineSpacingUnit : "行";
        TableLineSpacingBox.ApplyConfig(NumericUnitConfigProvider.GetConfig(unit));
    }

    private void AutoSplitCheck_Changed(object sender, RoutedEventArgs e)
    {
        GetVm().AutoSplit = AutoSplitCheck.IsChecked == true;
    }

    private void RepeatHeaderCheck_Changed(object sender, RoutedEventArgs e)
    {
        GetVm().RepeatHeader = RepeatHeaderCheck.IsChecked == true;
    }
}