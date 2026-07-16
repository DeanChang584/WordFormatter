namespace WordFormatterUI.Models.Files;

/// <summary>
/// Response from GET /api/files/recent.
/// Backend returns { "recent": [{ "path": "...", "type": "file"|"folder" }] }.
/// </summary>
public class RecentListDto
{
    public List<RecentRecordDto> Recent { get; set; } = new();
}

public class RecentRecordDto
{
    public string Path { get; set; } = "";
    public string Type { get; set; } = "";  // "file" or "folder"
}
