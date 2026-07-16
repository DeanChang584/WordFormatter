namespace WordFormatterUI.Models.Format;

public class FileResultDto
{
    public string File { get; set; } = "";
    public string Status { get; set; } = "";  // success / error / skipped
    public string Output { get; set; } = "";
    public string OutputPath { get; set; } = "";
    public string Message { get; set; } = "";
}