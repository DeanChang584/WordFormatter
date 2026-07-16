namespace WordFormatterUI.Models.Preview;

/// <summary>
/// Preview output — mirrors shared/schemas.py PreviewResult.
/// Level 1 (MVP) = parameter summary text.
/// Level 2 = real PDF preview.
/// </summary>
public class PreviewResultDto
{
    public int PageCount { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> PreviewImages { get; set; } = new();
}

/// <summary>POST /api/preview/pdf response (HTTP 202).</summary>
public class PdfPreviewStartDto
{
    public string TaskId { get; set; } = "";
}

/// <summary>GET /api/preview/pdf/{id} polling response.</summary>
public class PdfPreviewStatusDto
{
    public string State { get; set; } = "";
    public string? PreviewPath { get; set; }
    public string? Error { get; set; }
}
