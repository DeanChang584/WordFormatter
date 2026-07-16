using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WordFormatterUI.Controls;

// ═══════════════════════════════════════════════════════════════════════
//  Data model
// ═══════════════════════════════════════════════════════════════════════

/// <summary>Single navigation-item entry (icon glyph + label).</summary>
public sealed class NavBarItem
{
    public string Icon { get; }
    public string Label { get; }
    public int Index { get; }

    public NavBarItem(string icon, string label, int index)
    {
        Icon = icon;
        Label = label;
        Index = index;
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  NavBar control
// ═══════════════════════════════════════════════════════════════════════

/// <summary>
/// Left-side navigation bar.
///
/// Features:
///   • 8 icon+text items  (Segoe Fluent Icons)
///   • Selected-item highlight: 3px accent bar + tinted background + accent icon
///   • Keyboard: Ctrl+1 through Ctrl+8 for direct jump
///   • Compact mode: text hidden when width &lt; 760px; tooltips on every item
///   • CompactModeChanged event (MainWindow uses it to resize left column)
///
/// Items are built as <see cref="ItemSelector"/> controls in code-behind
/// to avoid DataTemplate x:DataType resolution issues with the WinUI 3
/// XAML compiler (WMC1509 / WMC0909 in unpackaged builds).
/// </summary>
public sealed partial class NavBar : UserControl
{
    // ── Segoe Fluent Icons glyphs ─────────────────────────────────────
    //  \uE8A5 = Document        \uE8D2 = Font
    //  \uE8E9 = FontSize (heading)  \uE7C3 = Page (header/footer)
    //  \uEB9F = Image            \uE80A = Table
    //  \uE713 = Settings         \uE946 = Info
    private static readonly (string Icon, string Label)[] NavEntries =
    {
        ("\uE81C", "页面设置"),   // Ruler — page layout, dimensions, margins
        ("\uE8D2", "正文样式"),   // Font — body text formatting
        ("\uE8E6", "标题样式"),   // Paragraph — heading hierarchy
        ("\uE7C3", "页眉页脚"),   // Page — header/footer area
        ("\uEB9F", "图片样式"),   // Image
        ("\uE80A", "表格样式"),   // Table
        ("\uE713", "高级设置"),   // Settings (gear)
        ("\uE946", "关于软件"),   // Info
    };

    // ── Dependency Properties ──────────────────────────────────────────

    public static readonly DependencyProperty SelectedIndexProperty =
        DependencyProperty.Register(
            nameof(SelectedIndex),
            typeof(int),
            typeof(NavBar),
            new PropertyMetadata(0, OnSelectedIndexChanged));

    public static readonly DependencyProperty IsCompactProperty =
        DependencyProperty.Register(
            nameof(IsCompact),
            typeof(bool),
            typeof(NavBar),
            new PropertyMetadata(false, OnIsCompactChanged));

    private static void OnIsCompactChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not NavBar nav) return;
        bool compact = e.NewValue is bool b && b;
        foreach (var s in nav._selectors)
            s.IsCompact = compact;
        nav.CompactModeChanged?.Invoke(nav, EventArgs.Empty);
    }

    // ── Events ─────────────────────────────────────────────────────────

    /// <summary>Fires after SelectedIndex changes (user click or Ctrl+N).</summary>
    public event EventHandler<NavBarSelectionChangedEventArgs>? SelectionChanged;

    /// <summary>Fires when compact/normal mode toggles.</summary>
    public event EventHandler? CompactModeChanged;

    // ── Public properties ──────────────────────────────────────────────

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public bool IsCompact
    {
        get => (bool)GetValue(IsCompactProperty);
        set => SetValue(IsCompactProperty, value);
    }

    // ── State ──────────────────────────────────────────────────────────

    private readonly List<ItemSelector> _selectors = new();

    /// <summary>Expose selectors for MainWindow to update IsCompact on each.</summary>
    public IReadOnlyList<ItemSelector> GetSelectors() => _selectors.AsReadOnly();

    // ── Constructor ────────────────────────────────────────────────────

    public NavBar()
    {
        InitializeComponent();
        PopulateItems();
    }

    // ── Initialization ─────────────────────────────────────────────────

    private void PopulateItems()
    {
        for (int i = 0; i < NavEntries.Length; i++)
        {
            var (icon, label) = NavEntries[i];
            var selector = new ItemSelector
            {
                IconGlyph = icon,
                Label     = label,
                IsCompact = false,
                Margin    = new Thickness(0, 0, 0, 8),
            };
            _selectors.Add(selector);
            NavListBox.Items.Add(selector);
        }

        // Select first item
        NavListBox.SelectedIndex = 0;
        _SyncSelectionVisuals(0);
    }

    // ── Selection ──────────────────────────────────────────────────────

    private void NavListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        int idx = NavListBox.SelectedIndex;
        if (idx < 0) return;

        // Sync DP (in case changed by click, not by SelectedIndex binding)
        if (idx != SelectedIndex)
            SelectedIndex = idx;

        _SyncSelectionVisuals(idx);

        SelectionChanged?.Invoke(this, new NavBarSelectionChangedEventArgs(idx));
    }

    private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not NavBar nav) return;
        int idx = nav.SelectedIndex;
        if (idx < 0 || idx >= nav._selectors.Count) return;

        // Sync ListBox if DP changed externally (e.g. Ctrl+N)
        if (nav.NavListBox.SelectedIndex != idx)
            nav.NavListBox.SelectedIndex = idx;

        nav._SyncSelectionVisuals(idx);
    }

    // ── Selection visual sync ──────────────────────────────────────────

    /// <summary>
    /// Set <see cref="ItemSelector.IsSelected"/> on every item to drive
    /// the VisualStateManager in each control.
    /// Called on selection change and on initial load.
    /// </summary>
    private void _SyncSelectionVisuals(int selected)
    {
        for (int i = 0; i < _selectors.Count; i++)
            _selectors[i].IsSelected = (i == selected);
    }

    // ── Keyboard (Ctrl+1..8) ───────────────────────────────────────────

    /// <summary>
    /// Call from MainWindow.KeyDown to handle Ctrl+1..Ctrl+8 shortcuts.
    /// Returns true if the key was consumed.
    /// </summary>
    public bool HandleKeyDown(Windows.System.VirtualKey key, bool ctrlPressed)
    {
        if (!ctrlPressed) return false;

        int index = key switch
        {
            Windows.System.VirtualKey.Number1 => 0,
            Windows.System.VirtualKey.Number2 => 1,
            Windows.System.VirtualKey.Number3 => 2,
            Windows.System.VirtualKey.Number4 => 3,
            Windows.System.VirtualKey.Number5 => 4,
            Windows.System.VirtualKey.Number6 => 5,
            Windows.System.VirtualKey.Number7 => 6,
            Windows.System.VirtualKey.Number8 => 7,
            _ => -1
        };

        if (index < 0 || index >= _selectors.Count) return false;
        SelectedIndex = index;
        return true;
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  Event args
// ═══════════════════════════════════════════════════════════════════════

public sealed class NavBarSelectionChangedEventArgs : EventArgs
{
    public int Index { get; }
    public NavBarSelectionChangedEventArgs(int index) => Index = index;
}
