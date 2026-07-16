namespace WordFormatterUI.Models.Profile;

public class PageConfigDto
{
    public string PaperSize { get; set; } = "A4";
    public string Orientation { get; set; } = "portrait";
    public double MarginTop { get; set; } = 25.4;
    public double MarginBottom { get; set; } = 25.4;
    public double MarginLeft { get; set; } = 31.7;
    public double MarginRight { get; set; } = 31.7;
    public double Gutter { get; set; }
    public string GutterPosition { get; set; } = "left";
    public bool PageNumber { get; set; } = true;
    public double CustomWidth { get; set; }
    public double CustomHeight { get; set; }
    public DocumentGridConfigDto DocumentGrid { get; set; } = new();
}
