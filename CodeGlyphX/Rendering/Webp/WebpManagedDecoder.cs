using System;

namespace CodeGlyphX.Rendering.Webp;

/// <summary>
/// Managed WebP decoder entry point (work in progress).
/// </summary>
internal static class WebpManagedDecoder {
    /// <summary>
    /// Attempts to decode WebP to RGBA32 using a managed implementation.
    /// </summary>
    /// <remarks>
    /// This currently returns <c>false</c> until the managed decoder is implemented.
    /// The call site prefers this path before falling back to native decode.
    /// </remarks>
    public static bool TryDecodeRgba32(ReadOnlySpan<byte> data, out byte[] rgba, out int width, out int height) {
        rgba = Array.Empty<byte>();
        width = 0;
        height = 0;
        if (!WebpReader.IsWebp(data)) return false;

        if (!TryFindVp8lChunk(data, out var vp8lPayload)) return false;
        return WebpVp8lDecoder.TryDecode(vp8lPayload, out rgba, out width, out height);
    }

    private static bool TryFindVp8lChunk(ReadOnlySpan<byte> data, out ReadOnlySpan<byte> payload) {
        payload = default;
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

            if (fourCc == FourCcVp8L) {
                payload = data.Slice(dataOffset, chunkLength);
                return true;
            }

            var padded = chunkLength + (chunkLength & 1);
            offset = dataOffset + padded;
        }

        return false;
    }

    private const uint FourCcVp8L = 0x4C385056; // "VP8L"

    private static uint ReadU32LE(ReadOnlySpan<byte> span, int offset) {
        if (offset < 0 || offset + 4 > span.Length) return 0;
        return (uint)(span[offset]
            | (span[offset + 1] << 8)
            | (span[offset + 2] << 16)
            | (span[offset + 3] << 24));
    }
}
