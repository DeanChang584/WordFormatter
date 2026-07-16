using WordFormatterUI.Models.Common;

namespace WordFormatterUI.Models.Format;

/// <summary>
/// Full task object — mirrors shared/schemas.py Task.
/// Represents a batch-formatting task with lifecycle metadata.
/// (Distinct from TaskStatusDto which is the polling status response.)
/// </summary>
public class TaskDto
{
    public string TaskId { get; set; } = "";
    public string Status { get; set; } = "idle";
    public string CreateTime { get; set; } = "";
    public string? StartTime { get; set; }
    public string? FinishTime { get; set; }
    public List<FileItemDto> Files { get; set; } = new();
}
