using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WordFormatterUI.Models.Profile;

namespace WordFormatterUI.ViewModels;

/// <summary>
/// Header / footer style ViewModel (design-document §9.2).
/// Reads/writes to the shared <see cref="ProfileConfigDto"/>.
/// </summary>
public partial class HeaderFooterViewModel : ObservableObject
{
    private ProfileConfigDto? _sharedProfile = new();
    private bool _isLoading;

    [ObservableProperty] private string _hfFontCn = "宋体";
    [ObservableProperty] private string _hfFontEn = "Times New Roman";
    [ObservableProperty] private double _hfFontSize = 10.5;
    [ObservableProperty] private string _hfFontStyle = "normal";
    [ObservableProperty] private string _hfAlignment = "center";
    [ObservableProperty] private double _hfHeaderDistance = 15.0;
    [ObservableProperty] private double _hfFooterDistance = 17.5;

    public void SetSharedProfile(ProfileConfigDto profile)
    {
        _sharedProfile = profile;
    }

    /// <summary>Set reference AND reload values from DTO (use only on init/template switch).</summary>
    public void LoadSharedProfile(ProfileConfigDto profile)
    {
        _sharedProfile = profile;
        LoadFromSharedProfile();
    }

    public void LoadFromSharedProfile()
    {
        if (_sharedProfile is null) return;
        _isLoading = true;
        var hf = _sharedProfile.HeaderFooter;
        HfFontCn = hf.FontCn; HfFontEn = hf.FontEn;
        HfFontSize = hf.FontSize; HfFontStyle = hf.FontStyle;
        HfAlignment = hf.Alignment;
        HfHeaderDistance = hf.HeaderDistance; HfFooterDistance = hf.FooterDistance;
        _isLoading = false;
    }

    private void WriteToSharedProfile()
    {
        if (_sharedProfile is null || _isLoading) return;
        var hf = _sharedProfile.HeaderFooter;
        hf.FontCn = HfFontCn; hf.FontEn = HfFontEn;
        hf.FontSize = HfFontSize; hf.FontStyle = HfFontStyle;
        hf.Alignment = HfAlignment;
        hf.HeaderDistance = HfHeaderDistance; hf.FooterDistance = HfFooterDistance;
    }

    partial void OnHfFontCnChanged(string value)         { IsDirty = true; WriteToSharedProfile(); }
    partial void OnHfFontEnChanged(string value)         { IsDirty = true; WriteToSharedProfile(); }
    partial void OnHfFontSizeChanged(double value)       { IsDirty = true; WriteToSharedProfile(); }
    partial void OnHfFontStyleChanged(string value)      { IsDirty = true; WriteToSharedProfile(); }
    partial void OnHfAlignmentChanged(string value)      { IsDirty = true; WriteToSharedProfile(); }
    partial void OnHfHeaderDistanceChanged(double value) { IsDirty = true; WriteToSharedProfile(); }
    partial void OnHfFooterDistanceChanged(double value) { IsDirty = true; WriteToSharedProfile(); }

    [ObservableProperty] private bool _isDirty;

    [RelayCommand]
    private void Reset()
    {
        HfFontCn = "宋体"; HfFontEn = "Times New Roman"; HfFontSize = 10.5;
        HfFontStyle = "normal"; HfAlignment = "center";
        HfHeaderDistance = 15.0; HfFooterDistance = 17.5;
        IsDirty = false;
        WriteToSharedProfile();
    }
}
