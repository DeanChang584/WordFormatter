using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WordFormatterUI.Models.Format;
using WordFormatterUI.Models.History;
using WordFormatterUI.Services;
using WordFormatterUI.ViewModels;

namespace WordFormatterUI.Views;

/// <summary>
/// Post-task results & history card (design-document §7.3).
///
/// Shows failed-file details from the current/last FormatVm task,
/// and a collapsible history list of the last 20 tasks with
/// one-click reuse of past config and files.
///
/// Events fire for operations the parent MainWindow must coordinate
/// (file list rebuild, profile application, single-file retry).
/// </summary>
public sealed partial class ResultHistoryView : UserControl
{
    // ── Events ──────────────────────────────────────────────────────

    /// <summary>Raised when user clicks "再次尝试" on a failed file.</summary>
    public event EventHandler<string>? RetrySingleFileRequested;

    /// <summary>Raised when user clicks "重新执行" on a history entry.
    /// Parent should load detail, apply profile, and rebuild file list.</summary>
    public event EventHandler<string>? ReuseHistoryRequested;

    // ── Internal state ──────────────────────────────────────────────

    private List<HistoryRecordDto> _historyRecords = new();
    private bool _historyLoaded;

    // ── Constructor ─────────────────────────────────────────────────

    public ResultHistoryView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    // ── Load ────────────────────────────────────────────────────────

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var vm = GetFormatVm();
        if (vm == null) return;

        // Subscribe to FormatVm.HasResults changes
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(FormatViewModel.HasResults))
            {
                DispatcherQueue.TryEnqueue(RefreshHasResults);
                // Reset history-loaded flag so the panel re-fetches next time it opens
                if (vm.HasResults)
                    _historyLoaded = false;
            }
        };

        // Initial refresh
        RefreshHasResults();
    }

    // ── Failed files (from current/last task) ───────────────────────

    private void RefreshHasResults()
    {
        var vm = GetFormatVm();
        if (vm == null || !vm.HasResults)
        {
            HasResultsPanel.Visibility = Visibility.Collapsed;
            return;
        }

        HasResultsPanel.Visibility = Visibility.Visible;

        // Summary
        SummaryOkLabel.Text = $"✓ 成功: {vm.OkCount}";
        SummaryFailLabel.Text = vm.FailCount > 0 ? $"✗ 失败: {vm.FailCount}" : "";

        if (vm.ElapsedSeconds > 0)
        {
            var ts = TimeSpan.FromSeconds(vm.ElapsedSeconds);
            SummaryElapsedLabel.Text = ts.TotalMinutes >= 1
                ? $"耗时 {ts.Minutes}m{ts.Seconds}s"
                : $"耗时 {ts.Seconds}s";
        }
        else
        {
            SummaryElapsedLabel.Text = "";
        }

        // Build failed files list
        var failed = vm.Results?
            .Where(r => r.Status is "error" or "failed")
            .ToList() ?? new List<FileResultDto>();

        FailedFilesLabel.Text = $"查看失败文件 ({failed.Count})";
        ToggleFailedFilesBtn.IsEnabled = failed.Count > 0;

        FailedFilesPanel.Children.Clear();
        foreach (var f in failed)
        {
            var container = new StackPanel { Spacing = 2, Margin = new Thickness(8, 4, 0, 4) };

            // File name
            var nameBlock = new TextBlock
            {
                Text = !string.IsNullOrWhiteSpace(f.Output) ? f.Output : System.IO.Path.GetFileName(f.File),
                FontSize = 12,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorPrimaryBrush"],
            };
            container.Children.Add(nameBlock);

            // Error message
            if (!string.IsNullOrWhiteSpace(f.Message))
            {
                var msgBlock = new TextBlock
                {
                    Text = f.Message,
                    FontSize = 11,
                    Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorTertiaryBrush"],
                    TextWrapping = TextWrapping.Wrap,
                };
                container.Children.Add(msgBlock);
            }

            // Retry button
            var retryBtn = new Button
            {
                Content = "再次尝试",
                HorizontalAlignment = HorizontalAlignment.Left,
                FontSize = 11,
                Padding = new Thickness(8, 2, 8, 2),
                Margin = new Thickness(0, 2, 0, 0),
            };
            var capturedPath = f.File;
            retryBtn.Click += (_, _) =>
            {
                RetrySingleFileRequested?.Invoke(this, capturedPath);
            };
            container.Children.Add(retryBtn);

            FailedFilesPanel.Children.Add(container);
        }
    }

    // ── Toggle failed files ─────────────────────────────────────────

    private void ToggleFailedFiles_Click(object sender, RoutedEventArgs e)
    {
        var isVisible = FailedFilesPanel.Visibility == Visibility.Visible;
        FailedFilesPanel.Visibility = isVisible ? Visibility.Collapsed : Visibility.Visible;
        FailedFilesArrow.Text = isVisible ? "▶" : "▼";
    }

    // ── History ─────────────────────────────────────────────────────

    private async void ToggleHistory_Click(object sender, RoutedEventArgs e)
    {
        var isVisible = HistoryPanel.Visibility == Visibility.Visible;

        if (isVisible)
        {
            // Collapse
            HistoryPanel.Visibility = Visibility.Collapsed;
            HistoryArrow.Text = "▶";
            return;
        }

        // Expand — load history on first open
        if (!_historyLoaded)
        {
            await LoadHistoryAsync();
            _historyLoaded = true;
        }

        HistoryPanel.Visibility = Visibility.Visible;
        HistoryArrow.Text = "▼";
    }

    private async Task LoadHistoryAsync()
    {
        try
        {
            var api = App.Api;
            if (api == null) return;

            var resp = await api.GetHistoryAsync();
            if (resp?.Success == true && resp.Data is not null)
            {
                _historyRecords = resp.Data.History;
                BuildHistoryList();
            }
        }
        catch
        {
            // Non-critical — show empty state
        }
    }

    private void BuildHistoryList()
    {
        // Update title with count
        HistoryTitle.Text = $"历史记录 ({_historyRecords.Count})";

        // Clear and rebuild items inside the scrollable panel
        HistoryItemsPanel.Children.Clear();

        if (_historyRecords.Count == 0)
        {
            HistoryEmptyLabel.Visibility = Visibility.Visible;
            ClearHistoryBtn.Visibility = Visibility.Collapsed;
            return;
        }

        HistoryEmptyLabel.Visibility = Visibility.Collapsed;
        ClearHistoryBtn.Visibility = Visibility.Visible;

        foreach (var rec in _historyRecords)
        {
            HistoryItemsPanel.Children.Add(BuildHistoryItem(rec));
        }
    }

    private FrameworkElement BuildHistoryItem(HistoryRecordDto rec)
    {
        var container = new Border
        {
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8),
            Margin = new Thickness(0, 0, 0, 2),
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // ── Row 0: file names (one per line) with status icons ──────
        var namePanel = new StackPanel { Spacing = 2 };

        // Build a list of (fileName, status) pairs
        var fileEntries = new List<(string Name, string? Status)>();
        if (rec.Results?.Results?.Count > 0)
        {
            // Prefer per-file status from Results
            foreach (var r in rec.Results.Results)
            {
                if (!string.IsNullOrWhiteSpace(r.Output) || !string.IsNullOrWhiteSpace(r.File))
                {
                    fileEntries.Add((r.Output ?? r.File, r.Status));
                }
            }
        }
        else if (rec.Files?.Count > 0)
        {
            // Fallback: file names with status from the Files list
            foreach (var f in rec.Files)
            {
                var displayName = !string.IsNullOrWhiteSpace(f.OutputName) ? f.OutputName : f.Name;
                if (!string.IsNullOrWhiteSpace(displayName))
                {
                    fileEntries.Add((displayName, f.Status));
                }
            }
        }

        if (fileEntries.Count > 0)
        {
            foreach (var (fPath, status) in fileEntries)
            {
                // Choose icon based on status
                var icon = status switch
                {
                    "success" => "✓",
                    "error" or "failed" => "✗",
                    "skipped" => "⏭",
                    _ => "📄",
                };

                // Choose color based on status
                var color = status switch
                {
                    "success" => "#13A10E",
                    "error" or "failed" => "#D13438",
                    _ => null, // use default
                };

                var tb = new TextBlock
                {
                    Text = $"{icon} {System.IO.Path.GetFileName(fPath)}",
                    FontSize = 12,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    TextTrimming = Microsoft.UI.Xaml.TextTrimming.CharacterEllipsis,
                    MaxWidth = 280,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Foreground = color != null
                        ? new Microsoft.UI.Xaml.Media.SolidColorBrush(
                            Microsoft.UI.ColorHelper.FromArgb(255,
                                byte.Parse(color[1..3], System.Globalization.NumberStyles.HexNumber),
                                byte.Parse(color[3..5], System.Globalization.NumberStyles.HexNumber),
                                byte.Parse(color[5..7], System.Globalization.NumberStyles.HexNumber)))
                        : (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorPrimaryBrush"],
                };

                // Tooltip with full file name
                ToolTipService.SetToolTip(tb, fPath);

                namePanel.Children.Add(tb);
            }
        }
        else
        {
            namePanel.Children.Add(new TextBlock
            {
                Text = "📄 文档",
                FontSize = 12,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorPrimaryBrush"],
            });
        }
        Grid.SetRow(namePanel, 0);
        Grid.SetColumn(namePanel, 0);
        grid.Children.Add(namePanel);

        // ── Row 1: time ────────────────────────────────────────────────
        var timeText = "";
        if (!string.IsNullOrWhiteSpace(rec.Time))
        {
            if (DateTime.TryParse(rec.Time, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
            {
                var local = dt.ToLocalTime();
                timeText = local.ToString("yyyy-MM-dd HH:mm");
            }
            else
            {
                timeText = rec.Time.Length >= 16 ? rec.Time[..16].Replace("T", " ") : rec.Time;
            }
        }
        var timeRow = new TextBlock
        {
            Text = timeText,
            FontSize = 11,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorTertiaryBrush"],
        };
        Grid.SetRow(timeRow, 1);
        Grid.SetColumn(timeRow, 0);
        grid.Children.Add(timeRow);

        // ── Row 2: stats ──────────────────────────────────────────────
        var statsParts = new List<string> { $"✓ 成功" };
        if (rec.Failed > 0)
            statsParts.Add($"✗ {rec.Failed} 失败");
        statsParts.Add($"{rec.FileCount} 个文件");
        if (!string.IsNullOrWhiteSpace(rec.Template))
            statsParts.Add(rec.Template);
        statsParts.Add(FormatDuration(rec.Duration));

        var statsRow = new TextBlock
        {
            Text = string.Join(" · ", statsParts),
            FontSize = 11,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
        };
        Grid.SetRow(statsRow, 2);
        Grid.SetColumn(statsRow, 0);
        grid.Children.Add(statsRow);

        // ── Reuse button (right column, spans all 3 rows) ─────────────
        var reuseBtn = new Button
        {
            Content = "重新执行",
            FontSize = 11,
            Padding = new Thickness(8, 2, 8, 2),
            MinWidth = 60,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var capturedId = rec.Id;
        reuseBtn.Click += (_, _) =>
        {
            ReuseHistoryRequested?.Invoke(this, capturedId);
        };
        Grid.SetRow(reuseBtn, 0);
        Grid.SetRowSpan(reuseBtn, 3);
        Grid.SetColumn(reuseBtn, 1);
        grid.Children.Add(reuseBtn);

        container.Child = grid;
        return container;
    }

    private static string FormatDuration(double seconds)
    {
        if (seconds < 60)
            return $"{seconds:F1} s";
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.Minutes > 0
            ? $"{ts.Minutes}m {ts.Seconds}s"
            : $"{ts.Seconds}s";
    }

    // ── Clear history ───────────────────────────────────────────────

    private async void ClearHistory_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var api = App.Api;
            if (api == null) return;

            var ok = await api.ClearHistoryAsync();
            if (ok)
            {
                _historyRecords.Clear();
                BuildHistoryList();
            }
        }
        catch
        {
            // Non-critical
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private FormatViewModel? GetFormatVm()
    {
        if (ViewRoot.DataContext is FormatViewModel direct)
            return direct;

        DependencyObject? current = ViewRoot;
        while (current != null)
        {
            if (current is FrameworkElement fe && fe.DataContext is FormatViewModel vm)
                return vm;
            current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}
