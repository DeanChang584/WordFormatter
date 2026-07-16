namespace WordFormatterUI.Models.Format;

/// <summary>
/// Response from POST /api/format/start.
/// Backend returns { "taskId": "..." }.
/// </summary>
public class FormatStartResultDto
{
    public string TaskId { get; set; } = "";
}
