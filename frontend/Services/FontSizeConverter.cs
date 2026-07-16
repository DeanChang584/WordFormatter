using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WordFormatterUI.Models;

namespace WordFormatterUI.Services;

/// <summary>
/// Bidirectional converter between Chinese font size names and pt values.
///
/// Provides four operations:
///   • Parse()     — "小四" → 12, "12pt" → 12, "abc" → null
///   • Format()    — 12 → "小四", 13.6 → "13.6"
///   • Normalize() — "12pt" → "12", "小四号" → "小四"
///   • Suggest()   — prefix-matching for AutoSuggestBox
/// </summary>
public sealed class FontSizeConverter
{
    private readonly IReadOnlyList<FontSizeDefinition> _sizes;

    public FontSizeConverter() : this(FontSizeDefinition.DefaultSet) { }

    public FontSizeConverter(IReadOnlyList<FontSizeDefinition> sizes)
    {
        _sizes = sizes ?? throw new ArgumentNullException(nameof(sizes));
    }

    // ── Parse ────────────────────────────────────────────────────────

    /// <summary>
    /// Parse user input into a pt value.
    /// Returns null if the input cannot be interpreted as a valid font size.
    /// </summary>
    public double? Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        var cleaned = Normalize(input);

        // 1) Try exact Chinese name match
        if (!string.IsNullOrWhiteSpace(cleaned))
        {
            var match = _sizes.FirstOrDefault(s =>
                string.Equals(s.Name, cleaned, StringComparison.Ordinal));
            if (match != null) return match.Pt;
        }

        // 2) Try numeric parse
        if (double.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out var pt))
            return pt;

        return null;
    }

    // ── Format ───────────────────────────────────────────────────────

    /// <summary>
    /// Format a pt value to display text.
    /// If the pt value matches a predefined Chinese name, returns that name.
    /// Otherwise returns the numeric string (e.g. "13.6").
    /// </summary>
    public string Format(double pt)
    {
        var match = _sizes.FirstOrDefault(s =>
            Math.Abs(s.Pt - pt) < 0.001);
        if (match != null) return match.Name;

        return pt.ToString("0.0############", CultureInfo.InvariantCulture);
    }

    // ── Normalize ────────────────────────────────────────────────────

    /// <summary>
    /// Clean user input for robust matching:
    ///   • Fullwidth → halfwidth  ("１２ＰＴ" → "12PT")
    ///   • Remove unit suffixes   "pt", "PT", "Pt", "磅"
    ///   • Remove "号" suffix from Chinese names
    ///   • Trim whitespace
    /// </summary>
    public string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";

        // Fullwidth → halfwidth (digits and letters)
        var chars = input.Normalize().ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (chars[i] >= 0xFF10 && chars[i] <= 0xFF19) // fullwidth digits
                chars[i] = (char)(chars[i] - 0xFEE0);
            else if (chars[i] >= 0xFF21 && chars[i] <= 0xFF3A) // fullwidth A-Z
                chars[i] = (char)(chars[i] - 0xFEE0);
            else if (chars[i] >= 0xFF41 && chars[i] <= 0xFF5A) // fullwidth a-z
                chars[i] = (char)(chars[i] - 0xFEE0);
        }

        var s = new string(chars).Trim();

        // Remove "号" suffix (e.g. "小四号" → "小四")
        if (s.EndsWith("号", StringComparison.Ordinal))
        {
            var withoutHao = s[..^1];
            // Check if the remaining is a known Chinese name
            if (_sizes.Any(fs => string.Equals(fs.Name, withoutHao, StringComparison.Ordinal)))
                s = withoutHao;
        }

        // Remove unit suffixes (case-insensitive)
        var unitSuffixes = new[] { "pt", "ＰＴ", "磅" };
        foreach (var suffix in unitSuffixes)
        {
            if (s.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                var stripped = s[..^suffix.Length].Trim();
                // Only strip if the remaining is numeric
                if (double.TryParse(stripped, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                {
                    s = stripped;
                    break;
                }
            }
        }

        return s;
    }

    // ── Slider helpers ──────────────────────────────────────────────

    /// <summary>
    /// Map a pt value to a slider index (0-based) and display label.
    /// Slider has 9 positions (0-8) mapping to the 9 Chinese font sizes.
    /// Returns (index, label). If pt doesn't match exactly, finds nearest.
    /// </summary>
    public (int Index, string Label) PtToSlider(double pt)
    {
        int bestIdx = 0;
        double bestDiff = double.MaxValue;
        for (int i = 0; i < _sizes.Count; i++)
        {
            var diff = Math.Abs(_sizes[i].Pt - pt);
            if (diff < bestDiff)
            {
                bestDiff = diff;
                bestIdx = i;
            }
        }
        return (bestIdx, _sizes[bestIdx].Name);
    }

    /// <summary>
    /// Map a slider index (0-8) back to the corresponding pt value.
    /// </summary>
    public double SliderToPt(int index)
    {
        if (index < 0) return _sizes[0].Pt;
        if (index >= _sizes.Count) return _sizes[^1].Pt;
        return _sizes[index].Pt;
    }

    // ── Suggest ──────────────────────────────────────────────────────

    /// <summary>
    /// Return suggestion strings for AutoSuggestBox based on query prefix.
    /// Matches against both Chinese names and pt values.
    /// </summary>
    public List<string> Suggest(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            // Return all names when query is empty
            return _sizes.Select(s => s.Name).ToList();
        }

        var q = query.Trim();

        // Try matching as Chinese name prefix
        var nameMatches = _sizes
            .Where(s => s.Name.StartsWith(q, StringComparison.Ordinal))
            .Select(s => s.Name);

        // Try matching as pt value prefix
        var ptMatches = _sizes
            .Where(s => s.Pt.ToString("0.0##", CultureInfo.InvariantCulture)
                            .StartsWith(q, StringComparison.Ordinal))
            .Select(s => s.Name);

        var result = nameMatches
            .Concat(ptMatches)
            .Distinct()
            .ToList();

        // If no match, return all names as fallback
        if (result.Count == 0)
            return _sizes.Select(s => s.Name).ToList();

        return result;
    }
}