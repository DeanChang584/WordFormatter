namespace WordFormatterUI.Models.Format;

public class TaskStatusDto
{
    public string State { get; set; } = "idle";
    public int Progress { get; set; }
    public int Current { get; set; }
    public int Total { get; set; }
    public string CurrentFile { get; set; } = "";
}
