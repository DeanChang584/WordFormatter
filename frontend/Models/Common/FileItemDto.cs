using System.Text.Json.Serialization;

namespace WordFormatterUI.Models.Common;

public class FileItemDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public int Size { get; set; }
    public string ModifiedTime { get; set; } = "";
    public string Status { get; set; } = "waiting";

    /// <summary>Used by the file-list CheckBox binding (not serialized).</summary>
    [JsonIgnore]
    public bool IsSelected { get; set; }

    /// <summary>Human-readable file size (e.g. "12.3 KB"). Not serialized.</summary>
    [JsonIgnore]
    public string SizeText => FormatSize(Size);

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        double kb = bytes / 1024.0;
        if (kb < 1024) return $"{kb:0.#} KB";
        double mb = kb / 1024.0;
        if (mb < 1024) return $"{mb:0.#} MB";
        double gb = mb / 1024.0;
        return $"{gb:0.#} GB";
    }
}
