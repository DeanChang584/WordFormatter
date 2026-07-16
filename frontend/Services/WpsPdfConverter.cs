using System.Reflection;
using System.Runtime.InteropServices;

namespace WordFormatterUI.Services;

/// <summary>
/// Converts .docx to PDF using WPS Office COM automation.
/// Maintains a global singleton WPS Application instance for speed.
/// </summary>
public sealed class WpsPdfConverter : IDocumentPdfConverter
{
    private static readonly string[] ProgIds =
        { "KWPS.Application", "WPS.Application", "wps.Application" };

    private static readonly object _lock = new();
    private static object? _wpsApp;
    private static Type? _wpsAppType;
    private static bool _initialised;

    /// <summary>
    /// Pre-start the WPS COM singleton at app startup (call once, fire-and-forget).
    /// Creates a hidden WPS instance so the first preview is fast.
    /// </summary>
    public static void InitializeAsync()
    {
        if (_initialised) return;
        Task.Run(() =>
        {
            lock (_lock)
            {
                if (_initialised) return;
                foreach (var pid in ProgIds)
                {
                    try
                    {
                        var t = Type.GetTypeFromProgID(pid);
                        if (t is null) continue;
                        var app = Activator.CreateInstance(t);
                        var appType = app!.GetType();
                        appType.InvokeMember("Visible", BindingFlags.SetProperty, null, app, [false]);
                        _wpsApp = app;
                        _wpsAppType = appType;
                        _initialised = true;
                        return;
                    }
                    catch { }
                }
            }
        });
    }

    public bool IsAvailable
    {
        get
        {
            if (_initialised && _wpsApp is not null) return true;
            foreach (var pid in ProgIds)
            {
                try { if (Type.GetTypeFromProgID(pid) is not null) return true; }
                catch { }
            }
            return false;
        }
    }

    public Task<string> ConvertToPdfAsync(string docxPath)
        => Task.Run(() => ConvertCore(docxPath));

    private string ConvertCore(string docxPath)
    {
        var pdfPath = Path.ChangeExtension(docxPath, ".pdf");
        var m = Missing.Value;

        // Use the pre-warmed singleton if available, else create fresh
        lock (_lock)
        {
            if (_wpsApp is not null)
            {
                return ConvertWithApp(_wpsApp, _wpsAppType!, docxPath, pdfPath, m);
            }
        }

        // Fallback: create fresh WPS instance
        foreach (var pid in ProgIds)
        {
            object? app = null, docs = null, doc = null;
            try
            {
                var t = Type.GetTypeFromProgID(pid);
                if (t is null) continue;
                app = Activator.CreateInstance(t)!;
                var appType = app.GetType();
                appType.InvokeMember("Visible", BindingFlags.SetProperty, null, app, [false]);
                return ConvertWithApp(app, appType, docxPath, pdfPath, m);
            }
            finally
            {
                if (app is not null) Marshal.ReleaseComObject(app);
            }
        }

        throw new InvalidOperationException("未检测到 WPS Office");
    }

    private static string ConvertWithApp(object app, Type appType,
        string docxPath, string pdfPath, Missing m)
    {
        object? docs = null;
        object? doc = null;

        lock (_lock) // serialise COM calls to the singleton
        {
            try
            {
                docs = appType.InvokeMember("Documents",
                    BindingFlags.GetProperty, null, app, null)!;

                doc = docs.GetType().InvokeMember("Open",
                    BindingFlags.InvokeMethod, null, docs,
                    [Path.GetFullPath(docxPath), m, true, m, m, m, m, m, m, m, m, m]);

                if (doc is null)
                    throw new InvalidOperationException("WPS 无法打开文档");

                doc.GetType().InvokeMember("ExportAsFixedFormat",
                    BindingFlags.InvokeMethod, null, doc,
                    [Path.GetFullPath(pdfPath), 17, m, m, m, m, m, m, m]);

                doc.GetType().InvokeMember("Close",
                    BindingFlags.InvokeMethod, null, doc, [m, m, m]);

                return pdfPath;
            }
            catch (TargetInvocationException ex)
            {
                throw new InvalidOperationException(
                    $"WPS PDF 转换失败: {ex.InnerException?.Message ?? ex.Message}", ex);
            }
            finally
            {
                if (doc is not null) Marshal.ReleaseComObject(doc);
                if (docs is not null) Marshal.ReleaseComObject(docs);
            }
        }
    }

    /// <summary>Release the singleton WPS COM instance (call on app exit).</summary>
    public static void Shutdown()
    {
        lock (_lock)
        {
            if (_wpsApp is not null)
            {
                try
                {
                    _wpsAppType?.InvokeMember("Quit",
                        BindingFlags.InvokeMethod, null, _wpsApp, null);
                }
                catch { }
                Marshal.ReleaseComObject(_wpsApp);
                _wpsApp = null;
                _wpsAppType = null;
                _initialised = false;
            }
        }
    }
}
