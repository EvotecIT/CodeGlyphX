using System;

namespace CodeGlyphX.Rendering.Webp;

/// <summary>
/// Minimal WebP container parsing plus managed decode (VP8/VP8L stills).
/// </summary>
public static class WebpReader {
    private const uint FourCcRiff = 0x46464952; // "RIFF"
    private const uint FourCcWebp = 0x50424557; // "WEBP"
    private const uint FourCcVp8X = 0x58385056; // "VP8X"
    private const uint FourCcVp8L = 0x4C385056; // "VP8L"
    private const uint FourCcVp8 = 0x20385056;  // "VP8 "

    /// <summary>
    /// Checks whether the buffer looks like a WebP RIFF container.
    /// </summary>
    public static bool IsWebp(ReadOnlySpan<byte> data) {
        if (data.Length < 12) return false;
        return ReadU32LE(data, 0) == FourCcRiff && ReadU32LE(data, 8) == FourCcWebp;
    }

    /// <summary>
    /// Attempts to read WebP dimensions from the RIFF container without decoding pixels.
    /// </summary>
    public static bool TryReadDimensions(ReadOnlySpan<byte> data, out int width, out int height) {
        width = 0;
        height = 0;
        if (!IsWebp(data)) return false;

        if (data.Length < 12) return false;
        var riffSize = ReadU32LE(data, 4);
        var riffLimit = data.Length;
        var declaredLimit = 8L + riffSize;
        if (declaredLimit > 0 && declaredLimit < riffLimit) {
            riffLimit = (int)declaredLimit;
        }
        if (riffLimit < 12) return false;

        var offset = 12;
        while (offset + 8 <= riffLimit) {
            var fourCc = ReadU32LE(data, offset);
            var chunkSize = ReadU32LE(data, offset + 4);
            var dataOffset = offset + 8;

            if (chunkSize > int.MaxValue) return false;
            var chunkLength = (int)chunkSize;
            if (dataOffset < 0 || dataOffset > riffLimit) return false;
            if (dataOffset + chunkLength > riffLimit) return false;

            var chunk = data.Slice(dataOffset, chunkLength);
            if (fourCc == FourCcVp8X && TryReadVp8XSize(chunk, out width, out height)) return true;
            if (fourCc == FourCcVp8L && TryReadVp8LSize(chunk, out width, out height)) return true;
            if (fourCc == FourCcVp8 && TryReadVp8Size(chunk, out width, out height)) return true;

            var padded = chunkLength + (chunkLength & 1);
            var nextOffset = (long)dataOffset + padded;
            if (nextOffset < 0 || nextOffset > riffLimit || nextOffset > int.MaxValue) return false;
            offset = (int)nextOffset;
        }

        return false;
    }

    /// <summary>
    /// Decodes a WebP image to RGBA32 using the managed VP8/VP8L decoder.
    /// </summary>
    public static byte[] DecodeRgba32(ReadOnlySpan<byte> data, out int width, out int height) {
        if (!IsWebp(data)) throw new FormatException("Invalid WebP container.");
        if (WebpManagedDecoder.TryDecodeRgba32(data, out var managedRgba, out width, out height)) {
            return managedRgba;
        }
        throw new FormatException("Unsupported or invalid WebP. Managed decode supports VP8/VP8L still images and animated WebP (first frame).");
    }

    private static bool TryReadVp8XSize(ReadOnlySpan<byte> chunk, out int width, out int height) {
        width = 0;
        height = 0;
        if (chunk.Length < 10) return false;
        var widthMinus1 = ReadU24LE(chunk, 4);
        var heightMinus1 = ReadU24LE(chunk, 7);
        width = widthMinus1 + 1;
        height = heightMinus1 + 1;
        return width > 0 && height > 0;
    }

    private static bool TryReadVp8LSize(ReadOnlySpan<byte> chunk, out int width, out int height) {
        width = 0;
        height = 0;
        if (chunk.Length < 5) return false;
        if (chunk[0] != 0x2F) return false;
        var bits = ReadU32LE(chunk, 1);
        width = (int)(bits & 0x3FFF) + 1;
        height = (int)((bits >> 14) & 0x3FFF) + 1;
        return width > 0 && height > 0;
    }

    private static bool TryReadVp8Size(ReadOnlySpan<byte> chunk, out int width, out int height) {
        width = 0;
        height = 0;
        if (chunk.Length < 10) return false;
        if (chunk[3] != 0x9D || chunk[4] != 0x01 || chunk[5] != 0x2A) return false;
        width = ReadU16LE(chunk, 6) & 0x3FFF;
        height = ReadU16LE(chunk, 8) & 0x3FFF;
        return width > 0 && height > 0;
    }

    private static int ReadU16LE(ReadOnlySpan<byte> data, int offset) {
        if (offset < 0 || offset + 2 > data.Length) return 0;
        return data[offset] | (data[offset + 1] << 8);
    }

    private static int ReadU24LE(ReadOnlySpan<byte> data, int offset) {
        if (offset < 0 || offset + 3 > data.Length) return 0;
        return data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16);
    }

    private static uint ReadU32LE(ReadOnlySpan<byte> data, int offset) {
        if (offset < 0 || offset + 4 > data.Length) return 0;
        return (uint)(data[offset]
            | (data[offset + 1] << 8)
            | (data[offset + 2] << 16)
            | (data[offset + 3] << 24));
    }

}
