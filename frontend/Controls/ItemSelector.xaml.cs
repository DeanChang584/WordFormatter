using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace WordFormatterUI.Controls;

/// <summary>
/// Single navigation-item row inside <see cref="NavBar"/>.
///
/// Win11-style: no blue accent bar. Selected = light-gray bg (#EFEFEF) +
/// blue icon/text (#0067C0). Hover = lighter bg (#F5F5F5) + blue icon/text.
/// </summary>
public sealed partial class ItemSelector : UserControl
{
    private bool _isHovering;

    // ── Dependency Properties ──────────────────────────────────────────

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(
            nameof(Label), typeof(string), typeof(ItemSelector),
            new PropertyMetadata(string.Empty, OnLabelChanged));

    public static readonly DependencyProperty IconGlyphProperty =
        DependencyProperty.Register(
            nameof(IconGlyph), typeof(string), typeof(ItemSelector),
            new PropertyMetadata(string.Empty, OnIconGlyphChanged));

    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(
            nameof(IsSelected), typeof(bool), typeof(ItemSelector),
            new PropertyMetadata(false, OnIsSelectedChanged));

    public static readonly DependencyProperty IsCompactProperty =
        DependencyProperty.Register(
            nameof(IsCompact), typeof(bool), typeof(ItemSelector),
            new PropertyMetadata(false, OnIsCompactChanged));

    // ── Public Properties ──────────────────────────────────────────────

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public bool IsCompact
    {
        get => (bool)GetValue(IsCompactProperty);
        set => SetValue(IsCompactProperty, value);
    }

    // ── Constructor ────────────────────────────────────────────────────

    public ItemSelector()
    {
        InitializeComponent();
    }

    // ── Hover ──────────────────────────────────────────────────────────

    private void ItemGrid_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _isHovering = true;
        if (!IsSelected)
            VisualStateManager.GoToState(this, "PointerOver", true);
    }

    private void ItemGrid_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        _isHovering = false;
        if (!IsSelected)
            VisualStateManager.GoToState(this, "NoHover", true);
    }

    // ── DP Changed Handlers ────────────────────────────────────────────

    private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ItemSelector selector)
        {
            selector.ItemText.Text = e.NewValue as string ?? string.Empty;
            selector.ToolTipService_SetTip();
        }
    }

    private static void OnIconGlyphChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ItemSelector selector)
            selector.ItemIcon.Glyph = e.NewValue as string ?? string.Empty;
    }

    private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ItemSelector selector)
        {
            bool selected = e.NewValue is bool b && b;
            if (selected)
                VisualStateManager.GoToState(selector, "Selected", true);
            else if (selector._isHovering)
                VisualStateManager.GoToState(selector, "PointerOver", true);
            else
                VisualStateManager.GoToState(selector, "Normal", true);
        }
    }

    private static void OnIsCompactChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ItemSelector selector)
        {
            bool compact = e.NewValue is bool b && b;
            selector.ItemText.Visibility = compact
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────

    private void ToolTipService_SetTip()
    {
        ToolTipService.SetToolTip(this, Label);
    }
}
