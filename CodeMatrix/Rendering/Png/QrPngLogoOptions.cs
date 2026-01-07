using System;
using CodeMatrix;

namespace CodeMatrix.Rendering.Png;

/// <summary>
/// Options and pixel data for a centered logo overlay in a QR PNG.
/// </summary>
public sealed class QrPngLogoOptions {
    /// <summary>
    /// Logo pixels in RGBA order, row-major, tightly packed.
    /// </summary>
    public byte[] Rgba { get; }

    /// <summary>
    /// Logo width in pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Logo height in pixels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Maximum logo size relative to the QR area (excluding quiet zone).
    /// </summary>
    public double Scale { get; set; } = 0.20;

    /// <summary>
    /// Padding in pixels around the logo (filled with <see cref="Background"/> when <see cref="DrawBackground"/> is true).
    /// </summary>
    public int PaddingPx { get; set; } = 4;

    /// <summary>
    /// Whether to draw a background plate behind the logo.
    /// </summary>
    public bool DrawBackground { get; set; } = true;

    /// <summary>
    /// Background color for the logo plate.
    /// </summary>
    public Rgba32 Background { get; set; } = Rgba32.White;

    /// <summary>
    /// Optional corner radius for the background plate.
    /// </summary>
    public int CornerRadiusPx { get; set; } = 0;

    /// <summary>
    /// Creates a logo option with a packed RGBA buffer.
    /// </summary>
    public QrPngLogoOptions(byte[] rgba, int width, int height) {
        if (rgba is null) throw new ArgumentNullException(nameof(rgba));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (rgba.Length != width * height * 4) throw new ArgumentException("Invalid RGBA buffer length.", nameof(rgba));

        Rgba = rgba;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Creates a logo option from a raw pixel buffer in BGRA/RGBA.
    /// </summary>
    public static QrPngLogoOptions FromPixels(byte[] pixels, int width, int height, int stride, PixelFormat fmt) {
        if (pixels is null) throw new ArgumentNullException(nameof(pixels));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (stride < width * 4) throw new ArgumentOutOfRangeException(nameof(stride));
        if (pixels.Length < height * stride) throw new ArgumentException("Pixel buffer too small.", nameof(pixels));

        var rgba = new byte[width * height * 4];
        for (var y = 0; y < height; y++) {
            var row = y * stride;
            var dst = y * width * 4;
            for (var x = 0; x < width; x++) {
                var src = row + x * 4;
                var d = dst + x * 4;
                if (fmt == PixelFormat.Bgra32) {
                    rgba[d + 0] = pixels[src + 2];
                    rgba[d + 1] = pixels[src + 1];
                    rgba[d + 2] = pixels[src + 0];
                    rgba[d + 3] = pixels[src + 3];
                } else {
                    rgba[d + 0] = pixels[src + 0];
                    rgba[d + 1] = pixels[src + 1];
                    rgba[d + 2] = pixels[src + 2];
                    rgba[d + 3] = pixels[src + 3];
                }
            }
        }

        return new QrPngLogoOptions(rgba, width, height);
    }

    /// <summary>
    /// Creates a logo option from a PNG (RGBA or RGB, non-interlaced).
    /// </summary>
    public static QrPngLogoOptions FromPng(byte[] png) {
        if (png is null) throw new ArgumentNullException(nameof(png));
        var rgba = PngDecoder.DecodeRgba32(png, out var width, out var height);
        return new QrPngLogoOptions(rgba, width, height);
    }
}
