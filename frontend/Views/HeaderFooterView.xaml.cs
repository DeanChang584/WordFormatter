using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WordFormatterUI.Controls;
using WordFormatterUI.Models;
using WordFormatterUI.Services;
using WordFormatterUI.Utilities;
using WordFormatterUI.ViewModels;

namespace WordFormatterUI.Views;

/// <summary>
/// Page header & footer settings (design-document §9.2).
///
/// Layout and controls aligned with BodyStyleView conventions:
///   • Font family (CN / EN — ComboBox)
///   • Font size   (ComboBox with Chinese size names, 初号→小六)
///   • Font style  (Bold / Italic / Underline / Strikethrough — CheckBoxes)
///   • Alignment   (left / center / right — ComboBox)
///   • Header distance from top    (NumericTextBox, mm base, cm/mm unit switch)
///   • Footer distance from bottom (NumericTextBox, mm base, cm/mm unit switch)
///
/// Binds to <see cref="HeaderFooterViewModel"/>.
/// </summary>
public sealed partial class HeaderFooterView : UserControl
{
    public HeaderFooterView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    // ── Source-of-truth (all values in mm) ────────────────────────────
    // Stored so switching between mm/cm is lossless.
    private double _headerDistanceMm;
    private double _footerDistanceMm;

    // Guard against re-entrant ValueChanged / SelectionChanged during unit switch
    private bool _isUnitSwitching;

    // ── Load ───────────────────────────────────────────────────────────

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Populate alignment combo (labels aligned with BodyStyleView)
        AlignmentBox.Items.Clear();
        foreach (var align in new[] { "左对齐", "居中对齐", "右对齐" })
            AlignmentBox.Items.Add(align);

        // Populate Chinese font combo
        FontCnBox.Items.Clear();
        foreach (var font in new[] { "等线", "宋体", "新宋体", "华文宋体", "华文中宋", "思源宋体", "仿宋", "仿宋_GB2312", "楷体", "楷体_GB2312", "黑体", "微软雅黑", "思源黑体", "方正小标宋简体", "方正小标宋_GBK" })
            FontCnBox.Items.Add(font);

        // Populate Western font combo
        FontEnBox.Items.Clear();
        foreach (var font in new[] { "Arial", "Calibri", "Cambria", "Century Gothic", "Consolas", "Courier New", "Georgia", "Helvetica", "Noto Sans Latin", "Noto Serif Latin", "Tahoma", "Times New Roman", "Verdana" })
            FontEnBox.Items.Add(font);

        // Populate font size ComboBox (初号 → 小六, matching BodyStyleView)
        FontSizeBox.Items.Clear();
        foreach (var def in FontSizeDefinition.DefaultSet)
            FontSizeBox.Items.Add(def.Name);

        var vm = GetVm();
        if (vm == null) return;

        // Store mm values from ViewModel
        _headerDistanceMm = vm.HfHeaderDistance;
        _footerDistanceMm = vm.HfFooterDistance;

        PushFieldsToUI(vm);

        // Apply per-field config (both start in mm)
        ApplyConfigForDistance(HeaderDistBox, HeaderDistUnitBox);
        ApplyConfigForDistance(FooterDistBox, FooterDistUnitBox);

        // Override step to 0.5 for header/footer distance controls
        HeaderDistBox.Step = 0.5;
        FooterDistBox.Step = 0.5;
    }

    /// <summary>
    /// Re-read all values from the ViewModel and push them to the UI controls.
    /// Called after profile reset / template apply / history reuse.
    /// </summary>
    public void RefreshUI()
    {
        var vm = GetVm();
        if (vm == null) return;

        _headerDistanceMm = vm.HfHeaderDistance;
        _footerDistanceMm = vm.HfFooterDistance;

        PushFieldsToUI(vm);
    }

    private void PushFieldsToUI(HeaderFooterViewModel vm)
    {
        SelectComboBoxItem(FontCnBox, vm.HfFontCn);
        SelectComboBoxItem(FontEnBox, vm.HfFontEn);

        // Font size: pt value → Chinese name → select ComboBox item
        var converter = new FontSizeConverter();
        var sizeName = converter.Format(vm.HfFontSize);
        SelectComboBoxItem(FontSizeBox, sizeName);

        AlignmentBox.SelectedIndex = vm.HfAlignment switch
        {
            "left"   => 0,
            "right"  => 2,
            _        => 1, // center
        };

        // Font style checkboxes (bold / italic / underline / strikethrough)
        var style = vm.HfFontStyle?.ToLowerInvariant() ?? "normal";
        BoldCheck.IsChecked          = style.Contains("bold");
        ItalicCheck.IsChecked        = style.Contains("italic");
        UnderlineCheck.IsChecked     = style.Contains("underline");
        StrikethroughCheck.IsChecked = style.Contains("strikethrough") || style.Contains("line-through");

        // Header/footer distances: push mm values to UI in current unit
        _isUnitSwitching = true;

        var headerUnitLabel = ComboSelectedString(HeaderDistUnitBox, "毫米");
        var footerUnitLabel = ComboSelectedString(FooterDistUnitBox, "毫米");
        HeaderDistBox.Value = headerUnitLabel == "厘米" ? _headerDistanceMm / 10.0 : _headerDistanceMm;
        FooterDistBox.Value = footerUnitLabel == "厘米" ? _footerDistanceMm / 10.0 : _footerDistanceMm;

        _isUnitSwitching = false;
    }

    // ── Font family ────────────────────────────────────────────────────

    private void FontCnBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FontCnBox.SelectedItem is string font)
        {
            var vm = GetVm();
            if (vm != null) vm.HfFontCn = font;
        }
    }

    private void FontEnBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FontEnBox.SelectedItem is string font)
        {
            var vm = GetVm();
            if (vm != null) vm.HfFontEn = font;
        }
    }

    // ── Font size (ComboBox with Chinese size names) ───────────────────

    private void FontSizeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FontSizeBox.SelectedItem is string name)
        {
            var converter = new FontSizeConverter();
            var pt = converter.Parse(name);
            if (pt.HasValue)
            {
                var vm = GetVm();
                if (vm != null) vm.HfFontSize = pt.Value;
            }
        }
    }

    // ── Font style (bold / italic / underline / strikethrough) ─────────

    private void FontStyleCheck_Changed(object sender, RoutedEventArgs e)
    {
        var vm = GetVm();
        if (vm == null) return;

        // Build a combined style string: "bold", "italic", "bold italic", etc.
        var parts = new List<string>();
        if (BoldCheck.IsChecked == true)          parts.Add("bold");
        if (ItalicCheck.IsChecked == true)        parts.Add("italic");
        if (UnderlineCheck.IsChecked == true)     parts.Add("underline");
        if (StrikethroughCheck.IsChecked == true) parts.Add("strikethrough");

        vm.HfFontStyle = parts.Count > 0 ? string.Join(" ", parts) : "normal";
    }

    // ── Alignment ──────────────────────────────────────────────────────

    private void AlignmentBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var vm = GetVm();
        if (vm == null) return;

        vm.HfAlignment = AlignmentBox.SelectedIndex switch
        {
            0 => "left",
            2 => "right",
            _ => "center",
        };
    }

    // ── Distances: value changed (user edits the NumericTextBox) ────────

    private void HeaderDistBox_ValueChanged(object sender, double newValue)
    {
        if (_isUnitSwitching) return;

        // Convert displayed value back to mm
        var unitLabel = ComboSelectedString(HeaderDistUnitBox, "毫米");
        double newMm = unitLabel == "厘米" ? newValue * 10.0 : newValue;
        _headerDistanceMm = newMm;

        var vm = GetVm();
        if (vm != null) vm.HfHeaderDistance = newMm;
    }

    private void FooterDistBox_ValueChanged(object sender, double newValue)
    {
        if (_isUnitSwitching) return;

        // Convert displayed value back to mm
        var unitLabel = ComboSelectedString(FooterDistUnitBox, "毫米");
        double newMm = unitLabel == "厘米" ? newValue * 10.0 : newValue;
        _footerDistanceMm = newMm;

        var vm = GetVm();
        if (vm != null) vm.HfFooterDistance = newMm;
    }

    // ── Distances: unit changed (mm ↔ cm) ──────────────────────────────

    private void HeaderDistUnitBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUnitSwitching) return;
        _isUnitSwitching = true;

        var unitLabel = ComboSelectedString(HeaderDistUnitBox, "毫米");

        // 1. Update DecimalPlaces/Step/etc via ApplyConfig
        ApplyConfigForDistance(HeaderDistBox, HeaderDistUnitBox);

        // 2. Override step: 0.5mm (或 0.05cm)
        HeaderDistBox.Step = unitLabel == "厘米" ? 0.05 : 0.5;

        // 3. Convert the stored mm value to the new unit
        if (unitLabel == "厘米")
            HeaderDistBox.Value = _headerDistanceMm / 10.0;
        else
            HeaderDistBox.Value = _headerDistanceMm;

        _isUnitSwitching = false;
    }

    private void FooterDistUnitBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUnitSwitching) return;
        _isUnitSwitching = true;

        var unitLabel = ComboSelectedString(FooterDistUnitBox, "毫米");

        // 1. Update DecimalPlaces/Step/etc via ApplyConfig
        ApplyConfigForDistance(FooterDistBox, FooterDistUnitBox);

        // 2. Override step: 0.5mm (或 0.05cm)
        FooterDistBox.Step = unitLabel == "厘米" ? 0.05 : 0.5;

        // 3. Convert the stored mm value to the new unit
        if (unitLabel == "厘米")
            FooterDistBox.Value = _footerDistanceMm / 10.0;
        else
            FooterDistBox.Value = _footerDistanceMm;

        _isUnitSwitching = false;
    }

    // ── Apply config for distance NumericTextBox ───────────────────────

    private void ApplyConfigForDistance(NumericTextBox nb, ComboBox unitCombo)
    {
        var unitLabel = ComboSelectedString(unitCombo, "毫米");
        var unit = unitLabel switch
        {
            "厘米" => "cm",
            _ => "mm",
        };
        nb.ApplyConfig(NumericUnitConfigProvider.GetConfig(unit));
    }

    // ── Helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Get the selected string from a plain-item ComboBox, or return <paramref name="fallback"/>.
    /// </summary>
    private static string ComboSelectedString(ComboBox combo, string fallback)
    {
        return combo.SelectedItem is string s ? s : (combo.SelectedItem is ComboBoxItem cbi ? cbi.Content as string ?? fallback : fallback);
    }

    private static void SelectComboBoxItem(ComboBox combo, string value)
    {
        for (int i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is string s && s == value)
            {
                combo.SelectedIndex = i;
                return;
            }
        }
        combo.SelectedIndex = -1;
    }

    private HeaderFooterViewModel? GetVm()
    {
        if (ViewRoot.DataContext is HeaderFooterViewModel direct)
            return direct;

        DependencyObject? current = ViewRoot;
        while (current != null)
        {
            if (current is FrameworkElement fe && fe.DataContext is HeaderFooterViewModel vm)
                return vm;
            current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}