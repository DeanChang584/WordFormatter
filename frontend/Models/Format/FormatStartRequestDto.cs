namespace WordFormatterUI.Models.Format;

public class FormatStartRequestDto
{
    public List<string> Files { get; set; } = new();
    public object? Profile { get; set; }  // string (template ID) or ProfileConfigDto
    public string OutputDir { get; set; } = "";
}
