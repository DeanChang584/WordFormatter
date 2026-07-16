using Microsoft.UI.Xaml;
using WordFormatterUI.Services;
using System;
using System.Runtime.InteropServices;

namespace WordFormatterUI;

public partial class App : Application
{
    private static readonly string LogPath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WordFormatter", "startup.log");

    public static ApiService Api { get; private set; } = null!;
    public static Window MainWindow { get; private set; } = null!;
    public static TrayIconService? TrayIcon { get; private set; }
    private static readonly TaskCompletionSource<bool> _backendReadyTcs = new();
    /// <summary>Wait for backend health check to pass (used by MainViewModel).</summary>
    public static Task WaitForBackendAsync() => _backendReadyTcs.Task;

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
            MainWindow.ExtendsContentIntoTitleBar = true;
            Log("OnLaunched - MainWindow created");
            MainWindow.Activate();
            Log("OnLaunched - MainWindow activated");

            // Initialize system tray icon
            try
            {
                TrayIcon = new TrayIconService(MainWindow, "WordFormatter");
                TrayIcon.ShowWindowRequested += () =>
                {
                    MainWindow.DispatcherQueue.TryEnqueue(() =>
                    {
                        MainWindow.Activate();
                        // Bring to foreground via native Win32
                        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(MainWindow);
                        ShowWindow(hwnd, 1); // SW_SHOWNORMAL
                        SetForegroundWindow(hwnd);
                    });
                };
                TrayIcon.Initialize();
                Log("OnLaunched - TrayIconService initialized");
            }
            catch (Exception ex)
            {
                Log($"OnLaunched - TrayIcon initialization failed (non-fatal): {ex.Message}");
            }

            // Ensure tray icon cleanup on exit
            AppDomain.CurrentDomain.ProcessExit += (_, _) => TrayIcon?.Dispose();

            // Fire-and-forget backend health check (non-blocking)
            _ = PollBackendHealthAsync();

            // Pre-warm WPS COM + WebView2 in the background
            Services.DocumentPreviewService.WarmUp();
            _ = Views.PreviewWindow.WarmUpAsync();
        }
        catch (Exception ex)
        {
            Log($"OnLaunched - EXCEPTION: {ex}");
        }
    }

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private async System.Threading.Tasks.Task PollBackendHealthAsync()
    {
        for (int i = 0; i < 30; i++)
        {
            try
            {
                if (await Api.HealthAsync())
                {
                    Log($"Backend ready after {(i + 1) * 500}ms");
                    _backendReadyTcs.TrySetResult(true);
                    return;
                }
            }
            catch { }
            await System.Threading.Tasks.Task.Delay(500);
        }
        Log("Backend NOT ready after 15s");
        _backendReadyTcs.TrySetResult(true); // allow InitializeAsync to proceed

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