using System;

namespace CodeGlyphX.Rendering.Webp;

/// <summary>
/// Managed WebP writer (VP8L lossless subset + minimal VP8 lossy + animation container).
/// </summary>
public static class WebpWriter {
    /// <summary>
    /// Encodes an RGBA32 buffer as WebP lossless (managed VP8L subset).
    /// </summary>
    /// <remarks>
    /// Current limitations:
    /// - Lossless only (VP8L). No lossy VP8 bitstream.
    /// - Single prefix-code group; no entropy tiling.
    /// - Limited LZ77/back-reference search; favors short distances.
    /// - Color indexing is limited to palettes up to 16 colors.
    /// - Metadata chunks (VP8X/ICCP/EXIF/XMP) are not written.
    /// </remarks>
    public static byte[] WriteRgba32(int width, int height, ReadOnlySpan<byte> rgba, int stride) {
        if (WebpVp8lEncoder.TryEncodeLiteralRgba32(rgba, width, height, stride, out var webp, out var reason)) {
            return webp;
        }

        throw new NotSupportedException($"Managed WebP encode is limited to a minimal VP8L subset: {reason}");
    }

    /// <summary>
    /// Encodes an RGBA32 buffer as WebP lossless (managed VP8L subset).
    /// </summary>
    public static byte[] WriteRgba32(int width, int height, byte[] rgba, int stride) {
        if (rgba is null) throw new ArgumentNullException(nameof(rgba));
        return WriteRgba32(width, height, rgba.AsSpan(), stride);
    }

    /// <summary>
    /// Encodes an RGBA32 buffer as a lossy WebP using a managed VP8 (lossy) bitstream when possible.
    /// </summary>
    /// <remarks>
    /// Falls back to VP8L with pre-quantized RGB when VP8 lossy encoding is unavailable.
    /// </remarks>
    public static byte[] WriteRgba32Lossy(int width, int height, ReadOnlySpan<byte> rgba, int stride, int quality) {
        if (quality is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(quality));
        if (quality >= 100) {
            return WriteRgba32(width, height, rgba, stride);
        }

        if (WebpVp8Encoder.TryEncodeLossyRgba32(rgba, width, height, stride, quality, out var webp, out _)) {
            return webp;
        }

        var quantized = QuantizeRgba(rgba, width, height, stride, quality);
        return WriteRgba32(width, height, quantized, width * 4);
    }

    /// <summary>
    /// Encodes an RGBA32 buffer as a lossy WebP by quantizing pixels and using VP8L.
    /// </summary>
    public static byte[] WriteRgba32Lossy(int width, int height, byte[] rgba, int stride, int quality) {
        if (rgba is null) throw new ArgumentNullException(nameof(rgba));
        return WriteRgba32Lossy(width, height, rgba.AsSpan(), stride, quality);
    }

    /// <summary>
    /// Encodes an animated WebP from RGBA32 frames (VP8L frames).
    /// </summary>
    public static byte[] WriteAnimationRgba32(
        int canvasWidth,
        int canvasHeight,
        ReadOnlySpan<WebpAnimationFrame> frames,
        WebpAnimationOptions options) {
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
            alphaUsed |= ComputeAlphaUsed(frame.Rgba, frame.Width, frame.Height, frame.Stride);

            if (!WebpVp8lEncoder.TryEncodeLiteralRgba32(frame.Rgba, frame.Width, frame.Height, frame.Stride, out var webp, out var reason)) {
                throw new NotSupportedException($"Managed WebP encode failed for animation frame {i}: {reason}");
            }
            if (!TryExtractVp8lPayload(webp, out var vp8lPayload)) {
                throw new FormatException("Encoded VP8L payload could not be extracted.");
            }

            framePayloads[i] = BuildAnmfPayload(frame, vp8lPayload);
        }

        return WriteAnimatedWebpContainer(canvasWidth, canvasHeight, alphaUsed, options, framePayloads);
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
        return WriteAnimationRgba32(canvasWidth, canvasHeight, frames.AsSpan(), options);
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

    private static bool ComputeAlphaUsed(byte[] rgba, int width, int height, int stride) {
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

    private static byte[] WriteAnimatedWebpContainer(
        int canvasWidth,
        int canvasHeight,
        bool alphaUsed,
        WebpAnimationOptions options,
        byte[][] framePayloads) {
        using var ms = new System.IO.MemoryStream();
        WriteAscii(ms, "RIFF");
        WriteU32LE(ms, 0);
        WriteAscii(ms, "WEBP");

        var vp8x = new byte[10];
        vp8x[0] = (byte)((alphaUsed ? 0x02 : 0x00) | 0x10); // alpha + animation
        WriteU24LE(vp8x, 4, canvasWidth - 1);
        WriteU24LE(vp8x, 7, canvasHeight - 1);
        WriteChunk(ms, "VP8X", vp8x);

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

    private static byte[] BuildAnmfPayload(WebpAnimationFrame frame, byte[] vp8lPayload) {
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

        WriteAscii(ms, "VP8L");
        WriteU32LE(ms, (uint)vp8lPayload.Length);
        ms.Write(vp8lPayload, 0, vp8lPayload.Length);
        if ((vp8lPayload.Length & 1) != 0) {
            ms.WriteByte(0);
        }

        return ms.ToArray();
    }

    private static void WriteChunk(System.IO.Stream stream, string fourCc, ReadOnlySpan<byte> payload) {
        WriteAscii(stream, fourCc);
        WriteU32LE(stream, (uint)payload.Length);
        if (!payload.IsEmpty) {
            var buffer = new byte[payload.Length];
            payload.CopyTo(buffer);
            stream.Write(buffer, 0, buffer.Length);
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
}
