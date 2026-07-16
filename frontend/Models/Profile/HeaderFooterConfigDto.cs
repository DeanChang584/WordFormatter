namespace WordFormatterUI.Models.Profile;

public class HeaderFooterConfigDto
{
    public string FontCn { get; set; } = "宋体";
    public string FontEn { get; set; } = "Times New Roman";
    public double FontSize { get; set; } = 10.5;
    public string FontStyle { get; set; } = "normal";
    public string Alignment { get; set; } = "center";
    public double HeaderDistance { get; set; } = 15.0;
    public double FooterDistance { get; set; } = 17.5;
}
