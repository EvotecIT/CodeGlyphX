using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Screen = System.Windows.Forms.Screen;
using WpfClipboard = System.Windows.Clipboard;
using WpfMessageBox = System.Windows.MessageBox;

namespace CodeMatrix.ScreenScan.Wpf;

public partial class MainWindow : Window {
    private CancellationTokenSource? _cts;
    private WriteableBitmap? _previewBitmap;

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
                UpdatePreview(pixels, w, h, stride);
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

    private void UpdatePreview(byte[] pixels, int width, int height, int stride) {
        if (_previewBitmap is null || _previewBitmap.PixelWidth != width || _previewBitmap.PixelHeight != height) {
            _previewBitmap = new WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
            PreviewImage.Source = _previewBitmap;
        }

        _previewBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);

        ComputeLumaRangeSample(pixels, width, height, stride, out var min, out var max);
        PreviewInfoText.Text = $"Captured {width}×{height} • Luma {min}–{max}";
    }

    private static void ComputeLumaRangeSample(ReadOnlySpan<byte> pixels, int width, int height, int stride, out byte min, out byte max) {
        min = 255;
        max = 0;

        var step = Math.Max(1, Math.Min(width, height) / 128);

        for (var y = 0; y < height; y += step) {
            var row = y * stride;
            for (var x = 0; x < width; x += step) {
                var p = row + x * 4;
                var b = pixels[p + 0];
                var g = pixels[p + 1];
                var r = pixels[p + 2];

                var lum = (r * 299 + g * 587 + b * 114 + 500) / 1000;
                var l = (byte)lum;

                if (l < min) min = l;
                if (l > max) max = l;
            }
        }
    }

    private void Copy_Click(object sender, RoutedEventArgs e) {
        var text = DecodedText.Text ?? string.Empty;
        if (text.Length == 0) return;
        WpfClipboard.SetText(text);
    }

    private void PickRegion_Click(object sender, RoutedEventArgs e) {
        if (_cts is not null) return;

        // Hide this window so it doesn't get in the way of selection.
        Hide();
        try {
            if (!ScreenRegionPicker.TryPick(out var region)) return;

            // Pick the monitor that contains the region center (best-effort).
            var cx = region.X + region.Width / 2;
            var cy = region.Y + region.Height / 2;
            var target = Screen.PrimaryScreen ?? Screen.AllScreens[0];
            foreach (var s in Screen.AllScreens) {
                if (s.Bounds.Contains(cx, cy)) {
                    target = s;
                    break;
                }
            }

            // Update monitor selection first, then set relative coords.
            for (var i = 0; i < MonitorBox.Items.Count; i++) {
                if (MonitorBox.Items[i] is not MonitorItem item) continue;
                if (string.Equals(item.Screen.DeviceName, target.DeviceName, StringComparison.OrdinalIgnoreCase) &&
                    item.Screen.Bounds == target.Bounds) {
                    MonitorBox.SelectedItem = item;
                    break;
                }
            }

            var selected = GetSelectedScreen();

            XBox.Text = (region.X - selected.Bounds.X).ToString();
            YBox.Text = (region.Y - selected.Bounds.Y).ToString();
            WidthBox.Text = region.Width.ToString();
            HeightBox.Text = region.Height.ToString();

            UpdateMonitorInfo();
            StatusText.Text = $"Selected {region.Width}x{region.Height} @ ({region.X},{region.Y})";
        } finally {
            Show();
            Activate();
        }
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
