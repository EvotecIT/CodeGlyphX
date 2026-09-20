using System;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Rendering.Art;

/// <summary>An opaque square image produced by <see cref="QrImageComposer"/>.</summary>
public sealed class QrImageComposition {
    private readonly byte[] _pixels;

    /// <summary>Width and height in pixels, including the quiet zone.</summary>
    public int Size { get; }

    internal QrImageComposition(byte[] pixels, int size) {
        _pixels = pixels;
        Size = size;
    }

    /// <summary>Returns an independent copy of the tightly packed RGBA pixels.</summary>
    public byte[] GetPixels() => (byte[])_pixels.Clone();

    /// <summary>Encodes the composed pixels as a lossless PNG.</summary>
    public byte[] ToPng() {
        var stride = Size * 4;
        var length = RenderGuards.EnsureOutputBytes((long)(stride + 1) * Size, "QR composition exceeds PNG output limits.");
        var scanlines = new byte[length];
        for (var y = 0; y < Size; y++) Buffer.BlockCopy(_pixels, y * stride, scanlines, y * (stride + 1) + 1, stride);
        return PngWriter.WriteRgba8(Size, Size, scanlines, length, compressionLevel: 6);
    }

    /// <summary>Saves the composed PNG and returns its path.</summary>
    public string SavePng(string path) => RenderIO.WriteBinary(path, ToPng());
}
