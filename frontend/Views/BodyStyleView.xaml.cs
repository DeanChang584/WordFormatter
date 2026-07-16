using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WordFormatterUI.Controls;
using WordFormatterUI.Models;
using WordFormatterUI.Services;
using WordFormatterUI.Utilities;
using WordFormatterUI.ViewModels;

namespace WordFormatterUI.Views;

/// <summary>
/// Body / paragraph style settings (design-document §9.3).
///
/// Fields:
///   • Font family   (CN / EN — TextBox)
///   • Font size     (NumberBox, pt)
///   • Font style    (Bold / Italic / Underline / Strikethrough — CheckBoxes)
///   • Alignment     (justify / left / center / right — ComboBox)
///   • Line spacing  (mode ComboBox + value NumberBox, pt)
///   • Space before  (NumberBox, pt)
///   • Space after   (NumberBox, pt)
///   • Indent        (NumberBox, characters)
///
/// Binds to <see cref="BodyStyleViewModel"/> body-style properties.
/// </summary>
public sealed partial class BodyStyleView : UserControl
{
    // Guard against firing ViewModel updates during PushFieldsToUI
    private bool _isLoadingFields;

    // Guard against recursive loop when ValueChanged updates the mode ComboBox
    private bool _isUpdatingModeFromValue;

    // ── Mode memory (remember last value per mode) ────────────────

    private readonly Dictionary<string, double> _lastLineSpacingByMode = new()
    {
        ["multiple"] = 1.5,
        ["fixed"]    = 6.0,
        ["at_least"] = 6.0,
    };

    /// <summary>
    /// Remember the *display* value for each unit combination.
    /// Key = "spaceBefore:{unit}", "spaceAfter:{unit}".
    /// </summary>
    private readonly Dictionary<string, double> _lastSpaceByUnit = new()
    {
        ["spaceBefore:行"] = 1.0,
        ["spaceBefore:pt"] = 6.0,
        ["spaceBefore:cm"] = 0.5,
        ["spaceBefore:mm"] = 5.0,
        ["spaceAfter:行"]  = 0.5,
        ["spaceAfter:pt"]  = 6.0,
        ["spaceAfter:cm"]  = 0.5,
        ["spaceAfter:mm"]  = 5.0,
    };

    // ── Constructor ────────────────────────────────────────────────────

    public BodyStyleView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    // ── Load ───────────────────────────────────────────────────────────

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Populate alignment combo
        AlignmentBox.Items.Clear();
        foreach (var align in new[] { "左对齐", "居中对齐", "右对齐", "两端对齐" })
            AlignmentBox.Items.Add(align);

        // Populate Chinese font combo
        FontCnBox.Items.Clear();
        foreach (var font in new[] { "等线", "宋体", "新宋体", "华文宋体", "华文中宋", "思源宋体", "仿宋", "仿宋_GB2312", "楷体", "楷体_GB2312", "黑体", "微软雅黑", "思源黑体", "方正小标宋简体", "方正小标宋_GBK" })
            FontCnBox.Items.Add(font);

        // Populate Western font combo
        FontEnBox.Items.Clear();
        foreach (var font in new[] { "Arial", "Calibri", "Cambria", "Century Gothic", "Consolas", "Courier New", "Georgia", "Helvetica", "Noto Sans Latin", "Noto Serif Latin", "Tahoma", "Times New Roman", "Verdana" })
            FontEnBox.Items.Add(font);

        // Populate font size ComboBox (初号 → 小六)
        FontSizeBox.Items.Clear();
        foreach (var def in FontSizeDefinition.DefaultSet)
            FontSizeBox.Items.Add(def.Name);

        var vm = GetVm();
        if (vm == null) return;

        PushFieldsToUI(vm);
    }

    /// <summary>
    /// Re-read all values from the ViewModel and push them to the UI controls.
    /// Called after profile reset / template apply / history reuse.
    /// </summary>
    public void RefreshUI()
    {
        var vm = GetVm();
        if (vm == null) return;
        PushFieldsToUI(vm);
    }

    private void PushFieldsToUI(BodyStyleViewModel vm)
    {
        _isLoadingFields = true;

        // Map ViewModel → UI
        SelectComboBoxItem(FontCnBox, vm.BodyFontCn);
        SelectComboBoxItem(FontEnBox, vm.BodyFontEn);
        // Font size: pt value → Chinese name → select ComboBox item
        var converter = new FontSizeConverter();
        var sizeName = converter.Format(vm.BodyFontSize);
        SelectComboBoxItem(FontSizeBox, sizeName);
        AlignmentBox.SelectedIndex = vm.BodyAlignment switch
        {
            "left"    => 0,
            "center"  => 1,
            "right"   => 2,
            _         => 3, // justify
        };

        // Font style checkboxes
        var style = vm.BodyFontStyle?.ToLowerInvariant() ?? "normal";
        BoldCheck.IsChecked          = style.Contains("bold");
        ItalicCheck.IsChecked        = style.Contains("italic");
        UnderlineCheck.IsChecked     = style.Contains("underline");
        StrikethroughCheck.IsChecked = style.Contains("strikethrough") || style.Contains("line-through");

        // Apply config BEFORE setting value so DecimalPlaces/Step are correct
        ApplyConfigForLineSpacing();

        // Line spacing
        var mode = vm.LineSpacingMode ?? "multiple";
        LineSpacingModeBox.SelectedIndex = mode switch
        {
            "fixed"    => 4,
            "at_least" => 5,
            _          => vm.LineSpacing switch
            {
                1.0 => 0,
                1.5 => 1,
                2.0 => 2,
                _   => 3, // 多倍行距 (other values)
            },
        };
        LineSpacingValueBox.Value = vm.LineSpacing;
        SelectComboBoxItem(LineSpacingUnitBox, vm.LineSpacingUnit);

        // Apply config BEFORE setting value so DecimalPlaces/Step are correct
        ApplyConfigForSpaceBefore();
        ApplyConfigForSpaceAfter();
        ApplyConfigForSpecialIndent();

        // Paragraph spacing
        SpaceBeforeBox.Value = vm.SpaceBefore;
        SpaceAfterBox.Value  = vm.SpaceAfter;

        // Special indent
        BodySpecialIndentBox.SelectedIndex = vm.IndentType switch
        {
            "firstLine" => 0,
            "hanging"   => 2,
            _           => 1, // none
        };
        BodySpecialIndentValueBox.Value = vm.IndentValue;
        var indentUnitIndex = Array.IndexOf(new[] { "字符", "磅", "厘米", "毫米" }, vm.IndentUnit);
        BodySpecialIndentUnitBox.SelectedIndex = indentUnitIndex >= 0 ? indentUnitIndex : 0;
        UpdateBodySpecialIndentVisibility();

        // Spacing units
        var unitIndex = Array.IndexOf(new[] { "行", "pt", "cm", "mm" }, vm.SpaceBeforeUnit);
        SpaceBeforeUnitBox.SelectedIndex = unitIndex >= 0 ? unitIndex : 0;
        unitIndex = Array.IndexOf(new[] { "行", "pt", "cm", "mm" }, vm.SpaceAfterUnit);
        SpaceAfterUnitBox.SelectedIndex = unitIndex >= 0 ? unitIndex : 0;

        UpdateLineSpacingUI();

        _isLoadingFields = false;
    }

    // ── Font family ────────────────────────────────────────────────────

    private void FontCnBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FontCnBox.SelectedItem is string font)
        {
            var vm = GetVm();
            if (vm != null) vm.BodyFontCn = font;
        }
    }

    private void FontEnBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FontEnBox.SelectedItem is string font)
        {
            var vm = GetVm();
            if (vm != null) vm.BodyFontEn = font;
        }
    }

    // ── Font size ──────────────────────────────────────────────────────

    private void FontSizeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FontSizeBox.SelectedItem is string name)
        {
            var converter = new FontSizeConverter();
            var pt = converter.Parse(name);
            if (pt.HasValue)
            {
                var vm = GetVm();
                if (vm != null) vm.BodyFontSize = pt.Value;
            }
        }
    }

    // ── Font style (bold/italic/underline/strikethrough) ───────────────

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

        vm.BodyFontStyle = parts.Count > 0 ? string.Join(" ", parts) : "normal";
    }

    // ── Alignment ──────────────────────────────────────────────────────

    private void AlignmentBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var vm = GetVm();
        if (vm == null) return;

        vm.BodyAlignment = AlignmentBox.SelectedIndex switch
        {
            0 => "left",
            1 => "center",
            2 => "right",
            _ => "justify",
        };
    }

    // ── Line spacing ───────────────────────────────────────────────────

    private void LineSpacingModeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var vm = GetVm();
        if (vm == null) return;
        if (_isLoadingFields) return;

        // If this change was triggered by ValueChanged auto-sync, only update
        // mode/UI without overwriting the value the user just typed.
        if (_isUpdatingModeFromValue)
        {
            var autoMode = LineSpacingModeBox.SelectedIndex switch
            {
                4 => "fixed",
                5 => "at_least",
                _ => "multiple",
            };
            vm.LineSpacingMode = autoMode;

            if (autoMode == "fixed" || autoMode == "at_least")
            {
                LineSpacingUnitBox.SelectedIndex = 0; // pt
                vm.LineSpacingUnit = "pt";
            }

            ApplyConfigForLineSpacing();
            UpdateLineSpacingUI();
            return;
        }

        // Determine the mode from the selected index
        // Index 0-3 are all "multiple" (单倍行距, 1.5倍行距, 2倍行距, 多倍行距)
        // Index 4 = "fixed" (固定值), Index 5 = "at_least" (最小值)
        var mode = LineSpacingModeBox.SelectedIndex switch
        {
            0 => "multiple",
            1 => "multiple",
            2 => "multiple",
            3 => "multiple",
            4 => "fixed",
            5 => "at_least",
            _ => "multiple",
        };

        // Save current value for the previous mode before switching
        var prevMode = vm.LineSpacingMode ?? "multiple";
        _lastLineSpacingByMode[prevMode] = LineSpacingValueBox.Value;

        // Apply config BEFORE setting value so DecimalPlaces/Step are correct
        ApplyConfigForLineSpacing();

        // For indices 0-2, set fixed common values; for index 3, use the remembered value
        if (LineSpacingModeBox.SelectedIndex == 0)
        {
            // 单倍行距
            vm.LineSpacing = 1.0;
            LineSpacingValueBox.Value = 1.0;
        }
        else if (LineSpacingModeBox.SelectedIndex == 1)
        {
            // 1.5 倍行距
            vm.LineSpacing = 1.5;
            LineSpacingValueBox.Value = 1.5;
        }
        else if (LineSpacingModeBox.SelectedIndex == 2)
        {
            // 2 倍行距
            vm.LineSpacing = 2.0;
            LineSpacingValueBox.Value = 2.0;
        }
        else
        {
            // 多倍行距 (index 3) or fixed / at_least → use remembered value
            vm.LineSpacing = _lastLineSpacingByMode[mode];
            LineSpacingValueBox.Value = _lastLineSpacingByMode[mode];
        }

        vm.LineSpacingMode = mode;

        if (mode == "fixed" || mode == "at_least")
        {
            LineSpacingUnitBox.SelectedIndex = 0; // pt
            vm.LineSpacingUnit = "pt";
        }

        UpdateLineSpacingUI();
    }

    private void LineSpacingValueBox_ValueChanged(object sender, double newValue)
    {
        if (_isLoadingFields) return;
        var vm = GetVm();
        if (vm != null) vm.LineSpacing = newValue;

        // Auto-sync the mode ComboBox to match the current value
        // (only when current mode is "multiple", i.e. SelectedIndex 0-3)
        if (LineSpacingModeBox.SelectedIndex < 4)
        {
            var newIndex = newValue switch
            {
                1.0 => 0,  // 单倍行距
                1.5 => 1,  // 1.5 倍行距
                2.0 => 2,  // 2 倍行距
                _   => 3,  // 多倍行距（其他值）
            };
            if (newIndex != LineSpacingModeBox.SelectedIndex)
            {
                _isUpdatingModeFromValue = true;
                LineSpacingModeBox.SelectedIndex = newIndex;
                _isUpdatingModeFromValue = false;
            }
        }
    }

    private void UpdateLineSpacingUI()
    {
        bool showUnit = LineSpacingModeBox.SelectedIndex >= 4; // fixed or at_least
        LineSpacingUnitBox.Visibility = showUnit ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ApplyConfigForLineSpacing()
    {
        var mode = (LineSpacingModeBox.SelectedIndex) switch
        {
            4 or 5 => "fixed",  // fixed or at_least → length units
            _      => "multiple",
        };

        if (mode == "multiple")
        {
            LineSpacingValueBox.ApplyConfig(NumericUnitConfigProvider.GetConfig("行"));
        }
        else
        {
            var unit = LineSpacingUnitBox.SelectedItem is ComboBoxItem ci
                ? ci.Tag?.ToString() ?? "pt" : "pt";
            LineSpacingValueBox.ApplyConfig(NumericUnitConfigProvider.GetConfig(unit));
        }
    }

    private void LineSpacingUnitBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var vm = GetVm();
        if (vm == null) return;

        var units = new[] { "pt", "cm", "mm" };
        if (LineSpacingUnitBox.SelectedIndex >= 0 && LineSpacingUnitBox.SelectedIndex < units.Length)
        {
            var newUnit = units[LineSpacingUnitBox.SelectedIndex];

            // 1. First update DecimalPlaces/Step/etc
            ApplyConfigForLineSpacing();

            // 2. Then set the value — OnValueChanged will use the correct DecimalPlaces
            var oldUnit = vm.LineSpacingUnit;
            if (!string.IsNullOrEmpty(oldUnit) && oldUnit != newUnit
                && NumericUnitConfigProvider.IsLengthUnit(oldUnit)
                && NumericUnitConfigProvider.IsLengthUnit(newUnit))
            {
                var converted = NumericUnitConfigProvider.ConvertLength(
                    LineSpacingValueBox.Value, oldUnit, newUnit);
                LineSpacingValueBox.Value = Math.Round(converted, 1);
            }

            vm.LineSpacingUnit = newUnit;
        }
    }

    // ── Paragraph spacing ──────────────────────────────────────────────

    private void SpaceBeforeBox_ValueChanged(object sender, double newValue)
    {
        var vm = GetVm();
        if (vm != null) vm.SpaceBefore = newValue;
    }

    private void SpaceAfterBox_ValueChanged(object sender, double newValue)
    {
        var vm = GetVm();
        if (vm != null) vm.SpaceAfter = newValue;
    }

    private void SpaceBeforeUnitBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var vm = GetVm();
        if (vm == null) return;

        var units = new[] { "行", "pt", "cm", "mm" };
        if (SpaceBeforeUnitBox.SelectedIndex >= 0 && SpaceBeforeUnitBox.SelectedIndex < units.Length)
        {
            var newUnit = units[SpaceBeforeUnitBox.SelectedIndex];

            // Save current value for old unit
            var oldUnit = vm.SpaceBeforeUnit ?? "行";
            _lastSpaceByUnit[$"spaceBefore:{oldUnit}"] = SpaceBeforeBox.Value;

            // 1. First update DecimalPlaces/Step/etc
            ApplyConfigForSpaceBefore();

            // 2. Then set the value from remembered value (no conversion)
            SpaceBeforeBox.Value = _lastSpaceByUnit.GetValueOrDefault(
                $"spaceBefore:{newUnit}", 1.0);

            vm.SpaceBeforeUnit = newUnit;
        }
    }

    private void ApplyConfigForSpaceBefore()
    {
        var unit = vmUnitValue(SpaceBeforeUnitBox, "行");
        SpaceBeforeBox.ApplyConfig(NumericUnitConfigProvider.GetConfig(unit));
    }

    private void SpaceAfterUnitBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var vm = GetVm();
        if (vm == null) return;

        var units = new[] { "行", "pt", "cm", "mm" };
        if (SpaceAfterUnitBox.SelectedIndex >= 0 && SpaceAfterUnitBox.SelectedIndex < units.Length)
        {
            var newUnit = units[SpaceAfterUnitBox.SelectedIndex];

            var oldUnit = vm.SpaceAfterUnit ?? "行";
            _lastSpaceByUnit[$"spaceAfter:{oldUnit}"] = SpaceAfterBox.Value;

            // 1. First update DecimalPlaces/Step/etc
            ApplyConfigForSpaceAfter();

            // 2. Then set the value from remembered value (no conversion)
            SpaceAfterBox.Value = _lastSpaceByUnit.GetValueOrDefault(
                $"spaceAfter:{newUnit}", 0.5);

            vm.SpaceAfterUnit = newUnit;
        }
    }

    private void ApplyConfigForSpaceAfter()
    {
        var unit = vmUnitValue(SpaceAfterUnitBox, "行");
        SpaceAfterBox.ApplyConfig(NumericUnitConfigProvider.GetConfig(unit));
    }

    // ── Special Indent ────────────────────────────────────────────────

    private void BodySpecialIndentBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var vm = GetVm();
        if (vm == null) return;

        if (BodySpecialIndentBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            vm.IndentType = tag;
            UpdateBodySpecialIndentVisibility();
        }
    }

    private void BodySpecialIndentValueBox_ValueChanged(object sender, double newValue)
    {
        var vm = GetVm();
        if (vm != null) vm.IndentValue = newValue;
    }

    private void BodySpecialIndentUnitBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var vm = GetVm();
        if (vm == null) return;

        if (BodySpecialIndentUnitBox.SelectedItem is string unit)
        {
            var oldUnit = vm.IndentUnit ?? "字符";

            // 1. First update DecimalPlaces/Step/etc
            ApplyConfigForSpecialIndent();

            // 2. Then set the value — OnValueChanged will use the correct DecimalPlaces
            if (NumericUnitConfigProvider.IsLengthUnit(oldUnit) && NumericUnitConfigProvider.IsLengthUnit(unit))
            {
                var converted = NumericUnitConfigProvider.ConvertLength(
                    BodySpecialIndentValueBox.Value, oldUnit, unit);
                BodySpecialIndentValueBox.Value = Math.Round(converted, 1);
            }

            vm.IndentUnit = unit;
        }
    }

    private void ApplyConfigForSpecialIndent()
    {
        var unit = vmUnitValue(BodySpecialIndentUnitBox, "字符");
        BodySpecialIndentValueBox.ApplyConfig(NumericUnitConfigProvider.GetConfig(unit));
    }

    private void UpdateBodySpecialIndentVisibility()
    {
        bool show = BodySpecialIndentBox.SelectedIndex != 1; // none → hide
        BodySpecialIndentValueBox.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        BodySpecialIndentUnitBox.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
    }

    // ── Helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Extract the canonical unit string from a ComboBox that uses
    /// ComboBoxItem content (e.g. "行", "磅", "厘米", "毫米").
    /// Falls back to <paramref name="fallback"/> if selection is invalid.
    /// </summary>
    private static string vmUnitValue(ComboBox combo, string fallback)
    {
        if (combo.SelectedItem is ComboBoxItem ci && ci.Content is string s)
            return NumericUnitConfigProvider.GetConfig(s) != null
                ? s
                : fallback;
        return fallback;
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
        // If not found, set text manually via the editable selection
        combo.SelectedIndex = -1;
    }

    private BodyStyleViewModel? GetVm()
    {
        if (ViewRoot.DataContext is BodyStyleViewModel direct)
            return direct;

        DependencyObject? current = ViewRoot;
        while (current != null)
        {
            if (current is FrameworkElement fe && fe.DataContext is BodyStyleViewModel vm)
                return vm;
            current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}