using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WordFormatterUI.Controls;

/// <summary>
/// Custom title bar for the main window.
///
/// Usage (in MainWindow.xaml / .xaml.cs):
///   1.  In App.xaml.cs set  <c>MainWindow.ExtendsContentIntoTitleBar = true;</c>
///   2.  In MainWindow constructor replace the placeholder title bar row with
///       a &lt;local:TitleBar /&gt; control.
///   3.  After InitializeComponent call <c>window.SetTitleBar(TitleBarControl)</c>
///       (the control exposes a <see cref="DragRegion"/> property for convenience).
///
/// The system renders caption buttons (min / max / close) on top of the
/// rightmost 136 px (at 1× DPI); the <see cref="CaptionButtonsPlaceholder"/>
/// Border reserves that space so the page title centres correctly.
/// </summary>
public sealed partial class TitleBar : UserControl
{
    // ── Dependency Properties ──────────────────────────────────────────

    /// <summary>Title text displayed in the center of the title bar.</summary>
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(TitleBar),
            new PropertyMetadata("Word Formatter"));

    /// <summary>32×32 icon glyph displayed on the left.</summary>
    public static readonly DependencyProperty IconGlyphProperty =
        DependencyProperty.Register(
            nameof(IconGlyph),
            typeof(string),
            typeof(TitleBar),
            new PropertyMetadata("\uE8A5"));  // default: Document icon

    /// <summary>Visibility of the icon. Collapse to hide the 32 px left column.</summary>
    public static readonly DependencyProperty IconVisibilityProperty =
        DependencyProperty.Register(
            nameof(IconVisibility),
            typeof(Visibility),
            typeof(TitleBar),
            new PropertyMetadata(Visibility.Visible));

    // ── Public Properties ──────────────────────────────────────────────

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    public Visibility IconVisibility
    {
        get => (Visibility)GetValue(IconVisibilityProperty);
        set => SetValue(IconVisibilityProperty, value);
    }

    /// <summary>
    /// The element registered as the window drag region via
    /// <c>window.SetTitleBar(DragRegion)</c>.  Exposed so MainWindow can
    /// call <c>window.SetTitleBar(titleBar.DragRegion)</c> without
    /// reaching into x:Name internals.
    /// </summary>
    public FrameworkElement DragRegion => TitleBarGrid;

    // ── Constructor ────────────────────────────────────────────────────

    public TitleBar()
    {
        InitializeComponent();
        // Load app logo (ms-appx:/// may not work in unpackaged apps)
        var logoPath = System.IO.Path.Combine(
            System.AppContext.BaseDirectory, "Assets", "Logo.png");
        if (System.IO.File.Exists(logoPath))
            AppIcon.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(
                new Uri(logoPath));
    }
}
