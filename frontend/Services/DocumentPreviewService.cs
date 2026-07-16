namespace WordFormatterUI.Services;

/// <summary>
/// Auto-detects an available PDF converter (WPS first, Word fallback)
/// and delegates conversion.  Pre-warms the WPS COM singleton at app
/// startup so the first preview is fast.
/// </summary>
public class DocumentPreviewService
{
    private static bool _warmed;

    private static readonly IDocumentPdfConverter[] Converters =
    {
        new WpsPdfConverter(),
        new WordPdfConverter(),
    };

    /// <summary>
    /// Call once at app startup to pre-warm WPS COM in the background.
    /// Fire-and-forget — errors are silently ignored (the converter will
    /// fall back to Word or show a prompt later).
    /// </summary>
    public static void WarmUp()
    {
        if (_warmed) return;
        _warmed = true;
        WpsPdfConverter.InitializeAsync();
    }

    /// <summary>
    /// Release COM resources on app shutdown.
    /// </summary>
    public static void Shutdown()
    {
        WpsPdfConverter.Shutdown();
    }

    public IDocumentPdfConverter? Detect()
    {
        foreach (var c in Converters)
            if (c.IsAvailable) return c;
        return null;
    }

    public async Task<string> ConvertToPdfAsync(string docxPath)
    {
        var converter = Detect();
        if (converter is null)
        {
            throw new InvalidOperationException(
                "未检测到 WPS 或 Microsoft Word。\n" +
                "预览功能需要其中任意一种办公软件。\n" +
                "请安装后重试。");
        }

        return await converter.ConvertToPdfAsync(docxPath);
    }
}
