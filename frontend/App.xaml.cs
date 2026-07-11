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

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        Log("OnLaunched - start");
        try
        {
            MainWindow = new MainWindow();
            Log("OnLaunched - MainWindow created");
            MainWindow.Activate();
            Log("OnLaunched - MainWindow activated");

            // Fire-and-forget backend health check (non-blocking)
            _ = PollBackendHealthAsync();
        }
        catch (Exception ex)
        {
            Log($"OnLaunched - EXCEPTION: {ex}");
        }
    }

    private async System.Threading.Tasks.Task PollBackendHealthAsync()
    {
        for (int i = 0; i < 30; i++)
        {
            try
            {
                if (await Api.HealthAsync())
                {
                    Log($"Backend ready after {(i + 1) * 500}ms");
                    return;
                }
            }
            catch { }
            await System.Threading.Tasks.Task.Delay(500);
        }
        Log("Backend NOT ready after 15s");

        // Show error on UI thread
        MainWindow?.DispatcherQueue.TryEnqueue(async () =>
        {
            var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
            {
                Title = "连接失败",
                Content = "无法连接到后端服务 (127.0.0.1:8765)。\n\n请确认后端已启动，然后重启本程序。",
                CloseButtonText = "确定",
                XamlRoot = MainWindow?.Content?.XamlRoot,
            };
            await dialog.ShowAsync();
        });
    }
}