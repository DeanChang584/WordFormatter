namespace WordFormatterUI.Models.Profile;

public class HeadingStyleConfigDto
{
    public int Level { get; set; } = 1;
    public string FontCn { get; set; } = "黑体";
    public string FontEn { get; set; } = "Times New Roman";
    public double FontSize { get; set; } = 22.0;
    public string FontStyle { get; set; } = "bold";
    public string FontColor { get; set; } = "#000000";
    public string Alignment { get; set; } = "left";
    public double LineSpacing { get; set; } = 1.5;
    public string LineSpacingMode { get; set; } = "multiple";
    public string LineSpacingUnit { get; set; } = "pt";
    public string IndentType { get; set; } = "none";
    public double IndentValue { get; set; }
    public string IndentUnit { get; set; } = "字符";
    public double SpaceBefore { get; set; } = 1.0;
    public double SpaceAfter { get; set; } = 1.0;
    public string SpaceBeforeUnit { get; set; } = "行";
    public string SpaceAfterUnit { get; set; } = "行";
}