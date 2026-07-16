using System.Reflection;
using System.Runtime.InteropServices;

namespace WordFormatterUI.Services;

/// <summary>
/// Converts .docx to PDF using Microsoft Word COM automation.
/// Detects Word via the "Word.Application" ProgID, then calls
/// <c>ExportAsFixedFormat</c> to produce the PDF.
/// </summary>
public sealed class WordPdfConverter : IDocumentPdfConverter
{
    private const string ProgId = "Word.Application";

    public bool IsAvailable
    {
        get
        {
            try { return Type.GetTypeFromProgID(ProgId) is not null; }
            catch { return false; }
        }
    }

    public Task<string> ConvertToPdfAsync(string docxPath)
        => Task.Run(() => ConvertCore(docxPath));

    private static string ConvertCore(string docxPath)
    {
        object? app = null;
        object? docs = null;
        object? doc = null;
        var pdfPath = Path.ChangeExtension(docxPath, ".pdf");
        var m = Missing.Value;

        try
        {
            var t = Type.GetTypeFromProgID(ProgId)!;
            app = Activator.CreateInstance(t)!;
            var appType = app.GetType();

            appType.InvokeMember("Visible", BindingFlags.SetProperty, null, app, [false]);
            appType.InvokeMember("DisplayAlerts", BindingFlags.SetProperty, null, app, [0]);

            docs = appType.InvokeMember("Documents", BindingFlags.GetProperty, null, app, null)!;

            doc = docs.GetType().InvokeMember("Open",
                BindingFlags.InvokeMethod, null, docs,
                [Path.GetFullPath(docxPath), m, true, m, m, m, m, m, m, m, m, m]);

            if (doc is null)
                throw new InvalidOperationException("Word 无法打开文档");

            doc.GetType().InvokeMember("ExportAsFixedFormat",
                BindingFlags.InvokeMethod, null, doc,
                [Path.GetFullPath(pdfPath), 17, m, m, m, m, m, m, m]);

            doc.GetType().InvokeMember("Close",
                BindingFlags.InvokeMethod, null, doc, [m, m, m]);

            appType.InvokeMember("Quit", BindingFlags.InvokeMethod, null, app, null);

            return pdfPath;
        }
        catch (TargetInvocationException ex)
        {
            throw new InvalidOperationException(
                $"Word PDF 转换失败: {ex.InnerException?.Message ?? ex.Message}", ex);
        }
        finally
        {
            if (doc is not null) Marshal.ReleaseComObject(doc);
            if (docs is not null) Marshal.ReleaseComObject(docs);
            if (app is not null) Marshal.ReleaseComObject(app);
        }
    }
}
