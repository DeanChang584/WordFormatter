using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WordFormatterUI.Controls;
using WordFormatterUI.Models;
using WordFormatterUI.Services;
using WordFormatterUI.Utilities;
using WordFormatterUI.ViewModels;

namespace WordFormatterUI.Views;

/// <summary>
/// Per-level heading style configuration (design-document §9.4).
///
/// Features:
///   • Level selector (标题一…标题六) — maps to HeadingStyleConfig levels 1-6
///   • Font family / size / style / alignment per level
///   • Paragraph spacing (before / after) and line spacing per level
///   • Special indent type (first-line / none / hanging) with value input
///   • ViewModel auto-saves current level fields when any field changes
///   • ViewModel auto-loads new level fields when CurrentHeadingLevel changes
///
/// Binds to <see cref="HeadingStyleViewModel"/> heading properties.
/// </summary>
public sealed partial class HeadingStyleView : UserControl
{
    // Guard against firing ViewModel updates during level-load
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

    // Mode-memory: remember last value per unit for space before/after
    private readonly Dictionary<string, double> _lastSpaceByUnit = new()
    {
        ["sb:行"] = 1.0,
        ["sb:pt"] = 12.0,
        ["sb:cm"] = 0.5,
        ["sb:mm"] = 5.0,
        ["sa:行"] = 0.5,
        ["sa:pt"] = 6.0,
        ["sa:cm"] = 0.5,
        ["sa:mm"] = 5.0,
    };

    // ── Constructor ────────────────────────────────────────────────────

    public HeadingStyleView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    // ── Load ───────────────────────────────────────────────────────────

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Populate level combo
        LevelBox.Items.Clear();
        for (int i = 1; i <= 6; i++)
            LevelBox.Items.Add($"标题{_LevelName(i)}");

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

        // Select current level
        LevelBox.SelectedIndex = vm.CurrentHeadingLevel - 1;

        // Push heading fields to UI
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

    // ── Level selector ─────────────────────────────────────────────────

    private void LevelBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var vm = GetVm();
        if (vm == null || LevelBox.SelectedIndex < 0) return;

        // Setting CurrentHeadingLevel triggers LoadHeadingFields in the ViewModel
        vm.CurrentHeadingLevel = LevelBox.SelectedIndex + 1;

        // Re-push the newly-loaded fields to UI
        PushFieldsToUI(vm);
    }

    // ── Field changed handlers ─────────────────────────────────────────

    private void FontCnBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingFields) return;
        if (FontCnBox.SelectedItem is string font)
        {
            var vm = GetVm();
            if (vm != null) vm.HeadingFontCn = font;
        }
    }

    private void FontEnBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingFields) return;
        if (FontEnBox.SelectedItem is string font)
        {
            var vm = GetVm();
            if (vm != null) vm.HeadingFontEn = font;
        }
    }

    private void Field_ValueChanged(object sender, double newValue)
    {
        if (_isLoadingFields) return;
        var vm = GetVm();
        if (vm == null) return;

        if (sender == SpaceBeforeBox) vm.HeadingSpaceBefore = newValue;
        else if (sender == SpaceAfterBox)  vm.HeadingSpaceAfter = newValue;
    }

    private void FontSizeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingFields) return;
        if (FontSizeBox.SelectedItem is string name)
        {
            var converter = new FontSizeConverter();
            var pt = converter.Parse(name);
            if (pt.HasValue)
            {
                var vm = GetVm();
                if (vm != null) vm.HeadingFontSize = pt.Value;
            }
        }
    }

    private void Field_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingFields) return;
        var vm = GetVm();
        if (vm == null) return;

        if (sender == AlignmentBox)
        {
            vm.HeadingAlignment = AlignmentBox.SelectedIndex switch
            {
                1 => "center",
                2 => "right",
                3 => "justify",
                _ => "left",
            };
        }
    }

    private void FontStyleCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoadingFields) return;
        var vm = GetVm();
        if (vm == null) return;

        var parts = new List<string>();
        if (BoldCheck.IsChecked == true)      parts.Add("bold");
        if (ItalicCheck.IsChecked == true)    parts.Add("italic");
        if (UnderlineCheck.IsChecked == true) parts.Add("underline");

        vm.HeadingFontStyle = parts.Count > 0 ? string.Join(" ", parts) : "normal";
    }

    // ── Special indent handlers ────────────────────────────────────────

    private void SpecialIndentBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingFields) return;
        var vm = GetVm();
        if (vm == null) return;

        if (SpecialIndentBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            vm.HeadingIndentType = tag;
        }

        // Sync the value to UI — ViewModel may have auto-set it (e.g. 0 → 2.0)
        SpecialIndentValueBox.Value = vm.HeadingIndentValue;

        UpdateSpecialIndentVisibility();
    }

    private void SpecialIndentValueBox_ValueChanged(object sender, double newValue)
    {
        if (_isLoadingFields) return;
        var vm = GetVm();
        if (vm == null) return;

        vm.HeadingIndentValue = newValue;
    }

    private void SpecialIndentUnitBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingFields) return;
        var vm = GetVm();
        if (vm == null) return;

        var units = new[] { "字符", "pt", "cm", "mm" };
        if (SpecialIndentUnitBox.SelectedIndex >= 0 && SpecialIndentUnitBox.SelectedIndex < units.Length)
        {
            var newUnit = units[SpecialIndentUnitBox.SelectedIndex];
            var oldUnit = vm.HeadingIndentUnit ?? "字符";

            // 1. First update DecimalPlaces/Step/etc
            ApplyConfigForSpecialIndent();

            // 2. Then set the value — OnValueChanged will use the correct DecimalPlaces
            if (NumericUnitConfigProvider.IsLengthUnit(oldUnit)
                && NumericUnitConfigProvider.IsLengthUnit(newUnit)
                && oldUnit != newUnit)
            {
                var converted = NumericUnitConfigProvider.ConvertLength(
                    SpecialIndentValueBox.Value, oldUnit, newUnit);
                SpecialIndentValueBox.Value = Math.Round(converted, 1);
            }

            vm.HeadingIndentUnit = newUnit;
        }
    }

    private void ApplyConfigForSpecialIndent()
    {
        var unit = ComboSelectedString(SpecialIndentUnitBox, "字符");
        SpecialIndentValueBox.ApplyConfig(NumericUnitConfigProvider.GetConfig(unit));
    }

    private void UpdateSpecialIndentVisibility()
    {
        // Show value box and unit combo only when firstLine or hanging is selected
        var show = SpecialIndentBox.SelectedIndex != 1; // 1 = "无缩进"
        SpecialIndentValueBox.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        SpecialIndentUnitBox.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
    }

    // ── Push ViewModel fields → UI ─────────────────────────────────────

    private void PushFieldsToUI(HeadingStyleViewModel vm)
    {
        _isLoadingFields = true;

        SelectComboBoxItem(FontCnBox, vm.HeadingFontCn);
        SelectComboBoxItem(FontEnBox, vm.HeadingFontEn);
        // Map font size (pt) to Chinese name and select ComboBox item
        var converter = new FontSizeConverter();
        var sizeName = converter.Format(vm.HeadingFontSize);
        SelectComboBoxItem(FontSizeBox, sizeName);

        var style = vm.HeadingFontStyle?.ToLowerInvariant() ?? "normal";
        BoldCheck.IsChecked      = style.Contains("bold");
        ItalicCheck.IsChecked    = style.Contains("italic");
        UnderlineCheck.IsChecked = style.Contains("underline");

        AlignmentBox.SelectedIndex = vm.HeadingAlignment switch
        {
            "center"  => 1,
            "right"   => 2,
            "justify" => 3,
            _         => 0,
        };

        // Apply config BEFORE setting value so DecimalPlaces/Step are correct
        ApplyConfigForLineSpacing();

        // Line spacing
        var mode = vm.HeadingLineSpacingMode ?? "multiple";
        LineSpacingModeBox.SelectedIndex = mode switch
        {
            "fixed"    => 4,
            "at_least" => 5,
            _          => vm.HeadingLineSpacing switch
            {
                1.0 => 0,
                1.5 => 1,
                2.0 => 2,
                _   => 3, // 多倍行距 (other values)
            },
        };
        LineSpacingValueBox.Value = vm.HeadingLineSpacing;
        SelectComboBoxItemByTag(LineSpacingUnitBox, vm.HeadingLineSpacingUnit);

        SpaceBeforeBox.Value = vm.HeadingSpaceBefore;
        SpaceAfterBox.Value  = vm.HeadingSpaceAfter;

        // Spacing units
        var units = new[] { "行", "pt", "cm", "mm" };
        var sbUnitIndex = Array.IndexOf(units, vm.HeadingSpaceBeforeUnit);
        SpaceBeforeUnitBox.SelectedIndex = sbUnitIndex >= 0 ? sbUnitIndex : 0;
        var saUnitIndex = Array.IndexOf(units, vm.HeadingSpaceAfterUnit);
        SpaceAfterUnitBox.SelectedIndex = saUnitIndex >= 0 ? saUnitIndex : 0;

        // Special indent
        SpecialIndentBox.SelectedIndex = vm.HeadingIndentType switch
        {
            "firstLine" => 0,
            "hanging"   => 2,
            _           => 1, // "none"
        };
        SpecialIndentValueBox.Value = vm.HeadingIndentValue;
        var unitLabels = new[] { "字符", "pt", "cm", "mm" };
        var indentUnitIdx = Array.IndexOf(unitLabels, vm.HeadingIndentUnit);
        SpecialIndentUnitBox.SelectedIndex = indentUnitIdx >= 0 ? indentUnitIdx : 0;
        UpdateSpecialIndentVisibility();

        // Apply per-unit config
        ApplyConfigForSpaceBefore();
        ApplyConfigForSpaceAfter();
        ApplyConfigForSpecialIndent();
        ApplyConfigForLineSpacing();

        _isLoadingFields = false;
    }

    // ── Space Before / After unit handlers ─────────────────────────────

    private void SpaceBeforeUnitBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingFields) return;
        var vm = GetVm();
        if (vm == null) return;

        var units = new[] { "行", "pt", "cm", "mm" };
        if (SpaceBeforeUnitBox.SelectedIndex >= 0 && SpaceBeforeUnitBox.SelectedIndex < units.Length)
        {
            var newUnit = units[SpaceBeforeUnitBox.SelectedIndex];
            var oldUnit = vm.HeadingSpaceBeforeUnit ?? "行";

            // Save current value for old unit
            _lastSpaceByUnit[$"sb:{oldUnit}"] = SpaceBeforeBox.Value;

            // 1. First update DecimalPlaces/Step/etc
            ApplyConfigForSpaceBefore();

            // 2. Then set the value from remembered value (no conversion)
            SpaceBeforeBox.Value = _lastSpaceByUnit.GetValueOrDefault($"sb:{newUnit}", 1.0);

            vm.HeadingSpaceBeforeUnit = newUnit;
        }
    }

    private void ApplyConfigForSpaceBefore()
    {
        var unit = ComboSelectedString(SpaceBeforeUnitBox, "行");
        SpaceBeforeBox.ApplyConfig(NumericUnitConfigProvider.GetConfig(unit));
    }

    private void SpaceAfterUnitBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingFields) return;
        var vm = GetVm();
        if (vm == null) return;

        var units = new[] { "行", "pt", "cm", "mm" };
        if (SpaceAfterUnitBox.SelectedIndex >= 0 && SpaceAfterUnitBox.SelectedIndex < units.Length)
        {
            var newUnit = units[SpaceAfterUnitBox.SelectedIndex];
            var oldUnit = vm.HeadingSpaceAfterUnit ?? "行";

            _lastSpaceByUnit[$"sa:{oldUnit}"] = SpaceAfterBox.Value;

            // 1. First update DecimalPlaces/Step/etc
            ApplyConfigForSpaceAfter();

            // 2. Then set the value from remembered value (no conversion)
            SpaceAfterBox.Value = _lastSpaceByUnit.GetValueOrDefault($"sa:{newUnit}", 0.5);

            vm.HeadingSpaceAfterUnit = newUnit;
        }
    }

    private void ApplyConfigForSpaceAfter()
    {
        var unit = ComboSelectedString(SpaceAfterUnitBox, "行");
        SpaceAfterBox.ApplyConfig(NumericUnitConfigProvider.GetConfig(unit));
    }

    private void ApplyConfigForLineSpacing()
    {
        // Line spacing value box uses "行" for multiple mode, or length units for fixed/at_least
        var vm2 = GetVm();
        var mode = vm2?.HeadingLineSpacingMode ?? "multiple";
        LineSpacingValueBox.ApplyConfig(
            mode == "multiple"
                ? NumericUnitConfigProvider.GetConfig("行")
                : NumericUnitConfigProvider.GetConfig(ComboSelectedTag(LineSpacingUnitBox, "pt")));
    }

    // ── Line spacing event handlers ────────────────────────────────────

    private void LineSpacingModeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingFields) return;
        var vm = GetVm();
        if (vm == null || LineSpacingModeBox.SelectedItem is not ComboBoxItem item) return;

        var mode = item.Tag as string ?? "multiple";
        vm.HeadingLineSpacingMode = mode;

        // Save current value into memory
        var oldModeKey = _lastLineSpacingByMode.ContainsKey(mode) ? mode : "multiple";
        _lastLineSpacingByMode[oldModeKey] = LineSpacingValueBox.Value;

        // Show/hide unit combo
        LineSpacingUnitBox.Visibility = mode == "multiple" ? Visibility.Collapsed : Visibility.Visible;

        // Apply config and restore last value for this mode
        ApplyConfigForLineSpacing();
        LineSpacingValueBox.Value = _lastLineSpacingByMode.GetValueOrDefault(mode, 1.5);

        // Ensure ViewModel is updated with new value
        vm.HeadingLineSpacing = LineSpacingValueBox.Value;
    }

    private void LineSpacingValueBox_ValueChanged(object sender, double newValue)
    {
        if (_isLoadingFields) return;
        var vm = GetVm();
        if (vm == null) return;

        vm.HeadingLineSpacing = newValue;

        // If value matches a preset, update the mode ComboBox selection accordingly
        if (!_isUpdatingModeFromValue && LineSpacingModeBox.SelectedItem is ComboBoxItem modeItem)
        {
            var mode = modeItem.Tag as string ?? "multiple";
            if (mode == "multiple")
            {
                _isUpdatingModeFromValue = true;
                LineSpacingModeBox.SelectedIndex = newValue switch
                {
                    1.0 => 0,
                    1.5 => 1,
                    2.0 => 2,
                    _   => 3,
                };
                _isUpdatingModeFromValue = false;
            }
        }
    }

    private void LineSpacingUnitBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingFields) return;
        var vm = GetVm();
        if (vm == null) return;

        var unit = ComboSelectedTag(LineSpacingUnitBox, "pt");
        vm.HeadingLineSpacingUnit = unit;

        ApplyConfigForLineSpacing();

        // Re-apply current value so DecimalPlaces/Step take effect
        LineSpacingValueBox.Value = vm.HeadingLineSpacing;
    }

    // ── Helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Get the selected string from a plain-item ComboBox, or return <paramref name="fallback"/>.
    /// </summary>
    private static string ComboSelectedString(ComboBox combo, string fallback)
    {
        if (combo.SelectedItem is ComboBoxItem ci && ci.Content is string s)
            return s;
        return fallback;
    }

    /// <summary>
    /// Get the Tag value from the selected ComboBoxItem, or return <paramref name="fallback"/>.
    /// </summary>
    private static string ComboSelectedTag(ComboBox combo, string fallback)
    {
        if (combo.SelectedItem is ComboBoxItem ci && ci.Tag is string s)
            return s;
        return fallback;
    }

    /// <summary>
    /// Select a ComboBoxItem by matching its Tag property.
    /// </summary>
    private static void SelectComboBoxItemByTag(ComboBox combo, string tag)
    {
        for (int i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is ComboBoxItem ci && ci.Tag is string t && t == tag)
            {
                combo.SelectedIndex = i;
                return;
            }
        }
        combo.SelectedIndex = -1;
    }

    private HeadingStyleViewModel? GetVm()
    {
        if (ViewRoot.DataContext is HeadingStyleViewModel direct)
            return direct;

        DependencyObject? current = ViewRoot;
        while (current != null)
        {
            if (current is FrameworkElement fe && fe.DataContext is HeadingStyleViewModel vm)
                return vm;
            current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
        }
        return null;
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
        // If not found, set no selection
        combo.SelectedIndex = -1;
    }

    private static string _LevelName(int level) => level switch
    {
        1 => "一",
        2 => "二",
        3 => "三",
        4 => "四",
        5 => "五",
        6 => "六",
        _ => level.ToString(),
    };
}