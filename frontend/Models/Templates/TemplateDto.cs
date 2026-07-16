using WordFormatterUI.Models.Profile;

namespace WordFormatterUI.Models.Templates;

public class TemplateDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsDefault { get; set; }
    public string Version { get; set; } = "2.0";
    public string Author { get; set; } = "";
    public string Description { get; set; } = "";
    public string CreateTime { get; set; } = "";
    public string UpdateTime { get; set; } = "";
    public ProfileConfigDto? Profile { get; set; }
}

/// <summary>
/// Wrapper for GET /api/templates response.
/// Backend returns { "templates": [...] }.
/// </summary>
public class TemplateListDto
{
    public List<TemplateDto> Templates { get; set; } = new();
}
