namespace WordFormatterUI.Models.Profile;

/// <summary>
/// Table formatting configuration — mirrors shared/schemas.py TableConfig.
/// </summary>
public class TableConfigDto
{
    // ── 表格对齐与宽度 ──
    public string TableAlignment { get; set; } = "center";
    public string WidthMode { get; set; } = "auto";
    public double WidthValue { get; set; } = 0.0;
    public string WidthUnit { get; set; } = "cm";
    public bool AutoFitColumns { get; set; } = true;

    // ── 表头字体 ──
    public string HeaderFontCn { get; set; } = "宋体";
    public string HeaderFontEn { get; set; } = "Times New Roman";
    public double HeaderSize { get; set; } = 10.5;
    public bool HeaderBold { get; set; } = true;
    public bool HeaderTextCenter { get; set; } = true;
    public string HeaderBgColor { get; set; } = "";
    public string BodyBgColor { get; set; } = "";

    // ── 边框 ──
    public string BorderStyle { get; set; } = "all";
    public string BorderColor { get; set; } = "#000000";
    public double BorderWidth { get; set; } = 0.5;

    // ── 单元格对齐 ──
    public string CellAlignH { get; set; } = "left";
    public string CellAlignV { get; set; } = "middle";

    // ── 单元格边距 ──
    public double CellMarginH { get; set; } = 0.19;
    public string CellMarginHUnit { get; set; } = "cm";
    public double CellMarginV { get; set; } = 0.0;
    public string CellMarginVUnit { get; set; } = "cm";

    // ── 特殊格式（缩进） ──
    public string IndentType { get; set; } = "none";
    public double IndentValue { get; set; } = 0.0;
    public string IndentUnit { get; set; } = "字符";

    // ── 全局字形（应用到所有单元格） ──
    public bool FontBold { get; set; } = false;
    public bool FontItalic { get; set; } = false;
    public bool FontUnderline { get; set; } = false;

    // ── 行距 ──
    public double LineSpacing { get; set; } = 1.5;
    public string LineSpacingMode { get; set; } = "multiple";
    public string LineSpacingUnit { get; set; } = "pt";

    // ── 行高 ──
    public string RowHeightMode { get; set; } = "auto";
    public double RowHeight { get; set; } = 0.8;
    public string RowHeightUnit { get; set; } = "cm";

    // ── 跨页选项 ──
    public bool AutoSplit { get; set; } = true;
    public bool RepeatHeader { get; set; } = false;
}