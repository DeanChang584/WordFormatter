namespace WordFormatterUI.Models.Preview;

/// <summary>
/// Wrapper for POST /api/preview response.
/// Backend returns { "preview": "..." }.
/// </summary>
public class PreviewDataDto
{
    public string Preview { get; set; } = "";
}
