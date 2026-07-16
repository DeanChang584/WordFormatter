using WordFormatterUI.Models.Common;

namespace WordFormatterUI.Models.Files;

/// <summary>
/// Response from GET /api/files and POST /api/files/search.
/// Backend returns { "files": [ {id,name,path,size,modifiedTime,status}, ... ] }.
/// </summary>
public class FileListDto
{
    public List<FileItemDto> Files { get; set; } = new();
}
