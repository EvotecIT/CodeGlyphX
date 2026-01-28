using System;

namespace CodeGlyphX.Rendering.Webp;

/// <summary>
/// Managed WebP decoder entry point (VP8 + VP8L still images).
/// </summary>
internal static class WebpManagedDecoder {
    /// <summary>
    /// Attempts to decode WebP to RGBA32 using a managed implementation.
    /// </summary>
    /// <remarks>
    /// Supported: VP8 (lossy, keyframe stills) and VP8L (lossless).
    /// Supported: VP8 + ALPH alpha chunk and animated WebP (first-frame decode).
    /// Not supported: metadata-only containers.
    /// The call site prefers this path and does not fall back to native decoding.
    /// </remarks>
    public static bool TryDecodeRgba32(ReadOnlySpan<byte> data, out byte[] rgba, out int width, out int height) {
        rgba = Array.Empty<byte>();
        width = 0;
        height = 0;
        if (!WebpReader.IsWebp(data)) return false;
        if (data.Length > WebpReader.MaxWebpBytes) return false;

        if (!TryEnumerateChunks(data, out var chunkSpan, out var chunks)) return false;

        if (TryDecodeAnimatedWebp(chunkSpan, chunks, out rgba, out width, out height)) {
            return true;
        }

        if (TryFindChunk(chunkSpan, chunks, FourCcVp8L, out var vp8lPayload)) {
            return WebpVp8lDecoder.TryDecode(vp8lPayload, out rgba, out width, out height);
        }

        if (TryFindChunk(chunkSpan, chunks, FourCcVp8, out var vp8Payload)) {
            if (!WebpVp8Decoder.TryDecode(vp8Payload, out rgba, out width, out height)) {
                return false;
            }

            if (TryFindChunk(chunkSpan, chunks, FourCcAlph, out var alphPayload)) {
                if (!TryDecodeAlphaChunk(alphPayload, width, height, out var alpha, out _)) {
                    return false;
                }
                ApplyAlphaToRgba(alpha, rgba);
            }

            return true;
        }

        return false;
    }

    internal static bool TryDecodeAnimationFrames(
        ReadOnlySpan<byte> data,
        out WebpAnimationFrame[] frames,
        out int canvasWidth,
        out int canvasHeight,
        out WebpAnimationOptions options) {
        frames = Array.Empty<WebpAnimationFrame>();
        canvasWidth = 0;
        canvasHeight = 0;
        options = default;

        if (!TryDecodeAnimationFrameInfoList(data, out var frameInfos, out canvasWidth, out canvasHeight, out options)) {
            return false;
        }

        var decodedFrames = new WebpAnimationFrame[frameInfos.Count];
        for (var i = 0; i < frameInfos.Count; i++) {
            var frame = frameInfos[i];
            decodedFrames[i] = new WebpAnimationFrame(
                frame.Rgba,
                frame.Width,
                frame.Height,
                frame.Width * 4,
                frame.DurationMs,
                frame.X,
                frame.Y,
                frame.Blend,
                frame.DisposeToBackground);
        }

        frames = decodedFrames;
        return true;
    }

    internal static bool TryDecodeAnimationCanvasFrames(
        ReadOnlySpan<byte> data,
        out WebpAnimationFrame[] frames,
        out int canvasWidth,
        out int canvasHeight,
        out WebpAnimationOptions options) {
        frames = Array.Empty<WebpAnimationFrame>();
        canvasWidth = 0;
        canvasHeight = 0;
        options = default;

        if (!TryDecodeAnimationFrameInfoList(data, out var frameInfos, out canvasWidth, out canvasHeight, out options)) {
            return false;
        }

        var backgroundBgra = options.BackgroundBgra;
        var canvas = new byte[checked(canvasWidth * canvasHeight * 4)];
        FillBackground(canvas, backgroundBgra);

        var renderedFrames = new WebpAnimationFrame[frameInfos.Count];
        for (var i = 0; i < frameInfos.Count; i++) {
            var frame = frameInfos[i];
            if (frame.Blend) {
                AlphaBlendFrame(canvas, canvasWidth, canvasHeight, frame);
            } else {
                ReplaceFrame(canvas, canvasWidth, canvasHeight, frame);
            }

            var snapshot = new byte[canvas.Length];
            Buffer.BlockCopy(canvas, 0, snapshot, 0, canvas.Length);
            renderedFrames[i] = new WebpAnimationFrame(
                snapshot,
                canvasWidth,
                canvasHeight,
                canvasWidth * 4,
                frame.DurationMs,
                0,
                0,
                blend: false,
                disposeToBackground: false);

            if (frame.DisposeToBackground) {
                ClearFrameToBackground(canvas, canvasWidth, frame, backgroundBgra);
            }
        }

        frames = renderedFrames;
        return true;
    }

    private static bool TryEnumerateChunks(
        ReadOnlySpan<byte> data,
        out ReadOnlySpan<byte> chunkSpan,
        out WebpChunk[] chunks) {
        chunkSpan = ReadOnlySpan<byte>.Empty;
        chunks = Array.Empty<WebpChunk>();
        if (data.Length < 12) return false;

        var riffSize = ReadU32LE(data, 4);
        var riffLimit = (long)data.Length;
        var declaredLimit = 8L + riffSize;
        if (declaredLimit > 0 && declaredLimit < riffLimit) {
            riffLimit = declaredLimit;
        }
        if (riffLimit < 12) return false;
        if (riffLimit > int.MaxValue) return false;

        chunkSpan = data.Slice(12, (int)riffLimit - 12);
        if (!TryEnumerateChunks(chunkSpan, out var list)) return false;
        chunks = list;
        return true;
    }

    private static bool TryDecodeAnimationFrameInfoList(
        ReadOnlySpan<byte> data,
        out System.Collections.Generic.List<WebpAnimationFrameInfo> frames,
        out int canvasWidth,
        out int canvasHeight,
        out WebpAnimationOptions options) {
        frames = new System.Collections.Generic.List<WebpAnimationFrameInfo>();
        canvasWidth = 0;
        canvasHeight = 0;
        options = default;
        if (!WebpReader.IsWebp(data)) return false;
        if (data.Length > WebpReader.MaxWebpBytes) return false;

        if (!TryEnumerateChunks(data, out var chunkSpan, out var chunks)) return false;

        var background = 0u;
        var loopCount = 0;
        if (TryFindChunk(chunkSpan, chunks, FourCcAnim, out var animPayload)) {
            if (animPayload.Length < 6) return false;
            background = ReadU32LE(animPayload, 0);
            loopCount = ReadU16LE(animPayload, 4);
        }

        if (TryFindChunk(chunkSpan, chunks, FourCcVp8X, out var vp8xPayload)) {
            if (!TryReadVp8X(vp8xPayload, out canvasWidth, out canvasHeight, out _)) return false;
        }

        for (var i = 0; i < chunks.Length; i++) {
            if (chunks[i].FourCc != FourCcAnmf) continue;
            var payload = chunkSpan.Slice(chunks[i].DataOffset, chunks[i].Length);
            if (!TryDecodeAnimationFrame(payload, out var frame)) return false;

            if (canvasWidth <= 0 || canvasHeight <= 0) {
                canvasWidth = frame.Width;
                canvasHeight = frame.Height;
            }

            if (frame.X < 0 || frame.Y < 0) return false;
            if (frame.X + frame.Width > canvasWidth || frame.Y + frame.Height > canvasHeight) return false;

            frames.Add(frame);
        }

        if (frames.Count == 0) return false;

        options = new WebpAnimationOptions(loopCount, background);
        return true;
    }

    private static bool TryEnumerateChunks(ReadOnlySpan<byte> data, out WebpChunk[] chunks) {
        chunks = Array.Empty<WebpChunk>();
        var offset = 0;
        var list = new System.Collections.Generic.List<WebpChunk>();

        while ((long)offset + 8 <= data.Length) {
            var fourCc = ReadU32LE(data, offset);
            var chunkSize = ReadU32LE(data, offset + 4);
            var dataOffset = (long)offset + 8;

            var chunkLength = (long)chunkSize;
            if (chunkLength < 0 || chunkLength > int.MaxValue) return false;
            if (dataOffset < 0 || dataOffset > data.Length) return false;
            if (dataOffset + chunkLength > data.Length) return false;

            list.Add(new WebpChunk(fourCc, (int)dataOffset, (int)chunkLength));

            var padded = chunkLength + (chunkLength & 1);
            var nextOffset = dataOffset + padded;
            if (nextOffset < 0 || nextOffset > int.MaxValue) return false;
            offset = (int)nextOffset;
        }

        chunks = list.ToArray();
        return true;
    }

    private static bool TryFindChunk(ReadOnlySpan<byte> data, WebpChunk[] chunks, uint targetFourCc, out ReadOnlySpan<byte> payload) {
        payload = default;
        for (var i = 0; i < chunks.Length; i++) {
            if (chunks[i].FourCc != targetFourCc) continue;
            payload = data.Slice(chunks[i].DataOffset, chunks[i].Length);
            return true;
        }
        return false;
    }

    private static bool TryDecodeAnimatedWebp(
        ReadOnlySpan<byte> chunkSpan,
        WebpChunk[] chunks,
        out byte[] rgba,
        out int width,
        out int height) {
        rgba = Array.Empty<byte>();
        width = 0;
        height = 0;

        if (!TryFindChunk(chunkSpan, chunks, FourCcAnmf, out var anmfPayload)) {
            return false;
        }

        var canvasWidth = 0;
        var canvasHeight = 0;
        if (TryFindChunk(chunkSpan, chunks, FourCcVp8X, out var vp8xPayload)) {
            if (!TryReadVp8X(vp8xPayload, out canvasWidth, out canvasHeight, out _)) return false;
        }

        var background = 0u;
        if (TryFindChunk(chunkSpan, chunks, FourCcAnim, out var animPayload)) {
            if (animPayload.Length < 4) return false;
            background = ReadU32LE(animPayload, 0);
        }

        if (!TryDecodeAnimationFrame(anmfPayload, out var frame)) return false;

        if (canvasWidth <= 0 || canvasHeight <= 0) {
            canvasWidth = frame.Width;
            canvasHeight = frame.Height;
        }

        if (frame.X < 0 || frame.Y < 0) return false;
        if (frame.X + frame.Width > canvasWidth || frame.Y + frame.Height > canvasHeight) return false;

        var canvas = new byte[checked(canvasWidth * canvasHeight * 4)];
        FillBackground(canvas, background);

        if (frame.Blend) {
            AlphaBlendFrame(canvas, canvasWidth, canvasHeight, frame);
        } else {
            ReplaceFrame(canvas, canvasWidth, canvasHeight, frame);
        }

        rgba = canvas;
        width = canvasWidth;
        height = canvasHeight;
        return true;
    }

    private readonly struct WebpChunk {
        public WebpChunk(uint fourCc, int dataOffset, int length) {
            FourCc = fourCc;
            DataOffset = dataOffset;
            Length = length;
        }

        public uint FourCc { get; }
        public int DataOffset { get; }
        public int Length { get; }
    }

    private readonly struct WebpAnimationFrameInfo {
        public WebpAnimationFrameInfo(int x, int y, int width, int height, int durationMs, bool blend, bool disposeToBackground, byte[] rgba) {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            DurationMs = durationMs;
            Blend = blend;
            DisposeToBackground = disposeToBackground;
            Rgba = rgba;
        }

        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }
        public int DurationMs { get; }
        public bool Blend { get; }
        public bool DisposeToBackground { get; }
        public byte[] Rgba { get; }
    }

    private static bool TryDecodeAnimationFrame(ReadOnlySpan<byte> payload, out WebpAnimationFrameInfo frame) {
        frame = default;
        if (payload.Length < 16) return false;

        var x = ReadU24LE(payload, 0) * 2;
        var y = ReadU24LE(payload, 3) * 2;
        var width = ReadU24LE(payload, 6) + 1;
        var height = ReadU24LE(payload, 9) + 1;
        if (width <= 0 || height <= 0) return false;

        var duration = ReadU24LE(payload, 12);
        if (duration <= 0) duration = 1;

        var flags = payload[15];
        var disposeToBackground = (flags & 0x01) != 0;
        var blend = (flags & 0x02) == 0; // 0 = alpha blend, 1 = no blend

        var frameData = payload.Slice(16);
        if (!TryEnumerateChunks(frameData, out var chunks)) return false;

        if (TryFindChunk(frameData, chunks, FourCcVp8L, out var vp8lPayload)) {
            if (!WebpVp8lDecoder.TryDecode(vp8lPayload, out var rgba, out var decodedWidth, out var decodedHeight)) return false;
            if (decodedWidth != width || decodedHeight != height) return false;
            frame = new WebpAnimationFrameInfo(x, y, width, height, duration, blend, disposeToBackground, rgba);
            return true;
        }

        if (TryFindChunk(frameData, chunks, FourCcVp8, out var vp8Payload)) {
            if (!WebpVp8Decoder.TryDecode(vp8Payload, out var rgba, out var decodedWidth, out var decodedHeight)) return false;
            if (decodedWidth != width || decodedHeight != height) return false;

            if (TryFindChunk(frameData, chunks, FourCcAlph, out var alphPayload)) {
                if (!TryDecodeAlphaChunk(alphPayload, width, height, out var alpha, out _)) return false;
                ApplyAlphaToRgba(alpha, rgba);
            }

            frame = new WebpAnimationFrameInfo(x, y, width, height, duration, blend, disposeToBackground, rgba);
            return true;
        }

        return false;
    }

    private static void FillBackground(byte[] rgba, uint bgraColor) {
        var b = (byte)(bgraColor & 0xFF);
        var g = (byte)((bgraColor >> 8) & 0xFF);
        var r = (byte)((bgraColor >> 16) & 0xFF);
        var a = (byte)((bgraColor >> 24) & 0xFF);
        for (var i = 0; i < rgba.Length; i += 4) {
            rgba[i] = r;
            rgba[i + 1] = g;
            rgba[i + 2] = b;
            rgba[i + 3] = a;
        }
    }

    private static void ClearFrameToBackground(byte[] canvas, int canvasWidth, WebpAnimationFrameInfo frame, uint bgraColor) {
        var b = (byte)(bgraColor & 0xFF);
        var g = (byte)((bgraColor >> 8) & 0xFF);
        var r = (byte)((bgraColor >> 16) & 0xFF);
        var a = (byte)((bgraColor >> 24) & 0xFF);
        for (var y = 0; y < frame.Height; y++) {
            var dstRow = (frame.Y + y) * canvasWidth + frame.X;
            for (var x = 0; x < frame.Width; x++) {
                var dstIndex = (dstRow + x) * 4;
                canvas[dstIndex] = r;
                canvas[dstIndex + 1] = g;
                canvas[dstIndex + 2] = b;
                canvas[dstIndex + 3] = a;
            }
        }
    }

    private static void ReplaceFrame(byte[] canvas, int canvasWidth, int canvasHeight, WebpAnimationFrameInfo frame) {
        var src = frame.Rgba;
        var srcWidth = frame.Width;
        var srcHeight = frame.Height;
        for (var y = 0; y < srcHeight; y++) {
            var dstRow = (frame.Y + y) * canvasWidth + frame.X;
            var srcRow = y * srcWidth;
            for (var x = 0; x < srcWidth; x++) {
                var dstIndex = (dstRow + x) * 4;
                var srcIndex = (srcRow + x) * 4;
                canvas[dstIndex] = src[srcIndex];
                canvas[dstIndex + 1] = src[srcIndex + 1];
                canvas[dstIndex + 2] = src[srcIndex + 2];
                canvas[dstIndex + 3] = src[srcIndex + 3];
            }
        }
    }

    private static void AlphaBlendFrame(byte[] canvas, int canvasWidth, int canvasHeight, WebpAnimationFrameInfo frame) {
        var src = frame.Rgba;
        var srcWidth = frame.Width;
        var srcHeight = frame.Height;
        for (var y = 0; y < srcHeight; y++) {
            var dstRow = (frame.Y + y) * canvasWidth + frame.X;
            var srcRow = y * srcWidth;
            for (var x = 0; x < srcWidth; x++) {
                var dstIndex = (dstRow + x) * 4;
                var srcIndex = (srcRow + x) * 4;

                var srcA = src[srcIndex + 3];
                if (srcA == 0) continue;
                if (srcA == 255) {
                    canvas[dstIndex] = src[srcIndex];
                    canvas[dstIndex + 1] = src[srcIndex + 1];
                    canvas[dstIndex + 2] = src[srcIndex + 2];
                    canvas[dstIndex + 3] = 255;
                    continue;
                }

                var dstA = canvas[dstIndex + 3];
                var invSrcA = 255 - srcA;
                var outA = srcA + (dstA * invSrcA + 127) / 255;
                if (outA == 0) {
                    canvas[dstIndex] = 0;
                    canvas[dstIndex + 1] = 0;
                    canvas[dstIndex + 2] = 0;
                    canvas[dstIndex + 3] = 0;
                    continue;
                }

                var srcR = src[srcIndex];
                var srcG = src[srcIndex + 1];
                var srcB = src[srcIndex + 2];
                var dstR = canvas[dstIndex];
                var dstG = canvas[dstIndex + 1];
                var dstB = canvas[dstIndex + 2];

                var dstAContribution = (dstA * invSrcA + 127) / 255;
                var outR = (srcR * srcA + dstR * dstAContribution + (outA / 2)) / outA;
                var outG = (srcG * srcA + dstG * dstAContribution + (outA / 2)) / outA;
                var outB = (srcB * srcA + dstB * dstAContribution + (outA / 2)) / outA;

                canvas[dstIndex] = (byte)outR;
                canvas[dstIndex + 1] = (byte)outG;
                canvas[dstIndex + 2] = (byte)outB;
                canvas[dstIndex + 3] = (byte)outA;
            }
        }
    }

    private static void ApplyAlphaToRgba(byte[] alpha, byte[] rgba) {
        var pixelCount = rgba.Length / 4;
        if (alpha.Length != pixelCount) return;
        var alphaIndex = 0;
        for (var i = 0; i < rgba.Length; i += 4) {
            rgba[i + 3] = alpha[alphaIndex++];
        }
    }

    private static bool TryDecodeAlphaChunk(
        ReadOnlySpan<byte> payload,
        int width,
        int height,
        out byte[] alpha,
        out string reason) {
        alpha = Array.Empty<byte>();
        reason = string.Empty;
        if (width <= 0 || height <= 0) {
            reason = "Alpha dimensions are invalid.";
            return false;
        }
        if (payload.Length < 1) {
            reason = "Alpha chunk is too small.";
            return false;
        }

        var header = payload[0];
        var compression = header & 0x3;
        var filter = (header >> 2) & 0x3;
        var preprocessing = (header >> 4) & 0x3;

        if (compression == 0) {
            var required = checked(width * height);
            if (payload.Length - 1 < required) {
                reason = "Alpha chunk payload is too small.";
                return false;
            }
            alpha = payload.Slice(1, required).ToArray();
        } else if (compression == 1) {
            if (!WebpVp8lDecoder.TryDecodeImageStream(payload.Slice(1), width, height, out var argb)) {
                reason = "Failed to decode lossless alpha image-stream.";
                return false;
            }
            alpha = new byte[checked(width * height)];
            for (var i = 0; i < argb.Length; i++) {
                alpha[i] = (byte)((argb[i] >> 8) & 0xFF); // green channel holds alpha values
            }
        } else {
            reason = "Alpha compression method is not supported.";
            return false;
        }

        if (filter != 0) {
            ApplyAlphaFilter(alpha, width, height, filter);
        }

        _ = preprocessing; // preprocessing is optional; ignore for now

        return true;
    }

    private static void ApplyAlphaFilter(byte[] alpha, int width, int height, int filter) {
        if (filter == 0) return;
        var stride = width;
        for (var y = 0; y < height; y++) {
            var rowStart = y * stride;
            for (var x = 0; x < width; x++) {
                var index = rowStart + x;
                var left = x > 0 ? alpha[index - 1] : (byte)0;
                var up = y > 0 ? alpha[index - stride] : (byte)0;
                var upLeft = (x > 0 && y > 0) ? alpha[index - stride - 1] : (byte)0;

                var predictor = filter switch {
                    1 => left,
                    2 => up,
                    3 => (byte)Clamp(left + up - upLeft),
                    _ => 0
                };

                alpha[index] = (byte)(alpha[index] + predictor);
            }
        }
    }

    private static bool TryReadVp8X(ReadOnlySpan<byte> payload, out int width, out int height, out byte flags) {
        width = 0;
        height = 0;
        flags = 0;
        if (payload.Length < 10) return false;

        flags = payload[0];
        var widthMinus1 = ReadU24LE(payload, 4);
        var heightMinus1 = ReadU24LE(payload, 7);
        width = widthMinus1 + 1;
        height = heightMinus1 + 1;
        return width > 0 && height > 0;
    }

    private const uint FourCcVp8L = 0x4C385056; // "VP8L"
    private const uint FourCcVp8 = 0x20385056;  // "VP8 "
    private const uint FourCcVp8X = 0x58385056; // "VP8X"
    private const uint FourCcAlph = 0x48504C41; // "ALPH"
    private const uint FourCcAnim = 0x4D494E41; // "ANIM"
    private const uint FourCcAnmf = 0x464D4E41; // "ANMF"

    private static int ReadU24LE(ReadOnlySpan<byte> span, int offset) {
        if (offset < 0 || offset + 3 > span.Length) return 0;
        return span[offset]
            | (span[offset + 1] << 8)
            | (span[offset + 2] << 16);
    }

    private static int ReadU16LE(ReadOnlySpan<byte> span, int offset) {
        if (offset < 0 || offset + 2 > span.Length) return 0;
        return span[offset] | (span[offset + 1] << 8);
    }

    private static int Clamp(int value) {
        if (value < 0) return 0;
        if (value > 255) return 255;
        return value;
    }

    private static uint ReadU32LE(ReadOnlySpan<byte> span, int offset) {
        if (offset < 0 || offset + 4 > span.Length) return 0;
        return (uint)(span[offset]
            | (span[offset + 1] << 8)
            | (span[offset + 2] << 16)
            | (span[offset + 3] << 24));
    }
}
