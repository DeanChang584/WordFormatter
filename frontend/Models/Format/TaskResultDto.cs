using System.Text.Json.Serialization;

namespace WordFormatterUI.Models.Format;

public class TaskResultDto
{
    [JsonPropertyName("success")]
    public int Ok { get; set; }
    [JsonPropertyName("failed")]
    public int Fail { get; set; }
    public int Skipped { get; set; }
    public int Total { get; set; }
    public double Elapsed { get; set; }
    public string OutputDir { get; set; } = "";
    public List<FileResultDto> Results { get; set; } = new();
    public List<FileResultDto> FailedFiles { get; set; } = new();
}
