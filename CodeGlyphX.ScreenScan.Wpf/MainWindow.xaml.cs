using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using CodeGlyphX;
using CodeGlyphX.Qr;
using Screen = System.Windows.Forms.Screen;
using WpfClipboard = System.Windows.Clipboard;
using WpfMessageBox = System.Windows.MessageBox;

namespace CodeGlyphX.ScreenScan.Wpf;

public partial class MainWindow : Window {
    private readonly object _captureLock = new();
    private CancellationTokenSource? _cts;
    private WriteableBitmap? _previewBitmap;

    private byte[]? _lastPixels;
    private int _lastWidth;
    private int _lastHeight;
    private int _lastStride;
    private string _lastDebugSummary = string.Empty;
    private long _lastDebugSummaryTickMs = -1;
    private volatile QrDecodeProfile _decodeProfile = QrDecodeProfile.Robust;

    public MainWindow() {
        InitializeComponent();
        LoadMonitors();
        ProfileBox.SelectedIndex = 2;
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

        var decoded = false;
        try {
            decoded = await ScanLoopAsync(gx, gy, w, h, _cts.Token);
        } finally {
            _cts?.Dispose();
            _cts = null;
            StartStopButton.Content = "Start scanning";
            if (!decoded) StatusText.Text = "Idle";
        }
    }

    private async Task<bool> ScanLoopAsync(int x, int y, int w, int h, CancellationToken ct) {
        return await Task.Run(() => ScanLoopWorker(x, y, w, h, ct), ct);
    }

    private bool ScanLoopWorker(int x, int y, int w, int h, CancellationToken ct) {
        using var capture = new ScreenCapture.Session(w, h);
        var stride = capture.Stride;

        while (!ct.IsCancellationRequested) {
            try {
                lock (_captureLock) {
                    capture.Capture(x, y);
                }

                var pixels = capture.Buffer;
                var options = new QrPixelDecodeOptions { Profile = _decodeProfile };

                var sw = Stopwatch.StartNew();
                var ok = QrDecoder.TryDecode(pixels, w, h, stride, PixelFormat.Bgra32, out var decoded, out var diag, options);
                var decodeMs = sw.ElapsedMilliseconds;
                var profileLabel = _decodeProfile.ToString();
                var status = ok
                    ? $"Decoded (v{decoded.Version}, {decoded.ErrorCorrectionLevel}, mask {decoded.Mask}) • {decodeMs}ms • {profileLabel}"
                    : $"Running (2–5 fps) • {diag} • {decodeMs}ms • {profileLabel}";

                Dispatcher.Invoke(() => {
                    UpdatePreview(pixels, w, h, stride);
                    StatusText.Text = status;
                    if (ok) DecodedText.Text = decoded.Text;
                });

                if (ok) return true;
            } catch (Exception ex) {
                Dispatcher.Invoke(() => StatusText.Text = ex.Message);
            }

            if (ct.WaitHandle.WaitOne(200)) return false;
        }

        return false;
    }

    private void UpdatePreview(byte[] pixels, int width, int height, int stride) {
        _lastPixels = pixels;
        _lastWidth = width;
        _lastHeight = height;
        _lastStride = stride;

        if (_previewBitmap is null || _previewBitmap.PixelWidth != width || _previewBitmap.PixelHeight != height) {
            _previewBitmap = new WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
            PreviewImage.Source = _previewBitmap;
        }

        _previewBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);

        ComputeLumaRangeSample(pixels, width, height, stride, out var min, out var max);
        _lastDebugSummary = GetDebugSummary(pixels, width, height, stride);
        PreviewInfoText.Text = $"Captured {width}×{height} • Luma {min}–{max} • {_lastDebugSummary}";
    }

    private void ProfileBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
        _decodeProfile = ProfileBox.SelectedIndex switch {
            0 => QrDecodeProfile.Fast,
            1 => QrDecodeProfile.Balanced,
            _ => QrDecodeProfile.Robust
        };
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

    private void SaveCapture_Click(object sender, RoutedEventArgs e) {
        if (_lastPixels is null || _lastWidth <= 0 || _lastHeight <= 0 || _lastStride <= 0) {
            WpfMessageBox.Show(this, "Nothing captured yet. Start scanning first.", "Screen scan", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var width = _lastWidth;
        var height = _lastHeight;
        var stride = _lastStride;

        byte[] pixels;
        lock (_captureLock) {
            pixels = new byte[stride * height];
            Buffer.BlockCopy(_lastPixels, 0, pixels, 0, pixels.Length);
        }

        var dialog = new Microsoft.Win32.SaveFileDialog {
            Filter = "PNG image (*.png)|*.png",
            FileName = $"capture-{DateTime.Now:yyyyMMdd-HHmmss}.png",
        };

        if (dialog.ShowDialog(this) != true) return;

        SavePng(dialog.FileName, pixels, width, height, stride);
        StatusText.Text = $"Saved capture: {Path.GetFileName(dialog.FileName)}";
    }

    private void SaveBinarized_Click(object sender, RoutedEventArgs e) {
        if (_lastPixels is null || _lastWidth <= 0 || _lastHeight <= 0 || _lastStride <= 0) {
            WpfMessageBox.Show(this, "Nothing captured yet. Start scanning first.", "Screen scan", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var width = _lastWidth;
        var height = _lastHeight;
        var stride = _lastStride;

        byte[] pixels;
        lock (_captureLock) {
            pixels = new byte[stride * height];
            Buffer.BlockCopy(_lastPixels, 0, pixels, 0, pixels.Length);
        }

        if (!QrGrayImage.TryCreate(pixels, width, height, stride, PixelFormat.Bgra32, scale: 1, out var gray)) {
            WpfMessageBox.Show(this, "Failed to build grayscale image (insufficient contrast?).", "Screen scan", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var bw = new byte[stride * height];
        for (var y = 0; y < height; y++) {
            var srcRow = y * gray.Width;
            var dstRow = y * stride;
            for (var x = 0; x < width; x++) {
                var lum = gray.Gray[srcRow + x];
                var v = lum < gray.Threshold ? (byte)0 : (byte)255;
                var p = dstRow + x * 4;
                bw[p + 0] = v;
                bw[p + 1] = v;
                bw[p + 2] = v;
                bw[p + 3] = 255;
            }
        }

        var dialog = new Microsoft.Win32.SaveFileDialog {
            Filter = "PNG image (*.png)|*.png",
            FileName = $"binarized-{DateTime.Now:yyyyMMdd-HHmmss}.png",
        };

        if (dialog.ShowDialog(this) != true) return;

        SavePng(dialog.FileName, bw, width, height, stride);
        StatusText.Text = $"Saved BW: {Path.GetFileName(dialog.FileName)}";
    }

    private static void SavePng(string filePath, byte[] bgra, int width, int height, int stride) {
        var wb = new WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
        wb.WritePixels(new Int32Rect(0, 0, width, height), bgra, stride, 0);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(wb));

        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        encoder.Save(fs);
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

    private string GetDebugSummary(byte[] pixels, int width, int height, int stride) {
        var nowMs = Environment.TickCount64;
        if (_lastDebugSummaryTickMs >= 0 && nowMs - _lastDebugSummaryTickMs < 1000) return _lastDebugSummary;
        _lastDebugSummaryTickMs = nowMs;

        var sb = new StringBuilder(64);

        AppendScale(sb, pixels, width, height, stride, scale: 1);

        var minDim = Math.Min(width, height);
        if (minDim >= 160) AppendScale(sb, pixels, width, height, stride, scale: 2);
        if (minDim >= 800) AppendScale(sb, pixels, width, height, stride, scale: 3);

        return sb.ToString().TrimEnd();
    }

    private static void AppendScale(StringBuilder sb, byte[] pixels, int width, int height, int stride, int scale) {
        if (!QrGrayImage.TryCreate(pixels, width, height, stride, PixelFormat.Bgra32, scale, out var gray)) {
            sb.Append($"s{scale}:grayfail ");
            return;
        }

        var candN = QrFinderPatternDetector.FindCandidates(gray, invert: false).Count;
        var candI = QrFinderPatternDetector.FindCandidates(gray, invert: true).Count;
        sb.Append($"s{scale}:t{gray.Threshold} cand{candN}/{candI} ");
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
