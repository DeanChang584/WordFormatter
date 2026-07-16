using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WordFormatterUI.Controls;
using WordFormatterUI.Utilities;
using WordFormatterUI.ViewModels;

namespace WordFormatterUI.Views;

/// <summary>
/// Image formatting settings (design-document §9.5).
/// Manages size mode, dimensions, scaling, layout, and compression controls.
/// </summary>
public sealed partial class PictureSettingsView : UserControl
{
    public PictureSettingsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // ── Size mode ──
        SizeModeBox.Items.Clear();
        foreach (var item in new[] { "指定宽度", "指定高度", "原始大小" })
            SizeModeBox.Items.Add(item);

        // ── Alignment (8 options matching Word/WPS) ──
        AlignmentBox.Items.Clear();
        foreach (var align in new[]
        {
            "左对齐", "水平居中", "右对齐",
            "顶端对齐", "垂直居中", "底端对齐",
            "横向分布", "纵向分布"
        })
            AlignmentBox.Items.Add(align);

        // ── Wrapping style ──
        WrappingStyleBox.Items.Clear();
        foreach (var style in new[] { "嵌入型", "四周型", "紧密型", "穿越型", "上下型", "衬于文字下方", "浮于文字上方" })
            WrappingStyleBox.Items.Add(style);

        // ── Width unit ──
        WidthUnitBox.Items.Clear();
        foreach (var u in new[] { "厘米", "毫米", "磅" })
            WidthUnitBox.Items.Add(u);

        // ── Height unit ──
        HeightUnitBox.Items.Clear();
        foreach (var u in new[] { "厘米", "毫米", "磅" })
            HeightUnitBox.Items.Add(u);

        // ── Quality unit ──
        QualityUnitBox.Items.Clear();
        QualityUnitBox.Items.Add("百分比");

        // ── Max pixels unit ──
        MaxPixelsUnitBox.Items.Clear();
        MaxPixelsUnitBox.Items.Add("像素");

        var vm = GetVm();
        if (vm == null) return;

        PushFieldsToUI(vm);

        // Apply per-field NumericTextBox config (use current unit)
        QualityBox.ApplyConfig(NumericUnitConfigProvider.GetConfig("percent"));
        MaxPixelsBox.ApplyConfig(NumericUnitConfigProvider.GetConfig("px"));
        WidthBox.ApplyConfig(NumericUnitConfigProvider.GetConfig(vm.PictureWidthUnit));
        HeightBox.ApplyConfig(NumericUnitConfigProvider.GetConfig(vm.PictureHeightUnit));
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

    private void PushFieldsToUI(PictureSettingsViewModel vm)
    {
        // 尺寸方式
        SizeModeBox.SelectedIndex = vm.PictureSizeMode switch
        {
            "height" => 1,
            "auto" => 2,
            _ => 0, // "width"
        };

        // 宽度 / 高度
        WidthBox.Value = vm.PictureWidth;
        HeightBox.Value = vm.PictureHeight;

        // 缩放
        KeepAspectCheck.IsChecked = vm.PictureKeepAspectRatio;
        NoEnlargeCheck.IsChecked = vm.PictureNoEnlarge;

        // 对齐方式 (8 项)
        AlignmentBox.SelectedIndex = vm.PictureAlignment switch
        {
            "left" => 0,
            "right" => 2,
            "top" => 3,
            "bottom" => 5,
            "distribute_h" => 6,
            "distribute_v" => 7,
            _ => 1, // center (horizontal center)
        };

        // 文字环绕
        WrappingStyleBox.SelectedIndex = vm.PictureWrappingStyle switch
        {
            "square" => 1,
            "tight" => 2,
            "through" => 3,
            "topBottom" => 4,
            "behindText" => 5,
            "inFrontOfText" => 6,
            _ => 0, // inline
        };

        // 单位
        WidthUnitBox.SelectedIndex = vm.PictureWidthUnit switch { "mm" => 1, "pt" => 2, _ => 0 };
        HeightUnitBox.SelectedIndex = vm.PictureHeightUnit switch { "mm" => 1, "pt" => 2, _ => 0 };
        QualityUnitBox.SelectedIndex = 0;
        MaxPixelsUnitBox.SelectedIndex = 0;

        // 压缩
        QualityBox.Value = vm.PictureQuality;
        MaxPixelsBox.Value = vm.PictureMaxSidePixels;
        AutoCompressCheck.IsChecked = vm.PictureAutoCompress;

        // 更新行可见性
        UpdateSizeModeVisibility(vm.PictureSizeMode);
    }

    private void UpdateSizeModeVisibility(string sizeMode)
    {
        WidthRow.Visibility = sizeMode == "width" ? Visibility.Visible : Visibility.Collapsed;
        HeightRow.Visibility = sizeMode == "height" ? Visibility.Visible : Visibility.Collapsed;
        AutoHintRow.Visibility = sizeMode == "auto" ? Visibility.Visible : Visibility.Collapsed;
    }

    // ──────────────────────────────────────────────
    // Event Handlers
    // ──────────────────────────────────────────────

    private void SizeModeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var vm = GetVm();
        if (vm == null) return;

        vm.PictureSizeMode = SizeModeBox.SelectedIndex switch
        {
            1 => "height",
            2 => "auto",
            _ => "width",
        };

        UpdateSizeModeVisibility(vm.PictureSizeMode);
    }

    private void WidthBox_ValueChanged(object sender, double newValue)
    {
        var vm = GetVm();
        if (vm != null) vm.PictureWidth = newValue;
    }

    private void HeightBox_ValueChanged(object sender, double newValue)
    {
        var vm = GetVm();
        if (vm != null) vm.PictureHeight = newValue;
    }

    private void KeepAspectCheck_Changed(object sender, RoutedEventArgs e)
    {
        var vm = GetVm();
        if (vm != null)
            vm.PictureKeepAspectRatio = KeepAspectCheck.IsChecked == true;
    }

    private void NoEnlargeCheck_Changed(object sender, RoutedEventArgs e)
    {
        var vm = GetVm();
        if (vm != null)
            vm.PictureNoEnlarge = NoEnlargeCheck.IsChecked == true;
    }

    private void AlignmentBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var vm = GetVm();
        if (vm == null) return;

        vm.PictureAlignment = AlignmentBox.SelectedIndex switch
        {
            0 => "left",
            2 => "right",
            3 => "top",
            4 => "middle",
            5 => "bottom",
            6 => "distribute_h",
            7 => "distribute_v",
            _ => "center",
        };
    }

    private void WrappingStyleBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var vm = GetVm();
        if (vm == null) return;

        vm.PictureWrappingStyle = WrappingStyleBox.SelectedIndex switch
        {
            1 => "square",
            2 => "tight",
            3 => "through",
            4 => "topBottom",
            5 => "behindText",
            6 => "inFrontOfText",
            _ => "inline",
        };
    }

    private void QualityBox_ValueChanged(object sender, double newValue)
    {
        var vm = GetVm();
        if (vm != null)
            vm.PictureQuality = (int)newValue;
    }

    private void MaxPixelsBox_ValueChanged(object sender, double newValue)
    {
        var vm = GetVm();
        if (vm != null)
            vm.PictureMaxSidePixels = (int)newValue;
    }

    private void WidthUnitBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var vm = GetVm();
        if (vm == null) return;
        var newUnit = WidthUnitBox.SelectedIndex switch { 1 => "mm", 2 => "pt", _ => "cm" };
        var oldUnit = vm.PictureWidthUnit;

        // Convert value when unit changes between cm and mm
        if (NumericUnitConfigProvider.IsLengthUnit(oldUnit) && NumericUnitConfigProvider.IsLengthUnit(newUnit) && oldUnit != newUnit)
        {
            var converted = NumericUnitConfigProvider.ConvertLength(vm.PictureWidth, oldUnit, newUnit);
            vm.PictureWidth = Math.Round(converted, 1);
        }

        vm.PictureWidthUnit = newUnit;
        WidthBox.ApplyConfig(NumericUnitConfigProvider.GetConfig(newUnit));
    }

    private void HeightUnitBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var vm = GetVm();
        if (vm == null) return;
        var newUnit = HeightUnitBox.SelectedIndex switch { 1 => "mm", 2 => "pt", _ => "cm" };
        var oldUnit = vm.PictureHeightUnit;

        // Convert value when unit changes between cm and mm
        if (NumericUnitConfigProvider.IsLengthUnit(oldUnit) && NumericUnitConfigProvider.IsLengthUnit(newUnit) && oldUnit != newUnit)
        {
            var converted = NumericUnitConfigProvider.ConvertLength(vm.PictureHeight, oldUnit, newUnit);
            vm.PictureHeight = Math.Round(converted, 1);
        }

        vm.PictureHeightUnit = newUnit;
        HeightBox.ApplyConfig(NumericUnitConfigProvider.GetConfig(newUnit));
    }

    private void QualityUnitBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Only "百分比" - no action needed
    }

    private void MaxPixelsUnitBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Only "像素" - no action needed
    }

    private void AutoCompressCheck_Changed(object sender, RoutedEventArgs e)
    {
        var vm = GetVm();
        if (vm != null)
            vm.PictureAutoCompress = AutoCompressCheck.IsChecked == true;
    }

    // ──────────────────────────────────────────────
    // Helper
    // ──────────────────────────────────────────────

    private PictureSettingsViewModel? GetVm()
    {
        if (ViewRoot.DataContext is PictureSettingsViewModel direct)
            return direct;

        DependencyObject? current = ViewRoot;
        while (current != null)
        {
            if (current is FrameworkElement fe && fe.DataContext is PictureSettingsViewModel vm)
                return vm;
            current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}