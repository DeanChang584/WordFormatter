namespace WordFormatterUI.Utilities;

/// <summary>
/// Configuration for a NumericTextBox controlling step, display precision,
/// and value range.  Used together with <see cref="NumericUnitConfigProvider"/>
/// to unify all unit-to-config mappings across the application.
/// </summary>
public sealed record NumericUnitConfig
{
    /// <summary>
    /// Amount added/subtracted on each increment / decrement, mouse wheel tick,
    /// or arrow‑key press.
    /// </summary>
    public double Step { get; init; }

    /// <summary>
    /// Number of decimal places to display (and round to).
    /// </summary>
    public int DecimalPlaces { get; init; }

    /// <summary>
    /// Inclusive lower bound for the value.
    /// </summary>
    public double Minimum { get; init; }

    /// <summary>
    /// Inclusive upper bound for the value.
    /// </summary>
    public double Maximum { get; init; }
}