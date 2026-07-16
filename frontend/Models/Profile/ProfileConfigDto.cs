namespace WordFormatterUI.Models.Profile;

/// <summary>
/// Complete formatting profile — mirrors shared/schemas.py ProfileConfig.
/// Contains page, headerFooter, body, heading (1-6), picture, table.
/// </summary>
public class ProfileConfigDto
{
    public PageConfigDto Page { get; set; } = new();
    public HeaderFooterConfigDto HeaderFooter { get; set; } = new();
    public BodyConfigDto Body { get; set; } = new();
    public Dictionary<string, HeadingStyleConfigDto> Heading { get; set; } = CreateDefaultHeadings();
    public PictureConfigDto Picture { get; set; } = new();
    public TableConfigDto Table { get; set; } = new();

    private static Dictionary<string, HeadingStyleConfigDto> CreateDefaultHeadings()
    {
        return new Dictionary<string, HeadingStyleConfigDto>
        {
            ["1"] = new() { Level = 1, FontCn = "黑体", FontEn = "Times New Roman", FontSize = 22.0, FontStyle = "bold", Alignment = "left", LineSpacing = 1.5, LineSpacingMode = "multiple", SpaceBefore = 1.0, SpaceAfter = 1.0, SpaceBeforeUnit = "行", SpaceAfterUnit = "行", IndentType = "none", IndentValue = 0.0 },
            ["2"] = new() { Level = 2, FontCn = "黑体", FontEn = "Times New Roman", FontSize = 18.0, FontStyle = "bold", Alignment = "left", LineSpacing = 1.5, LineSpacingMode = "multiple", SpaceBefore = 1.0, SpaceAfter = 1.0, SpaceBeforeUnit = "行", SpaceAfterUnit = "行", IndentType = "none", IndentValue = 0.0 },
            ["3"] = new() { Level = 3, FontCn = "黑体", FontEn = "Times New Roman", FontSize = 16.0, FontStyle = "bold", Alignment = "left", LineSpacing = 1.5, LineSpacingMode = "multiple", SpaceBefore = 0.5, SpaceAfter = 0.5, SpaceBeforeUnit = "行", SpaceAfterUnit = "行", IndentType = "none", IndentValue = 0.0 },
            ["4"] = new() { Level = 4, FontCn = "黑体", FontEn = "Times New Roman", FontSize = 14.0, FontStyle = "bold", Alignment = "left", LineSpacing = 1.5, LineSpacingMode = "multiple", SpaceBefore = 0.5, SpaceAfter = 0.5, SpaceBeforeUnit = "行", SpaceAfterUnit = "行", IndentType = "none", IndentValue = 0.0 },
            ["5"] = new() { Level = 5, FontCn = "黑体", FontEn = "Times New Roman", FontSize = 12.0, FontStyle = "bold", Alignment = "left", LineSpacing = 1.5, LineSpacingMode = "multiple", SpaceBefore = 0.25, SpaceAfter = 0.25, SpaceBeforeUnit = "行", SpaceAfterUnit = "行", IndentType = "none", IndentValue = 0.0 },
            ["6"] = new() { Level = 6, FontCn = "黑体", FontEn = "Times New Roman", FontSize = 10.5, FontStyle = "bold", Alignment = "left", LineSpacing = 1.5, LineSpacingMode = "multiple", SpaceBefore = 0.25, SpaceAfter = 0.25, SpaceBeforeUnit = "行", SpaceAfterUnit = "行", IndentType = "none", IndentValue = 0.0 },
        };
    }
}

/// <summary>
/// Wrapper for GET /api/profile response.
/// Backend returns { "profile": { ... } } inside data.
/// </summary>
public class ProfileResponseDto
{
    public ProfileConfigDto Profile { get; set; } = new();
}