namespace WordFormatterUI.Services;

/// <summary>
/// Converts a formatted .docx file to PDF via COM automation
/// (WPS or Microsoft Word).  Implementations handle detection,
/// conversion, and COM cleanup for their respective office suite.
/// </summary>
public interface IDocumentPdfConverter
{
    /// <summary>Whether the target office suite is installed and usable.</summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Convert <paramref name="docxPath"/> to a PDF file.
    /// Returns the absolute path to the generated PDF.
    /// </summary>
    Task<string> ConvertToPdfAsync(string docxPath);
}
