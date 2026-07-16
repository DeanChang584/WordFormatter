using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WordFormatterUI.Controls;

/// <summary>
/// Reusable configuration-section card (design-document §6).
/// Provides a title and arbitrary content.
/// </summary>
public sealed partial class ConfigCard : UserControl
{
    // ── Dependency Properties ──────────────────────────────────────────

    /// <summary>Section title displayed at the top of the card.</summary>
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title), typeof(string), typeof(ConfigCard),
            new PropertyMetadata(string.Empty, OnTitleChanged));

    /// <summary>
    /// Arbitrary content injected into the card body.
    /// In XAML, child elements of <see cref="ConfigCard"/> are automatically
    /// wrapped in this property by the ContentPresenter.
    /// </summary>
    public new static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register(
            nameof(Content), typeof(object), typeof(ConfigCard),
            new PropertyMetadata(null, OnContentChanged));

    // ── Public Properties ──────────────────────────────────────────────

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public new object Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    // ── Constructor ────────────────────────────────────────────────────

    public ConfigCard()
    {
        InitializeComponent();
    }

    // ── DP Changed Handlers ────────────────────────────────────────────

    private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ConfigCard card)
            card.TitleBlock.Text = e.NewValue as string ?? string.Empty;
    }

    private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // ContentPresenter is bound to Content via x:Bind; no manual wiring needed.
    }
}
