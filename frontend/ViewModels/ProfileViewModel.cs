using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WordFormatterUI.Services;

namespace WordFormatterUI.ViewModels;

public partial class ProfileViewModel : ObservableObject
{
    private readonly ApiService _api;

    public ProfileViewModel(ApiService api)
    {
        _api = api;
    }

    [ObservableProperty]
    private ProfileDto? _profile;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private bool _isDirty;

    // ── Page fields ──
    [ObservableProperty] private double _marginTop = 25.4;
    [ObservableProperty] private double _marginBottom = 25.4;
    [ObservableProperty] private double _marginLeft = 31.8;
    [ObservableProperty] private double _marginRight = 31.8;
    [ObservableProperty] private string _paperSize = "A4";
    [ObservableProperty] private string _textDirection = "纵向";
    [ObservableProperty] private string _sectionMode = "全文排版";

    // ── Body fields ──
    [ObservableProperty] private string _bodyFontCn = "宋体";
    [ObservableProperty] private string _bodyFontEn = "Times New Roman";
    [ObservableProperty] private double _bodyFontSize = 12.0;
    [ObservableProperty] private string _bodyFontColor = "#000000";
    [ObservableProperty] private bool _bodyFontBold;
    [ObservableProperty] private bool _bodyFontItalic;

    // ── Paragraph fields ──
    [ObservableProperty] private double _lineSpacingValue = 1.5;
    [ObservableProperty] private string _specialFormat = "首行";
    [ObservableProperty] private string _alignment = "justify";
    [ObservableProperty] private double _firstLineIndent = 7.4;

    [RelayCommand]
    public async Task LoadProfileAsync()
    {
        IsLoading = true;
        try
        {
            var dto = await _api.GetProfileAsync();
            if (dto is not null)
            {
                Profile = dto;
                // Map to UI fields
                MarginTop = dto.Page.MarginTop;
                MarginBottom = dto.Page.MarginBottom;
                MarginLeft = dto.Page.MarginLeft;
                MarginRight = dto.Page.MarginRight;
                PaperSize = dto.Page.PaperSize;
                TextDirection = dto.Page.TextDirection;
                SectionMode = dto.Page.SectionMode;

                BodyFontCn = dto.Body.FontCn;
                BodyFontEn = dto.Body.FontEn;
                BodyFontSize = dto.Body.FontSize;
                BodyFontColor = dto.Body.FontColor;
                BodyFontBold = dto.Body.FontBold;
                BodyFontItalic = dto.Body.FontItalic;

                LineSpacingValue = dto.Paragraph.LineSpacingValue;
                SpecialFormat = dto.Paragraph.SpecialFormat;
                Alignment = dto.Paragraph.Alignment;
                FirstLineIndent = dto.Paragraph.FirstLineIndent;

                IsDirty = false;
                StatusMessage = "";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载配置失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SaveProfileAsync()
    {
        if (Profile is null) return;

        IsLoading = true;
        try
        {
            // Map UI fields back to DTO
            Profile.Page.MarginTop = MarginTop;
            Profile.Page.MarginBottom = MarginBottom;
            Profile.Page.MarginLeft = MarginLeft;
            Profile.Page.MarginRight = MarginRight;
            Profile.Page.PaperSize = PaperSize;
            Profile.Page.TextDirection = TextDirection;
            Profile.Page.SectionMode = SectionMode;

            Profile.Body.FontCn = BodyFontCn;
            Profile.Body.FontEn = BodyFontEn;
            Profile.Body.FontSize = BodyFontSize;
            Profile.Body.FontColor = BodyFontColor;
            Profile.Body.FontBold = BodyFontBold;
            Profile.Body.FontItalic = BodyFontItalic;

            Profile.Paragraph.LineSpacingValue = LineSpacingValue;
            Profile.Paragraph.SpecialFormat = SpecialFormat;
            Profile.Paragraph.Alignment = Alignment;
            Profile.Paragraph.FirstLineIndent = FirstLineIndent;

            var ok = await _api.UpdateProfileAsync(Profile);
            if (ok)
            {
                IsDirty = false;
                StatusMessage = "配置已保存";
            }
            else
            {
                StatusMessage = "保存失败";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"保存失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnMarginTopChanged(double value) => IsDirty = true;
    partial void OnMarginBottomChanged(double value) => IsDirty = true;
    partial void OnMarginLeftChanged(double value) => IsDirty = true;
    partial void OnMarginRightChanged(double value) => IsDirty = true;
    partial void OnPaperSizeChanged(string value) => IsDirty = true;
    partial void OnTextDirectionChanged(string value) => IsDirty = true;
    partial void OnSectionModeChanged(string value) => IsDirty = true;
    partial void OnBodyFontCnChanged(string value) => IsDirty = true;
    partial void OnBodyFontEnChanged(string value) => IsDirty = true;
    partial void OnBodyFontSizeChanged(double value) => IsDirty = true;
    partial void OnBodyFontColorChanged(string value) => IsDirty = true;
    partial void OnBodyFontBoldChanged(bool value) => IsDirty = true;
    partial void OnBodyFontItalicChanged(bool value) => IsDirty = true;
    partial void OnLineSpacingValueChanged(double value) => IsDirty = true;
    partial void OnSpecialFormatChanged(string value) => IsDirty = true;
    partial void OnAlignmentChanged(string value) => IsDirty = true;
    partial void OnFirstLineIndentChanged(double value) => IsDirty = true;
}