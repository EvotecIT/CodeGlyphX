using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CodeMatrix.Wpf.Rendering;

namespace CodeMatrix.Wpf;

/// <summary>
/// WPF control that renders a QR code for the provided <see cref="Text"/>.
/// </summary>
public sealed class QrCodeControl : Image {
    /// <summary>
    /// Identifies the <see cref="Text"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(QrCodeControl),
            new FrameworkPropertyMetadata(string.Empty, OnAnyChanged));

    /// <summary>
    /// Identifies the <see cref="Ecc"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty EccProperty =
        DependencyProperty.Register(
            nameof(Ecc),
            typeof(QrErrorCorrectionLevel),
            typeof(QrCodeControl),
            new FrameworkPropertyMetadata(QrErrorCorrectionLevel.M, OnAnyChanged));

    /// <summary>
    /// Identifies the <see cref="ModuleSize"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ModuleSizeProperty =
        DependencyProperty.Register(
            nameof(ModuleSize),
            typeof(int),
            typeof(QrCodeControl),
            new FrameworkPropertyMetadata(4, OnAnyChanged));

    /// <summary>
    /// Identifies the <see cref="QuietZone"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty QuietZoneProperty =
        DependencyProperty.Register(
            nameof(QuietZone),
            typeof(int),
            typeof(QrCodeControl),
            new FrameworkPropertyMetadata(4, OnAnyChanged));

    /// <summary>
    /// Identifies the <see cref="Foreground"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ForegroundProperty =
        DependencyProperty.Register(
            nameof(Foreground),
            typeof(Brush),
            typeof(QrCodeControl),
            new FrameworkPropertyMetadata(Brushes.Black, OnAnyChanged));

    /// <summary>
    /// Identifies the <see cref="Background"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty BackgroundProperty =
        DependencyProperty.Register(
            nameof(Background),
            typeof(Brush),
            typeof(QrCodeControl),
            new FrameworkPropertyMetadata(Brushes.White, OnAnyChanged));

    private bool _queued;

    /// <summary>
    /// Creates a new <see cref="QrCodeControl"/>.
    /// </summary>
    public QrCodeControl() {
        Stretch = Stretch.None;
        SnapsToDevicePixels = true;
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
        RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
        UseLayoutRounding = true;
    }

    /// <summary>
    /// Gets or sets the text payload to encode.
    /// </summary>
    public string Text {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// Gets or sets the QR error correction level.
    /// </summary>
    public QrErrorCorrectionLevel Ecc {
        get => (QrErrorCorrectionLevel)GetValue(EccProperty);
        set => SetValue(EccProperty, value);
    }

    /// <summary>
    /// Gets or sets the module size in pixels.
    /// </summary>
    public int ModuleSize {
        get => (int)GetValue(ModuleSizeProperty);
        set => SetValue(ModuleSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the quiet zone size in modules.
    /// </summary>
    public int QuietZone {
        get => (int)GetValue(QuietZoneProperty);
        set => SetValue(QuietZoneProperty, value);
    }

    /// <summary>
    /// Gets or sets the foreground (dark modules) brush.
    /// </summary>
    public Brush Foreground {
        get => (Brush)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    /// <summary>
    /// Gets or sets the background (light modules) brush.
    /// </summary>
    public Brush Background {
        get => (Brush)GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    private static void OnAnyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        ((QrCodeControl)d).QueueUpdate();
    }

    private void QueueUpdate() {
        if (_queued) return;
        _queued = true;
        Dispatcher.BeginInvoke((Action)(() => {
            _queued = false;
            UpdateSource();
        }), System.Windows.Threading.DispatcherPriority.Background);
    }

    private void UpdateSource() {
        var text = Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text)) {
            Source = null;
            return;
        }

        var moduleSize = Math.Max(1, ModuleSize);
        var quietZone = Math.Max(0, QuietZone);

        var fg = ToBgra32(Foreground, 0xFF000000u);
        var bg = ToBgra32(Background, 0xFFFFFFFFu);

        var qr = QrCodeEncoder.EncodeText(text, Ecc, minVersion: 1, maxVersion: 40, forceMask: null);
        Source = WpfBitmapRenderer.RenderQr(qr.Modules, moduleSize, quietZone, fg, bg);
    }

    private static uint ToBgra32(Brush brush, uint fallback) {
        if (brush is SolidColorBrush scb) {
            var c = scb.Color;
            return ((uint)c.A << 24) | ((uint)c.R << 16) | ((uint)c.G << 8) | c.B;
        }
        return fallback;
    }
}
