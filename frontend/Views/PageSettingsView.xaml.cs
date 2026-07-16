using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WordFormatterUI.Controls;
using WordFormatterUI.Utilities;
using WordFormatterUI.ViewModels;

namespace WordFormatterUI.Views;

/// <summary>
/// Page settings form (design-document §9.1).
///
/// Fields:
///   • Paper size   (A4 / A3 / B5 / Letter / Legal / 自定义)
///   • Orientation  (portrait / landscape)
///   • Margins      (top / bottom / left / right — mm base, UI supports mm↔cm)
///   • Page number  (show / hide)
///
/// Binds to <see cref="PageSettingsViewModel"/>.
/// </summary>
public sealed partial class PageSettingsView : UserControl
{
    // ── Available paper sizes ──────────────────────────────────────────
    private static readonly string[] PaperSizes = { "A4", "A3", "B5", "Letter", "Legal" };

    // ── Margin source-of-truth (all values in mm) ─────────────────────
    //  Stored so switching between mm/cm is lossless.
    private double _marginTopMm, _marginBottomMm, _marginLeftMm, _marginRightMm;

    // Guard against re-entrant ValueChanged / SelectionChanged during unit switch
    private bool _isUnitSwitching;

    // ── Constructor ────────────────────────────────────────────────────

    public PageSettingsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    // ── Load: populate combos + push ViewModel values → UI ────────────

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Paper size options
        PaperSizeBox.Items.Clear();
        foreach (var s in PaperSizes)
            PaperSizeBox.Items.Add(s);

        var vm = GetVm();
        if (vm == null) return;

        // Paper size
        PaperSizeBox.SelectedItem = vm.PaperSize;
        if (PaperSizeBox.SelectedIndex < 0)
            PaperSizeBox.SelectedIndex = 0; // fallback to A4

        // Orientation
        OrientationBox.SelectedIndex = vm.Orientation == "landscape" ? 1 : 0;

        // Margins (store mm, display in current unit)
        _marginTopMm    = vm.MarginTop;
        _marginBottomMm = vm.MarginBottom;
        _marginLeftMm   = vm.MarginLeft;
        _marginRightMm  = vm.MarginRight;
        PushMarginsToUI();

        // Apply config for margin boxes
        ApplyConfigForMargin(MarginTopBox, MarginTopUnitBox);
        ApplyConfigForMargin(MarginBottomBox, MarginBottomUnitBox);
        ApplyConfigForMargin(MarginLeftBox, MarginLeftUnitBox);
        ApplyConfigForMargin(MarginRightBox, MarginRightUnitBox);

        // Document grid
        InitGridModeBox();
    }

    /// <summary>
    /// Re-read all values from the ViewModel and push them to the UI controls.
    /// Called after profile reset / template apply / history reuse.
    /// </summary>
    public void RefreshUI()
    {
        var vm = GetVm();
        if (vm == null) return;

        // Paper size
        PaperSizeBox.SelectedItem = vm.PaperSize;
        if (PaperSizeBox.SelectedIndex < 0)
            PaperSizeBox.SelectedIndex = 0;

        // Orientation
        OrientationBox.SelectedIndex = vm.Orientation == "landscape" ? 1 : 0;

        // Margins
        _marginTopMm    = vm.MarginTop;
        _marginBottomMm = vm.MarginBottom;
        _marginLeftMm   = vm.MarginLeft;
        _marginRightMm  = vm.MarginRight;
        PushMarginsToUI();

        // Document grid
        SyncGridToUI();
    }

    // ── Paper size ─────────────────────────────────────────────────────

    private void PaperSizeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PaperSizeBox.SelectedItem is string size)
        {
            var vm = GetVm();
            if (vm != null) vm.PaperSize = size;
        }
    }

    // ── Orientation ────────────────────────────────────────────────────

    private void OrientationBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (OrientationBox.SelectedItem is ComboBoxItem item &&
            item.Tag is string tag)
        {
            var vm = GetVm();
            if (vm != null) vm.Orientation = tag;
        }
    }

    // ── Margins: value changed ─────────────────────────────────────────

    private void MarginBox_ValueChanged(object sender, double newValue)
    {
        if (_isUnitSwitching) return;

        // Convert displayed value back to mm
        double newMm = newValue;
        if (sender is NumericTextBox nb)
        {
            var unitCombo = GetUnitComboBox(nb);
            var unitLabel = ComboSelectedString(unitCombo, "毫米");
            if (unitLabel == "厘米")
                newMm *= 10.0;
        }

        var vm = GetVm();
        if (vm == null) return;

        if (sender == MarginTopBox)
        {
            _marginTopMm = newMm;
            vm.MarginTop = newMm;
        }
        else if (sender == MarginBottomBox)
        {
            _marginBottomMm = newMm;
            vm.MarginBottom = newMm;
        }
        else if (sender == MarginLeftBox)
        {
            _marginLeftMm = newMm;
            vm.MarginLeft = newMm;
        }
        else if (sender == MarginRightBox)
        {
            _marginRightMm = newMm;
            vm.MarginRight = newMm;
        }
    }

    // ── Margins: unit changed (mm ↔ cm) ────────────────────────────────

    private void MarginUnit_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUnitSwitching) return;
        _isUnitSwitching = true;

        // Determine which NumericTextBox + ComboBox pair changed
        (NumericTextBox nb, ComboBox unit) = sender switch
        {
            _ when sender == MarginTopUnitBox    => (MarginTopBox,    MarginTopUnitBox),
            _ when sender == MarginBottomUnitBox => (MarginBottomBox, MarginBottomUnitBox),
            _ when sender == MarginLeftUnitBox   => (MarginLeftBox,   MarginLeftUnitBox),
            _ when sender == MarginRightUnitBox  => (MarginRightBox,  MarginRightUnitBox),
            _ => (null!, null!)
        };

        if (nb is not null)
        {
            var unitLabel = ComboSelectedString(unit, "毫米");

            // 1. First update DecimalPlaces/Step/etc via ApplyConfig
            ApplyConfigForMargin(nb, unit);

            // 2. Then set the value — OnValueChanged will use the correct DecimalPlaces
            if (unitLabel == "厘米")
                nb.Value = nb.Value / 10.0;
            else
                nb.Value = nb.Value * 10.0;
        }

        _isUnitSwitching = false;
    }

    // ── Apply config for a margin box ─────────────────────────────────

    private void ApplyConfigForMargin(NumericTextBox nb, ComboBox unitCombo)
    {
        var unitLabel = ComboSelectedString(unitCombo, "毫米");
        var unit = unitLabel switch
        {
            "厘米" => "cm",
            _ => "mm",
        };
        nb.ApplyConfig(NumericUnitConfigProvider.GetConfig(unit));
    }

    // ── Margins: push stored mm values to UI in current unit ───────────

    private void PushMarginsToUI()
    {
        _isUnitSwitching = true;

        // All start as mm (SelectedIndex=0)
        MarginTopBox.Value    = _marginTopMm;
        MarginBottomBox.Value = _marginBottomMm;
        MarginLeftBox.Value   = _marginLeftMm;
        MarginRightBox.Value  = _marginRightMm;

        _isUnitSwitching = false;
    }

    // ── Document grid ──────────────────────────────────────────────────

    /// <summary>
    /// Populate the grid-mode ComboBox and sync from ViewModel.
    /// Called from <see cref="OnLoaded"/> and <see cref="RefreshUI"/>.
    /// </summary>
    private void InitGridModeBox()
    {
        var vm = GetVm();
        if (vm == null) return;

        // Populate items
        GridModeBox.Items.Clear();
        foreach (var option in vm.GridModeOptions)
            GridModeBox.Items.Add(option);

        // Apply config for grid NumericTextBoxes (integer step/range)
        LinesPerPageBox.ApplyConfig(NumericUnitConfigProvider.GetConfig(NumericUnitConfigProvider.UnitLinesPerPage));
        CharsPerLineBox.ApplyConfig(NumericUnitConfigProvider.GetConfig(NumericUnitConfigProvider.UnitCharsPerLine));

        // Select current
        GridModeBox.SelectedItem = vm.DocumentGridMode;

        // Sync grid visibility + values
        SyncGridToUI();
    }

    private void UpdateGridVisibility()
    {
        var vm = GetVm();
        if (vm == null) return;

        bool showGrid = vm.ShowGridSettings;
        LinesPerPageRow.Visibility    = showGrid ? Visibility.Visible : Visibility.Collapsed;
        AdjustRightIndentRow.Visibility = showGrid ? Visibility.Visible : Visibility.Collapsed;
        AlignToGridRow.Visibility      = showGrid ? Visibility.Visible : Visibility.Collapsed;

        bool showChar = vm.ShowCharSettings;
        CharsPerLineRow.Visibility = showChar ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SyncGridToUI()
    {
        var vm = GetVm();
        if (vm == null) return;

        GridModeBox.SelectedItem = vm.DocumentGridMode;
        LinesPerPageBox.Value    = vm.LinesPerPage;
        CharsPerLineBox.Value    = vm.CharsPerLine;
        AdjustRightIndentBox.IsChecked = vm.AdjustRightIndent;
        AlignToGridBox.IsChecked       = vm.AlignToGrid;

        UpdateGridVisibility();
    }

    private void GridModeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GridModeBox.SelectedItem is string mode)
        {
            var vm = GetVm();
            if (vm != null) vm.DocumentGridMode = mode;
        }
        UpdateGridVisibility();
    }

    private void LinesPerPageBox_ValueChanged(object sender, double newValue)
    {
        var vm = GetVm();
        if (vm != null) vm.LinesPerPage = (int)newValue;
    }

    private void CharsPerLineBox_ValueChanged(object sender, double newValue)
    {
        var vm = GetVm();
        if (vm != null) vm.CharsPerLine = (int)newValue;
    }

    private void AdjustRightIndentBox_Checked(object sender, RoutedEventArgs e)
    {
        var vm = GetVm();
        if (vm != null) vm.AdjustRightIndent = AdjustRightIndentBox.IsChecked == true;
    }

    private void AlignToGridBox_Checked(object sender, RoutedEventArgs e)
    {
        var vm = GetVm();
        if (vm != null) vm.AlignToGrid = AlignToGridBox.IsChecked == true;
    }

    // ── Helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Get the selected string from a plain-item ComboBox, or return <paramref name="fallback"/>.
    /// </summary>
    private static string ComboSelectedString(ComboBox combo, string fallback)
    {
        return combo.SelectedItem is string s ? s : (combo.SelectedItem is ComboBoxItem cbi ? cbi.Content as string ?? fallback : fallback);
    }

    /// <summary>
    /// Resolve the PageSettingsViewModel.  Walk up the tree looking for the
    /// DataContext set by MainWindow on the ScrollViewer/StackPanel ancestor.
    /// Falls back to <see cref="ViewRoot.DataContext"/> if set directly.
    /// </summary>
    private PageSettingsViewModel? GetVm()
    {
        if (ViewRoot.DataContext is PageSettingsViewModel direct)
            return direct;

        DependencyObject? current = ViewRoot;
        while (current != null)
        {
            if (current is FrameworkElement fe && fe.DataContext is PageSettingsViewModel vm)
                return vm;
            current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    /// <summary>Returns the unit ComboBox paired with the given margin NumberBox.</summary>
    private ComboBox? GetUnitComboBox(NumericTextBox nb)
    {
        if (nb == MarginTopBox)    return MarginTopUnitBox;
        if (nb == MarginBottomBox) return MarginBottomUnitBox;
        if (nb == MarginLeftBox)   return MarginLeftUnitBox;
        if (nb == MarginRightBox)  return MarginRightUnitBox;
        return null;
    }
}