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
    private double _cellMarginMm = 1.9;  // canonical value in mm (0.19 cm → 1.9 mm)
    private bool _isUnitSwitching;

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

        AutoSplitCheck.IsChecked = vm.AutoSplit;
        RepeatHeaderCheck.IsChecked = vm.RepeatHeader;

        // Apply distance configs for RowHeightBox and CellMarginBox
        ApplyConfigForDistance(RowHeightBox, RowHeightUnitBox);
        ApplyCellMarginConfig();

        // Initialize NumericTextBox values from ViewModel
        BorderWidthBox.Value = vm.BorderWidth;

        var rowHeightUnitLabel = GetUnitLabel(RowHeightUnitBox);
        RowHeightBox.Value = rowHeightUnitLabel == "厘米" ? _rowHeightMm / 10.0 : _rowHeightMm;

        var cellMarginUnitLabel = GetUnitLabel(CellMarginUnitBox);
        CellMarginBox.Value = cellMarginUnitLabel == "厘米" ? _cellMarginMm / 10.0 : _cellMarginMm;
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

    private void CellMarginBox_ValueChanged(object sender, double value)
    {
        if (_isUnitSwitching) return;
        var unitLabel = GetUnitLabel(CellMarginUnitBox);
        _cellMarginMm = unitLabel == "厘米" ? value * 10.0 : value;
        GetVm().CellMargin = _cellMarginMm / 10.0; // ViewModel stores in cm
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

    private void CellMarginUnitBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUnitSwitching) return;
        _isUnitSwitching = true;

        var unitLabel = GetUnitLabel(CellMarginUnitBox);
        ApplyCellMarginConfig();

        // Convert stored mm value to the new unit
        CellMarginBox.Value = unitLabel == "厘米" ? _cellMarginMm / 10.0 : _cellMarginMm;

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
    private void ApplyCellMarginConfig()
    {
        var unitLabel = GetUnitLabel(CellMarginUnitBox);
        var config = NumericUnitConfigProvider.GetConfig(unitLabel);
        if (unitLabel == "毫米")
        {
            config = config with { Step = 0.1, DecimalPlaces = 1 };
        }
        else // 厘米
        {
            config = config with { Step = 0.01, DecimalPlaces = 2 };
        }
        CellMarginBox.ApplyConfig(config);
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

    private void AutoSplitCheck_Changed(object sender, RoutedEventArgs e)
    {
        GetVm().AutoSplit = AutoSplitCheck.IsChecked == true;
    }

    private void RepeatHeaderCheck_Changed(object sender, RoutedEventArgs e)
    {
        GetVm().RepeatHeader = RepeatHeaderCheck.IsChecked == true;
    }
}