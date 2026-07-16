namespace WordFormatterUI.Models.Profile;

public class BodyConfigDto
{
    public string FontCn { get; set; } = "宋体";
    public string FontEn { get; set; } = "Times New Roman";
    public double FontSize { get; set; } = 12.0;
    public string FontStyle { get; set; } = "normal";
    public string Alignment { get; set; } = "justify";
    public double LineSpacing { get; set; } = 1.5;
    public string LineSpacingMode { get; set; } = "multiple";
    public string LineSpacingUnit { get; set; } = "pt";
    public string IndentType { get; set; } = "firstLine";
    public double IndentValue { get; set; } = 2.0;
    public string IndentUnit { get; set; } = "字符";
    public double SpaceBefore { get; set; }
    public double SpaceAfter { get; set; }
    public string SpaceBeforeUnit { get; set; } = "行";
    public string SpaceAfterUnit { get; set; } = "行";
}
