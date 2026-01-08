using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CodeGlyphX.Wpf.Rendering;

namespace CodeGlyphX.Wpf;

/// <summary>
/// WPF control that renders a Code 128 barcode for the provided <see cref="Value"/>.
/// </summary>
public sealed class Barcode128Control : Image {
    /// <summary>
    /// Identifies the <see cref="Value"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value),
            typeof(string),
            typeof(Barcode128Control),
            new FrameworkPropertyMetadata(string.Empty, OnAnyChanged));

    /// <summary>
    /// Identifies the <see cref="ModuleSize"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ModuleSizeProperty =
        DependencyProperty.Register(
            nameof(ModuleSize),
            typeof(int),
            typeof(Barcode128Control),
            new FrameworkPropertyMetadata(2, OnAnyChanged));

    /// <summary>
    /// Identifies the <see cref="QuietZone"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty QuietZoneProperty =
        DependencyProperty.Register(
            nameof(QuietZone),
            typeof(int),
            typeof(Barcode128Control),
            new FrameworkPropertyMetadata(10, OnAnyChanged));

    /// <summary>
    /// Identifies the <see cref="Foreground"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ForegroundProperty =
        DependencyProperty.Register(
            nameof(Foreground),
            typeof(Brush),
            typeof(Barcode128Control),
            new FrameworkPropertyMetadata(Brushes.Black, OnAnyChanged));

    /// <summary>
    /// Identifies the <see cref="Background"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty BackgroundProperty =
        DependencyProperty.Register(
            nameof(Background),
            typeof(Brush),
            typeof(Barcode128Control),
            new FrameworkPropertyMetadata(Brushes.White, OnAnyChanged));

    private bool _queued;

    /// <summary>
    /// Creates a new <see cref="Barcode128Control"/>.
    /// </summary>
    public Barcode128Control() {
        Stretch = Stretch.None;
        SnapsToDevicePixels = true;
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
        RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
        UseLayoutRounding = true;
    }

    /// <summary>
    /// Gets or sets the barcode value to encode.
    /// </summary>
    public string Value {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
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
    /// Gets or sets the foreground (bars) brush.
    /// </summary>
    public Brush Foreground {
        get => (Brush)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    /// <summary>
    /// Gets or sets the background (spaces) brush.
    /// </summary>
    public Brush Background {
        get => (Brush)GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    private static void OnAnyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        ((Barcode128Control)d).QueueUpdate();
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
        var value = Value ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value)) {
            Source = null;
            return;
        }

        var moduleSize = Math.Max(1, ModuleSize);
        var quietZone = Math.Max(0, QuietZone);
        const int heightModules = 40;

        var fg = ToBgra32(Foreground, 0xFF000000u);
        var bg = ToBgra32(Background, 0xFFFFFFFFu);

        var barcode = BarcodeEncoder.Encode(BarcodeType.Code128, value);
        Source = WpfBitmapRenderer.RenderBarcode(barcode, moduleSize, quietZone, heightModules, fg, bg);
    }

    private static uint ToBgra32(Brush brush, uint fallback) {
        if (brush is SolidColorBrush scb) {
            var c = scb.Color;
            return ((uint)c.A << 24) | ((uint)c.R << 16) | ((uint)c.G << 8) | c.B;
        }
        return fallback;
    }
}
