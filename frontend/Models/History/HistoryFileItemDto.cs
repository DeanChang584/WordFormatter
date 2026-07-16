namespace WordFormatterUI.Models.History;

/// <summary>
/// A file entry within a history record with metadata.
/// Matches the backend's HistoryFileItem (shared/schemas.py).
/// </summary>
public class HistoryFileItemDto
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public string Status { get; set; } = "";  // "success" / "error" / "skipped" / ""
    public string OutputName { get; set; } = "";
    public string OutputPath { get; set; } = "";
}