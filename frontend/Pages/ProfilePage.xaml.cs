using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WordFormatterUI.Services;
using WordFormatterUI.ViewModels;

namespace WordFormatterUI.Pages;

public sealed partial class ProfilePage : Page
{
    public ProfileViewModel Vm { get; }

    public ProfilePage()
    {
        InitializeComponent();
        Vm = App.Api is not null ? new ProfileViewModel(App.Api) : throw new InvalidOperationException("ApiService not initialized");
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        // Populate combo boxes
        PaperSizeBox.Items.Clear();
        foreach (var size in new[] { "A4", "A3", "B5", "Letter", "Legal", "16K" })
            PaperSizeBox.Items.Add(size);

        AlignmentBox.Items.Clear();
        foreach (var align in new[] { "justify", "left", "center", "right" })
            AlignmentBox.Items.Add(align);

        await Vm.LoadProfileCommand.ExecuteAsync(null);

        // Map ViewModel → UI controls
        PaperSizeBox.SelectedItem = Vm.PaperSize;
        MarginTopBox.Value = Vm.MarginTop;
        MarginBottomBox.Value = Vm.MarginBottom;
        MarginLeftBox.Value = Vm.MarginLeft;
        FontCnBox.Text = Vm.BodyFontCn;
        FontEnBox.Text = Vm.BodyFontEn;
        FontSizeBox.Value = Vm.BodyFontSize;
        LineSpacingBox.Value = Vm.LineSpacingValue;
        IndentBox.Value = Vm.FirstLineIndent;
        AlignmentBox.SelectedItem = Vm.Alignment;

        SaveBtn.IsEnabled = false;
        Vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(Vm.IsDirty))
                SaveBtn.IsEnabled = Vm.IsDirty;
            else if (args.PropertyName == nameof(Vm.StatusMessage))
                StatusText.Text = Vm.StatusMessage;
        };
    }

    private void Field_Changed(object sender, object e)
    {
        // Sync UI → ViewModel
        if (PaperSizeBox.SelectedItem is string ps) Vm.PaperSize = ps;
        if (AlignmentBox.SelectedItem is string al) Vm.Alignment = al;
        if (FontCnBox.Text is string fc) Vm.BodyFontCn = fc;
        if (FontEnBox.Text is string fe) Vm.BodyFontEn = fe;
        SaveBtn.IsEnabled = true;
    }

    private void NumberBox_Changed(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (sender == MarginTopBox) Vm.MarginTop = args.NewValue;
        else if (sender == MarginBottomBox) Vm.MarginBottom = args.NewValue;
        else if (sender == MarginLeftBox) Vm.MarginLeft = args.NewValue;
        else if (sender == FontSizeBox) Vm.BodyFontSize = args.NewValue;
        else if (sender == LineSpacingBox) Vm.LineSpacingValue = args.NewValue;
        else if (sender == IndentBox) Vm.FirstLineIndent = args.NewValue;
        SaveBtn.IsEnabled = true;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        // Final sync from UI
        if (PaperSizeBox.SelectedItem is string ps) Vm.PaperSize = ps;
        if (AlignmentBox.SelectedItem is string al) Vm.Alignment = al;
        Vm.BodyFontCn = FontCnBox.Text;
        Vm.BodyFontEn = FontEnBox.Text;

        await Vm.SaveProfileCommand.ExecuteAsync(null);
        SaveBtn.IsEnabled = Vm.IsDirty;
    }
}