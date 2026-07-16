namespace WordFormatterUI.Models.Files;

/// <summary>
/// Response from POST /api/files/add and POST /api/files/add-folder.
/// Backend returns { "count": N }.
/// </summary>
public class AddCountDto
{
    public int Count { get; set; }
}
