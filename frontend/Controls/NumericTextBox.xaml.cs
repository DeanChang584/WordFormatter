using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace WordFormatterUI.Controls;

/// <summary>
/// A clean numeric input control — replacement for WinUI NumberBox.
/// No spin buttons, no clear button.  Supports mouse wheel, keyboard
/// shortcuts, min/max clamping, decimal rounding, and two-way binding.
/// </summary>
public sealed partial class NumericTextBox : UserControl
{
    // ── Dependency Properties ──────────────────────────────────────

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(NumericTextBox),
            new PropertyMetadata(0.0, OnValueChanged));

    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(NumericTextBox),
            new PropertyMetadata(double.MinValue));

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(NumericTextBox),
            new PropertyMetadata(double.MaxValue));

    public static readonly DependencyProperty StepProperty =
        DependencyProperty.Register(nameof(Step), typeof(double), typeof(NumericTextBox),
            new PropertyMetadata(1.0));

    public static readonly DependencyProperty DecimalPlacesProperty =
        DependencyProperty.Register(nameof(DecimalPlaces), typeof(int), typeof(NumericTextBox),
            new PropertyMetadata(0));

    public static readonly DependencyProperty IsReadOnlyProperty =
        DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(NumericTextBox),
            new PropertyMetadata(false));

    public static readonly DependencyProperty PlaceholderTextProperty =
        DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(NumericTextBox),
            new PropertyMetadata(""));

    // ── Public properties ──────────────────────────────────────────

    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public double Minimum { get => (double)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public double Maximum { get => (double)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    public double Step { get => (double)GetValue(StepProperty); set => SetValue(StepProperty, value); }
    public int DecimalPlaces { get => (int)GetValue(DecimalPlacesProperty); set => SetValue(DecimalPlacesProperty, value); }
    public bool IsReadOnly { get => (bool)GetValue(IsReadOnlyProperty); set => SetValue(IsReadOnlyProperty, value); }
    public string PlaceholderText { get => (string)GetValue(PlaceholderTextProperty); set => SetValue(PlaceholderTextProperty, value); }

    // ── Event ──────────────────────────────────────────────────────

    public event EventHandler<double>? ValueChanged;

    // ── ApplyConfig (unified unit config) ──────────────────────────

    /// <summary>
    /// Apply a <see cref="Utilities.NumericUnitConfig"/> to this control,
    /// setting Step, DecimalPlaces, Minimum, and Maximum in one call.
    /// </summary>
    public void ApplyConfig(Utilities.NumericUnitConfig config)
    {
        Step = config.Step;
        DecimalPlaces = config.DecimalPlaces;
        Minimum = config.Minimum;
        Maximum = config.Maximum;
        // Force refresh display text to apply new DecimalPlaces
        InputBox.Text = FormatValue(Value, DecimalPlaces);
    }

    // ── Internal state ─────────────────────────────────────────────

    private double _editStartValue;
    private bool _isUpdating; // guard against re-entrant updates

    // ── Constructor ────────────────────────────────────────────────

    public NumericTextBox()
    {
        InitializeComponent();
    }

    // ── DP change → sync TextBox ───────────────────────────────────

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not NumericTextBox self || self._isUpdating) return;
        var clamped = Clamp((double)e.NewValue, self.Minimum, self.Maximum);
        var formatted = FormatValue(clamped, self.DecimalPlaces);
        self.InputBox.Text = formatted;
    }

    // ── TextBox → Value sync ───────────────────────────────────────

    private void CommitText()
    {
        _isUpdating = true;
        if (double.TryParse(InputBox.Text, out var v))
        {
            v = Clamp(v, Minimum, Maximum);
            v = Math.Round(v, DecimalPlaces, MidpointRounding.AwayFromZero);
            Value = v;
            InputBox.Text = FormatValue(v, DecimalPlaces);
            ValueChanged?.Invoke(this, v);
        }
        else
        {
            // Restore previous valid value
            InputBox.Text = FormatValue(Value, DecimalPlaces);
        }
        _isUpdating = false;
    }

    // ── Text filtering ─────────────────────────────────────────────

    private void InputBox_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
    {
        // Allow only digits, one decimal point, and one leading minus
        var text = sender.Text;
        var filtered = "";
        var hasPoint = false;
        var hasMinus = false;

        foreach (var ch in text)
        {
            if (ch == '-' && filtered.Length == 0)
            {
                hasMinus = true; filtered += ch;
            }
            else if ((ch == '.' || ch == ',') && !hasPoint)
            {
                hasPoint = true; filtered += '.';
            }
            else if (ch >= '0' && ch <= '9')
            {
                filtered += ch;
            }
            // else: drop the character
        }

        if (filtered != text)
        {
            var pos = sender.SelectionStart;
            sender.Text = filtered;
            sender.SelectionStart = Math.Min(pos, filtered.Length);
        }
    }

    // ── Focus ──────────────────────────────────────────────────────

    private void InputBox_GotFocus(object sender, RoutedEventArgs e)
    {
        _editStartValue = Value;
        InputBox.SelectAll();
        // Blue accent border on outer container
        ContainerBorder.BorderBrush = (Brush)Application.Current.Resources["SystemControlHighlightAccentBrush"];
    }

    private void InputBox_LostFocus(object sender, RoutedEventArgs e)
    {
        CommitText();
        // Restore default border
        ContainerBorder.BorderBrush = (Brush)Application.Current.Resources["TextControlBorderBrush"];
        // Deselect
        InputBox.SelectionStart = 0;
        InputBox.SelectionLength = 0;
    }

    // ── Keyboard ───────────────────────────────────────────────────

    private void InputBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case VirtualKey.Up:
                _editStartValue = Value; // reset Esc baseline
                AdjustValue(+Step);
                e.Handled = true;
                break;

            case VirtualKey.Down:
                _editStartValue = Value;
                AdjustValue(-Step);
                e.Handled = true;
                break;

            case VirtualKey.PageUp:
                _editStartValue = Value;
                AdjustValue(+Step * 10);
                e.Handled = true;
                break;

            case VirtualKey.PageDown:
                _editStartValue = Value;
                AdjustValue(-Step * 10);
                e.Handled = true;
                break;

            case VirtualKey.Enter:
                CommitText();
                // Move focus to next control
                InputBox.SelectionStart = 0;
                InputBox.SelectionLength = 0;
                e.Handled = true;
                break;

            case VirtualKey.Escape:
                // Restore value before editing started
                _isUpdating = true;
                Value = Clamp(_editStartValue, Minimum, Maximum);
                InputBox.Text = FormatValue(Value, DecimalPlaces);
                _isUpdating = false;
                InputBox.SelectionStart = 0;
                InputBox.SelectionLength = 0;
                e.Handled = true;
                break;
        }
    }

    // ── Mouse wheel ────────────────────────────────────────────────

    private void InputBox_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var delta = e.GetCurrentPoint(this).Properties.MouseWheelDelta;
        _editStartValue = Value;
        AdjustValue(delta > 0 ? +Step : -Step);
        e.Handled = true;
    }

    // ── Spin button click handlers ─────────────────────────────────

    private void UpButton_Click(object sender, RoutedEventArgs e)
    {
        if (IsReadOnly) return;
        _editStartValue = Value;
        AdjustValue(+Step);
        // Keep focus on TextBox for continued keyboard input
        InputBox.Focus(FocusState.Programmatic);
    }

    private void DownButton_Click(object sender, RoutedEventArgs e)
    {
        if (IsReadOnly) return;
        _editStartValue = Value;
        AdjustValue(-Step);
        // Keep focus on TextBox for continued keyboard input
        InputBox.Focus(FocusState.Programmatic);
    }

    // ── Helpers ────────────────────────────────────────────────────

    private void AdjustValue(double delta)
    {
        var v = Math.Round(Value + delta, DecimalPlaces, MidpointRounding.AwayFromZero);
        v = Clamp(v, Minimum, Maximum);
        _isUpdating = true;
        Value = v;
        InputBox.Text = FormatValue(v, DecimalPlaces);
        _isUpdating = false;
        ValueChanged?.Invoke(this, v);
    }

    private static double Clamp(double v, double min, double max)
        => v < min ? min : v > max ? max : v;

    private static string FormatValue(double v, int decimals)
    {
        var rounded = Math.Round(v, decimals, MidpointRounding.AwayFromZero);
        return rounded.ToString("F" + decimals);
    }
}