using System;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Art;

/// <summary>An opaque square image produced by <see cref="QrImageComposer"/>.</summary>
public sealed class QrImageComposition {
    private readonly byte[] _pixels;
    internal ReadOnlySpan<byte> PixelSpan => _pixels;

    /// <summary>Width and height in pixels, including the quiet zone.</summary>
    public int Size { get; }

    /// <summary>Left edge of the QR including its quiet zone, in canvas pixels.</summary>
    public int QrOffsetX { get; }
    /// <summary>Top edge of the QR including its quiet zone, in canvas pixels.</summary>
    public int QrOffsetY { get; }
    /// <summary>QR width/height including its quiet zone, in pixels.</summary>
    public int QrSize { get; }

    internal QrImageComposition(byte[] pixels, int size, int qrOffsetX, int qrOffsetY, int qrSize) {
        _pixels = pixels;
        Size = size;
        QrOffsetX = qrOffsetX;
        QrOffsetY = qrOffsetY;
        QrSize = qrSize;
    }

    /// <summary>Returns an independent copy of the tightly packed RGBA pixels.</summary>
    public byte[] GetPixels() => (byte[])_pixels.Clone();

    /// <summary>Encodes the composed pixels as a lossless PNG.</summary>
    public byte[] ToPng() => PngImageEncoder.EncodeRgba32(_pixels, Size, Size);

    /// <summary>Saves the composed PNG and returns its path.</summary>
    public string SavePng(string path) => RenderIO.WriteBinary(path, ToPng());
}
