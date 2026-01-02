using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Screen = System.Windows.Forms.Screen;
using WpfClipboard = System.Windows.Clipboard;
using WpfMessageBox = System.Windows.MessageBox;

namespace CodeMatrix.ScreenScan.Wpf;

public partial class MainWindow : Window {
    private CancellationTokenSource? _cts;

    public MainWindow() {
        InitializeComponent();
        LoadMonitors();
    }

    private async void StartStop_Click(object sender, RoutedEventArgs e) {
        if (_cts is not null) {
            _cts.Cancel();
            return;
        }

        if (!TryReadInt(XBox.Text, out var x) ||
            !TryReadInt(YBox.Text, out var y) ||
            !TryReadInt(WidthBox.Text, out var w) ||
            !TryReadInt(HeightBox.Text, out var h) ||
            w <= 0 || h <= 0) {
            WpfMessageBox.Show(this, "Invalid region inputs.", "Screen scan", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var screen = GetSelectedScreen();
        var gx = screen.Bounds.X + x;
        var gy = screen.Bounds.Y + y;

        _cts = new CancellationTokenSource();
        StartStopButton.Content = "Stop scanning";
        StatusText.Text = "Running (2–5 fps)";
        DecodedText.Text = string.Empty;

        try {
            await ScanLoopAsync(gx, gy, w, h, _cts.Token);
        } finally {
            _cts?.Dispose();
            _cts = null;
            StartStopButton.Content = "Start scanning";
            StatusText.Text = "Idle";
        }
    }

    private async Task ScanLoopAsync(int x, int y, int w, int h, CancellationToken ct) {
        while (!ct.IsCancellationRequested) {
            try {
                var pixels = ScreenCapture.CaptureBgra32(x, y, w, h, out var stride);
                if (QrDecoder.TryDecode(pixels, w, h, stride, PixelFormat.Bgra32, out var decoded)) {
                    DecodedText.Text = decoded.Text;
                    StatusText.Text = $"Decoded (v{decoded.Version}, {decoded.ErrorCorrectionLevel}, mask {decoded.Mask})";
                }
            } catch (Exception ex) {
                StatusText.Text = ex.Message;
            }

            try {
                await Task.Delay(250, ct);
            } catch (TaskCanceledException) {
                return;
            }
        }
    }

    private void Copy_Click(object sender, RoutedEventArgs e) {
        var text = DecodedText.Text ?? string.Empty;
        if (text.Length == 0) return;
        WpfClipboard.SetText(text);
    }

    private void MonitorBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
        UpdateMonitorInfo();
    }

    private void LoadMonitors() {
        MonitorBox.Items.Clear();
        var screens = Screen.AllScreens;

        MonitorItem? select = null;
        for (var i = 0; i < screens.Length; i++) {
            var s = screens[i];
            var b = s.Bounds;
            var label = $"{i + 1}: {b.Width}x{b.Height} @ ({b.X},{b.Y}){(s.Primary ? " (Primary)" : "")}";
            var item = new MonitorItem(s, label);
            MonitorBox.Items.Add(item);
            if (s.Primary) select = item;
        }

        if (select is not null) MonitorBox.SelectedItem = select;
        if (MonitorBox.SelectedItem is null && MonitorBox.Items.Count > 0) MonitorBox.SelectedIndex = 0;
        UpdateMonitorInfo();
    }

    private Screen GetSelectedScreen() {
        return (MonitorBox.SelectedItem as MonitorItem)?.Screen
            ?? Screen.PrimaryScreen
            ?? Screen.AllScreens[0];
    }

    private void UpdateMonitorInfo() {
        var s = GetSelectedScreen();
        var b = s.Bounds;
        MonitorInfoText.Text = $"{s.DeviceName}  {b.Width}x{b.Height} @ ({b.X},{b.Y}){(s.Primary ? " (Primary)" : "")}";
    }

    private static bool TryReadInt(string? text, out int value) {
        value = 0;
        return int.TryParse(text, out value);
    }

    private sealed class MonitorItem {
        public Screen Screen { get; }
        public string Label { get; }

        public MonitorItem(Screen screen, string label) {
            Screen = screen;
            Label = label;
        }

        public override string ToString() => Label;
    }
}
