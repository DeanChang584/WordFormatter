using System.Collections.Generic;

namespace WordFormatterUI.Models;

/// <summary>
/// Defines a Chinese font size with its display name and pt value.
/// Used by <see cref="Services.FontSizeConverter"/> for bidirectional
/// conversion between Chinese names (e.g. "小四") and pt values (12).
/// </summary>
public sealed class FontSizeDefinition
{
    /// <summary>Chinese display name, e.g. "小四", "四号", "初号".</summary>
    public string Name { get; init; } = "";

    /// <summary>Corresponding pt value, e.g. 12, 14, 42.</summary>
    public double Pt { get; init; }

    // ── Predefined set ──────────────────────────────────────────────

    /// <summary>
    /// The standard 14-level Chinese font size table (初号 … 小六).
    /// </summary>
    public static readonly IReadOnlyList<FontSizeDefinition> DefaultSet = new List<FontSizeDefinition>
    {
        new() { Name = "初号", Pt = 42 },
        new() { Name = "小初", Pt = 36 },
        new() { Name = "一号", Pt = 26 },
        new() { Name = "小一", Pt = 24 },
        new() { Name = "二号", Pt = 22 },
        new() { Name = "小二", Pt = 18 },
        new() { Name = "三号", Pt = 16 },
        new() { Name = "小三", Pt = 15 },
        new() { Name = "四号", Pt = 14 },
        new() { Name = "小四", Pt = 12 },
        new() { Name = "五号", Pt = 10.5 },
        new() { Name = "小五", Pt = 9 },
        new() { Name = "六号", Pt = 7.5 },
        new() { Name = "小六", Pt = 6.5 },
    }.AsReadOnly();
}