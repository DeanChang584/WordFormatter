namespace WordFormatterUI.Models.Profile;

public class PictureConfigDto
{
    // Size mode: "width" / "height" / "auto"
    public string SizeMode { get; set; } = "auto";

    // Dimensions
    public double Width { get; set; } = 14.0;
    public string WidthUnit { get; set; } = "cm";
    public double Height { get; set; } = 8.0;
    public string HeightUnit { get; set; } = "cm";

    // Scaling
    public bool KeepAspectRatio { get; set; } = true;
    public bool NoEnlarge { get; set; } = true;

    // Layout
    public string Alignment { get; set; } = "center";       // left / center / right / top / middle / bottom / distribute_h / distribute_v
    public string WrappingStyle { get; set; } = "inline";

    // Compression
    public int Quality { get; set; } = 85;
    public int MaxSidePixels { get; set; } = 1600;
    public int MaxFileSize { get; set; } = 2 * 1024 * 1024;  // 2MB
    public bool AutoCompress { get; set; } = false;
}