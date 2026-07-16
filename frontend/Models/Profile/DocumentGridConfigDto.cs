namespace WordFormatterUI.Models.Profile;

public class DocumentGridConfigDto
{
    public string Mode { get; set; } = "none";

    public int LinesPerPage { get; set; } = 30;

    public int CharsPerLine { get; set; } = 35;

    public bool AdjustRightIndent { get; set; } = true;

    public bool AlignToGrid { get; set; } = true;
}