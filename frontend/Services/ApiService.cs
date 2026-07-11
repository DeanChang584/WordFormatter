using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WordFormatterUI.Services;

/// <summary>
/// HTTP client wrapper for WordFormatter backend API.
/// All DTOs are deserialized from the same JSON contract as shared/schemas.py.
/// </summary>
public sealed class ApiService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public ApiService(string baseUrl = "http://127.0.0.1:8765")
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    // ───────────────────────── Health ─────────────────────────

    public async Task<bool> HealthAsync()
    {
        try
        {
            var resp = await _http.GetAsync("/api/health");
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    // ───────────────────────── Profile ─────────────────────────

    public async Task<ProfileDto?> GetProfileAsync()
    {
        return await _http.GetFromJsonAsync<ProfileDto>("/api/profile", JsonOptions);
    }

    public async Task<bool> UpdateProfileAsync(ProfileDto profile)
    {
        var resp = await _http.PutAsJsonAsync("/api/profile", profile, JsonOptions);
        return resp.IsSuccessStatusCode;
    }

    // ───────────────────────── Files ─────────────────────────

    public async Task<FilesResponse?> GetFilesAsync()
    {
        return await _http.GetFromJsonAsync<FilesResponse>("/api/files", JsonOptions);
    }

    public async Task<FilesResponse?> SelectFilesAsync(IEnumerable<string> paths)
    {
        var body = new FileSelectRequest { Paths = paths.ToList() };
        var resp = await _http.PostAsJsonAsync("/api/files/select", body, JsonOptions);
        return await resp.Content.ReadFromJsonAsync<FilesResponse>(JsonOptions);
    }

    public async Task<FilesResponse?> SelectFolderAsync(string folder)
    {
        var body = new FolderRequest { Folder = folder };
        var resp = await _http.PostAsJsonAsync("/api/files/folder", body, JsonOptions);
        return await resp.Content.ReadFromJsonAsync<FilesResponse>(JsonOptions);
    }

    public async Task<FilesResponse?> RemoveFilesAsync(IEnumerable<string> paths)
    {
        var body = new FileDeleteRequest { Paths = paths.ToList() };
        var resp = await _http.PostAsJsonAsync("/api/files/remove", body, JsonOptions);
        return await resp.Content.ReadFromJsonAsync<FilesResponse>(JsonOptions);
    }

    public async Task<bool> ClearFilesAsync()
    {
        var resp = await _http.DeleteAsync("/api/files/all");
        return resp.IsSuccessStatusCode;
    }

    // ───────────────────────── Format Tasks ─────────────────────────

    public async Task<FormatStartResponse?> StartFormatAsync(FormatStartRequest request)
    {
        var resp = await _http.PostAsJsonAsync("/api/format/start", request, JsonOptions);
        return await resp.Content.ReadFromJsonAsync<FormatStartResponse>(JsonOptions);
    }

    public async Task<FormatProgressResponse?> GetFormatProgressAsync(string taskId)
    {
        return await _http.GetFromJsonAsync<FormatProgressResponse>($"/api/format/{taskId}/progress", JsonOptions);
    }

    public async Task<FormatResultResponse?> GetFormatResultAsync(string taskId)
    {
        return await _http.GetFromJsonAsync<FormatResultResponse>($"/api/format/{taskId}/result", JsonOptions);
    }

    // ───────────────────────── Theme ─────────────────────────

    public async Task<ThemeResponse?> GetThemeAsync()
    {
        return await _http.GetFromJsonAsync<ThemeResponse>("/api/theme", JsonOptions);
    }

    public async Task<bool> SetThemeAsync(string mode)
    {
        var body = new ThemeRequest { Mode = mode };
        var resp = await _http.PutAsJsonAsync("/api/theme", body, JsonOptions);
        return resp.IsSuccessStatusCode;
    }
}

// ═══════════════════════════════════════════════════════════
// DTOs — mirror shared/schemas.py snake_case JSON contract
// ═══════════════════════════════════════════════════════════

#region Profile DTOs

public class ProfileDto
{
    public PageDto Page { get; set; } = new();
    public BodyDto Body { get; set; } = new();
    public ParagraphDto Paragraph { get; set; } = new();
    public Dictionary<string, HeadingDto> Headings { get; set; } = new();
    public string OutputDir { get; set; } = "";
}

public class PageDto
{
    public double MarginTop { get; set; } = 25.4;
    public double MarginBottom { get; set; } = 25.4;
    public double MarginLeft { get; set; } = 31.8;
    public double MarginRight { get; set; } = 31.8;
    public string TextDirection { get; set; } = "纵向";
    public string PaperSize { get; set; } = "A4";
    public string MarginTopUnit { get; set; } = "mm";
    public string MarginBottomUnit { get; set; } = "mm";
    public string MarginLeftUnit { get; set; } = "mm";
    public string MarginRightUnit { get; set; } = "mm";
    public double HeaderMargin { get; set; } = 15.0;
    public string HeaderMarginUnit { get; set; } = "mm";
    public double FooterMargin { get; set; } = 17.5;
    public string FooterMarginUnit { get; set; } = "mm";
    public string SectionMode { get; set; } = "全文排版";
}

public class BodyDto
{
    public string FontCn { get; set; } = "宋体";
    public string FontEn { get; set; } = "Times New Roman";
    public double FontSize { get; set; } = 12.0;
    public string FontColor { get; set; } = "#000000";
    public bool FontBold { get; set; } = false;
    public bool FontItalic { get; set; } = false;
}

public class ParagraphDto
{
    public string LineSpacingMode { get; set; } = "multiple";
    public double LineSpacingValue { get; set; } = 1.5;
    public string SpecialFormat { get; set; } = "首行";
    public double IndentValue { get; set; } = 2.0;
    public string IndentUnit { get; set; } = "ch";
    public double FirstLineIndent { get; set; } = 7.4;
    public string FirstLineIndentUnit { get; set; } = "mm";
    public string Alignment { get; set; } = "justify";
    public double SpaceBefore { get; set; } = 0.0;
    public string SpaceBeforeUnit { get; set; } = "行";
    public double SpaceAfter { get; set; } = 0.0;
    public string SpaceAfterUnit { get; set; } = "行";
}

public class HeadingDto
{
    public int Level { get; set; } = 1;
    public string FontCn { get; set; } = "黑体";
    public string FontEn { get; set; } = "Times New Roman";
    public double FontSize { get; set; } = 22.0;
    public string FontColor { get; set; } = "#000000";
    public bool FontBold { get; set; } = true;
    public bool FontItalic { get; set; } = false;
    public string Alignment { get; set; } = "left";
    public string SpecialFormat { get; set; } = "首行";
    public double IndentValue { get; set; } = 0.0;
    public string IndentUnit { get; set; } = "ch";
    public double SpaceBefore { get; set; } = 1.0;
    public string SpaceBeforeUnit { get; set; } = "行";
    public double SpaceAfter { get; set; } = 1.0;
    public string SpaceAfterUnit { get; set; } = "行";
    public string LineSpacingMode { get; set; } = "multiple";
    public double LineSpacingValue { get; set; } = 1.5;
    public double FirstLineIndent { get; set; } = 0.0;
    public string FirstLineIndentUnit { get; set; } = "字符";
}

#endregion

#region File DTOs

public class FileSelectRequest
{
    public List<string> Paths { get; set; } = new();
}

public class FolderRequest
{
    public string Folder { get; set; } = "";
}

public class FileDeleteRequest
{
    public List<string> Paths { get; set; } = new();
}

public class FilesResponse
{
    public List<string> Files { get; set; } = new();
    public int Count { get; set; }
    public List<string> Added { get; set; } = new();
}

#endregion

#region Format Task DTOs

public class FormatStartRequest
{
    public List<string> Files { get; set; } = new();
    public ProfileDto? Profile { get; set; }
    public string OutputDir { get; set; } = "";
}

public class FormatStartResponse
{
    public string TaskId { get; set; } = "";
}

public class FormatProgressResponse
{
    public string TaskId { get; set; } = "";
    public int Current { get; set; }
    public int Total { get; set; }
    public string Status { get; set; } = "idle";
}

public class FormatResultItem
{
    public string FilePath { get; set; } = "";
    public bool Success { get; set; }
    public string Message { get; set; } = "";
}

public class FormatResultResponse
{
    public string TaskId { get; set; } = "";
    public List<FormatResultItem> Results { get; set; } = new();
    public int OkCount { get; set; }
    public int FailCount { get; set; }
}

#endregion

#region Theme DTOs

public class ThemeResponse
{
    public string Mode { get; set; } = "system";
}

public class ThemeRequest
{
    public string Mode { get; set; } = "system";
}

#endregion

#region Common DTOs

public class OkResponse
{
    public bool Ok { get; set; } = true;
    public string Detail { get; set; } = "";
}

#endregion