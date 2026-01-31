using System;

namespace CodeGlyphX.Rendering.Webp;

/// <summary>
/// Managed WebP writer (VP8L lossless subset + VP8 lossy intra + animation container).
/// </summary>
public static class WebpWriter {
    /// <summary>
    /// Encodes an RGBA32 buffer as WebP lossless (managed VP8L subset).
    /// </summary>
    /// <remarks>
    /// Current limitations:
    /// - VP8L lossless is a managed subset; single prefix-code group (no entropy tiling).
    /// - Limited LZ77/back-reference search; favors short distances.
    /// - Color indexing is limited to palettes up to 256 colors.
    /// </remarks>
    public static byte[] WriteRgba32(int width, int height, ReadOnlySpan<byte> rgba, int stride) {
        return WriteRgba32(width, height, rgba, stride, default);
    }

    /// <summary>
    /// Encodes an RGBA32 buffer as WebP lossless (managed VP8L subset).
    /// </summary>
    public static byte[] WriteRgba32(int width, int height, byte[] rgba, int stride) {
        if (rgba is null) throw new ArgumentNullException(nameof(rgba));
        return WriteRgba32(width, height, rgba.AsSpan(), stride, default);
    }

    /// <summary>
    /// Encodes an RGBA32 buffer as WebP lossless (managed VP8L subset) with metadata chunks.
    /// </summary>
    public static byte[] WriteRgba32(int width, int height, ReadOnlySpan<byte> rgba, int stride, WebpMetadata metadata) {
        if (WebpVp8lEncoder.TryEncodeLiteralRgba32(rgba, width, height, stride, out var webp, out var reason)) {
            if (!metadata.HasData) return webp;
            if (!TryExtractVp8lPayload(webp, out var vp8lPayload)) {
                throw new FormatException("Encoded VP8L payload could not be extracted.");
            }
            var alphaUsed = ComputeAlphaUsed(rgba, width, height, stride);
            return WriteStillWebpContainer(width, height, alphaUsed, "VP8L", vp8lPayload, alphPayload: null, metadata);
        }

        throw new NotSupportedException($"Managed WebP encode is limited to a minimal VP8L subset: {reason}");
    }

    /// <summary>
    /// Encodes an RGBA32 buffer as a lossy WebP using a managed VP8 (lossy) bitstream when possible.
    /// </summary>
    /// <remarks>
    /// Falls back to VP8L with pre-quantized RGB when VP8 lossy encoding is unavailable.
    /// </remarks>
    public static byte[] WriteRgba32Lossy(int width, int height, ReadOnlySpan<byte> rgba, int stride, int quality) {
        return WriteRgba32Lossy(width, height, rgba, stride, quality, default);
    }

    /// <summary>
    /// Encodes an RGBA32 buffer as a lossy WebP using a managed VP8 (lossy) bitstream when possible, with metadata.
    /// </summary>
    public static byte[] WriteRgba32Lossy(int width, int height, ReadOnlySpan<byte> rgba, int stride, int quality, WebpMetadata metadata) {
        if (quality is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(quality));
        if (quality >= 100) {
            return WriteRgba32(width, height, rgba, stride, metadata);
        }

        if (WebpVp8Encoder.TryEncodeLossyRgba32(rgba, width, height, stride, quality, out var webp, out _)) {
            if (!metadata.HasData) return webp;
            if (!TryExtractVp8Payload(webp, out var vp8Payload, out var alphPayload)) {
                throw new FormatException("Encoded VP8 payload could not be extracted.");
            }
            var alphaUsed = alphPayload is { Length: > 0 };
            return WriteStillWebpContainer(width, height, alphaUsed, "VP8 ", vp8Payload, alphPayload, metadata);
        }

        var quantized = QuantizeRgba(rgba, width, height, stride, quality);
        return WriteRgba32(width, height, quantized, width * 4, metadata);
    }

    /// <summary>
    /// Encodes an RGBA32 buffer as a lossy WebP by quantizing pixels and using VP8L.
    /// </summary>
    public static byte[] WriteRgba32Lossy(int width, int height, byte[] rgba, int stride, int quality) {
        if (rgba is null) throw new ArgumentNullException(nameof(rgba));
        return WriteRgba32Lossy(width, height, rgba.AsSpan(), stride, quality, default);
    }

    /// <summary>
    /// Encodes an RGBA32 buffer as a lossy WebP by quantizing pixels and using VP8L, with metadata.
    /// </summary>
    public static byte[] WriteRgba32Lossy(int width, int height, byte[] rgba, int stride, int quality, WebpMetadata metadata) {
        if (rgba is null) throw new ArgumentNullException(nameof(rgba));
        return WriteRgba32Lossy(width, height, rgba.AsSpan(), stride, quality, metadata);
    }

    /// <summary>
    /// Encodes an animated WebP from RGBA32 frames (VP8L frames).
    /// </summary>
    public static byte[] WriteAnimationRgba32(
        int canvasWidth,
        int canvasHeight,
        ReadOnlySpan<WebpAnimationFrame> frames,
        WebpAnimationOptions options) {
        return WriteAnimationRgba32(canvasWidth, canvasHeight, frames, options, default);
    }

    /// <summary>
    /// Encodes an animated WebP from RGBA32 frames (VP8L frames) with metadata chunks.
    /// </summary>
    public static byte[] WriteAnimationRgba32(
        int canvasWidth,
        int canvasHeight,
        ReadOnlySpan<WebpAnimationFrame> frames,
        WebpAnimationOptions options,
        WebpMetadata metadata) {
        if (canvasWidth <= 0 || canvasHeight <= 0) throw new ArgumentOutOfRangeException(nameof(canvasWidth));
        if (canvasWidth > 0x1000000 || canvasHeight > 0x1000000) {
            throw new ArgumentOutOfRangeException(nameof(canvasWidth), "Canvas dimensions must fit in 24-bit WebP size fields.");
        }
        if (frames.Length == 0) throw new ArgumentException("At least one frame is required.", nameof(frames));

        var alphaUsed = false;
        var framePayloads = new byte[frames.Length][];

        for (var i = 0; i < frames.Length; i++) {
            var frame = frames[i];
            ValidateFrame(frame, canvasWidth, canvasHeight);
            if (!alphaUsed) {
                alphaUsed = ComputeAlphaUsed(frame.Rgba, frame.Width, frame.Height, frame.Stride);
            }

            if (!WebpVp8lEncoder.TryEncodeLiteralRgba32(frame.Rgba, frame.Width, frame.Height, frame.Stride, out var webp, out var reason)) {
                throw new NotSupportedException($"Managed WebP encode failed for animation frame {i}: {reason}");
            }
            if (!TryExtractVp8lPayload(webp, out var vp8lPayload)) {
                throw new FormatException("Encoded VP8L payload could not be extracted.");
            }

            framePayloads[i] = BuildAnmfPayload(frame, vp8lPayload, "VP8L", alphPayload: null);
        }

        return WriteAnimatedWebpContainer(canvasWidth, canvasHeight, alphaUsed, options, framePayloads, metadata);
    }

    /// <summary>
    /// Encodes an animated WebP from RGBA32 frames (VP8L frames).
    /// </summary>
    public static byte[] WriteAnimationRgba32(
        int canvasWidth,
        int canvasHeight,
        WebpAnimationFrame[] frames,
        WebpAnimationOptions options) {
        if (frames is null) throw new ArgumentNullException(nameof(frames));
        return WriteAnimationRgba32(canvasWidth, canvasHeight, frames.AsSpan(), options, default);
    }

    /// <summary>
    /// Encodes an animated WebP from RGBA32 frames (VP8L frames) with metadata chunks.
    /// </summary>
    public static byte[] WriteAnimationRgba32(
        int canvasWidth,
        int canvasHeight,
        WebpAnimationFrame[] frames,
        WebpAnimationOptions options,
        WebpMetadata metadata) {
        if (frames is null) throw new ArgumentNullException(nameof(frames));
        return WriteAnimationRgba32(canvasWidth, canvasHeight, frames.AsSpan(), options, metadata);
    }

    /// <summary>
    /// Encodes an animated WebP from RGBA32 frames (managed VP8 lossy intra, with VP8L fallback).
    /// </summary>
    public static byte[] WriteAnimationRgba32Lossy(
        int canvasWidth,
        int canvasHeight,
        ReadOnlySpan<WebpAnimationFrame> frames,
        WebpAnimationOptions options,
        int quality) {
        return WriteAnimationRgba32Lossy(canvasWidth, canvasHeight, frames, options, quality, default);
    }

    /// <summary>
    /// Encodes an animated WebP from RGBA32 frames (managed VP8 lossy intra, with VP8L fallback) with metadata.
    /// </summary>
    public static byte[] WriteAnimationRgba32Lossy(
        int canvasWidth,
        int canvasHeight,
        ReadOnlySpan<WebpAnimationFrame> frames,
        WebpAnimationOptions options,
        int quality,
        WebpMetadata metadata) {
        if (quality is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(quality));
        if (quality >= 100) {
            return WriteAnimationRgba32(canvasWidth, canvasHeight, frames, options, metadata);
        }
        if (canvasWidth <= 0 || canvasHeight <= 0) throw new ArgumentOutOfRangeException(nameof(canvasWidth));
        if (canvasWidth > 0x1000000 || canvasHeight > 0x1000000) {
            throw new ArgumentOutOfRangeException(nameof(canvasWidth), "Canvas dimensions must fit in 24-bit WebP size fields.");
        }
        if (frames.Length == 0) throw new ArgumentException("At least one frame is required.", nameof(frames));

        var alphaUsed = false;
        var framePayloads = new byte[frames.Length][];

        for (var i = 0; i < frames.Length; i++) {
            var frame = frames[i];
            ValidateFrame(frame, canvasWidth, canvasHeight);
            if (!alphaUsed) {
                alphaUsed = ComputeAlphaUsed(frame.Rgba, frame.Width, frame.Height, frame.Stride);
            }

            if (WebpVp8Encoder.TryEncodeLossyRgba32(frame.Rgba, frame.Width, frame.Height, frame.Stride, quality, out var webp, out _)) {
                if (!TryExtractVp8Payload(webp, out var vp8Payload, out var alphPayload)) {
                    throw new FormatException("Encoded VP8 payload could not be extracted.");
                }
                framePayloads[i] = BuildAnmfPayload(frame, vp8Payload, "VP8 ", alphPayload);
                continue;
            }

            var quantized = QuantizeRgba(frame.Rgba, frame.Width, frame.Height, frame.Stride, quality);
            if (!WebpVp8lEncoder.TryEncodeLiteralRgba32(quantized, frame.Width, frame.Height, frame.Width * 4, out var fallbackWebp, out var reason)) {
                throw new NotSupportedException($"Managed WebP encode failed for animation frame {i}: {reason}");
            }
            if (!TryExtractVp8lPayload(fallbackWebp, out var vp8lPayload)) {
                throw new FormatException("Encoded VP8L payload could not be extracted.");
            }
            framePayloads[i] = BuildAnmfPayload(frame, vp8lPayload, "VP8L", alphPayload: null);
        }

        return WriteAnimatedWebpContainer(canvasWidth, canvasHeight, alphaUsed, options, framePayloads, metadata);
    }

    /// <summary>
    /// Encodes an animated WebP from RGBA32 frames (managed VP8 lossy intra, with VP8L fallback).
    /// </summary>
    public static byte[] WriteAnimationRgba32Lossy(
        int canvasWidth,
        int canvasHeight,
        WebpAnimationFrame[] frames,
        WebpAnimationOptions options,
        int quality) {
        if (frames is null) throw new ArgumentNullException(nameof(frames));
        return WriteAnimationRgba32Lossy(canvasWidth, canvasHeight, frames.AsSpan(), options, quality, default);
    }

    /// <summary>
    /// Encodes an animated WebP from RGBA32 frames (managed VP8 lossy intra, with VP8L fallback) with metadata.
    /// </summary>
    public static byte[] WriteAnimationRgba32Lossy(
        int canvasWidth,
        int canvasHeight,
        WebpAnimationFrame[] frames,
        WebpAnimationOptions options,
        int quality,
        WebpMetadata metadata) {
        if (frames is null) throw new ArgumentNullException(nameof(frames));
        return WriteAnimationRgba32Lossy(canvasWidth, canvasHeight, frames.AsSpan(), options, quality, metadata);
    }

    private static byte[] QuantizeRgba(ReadOnlySpan<byte> rgba, int width, int height, int stride, int quality) {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        var minStride = checked(width * 4);
        if (stride < minStride) throw new ArgumentOutOfRangeException(nameof(stride));

        var requiredBytes = checked((height - 1) * stride + minStride);
        if (rgba.Length < requiredBytes) throw new ArgumentException("Input RGBA buffer is too small.", nameof(rgba));

        var bits = 1 + (quality * 7 / 100);
        if (bits > 8) bits = 8;
        var shift = 8 - bits;

        var output = new byte[checked(width * height * 4)];
        var dstStride = minStride;
        for (var y = 0; y < height; y++) {
            var srcOffset = y * stride;
            var dstOffset = y * dstStride;
            for (var x = 0; x < width; x++) {
                var r = rgba[srcOffset];
                var g = rgba[srcOffset + 1];
                var b = rgba[srcOffset + 2];
                var a = rgba[srcOffset + 3];
                if (shift > 0) {
                    r = (byte)((r >> shift) << shift);
                    g = (byte)((g >> shift) << shift);
                    b = (byte)((b >> shift) << shift);
                }
                output[dstOffset] = r;
                output[dstOffset + 1] = g;
                output[dstOffset + 2] = b;
                output[dstOffset + 3] = a;
                srcOffset += 4;
                dstOffset += 4;
            }
        }

        return output;
    }

    private static void ValidateFrame(in WebpAnimationFrame frame, int canvasWidth, int canvasHeight) {
        if (frame.Width <= 0 || frame.Height <= 0) throw new ArgumentOutOfRangeException(nameof(frame));
        if (frame.Width > 0x1000000 || frame.Height > 0x1000000) {
            throw new ArgumentOutOfRangeException(nameof(frame), "Frame dimensions must fit in 24-bit WebP size fields.");
        }
        if (frame.Stride < frame.Width * 4) throw new ArgumentOutOfRangeException(nameof(frame.Stride));
        if (frame.X < 0 || frame.Y < 0) throw new ArgumentOutOfRangeException(nameof(frame));
        if (frame.X + frame.Width > canvasWidth || frame.Y + frame.Height > canvasHeight) {
            throw new ArgumentOutOfRangeException(nameof(frame));
        }
        if ((frame.X & 1) != 0 || (frame.Y & 1) != 0) {
            throw new ArgumentException("Animation frame X/Y offsets must be even.", nameof(frame));
        }

        var requiredBytes = checked((frame.Height - 1) * frame.Stride + frame.Width * 4);
        if (frame.Rgba.Length < requiredBytes) {
            throw new ArgumentException("Animation frame RGBA buffer is too small.", nameof(frame));
        }
    }

    private static bool ComputeAlphaUsed(ReadOnlySpan<byte> rgba, int width, int height, int stride) {
        var alphaOffset = 3;
        for (var y = 0; y < height; y++) {
            var offset = y * stride + alphaOffset;
            for (var x = 0; x < width; x++) {
                if (rgba[offset] != 255) return true;
                offset += 4;
            }
        }
        return false;
    }

    private static bool TryExtractVp8lPayload(byte[] webp, out byte[] payload) {
        payload = Array.Empty<byte>();
        if (webp is null || webp.Length < 12) return false;
        var riffSize = ReadU32LE(webp, 4);
        var riffLimit = webp.Length;
        var declaredLimit = 8L + riffSize;
        if (declaredLimit > 0 && declaredLimit < riffLimit) {
            riffLimit = (int)declaredLimit;
        }
        if (riffLimit < 12) return false;

        var offset = 12;
        while (offset + 8 <= riffLimit) {
            var fourCc = ReadU32LE(webp, offset);
            var chunkSize = ReadU32LE(webp, offset + 4);
            var dataOffset = offset + 8;
            if (chunkSize > int.MaxValue) return false;
            var chunkLength = (int)chunkSize;
            if (dataOffset + chunkLength > riffLimit) return false;

            if (fourCc == FourCcVp8L) {
                payload = new byte[chunkLength];
                Buffer.BlockCopy(webp, dataOffset, payload, 0, chunkLength);
                return true;
            }

            var padded = chunkLength + (chunkLength & 1);
            offset = dataOffset + padded;
        }

        return false;
    }

    private static bool TryExtractVp8Payload(byte[] webp, out byte[] payload, out byte[]? alphPayload) {
        payload = Array.Empty<byte>();
        alphPayload = null;
        if (webp is null || webp.Length < 12) return false;
        var riffSize = ReadU32LE(webp, 4);
        var riffLimit = webp.Length;
        var declaredLimit = 8L + riffSize;
        if (declaredLimit > 0 && declaredLimit < riffLimit) {
            riffLimit = (int)declaredLimit;
        }
        if (riffLimit < 12) return false;

        var offset = 12;
        while (offset + 8 <= riffLimit) {
            var fourCc = ReadU32LE(webp, offset);
            var chunkSize = ReadU32LE(webp, offset + 4);
            var dataOffset = offset + 8;
            if (chunkSize > int.MaxValue) return false;
            var chunkLength = (int)chunkSize;
            if (dataOffset + chunkLength > riffLimit) return false;

            if (fourCc == FourCcVp8) {
                payload = new byte[chunkLength];
                Buffer.BlockCopy(webp, dataOffset, payload, 0, chunkLength);
            } else if (fourCc == FourCcAlph && alphPayload is null) {
                alphPayload = new byte[chunkLength];
                Buffer.BlockCopy(webp, dataOffset, alphPayload, 0, chunkLength);
            }

            var padded = chunkLength + (chunkLength & 1);
            offset = dataOffset + padded;
        }

        return payload.Length > 0;
    }

    private static byte[] WriteAnimatedWebpContainer(
        int canvasWidth,
        int canvasHeight,
        bool alphaUsed,
        WebpAnimationOptions options,
        byte[][] framePayloads,
        WebpMetadata metadata) {
        using var ms = new System.IO.MemoryStream();
        WriteAscii(ms, "RIFF");
        WriteU32LE(ms, 0);
        WriteAscii(ms, "WEBP");

        var vp8x = new byte[10];
        vp8x[0] = BuildVp8xFlags(alphaUsed, metadata, animation: true);
        WriteU24LE(vp8x, 4, canvasWidth - 1);
        WriteU24LE(vp8x, 7, canvasHeight - 1);
        WriteChunk(ms, "VP8X", vp8x);

        WriteMetadataChunks(ms, metadata);

        var animPayload = new byte[6];
        if (options.LoopCount is < 0 or > 0xFFFF) {
            throw new ArgumentOutOfRangeException(nameof(options.LoopCount));
        }
        WriteU32LE(animPayload, 0, options.BackgroundBgra);
        WriteU16LE(animPayload, 4, options.LoopCount);
        WriteChunk(ms, "ANIM", animPayload);

        for (var i = 0; i < framePayloads.Length; i++) {
            WriteChunk(ms, "ANMF", framePayloads[i]);
        }

        var bytes = ms.ToArray();
        var riffSize = checked((uint)(bytes.Length - 8));
        WriteU32LE(bytes, 4, riffSize);
        return bytes;
    }

    private static byte[] WriteStillWebpContainer(
        int width,
        int height,
        bool alphaUsed,
        string imageFourCc,
        byte[] imagePayload,
        byte[]? alphPayload,
        WebpMetadata metadata) {
        using var ms = new System.IO.MemoryStream();
        WriteAscii(ms, "RIFF");
        WriteU32LE(ms, 0);
        WriteAscii(ms, "WEBP");

        var vp8x = new byte[10];
        vp8x[0] = BuildVp8xFlags(alphaUsed, metadata, animation: false);
        WriteU24LE(vp8x, 4, width - 1);
        WriteU24LE(vp8x, 7, height - 1);
        WriteChunk(ms, "VP8X", vp8x);

        WriteMetadataChunks(ms, metadata);

        if (alphPayload is { Length: > 0 }) {
            WriteChunk(ms, "ALPH", alphPayload);
        }
        WriteChunk(ms, imageFourCc, imagePayload);

        var bytes = ms.ToArray();
        var riffSize = checked((uint)(bytes.Length - 8));
        WriteU32LE(bytes, 4, riffSize);
        return bytes;
    }

    private static void WriteMetadataChunks(System.IO.Stream stream, WebpMetadata metadata) {
        if (metadata.Icc is { Length: > 0 }) {
            WriteChunk(stream, "ICCP", metadata.Icc);
        }
        if (metadata.Exif is { Length: > 0 }) {
            WriteChunk(stream, "EXIF", metadata.Exif);
        }
        if (metadata.Xmp is { Length: > 0 }) {
            WriteChunk(stream, "XMP ", metadata.Xmp);
        }
    }

    private static byte BuildVp8xFlags(bool alphaUsed, WebpMetadata metadata, bool animation) {
        byte flags = 0;
        if (metadata.Icc is { Length: > 0 }) flags |= 0x01;
        if (alphaUsed) flags |= 0x02;
        if (metadata.Exif is { Length: > 0 }) flags |= 0x04;
        if (metadata.Xmp is { Length: > 0 }) flags |= 0x08;
        if (animation) flags |= 0x10;
        return flags;
    }

    private static byte[] BuildAnmfPayload(WebpAnimationFrame frame, byte[] imagePayload, string imageFourCc, byte[]? alphPayload) {
        using var ms = new System.IO.MemoryStream();
        WriteU24LE(ms, frame.X / 2);
        WriteU24LE(ms, frame.Y / 2);
        WriteU24LE(ms, frame.Width - 1);
        WriteU24LE(ms, frame.Height - 1);

        var duration = frame.DurationMs;
        if (duration <= 0) duration = 1;
        if (duration > 0xFFFFFF) duration = 0xFFFFFF;
        WriteU24LE(ms, duration);

        var flags = 0;
        if (frame.DisposeToBackground) flags |= 0x01;
        if (!frame.Blend) flags |= 0x02;
        ms.WriteByte((byte)flags);

        if (alphPayload is { Length: > 0 }) {
            WriteChunk(ms, "ALPH", alphPayload);
        }
        WriteChunk(ms, imageFourCc, imagePayload);

        return ms.ToArray();
    }

    private static void WriteChunk(System.IO.Stream stream, string fourCc, byte[] payload) {
        WriteAscii(stream, fourCc);
        WriteU32LE(stream, (uint)payload.Length);
        if (payload.Length > 0) {
            stream.Write(payload, 0, payload.Length);
        }
        if ((payload.Length & 1) != 0) {
            stream.WriteByte(0);
        }
    }

    private static void WriteAscii(System.IO.Stream stream, string text) {
        var bytes = System.Text.Encoding.ASCII.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static void WriteU16LE(byte[] buffer, int offset, int value) {
        buffer[offset] = (byte)(value & 0xFF);
        buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
    }

    private static void WriteU24LE(byte[] buffer, int offset, int value) {
        buffer[offset] = (byte)(value & 0xFF);
        buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
        buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
    }

    private static void WriteU24LE(System.IO.Stream stream, int value) {
        stream.WriteByte((byte)(value & 0xFF));
        stream.WriteByte((byte)((value >> 8) & 0xFF));
        stream.WriteByte((byte)((value >> 16) & 0xFF));
    }

    private static void WriteU32LE(System.IO.Stream stream, uint value) {
        stream.WriteByte((byte)(value & 0xFF));
        stream.WriteByte((byte)((value >> 8) & 0xFF));
        stream.WriteByte((byte)((value >> 16) & 0xFF));
        stream.WriteByte((byte)((value >> 24) & 0xFF));
    }

    private static void WriteU32LE(byte[] buffer, int offset, uint value) {
        buffer[offset] = (byte)(value & 0xFF);
        buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
        buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
        buffer[offset + 3] = (byte)((value >> 24) & 0xFF);
    }

    private static uint ReadU32LE(byte[] data, int offset) {
        if (offset < 0 || offset + 4 > data.Length) return 0;
        return (uint)(data[offset]
            | (data[offset + 1] << 8)
            | (data[offset + 2] << 16)
            | (data[offset + 3] << 24));
    }

    private const uint FourCcVp8L = 0x4C385056; // "VP8L"
    private const uint FourCcVp8 = 0x20385056; // "VP8 "
    private const uint FourCcAlph = 0x48504C41; // "ALPH"
}
