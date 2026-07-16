using System.Collections.Generic;

namespace WordFormatterUI.Utilities;

/// <summary>
/// Central registry for all unit-to-<see cref="NumericUnitConfig"/> mappings.
///
/// <para>Every NumericTextBox in the application should go through
/// <c>ApplyConfig(NumericUnitConfigProvider.GetConfig(unit))</c> instead of
/// setting Step / DecimalPlaces / Minimum / Maximum individually.</para>
///
/// <para>Length‑unit conversion factors (mm base):
/// <c>pt = 0.352778</c>, <c>cm = 10</c>, <c>mm = 1</c>.</para>
/// </summary>
public static class NumericUnitConfigProvider
{
    // ── Unit identifiers ──────────────────────────────────────────────
    // These match the strings used in XAML ComboBoxItem content.
    public const string UnitPt   = "pt";
    public const string UnitLine = "行";
    public const string UnitCm   = "cm";
    public const string UnitMm   = "mm";
    public const string UnitChar    = "字符";
    public const string UnitPercent = "percent";
    public const string UnitPx      = "px";
    public const string UnitLinesPerPage = "lines_per_page";
    public const string UnitCharsPerLine = "chars_per_line";

    // ── Lookup ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the <see cref="NumericUnitConfig"/> for the given unit string.
    /// Falls back to <c>pt</c> config if the unit is unknown.
    /// </summary>
    public static NumericUnitConfig GetConfig(string unit)
    {
        // Normalise: "pt" (from Tag) vs "磅" (from ComboBoxItem content)
        var normalised = NormaliseUnit(unit);
        return _configs.TryGetValue(normalised, out var cfg) ? cfg : _configs[UnitPt];
    }

    // ── Length conversion ─────────────────────────────────────────────

    /// <summary>
    /// Conversion factor to millimetres for each length unit.
    /// </summary>
    private static readonly Dictionary<string, double> ToMm = new()
    {
        [UnitPt] = 0.352778,
        [UnitCm] = 10.0,
        [UnitMm] = 1.0,
    };

    /// <summary>
    /// Converts a value from <paramref name="fromUnit"/> to <paramref name="toUnit"/>.
    /// Both must be length units (pt / cm / mm).  Returns <paramref name="value"/>
    /// unchanged if either unit is not a recognised length unit.
    /// </summary>
    public static double ConvertLength(double value, string fromUnit, string toUnit)
    {
        var from = NormaliseUnit(fromUnit);
        var to   = NormaliseUnit(toUnit);

        if (!ToMm.ContainsKey(from) || !ToMm.ContainsKey(to))
            return value; // not a length conversion — leave unchanged

        var mm = value * ToMm[from];
        return mm / ToMm[to];
    }

    /// <summary>
    /// Returns true if the given unit string represents a length unit (pt/cm/mm).
    /// </summary>
    public static bool IsLengthUnit(string unit)
    {
        return ToMm.ContainsKey(NormaliseUnit(unit));
    }

    // ── Normalisation ─────────────────────────────────────────────────

    /// <summary>
    /// Maps ComboBox display strings to canonical unit identifiers.
    /// "磅" → "pt", "厘米" → "cm", "毫米" → "mm", "字符" → "字符", "行" → "行".
    /// </summary>
    private static string NormaliseUnit(string unit)
    {
        return unit?.Trim() switch
        {
            "磅"   => UnitPt,
            "厘米" => UnitCm,
            "毫米" => UnitMm,
            "字符" => UnitChar,
            "行"   => UnitLine,
            _      => unit ?? UnitPt,
        };
    }

    // ── Config registry ───────────────────────────────────────────────

    private static readonly Dictionary<string, NumericUnitConfig> _configs = new()
    {
        [UnitPt]   = new() { Step = 1,     DecimalPlaces = 0, Minimum = 0, Maximum = 200   },
        [UnitLine] = new() { Step = 0.25,  DecimalPlaces = 2, Minimum = 0, Maximum = 10    },
        [UnitCm]   = new() { Step = 0.1,   DecimalPlaces = 2, Minimum = 0, Maximum = 50    },
        [UnitMm]   = new() { Step = 1,     DecimalPlaces = 1, Minimum = 0, Maximum = 500   },
        [UnitChar]    = new() { Step = 0.5, DecimalPlaces = 1, Minimum = 0, Maximum = 20 },
        [UnitPercent] = new() { Step = 1,   DecimalPlaces = 0, Minimum = 0, Maximum = 100 },
        [UnitPx]      = new() { Step = 1,   DecimalPlaces = 0, Minimum = 0, Maximum = 5000 },
        [UnitLinesPerPage] = new() { Step = 1, DecimalPlaces = 0, Minimum = 1, Maximum = 60 },
        [UnitCharsPerLine] = new() { Step = 1, DecimalPlaces = 0, Minimum = 1, Maximum = 80 },
    };
}