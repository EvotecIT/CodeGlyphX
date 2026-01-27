using System;

namespace CodeGlyphX.Rendering.Webp;

/// <summary>
/// Managed VP8 (lossy) decoder scaffold. Currently parses only the keyframe header.
/// </summary>
internal static class WebpVp8Decoder {
    private const int FrameTagBytes = 3;
    private const int StartCodeBytes = 3;
    private const int DimensionBytes = 4;
    private const int MinimumHeaderBytes = FrameTagBytes + StartCodeBytes + DimensionBytes; // 10

    internal static bool TryDecode(ReadOnlySpan<byte> payload, out byte[] rgba, out int width, out int height) {
        rgba = Array.Empty<byte>();
        width = 0;
        height = 0;

        if (!TryReadHeader(payload, out var header)) return false;

        // Parsing-only scaffold for now.
        width = header.Width;
        height = header.Height;
        return false;
    }

    internal static bool TryReadHeader(ReadOnlySpan<byte> payload, out WebpVp8Header header) {
        header = default;
        if (payload.Length < MinimumHeaderBytes) return false;

        var frameTag = ReadU24LE(payload, 0);
        var frameType = frameTag & 1;
        if (frameType != 0) return false; // interframes are not valid for WebP stills

        var version = (frameTag >> 1) & 0x7;
        var showFrame = (frameTag >> 4) & 1;
        var partitionSize = frameTag >> 5;

        // Keyframe start code.
        if (payload[3] != 0x9D || payload[4] != 0x01 || payload[5] != 0x2A) return false;

        var widthRaw = ReadU16LE(payload, 6);
        var heightRaw = ReadU16LE(payload, 8);
        var width = widthRaw & 0x3FFF;
        var height = heightRaw & 0x3FFF;
        if (width <= 0 || height <= 0) return false;

        header = new WebpVp8Header(
            width,
            height,
            version,
            showFrame != 0,
            partitionSize,
            bitsConsumed: MinimumHeaderBytes * 8);
        return true;
    }

    private static int ReadU16LE(ReadOnlySpan<byte> data, int offset) {
        if (offset < 0 || offset + 2 > data.Length) return 0;
        return data[offset] | (data[offset + 1] << 8);
    }

    private static int ReadU24LE(ReadOnlySpan<byte> data, int offset) {
        if (offset < 0 || offset + 3 > data.Length) return 0;
        return data[offset]
            | (data[offset + 1] << 8)
            | (data[offset + 2] << 16);
    }
}

internal readonly struct WebpVp8Header {
    public WebpVp8Header(int width, int height, int version, bool showFrame, int partitionSize, int bitsConsumed) {
        Width = width;
        Height = height;
        Version = version;
        ShowFrame = showFrame;
        PartitionSize = partitionSize;
        BitsConsumed = bitsConsumed;
    }

    public int Width { get; }
    public int Height { get; }
    public int Version { get; }
    public bool ShowFrame { get; }
    public int PartitionSize { get; }
    public int BitsConsumed { get; }
}

