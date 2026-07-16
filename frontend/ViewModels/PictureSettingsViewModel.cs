using CommunityToolkit.Mvvm.ComponentModel;
using WordFormatterUI.Models.Profile;

namespace WordFormatterUI.ViewModels;

/// <summary>
/// Picture settings ViewModel (design-document §9.5).
/// Reads/writes to the shared <see cref="ProfileConfigDto"/>.
/// Provides SizeMode-driven visibility flags for the XAML view.
/// </summary>
public partial class PictureSettingsViewModel : ObservableObject
{
    private ProfileConfigDto? _sharedProfile = new();
    private bool _isLoading;

    // ── Size mode ──
    [ObservableProperty] private string _pictureSizeMode = "auto";

    // ── Dimensions ──
    [ObservableProperty] private double _pictureWidth = 14.0;
    [ObservableProperty] private string _pictureWidthUnit = "cm";
    [ObservableProperty] private double _pictureHeight = 8.0;
    [ObservableProperty] private string _pictureHeightUnit = "cm";

    // ── Scaling ──
    [ObservableProperty] private bool _pictureKeepAspectRatio = true;
    [ObservableProperty] private bool _pictureNoEnlarge = true;

    // ── Layout ──
    [ObservableProperty] private string _pictureAlignment = "center";        // left / center / right / top / middle / bottom / distribute_h / distribute_v
    [ObservableProperty] private string _pictureWrappingStyle = "inline";

    // ── Compression ──
    [ObservableProperty] private int _pictureQuality = 85;
    [ObservableProperty] private int _pictureMaxSidePixels = 1600;
    [ObservableProperty] private bool _pictureAutoCompress = false;

    // ── Visibility helpers for XAML binding ──
    public bool IsWidthMode => PictureSizeMode == "width";
    public bool IsHeightMode => PictureSizeMode == "height";
    public bool IsAutoMode => PictureSizeMode == "auto";

    [ObservableProperty] private bool _isDirty;

    // ──────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────

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
        var pic = _sharedProfile.Picture;
        PictureSizeMode = pic.SizeMode;
        PictureWidth = pic.Width;
        PictureWidthUnit = pic.WidthUnit;
        PictureHeight = pic.Height;
        PictureHeightUnit = pic.HeightUnit;
        PictureKeepAspectRatio = pic.KeepAspectRatio;
        PictureNoEnlarge = pic.NoEnlarge;
        PictureAlignment = pic.Alignment;
        PictureWrappingStyle = pic.WrappingStyle;
        PictureQuality = pic.Quality;
        PictureMaxSidePixels = pic.MaxSidePixels;
        PictureAutoCompress = pic.AutoCompress;
        _isLoading = false;
    }

    private void WriteToSharedProfile()
    {
        if (_sharedProfile is null || _isLoading) return;
        var pic = _sharedProfile.Picture;
        pic.SizeMode = PictureSizeMode;
        pic.Width = PictureWidth;
        pic.WidthUnit = PictureWidthUnit;
        pic.Height = PictureHeight;
        pic.HeightUnit = PictureHeightUnit;
        pic.KeepAspectRatio = PictureKeepAspectRatio;
        pic.NoEnlarge = PictureNoEnlarge;
        pic.Alignment = PictureAlignment;
        pic.WrappingStyle = PictureWrappingStyle;
        pic.Quality = PictureQuality;
        pic.MaxSidePixels = PictureMaxSidePixels;
        pic.AutoCompress = PictureAutoCompress;
    }

    // ──────────────────────────────────────────────
    // Change handlers
    // ──────────────────────────────────────────────

    partial void OnPictureSizeModeChanged(string value)
    {
        OnPropertyChanged(nameof(IsWidthMode));
        OnPropertyChanged(nameof(IsHeightMode));
        OnPropertyChanged(nameof(IsAutoMode));
        IsDirty = true;
        WriteToSharedProfile();
    }

    partial void OnPictureWidthChanged(double value) { IsDirty = true; WriteToSharedProfile(); }
    partial void OnPictureWidthUnitChanged(string value) { IsDirty = true; WriteToSharedProfile(); }
    partial void OnPictureHeightChanged(double value) { IsDirty = true; WriteToSharedProfile(); }
    partial void OnPictureHeightUnitChanged(string value) { IsDirty = true; WriteToSharedProfile(); }
    partial void OnPictureKeepAspectRatioChanged(bool value) { IsDirty = true; WriteToSharedProfile(); }
    partial void OnPictureNoEnlargeChanged(bool value) { IsDirty = true; WriteToSharedProfile(); }
    partial void OnPictureAlignmentChanged(string value) { IsDirty = true; WriteToSharedProfile(); }
    partial void OnPictureWrappingStyleChanged(string value) { IsDirty = true; WriteToSharedProfile(); }
    partial void OnPictureQualityChanged(int value) { IsDirty = true; WriteToSharedProfile(); }
    partial void OnPictureMaxSidePixelsChanged(int value) { IsDirty = true; WriteToSharedProfile(); }
    partial void OnPictureAutoCompressChanged(bool value) { IsDirty = true; WriteToSharedProfile(); }

    // ──────────────────────────────────────────────
    // Reset
    // ──────────────────────────────────────────────

    public void ResetDefaults()
    {
        PictureSizeMode = "auto";
        PictureWidth = 14.0;
        PictureWidthUnit = "cm";
        PictureHeight = 8.0;
        PictureHeightUnit = "cm";
        PictureKeepAspectRatio = true;
        PictureNoEnlarge = true;
        PictureAlignment = "center";
        PictureWrappingStyle = "inline";
        PictureQuality = 85;
        PictureMaxSidePixels = 1600;
        PictureAutoCompress = false;
        IsDirty = false;
        WriteToSharedProfile();
    }
}