using System.Text.Json.Serialization;

namespace WordFormatterUI.Models;

/// <summary>
/// Generic wrapper for the backend's standard JSON envelope:
/// { "success": true, "code": 0, "message": "OK", "data": { ... } }
/// </summary>
public class ApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}
