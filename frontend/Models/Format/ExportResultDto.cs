namespace WordFormatterUI.Models.Format;

/// <summary>
/// Response from POST /api/templates/export.
/// Backend returns { "exportedFile": "..." }.
/// </summary>
public class ExportResultDto
{
    public string ExportedFile { get; set; } = "";
}
