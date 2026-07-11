using Microsoft.UI.Xaml;
using WordFormatterUI.Services;

namespace WordFormatterUI;

public partial class App : Application
{
    private static readonly string LogPath = System.IO.Path.Combine(
        AppContext.BaseDirectory, "startup.log");

    public static ApiService Api { get; private set; } = null!;
    public static Window MainWindow { get; private set; } = null!;

    private static void Log(string msg)
    {
        try { System.IO.File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss}] {msg}\n"); }
        catch { }
    }

    public App()
    {
        Log("App() - constructor start");
        try
        {
            InitializeComponent();
            Log("App() - InitializeComponent OK");
            Api = new ApiService("http://127.0.0.1:8765");
            Log("App() - ApiService created OK");
            UnhandledException += (_, e) =>
            {
                Log($"UnhandledException: {e.Exception}");
                e.Handled = true;
            };
            Log("App() - constructor done");
        }
        catch (Exception ex)
        {
            Log($"App() - EXCEPTION: {ex}");
        }
    }

    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        Log("OnLaunched - start");
        try
        {
            // Show a minimal loading window while waiting for backend
            var splash = new Window();
            var splashContent = new Microsoft.UI.Xaml.Controls.StackPanel
            {
                HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
                VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
                Spacing = 12,
                Children =
                {
                    new Microsoft.UI.Xaml.Controls.ProgressRing
                    {
                        IsActive = true,
                        Width = 48,
                        Height = 48,
                    },
                    new Microsoft.UI.Xaml.Controls.TextBlock
                    {
                        Text = "正在连接后端服务...",
                        FontSize = 14,
                        HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
                    },
                },
            };
            splash.Content = splashContent;
            splash.Activate();
            Log("OnLaunched - splash shown");

            // Poll backend health until ready (max 15 seconds)
            bool backendReady = false;
            for (int i = 0; i < 30; i++)
            {
                if (Api is not null && await Api.HealthAsync())
                {
                    backendReady = true;
                    Log($"OnLaunched - backend ready after {(i + 1) * 500}ms");
                    break;
                }
                await System.Threading.Tasks.Task.Delay(500);
            }

            splash.Close();

            if (!backendReady)
            {
                Log("OnLaunched - backend NOT ready, launching anyway");
                var errorDialog = new Microsoft.UI.Xaml.Controls.ContentDialog
                {
                    Title = "连接失败",
                    Content = "无法连接到后端服务 (127.0.0.1:8765)。\n\n请先运行 run.bat 启动后端，然后再打开本程序。",
                    CloseButtonText = "确定",
                    XamlRoot = splash.Content.XamlRoot,
                };
                // Can't show dialog without a window; just log and continue
            }

            MainWindow = new MainWindow();
            Log("OnLaunched - MainWindow created");
            MainWindow.Activate();
            Log("OnLaunched - MainWindow activated");
        }
        catch (Exception ex)
        {
            Log($"OnLaunched - EXCEPTION: {ex}");
        }
    }
}