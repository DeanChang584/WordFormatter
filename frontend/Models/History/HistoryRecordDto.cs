using WordFormatterUI.Models.Format;
using WordFormatterUI.Models.Profile;

namespace WordFormatterUI.Models.History;

/// <summary>
/// History record — matches the backend history_manager response shape.
/// Summary fields: id, time, duration, success, failed, template.
/// Detail fields (added by detail endpoint): profile, files, results.
/// </summary>
public class HistoryRecordDto
{
    public string Id { get; set; } = "";
    public string Time { get; set; } = "";
    public double Duration { get; set; }
    public int Success { get; set; }
    public int Failed { get; set; }
    public string Template { get; set; } = "";
    public int FileCount { get; set; }
    public ProfileConfigDto? Profile { get; set; }
    public List<HistoryFileItemDto>? Files { get; set; }
    public TaskResultDto? Results { get; set; }
}

/// <summary>
/// Wrapper for GET /api/history response.
/// Backend returns { "history": [...] }.
/// </summary>
public class HistoryListDto
{
    public List<HistoryRecordDto> History { get; set; } = new();
}
