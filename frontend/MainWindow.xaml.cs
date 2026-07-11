using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WordFormatterUI.Pages;

namespace WordFormatterUI;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Set window size (WinUI 3 doesn't support Width/Height in XAML)
        var appWindow = AppWindow;
        appWindow.Resize(new Windows.Graphics.SizeInt32(1280, 800));

        // Navigate to the default page
        NavFrame.Navigate(typeof(FilesPage));
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            NavFrame.Navigate(typeof(SettingsPage));
        }
        else if (args.SelectedItem is NavigationViewItem item)
        {
            switch (item.Tag)
            {
                case "files":
                    NavFrame.Navigate(typeof(FilesPage));
                    break;
                case "profile":
                    NavFrame.Navigate(typeof(ProfilePage));
                    break;
                case "format":
                    NavFrame.Navigate(typeof(FormatPage));
                    break;
            }
        }
    }
}