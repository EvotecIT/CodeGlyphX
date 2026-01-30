using System;
using System.Collections.Generic;
using System.IO;

namespace CodeGlyphX.Rendering.Gif;

/// <summary>
/// Writes single-frame GIF images from RGBA buffers with palette optimization and dithering.
/// </summary>
public static class GifWriter {
    private const int MaxCodeSize = 12;
    private const int PaletteSize = 256;
    private const int MinDiffFrameArea = 16;

    /// <summary>
    /// Encodes an RGBA buffer into a GIF byte array.
    /// </summary>
    public static byte[] WriteRgba32(int width, int height, ReadOnlySpan<byte> rgba, int stride) {
        using var ms = new MemoryStream();
        WriteRgba32(ms, width, height, rgba, stride);
        return ms.ToArray();
    }

    /// <summary>
    /// Encodes multiple RGBA buffers into an animated GIF byte array.
    /// </summary>
    public static byte[] WriteAnimationRgba32(
        int canvasWidth,
        int canvasHeight,
        ReadOnlySpan<GifAnimationFrame> frames,
        GifAnimationOptions options) {
        using var ms = new MemoryStream();
        WriteAnimationRgba32(ms, canvasWidth, canvasHeight, frames, options);
        return ms.ToArray();
    }

    /// <summary>
    /// Encodes multiple RGBA buffers into an animated GIF byte array.
    /// </summary>
    public static byte[] WriteAnimationRgba32(
        int canvasWidth,
        int canvasHeight,
        GifAnimationFrame[] frames,
        GifAnimationOptions options) {
        if (frames is null) throw new ArgumentNullException(nameof(frames));
        return WriteAnimationRgba32(canvasWidth, canvasHeight, frames.AsSpan(), options);
    }

    /// <summary>
    /// Encodes an RGBA buffer into a GIF stream.
    /// </summary>
    public static void WriteRgba32(Stream stream, int width, int height, ReadOnlySpan<byte> rgba, int stride) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (width > ushort.MaxValue || height > ushort.MaxValue) throw new ArgumentOutOfRangeException(nameof(width), "GIF dimensions exceed 65535.");
        if (stride < width * 4) throw new ArgumentOutOfRangeException(nameof(stride));
        if (rgba.Length < stride * height) throw new ArgumentException("RGBA buffer is too small.", nameof(rgba));

        var frame = BuildIndexedFrame(rgba, width, height, stride);
        WriteHeader(stream, width, height, frame.Palette, frame.PaletteSize, frame.HasTransparency, frame.TransparentIndex);
        WriteImage(stream, width, height, frame.Pixels, frame.HasTransparency, frame.TransparentIndex, frame.MinCodeSize);
        stream.WriteByte(0x3B); // Trailer
    }

    /// <summary>
    /// Encodes multiple RGBA buffers into an animated GIF stream.
    /// </summary>
    public static void WriteAnimationRgba32(
        Stream stream,
        int canvasWidth,
        int canvasHeight,
        ReadOnlySpan<GifAnimationFrame> frames,
        GifAnimationOptions options) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (canvasWidth <= 0 || canvasHeight <= 0) throw new ArgumentOutOfRangeException(nameof(canvasWidth));
        if (canvasWidth > ushort.MaxValue || canvasHeight > ushort.MaxValue) {
            throw new ArgumentOutOfRangeException(nameof(canvasWidth), "GIF dimensions exceed 65535.");
        }
        if (frames.Length == 0) throw new ArgumentException("At least one frame is required.", nameof(frames));
        if (options.LoopCount < 0) throw new ArgumentOutOfRangeException(nameof(options.LoopCount));

        var indexedFrames = new GifIndexedFrame[frames.Length];
        for (var i = 0; i < frames.Length; i++) {
            var frame = frames[i];
            if (frame.Rgba is null) throw new ArgumentNullException(nameof(frame.Rgba));
            if (frame.Width <= 0 || frame.Height <= 0) throw new ArgumentOutOfRangeException(nameof(frame.Width));
            if (frame.Width > ushort.MaxValue || frame.Height > ushort.MaxValue) {
                throw new ArgumentOutOfRangeException(nameof(frame.Width), "GIF dimensions exceed 65535.");
            }
            if (frame.Stride < frame.Width * 4) throw new ArgumentOutOfRangeException(nameof(frame.Stride));
            if (frame.Rgba.Length < frame.Stride * frame.Height) throw new ArgumentException("RGBA buffer is too small.", nameof(frame.Rgba));
            if (frame.X < 0 || frame.Y < 0) throw new ArgumentOutOfRangeException(nameof(frame.X));
            if (frame.X + frame.Width > canvasWidth || frame.Y + frame.Height > canvasHeight) {
                throw new ArgumentOutOfRangeException(nameof(frame), "Frame exceeds canvas bounds.");
            }
            if (frame.DurationMs < 0) throw new ArgumentOutOfRangeException(nameof(frame.DurationMs));

            indexedFrames[i] = BuildIndexedFrame(frame.Rgba, frame.Width, frame.Height, frame.Stride);
        }

        var useGlobalPalette = TryBuildGlobalPalette(frames, options.BackgroundRgba, out var globalFrames, out var globalPalette, out var globalPaletteSize, out var globalMinCodeSize, out var backgroundIndex);
        if (useGlobalPalette) {
            indexedFrames = globalFrames;
            WriteHeader(stream, canvasWidth, canvasHeight, globalPalette, globalPaletteSize, (byte)backgroundIndex);
        } else {
            WriteHeader(stream, canvasWidth, canvasHeight, BuildBackgroundPalette(options.BackgroundRgba), paletteSize: 2, backgroundIndex: 0);
        }
        WriteLoopExtension(stream, options.LoopCount);

        var prevFrame = default(GifAnimationFrame);
        var prevIndex = default(GifIndexedFrame);
        var hasPrev = false;
        for (var i = 0; i < frames.Length; i++) {
            var frame = frames[i];
            var indexed = indexedFrames[i];
            WriteFrameWithDiff(
                stream,
                frame,
                indexed,
                useGlobalPalette,
                globalMinCodeSize,
                hasPrev,
                prevFrame,
                prevIndex,
                options.BackgroundRgba);
            prevFrame = frame;
            prevIndex = indexed;
            hasPrev = true;
        }

        stream.WriteByte(0x3B); // Trailer
    }

    private static void WriteHeader(Stream stream, int width, int height, byte[] palette, int paletteSize, bool hasTransparency, int transparentIndex) {
        WriteHeader(stream, width, height, palette, paletteSize, (byte)(hasTransparency ? transparentIndex : 0));
    }

    private static void WriteHeader(Stream stream, int width, int height, byte[] palette, int paletteSize, byte backgroundIndex) {
        WriteAscii(stream, "GIF89a");
        WriteUInt16(stream, (ushort)width);
        WriteUInt16(stream, (ushort)height);

        const int colorResolution = 7; // 8 bits per channel
        var gctSize = Log2(paletteSize) - 1; // 2^(gctSize+1)
        var packed = (byte)(0x80 | (colorResolution << 4) | gctSize);
        stream.WriteByte(packed);
        stream.WriteByte(backgroundIndex); // Background color index
        stream.WriteByte(0); // Pixel aspect ratio
        stream.Write(palette, 0, paletteSize * 3);
    }

    private static void WriteImage(Stream stream, int width, int height, byte[] pixels, bool hasTransparency, int transparentIndex, int minCodeSize) {
        if (hasTransparency) {
            stream.WriteByte(0x21); // Extension introducer
            stream.WriteByte(0xF9); // GCE label
            stream.WriteByte(4); // Block size
            stream.WriteByte(0x01); // Transparency flag
            WriteUInt16(stream, 0); // Delay
            stream.WriteByte((byte)transparentIndex);
            stream.WriteByte(0); // Block terminator
        }

        stream.WriteByte(0x2C); // Image descriptor
        WriteUInt16(stream, 0);
        WriteUInt16(stream, 0);
        WriteUInt16(stream, (ushort)width);
        WriteUInt16(stream, (ushort)height);
        stream.WriteByte(0); // No local color table

        stream.WriteByte((byte)minCodeSize);
        var lzwData = EncodeLzw(pixels, minCodeSize);
        WriteSubBlocks(stream, lzwData);
    }

    private static void WriteFrame(Stream stream, in GifAnimationFrame frame, in GifIndexedFrame indexed, bool useGlobalPalette, int globalMinCodeSize) {
        stream.WriteByte(0x21); // Extension introducer
        stream.WriteByte(0xF9); // GCE label
        stream.WriteByte(4); // Block size

        var packed = (byte)((((int)frame.DisposalMethod) & 0x7) << 2);
        if (indexed.HasTransparency) {
            packed |= 0x01;
        }
        stream.WriteByte(packed);

        var delay = (frame.DurationMs + 5) / 10;
        if (delay < 0) delay = 0;
        if (delay > ushort.MaxValue) delay = ushort.MaxValue;
        WriteUInt16(stream, (ushort)delay);
        stream.WriteByte((byte)(indexed.HasTransparency ? indexed.TransparentIndex : 0));
        stream.WriteByte(0); // Block terminator

        stream.WriteByte(0x2C); // Image descriptor
        WriteUInt16(stream, (ushort)frame.X);
        WriteUInt16(stream, (ushort)frame.Y);
        WriteUInt16(stream, (ushort)frame.Width);
        WriteUInt16(stream, (ushort)frame.Height);

        if (useGlobalPalette) {
            stream.WriteByte(0); // No local color table
        } else {
            var lctSize = Log2(indexed.PaletteSize) - 1;
            stream.WriteByte((byte)(0x80 | lctSize));
            stream.Write(indexed.Palette, 0, indexed.PaletteSize * 3);
        }

        var minCodeSize = useGlobalPalette ? globalMinCodeSize : indexed.MinCodeSize;
        stream.WriteByte((byte)minCodeSize);
        var lzwData = EncodeLzw(indexed.Pixels, minCodeSize);
        WriteSubBlocks(stream, lzwData);
    }

    private static void WriteFrameWithDiff(
        Stream stream,
        in GifAnimationFrame frame,
        in GifIndexedFrame indexed,
        bool useGlobalPalette,
        int globalMinCodeSize,
        bool hasPrev,
        in GifAnimationFrame prevFrame,
        in GifIndexedFrame prevIndexed,
        uint backgroundRgba) {
        if (!hasPrev ||
            frame.Width != prevFrame.Width ||
            frame.Height != prevFrame.Height ||
            frame.X != prevFrame.X ||
            frame.Y != prevFrame.Y) {
            WriteFrame(stream, frame, indexed, useGlobalPalette, globalMinCodeSize);
            return;
        }

        if (prevFrame.DisposalMethod == GifDisposalMethod.RestorePrevious) {
            WriteFrame(stream, frame, indexed, useGlobalPalette, globalMinCodeSize);
            return;
        }

        var diff = ComputeDiffRectRgba(
            frame.Rgba,
            frame.Stride,
            prevFrame.Rgba,
            prevFrame.Stride,
            frame.Width,
            frame.Height,
            prevFrame.DisposalMethod == GifDisposalMethod.RestoreBackground,
            backgroundRgba);
        if (diff.IsEmpty) {
            WriteFrame(stream, frame, indexed, useGlobalPalette, globalMinCodeSize);
            return;
        }

        if (diff.Width * diff.Height < MinDiffFrameArea) {
            WriteFrame(stream, frame, indexed, useGlobalPalette, globalMinCodeSize);
            return;
        }

        var subPixels = ExtractSubPixels(indexed.Pixels, frame.Width, diff);
        WriteFrameDiff(stream, frame, indexed, useGlobalPalette, globalMinCodeSize, diff, subPixels);
    }

    private static void WriteFrameDiff(
        Stream stream,
        in GifAnimationFrame frame,
        in GifIndexedFrame indexed,
        bool useGlobalPalette,
        int globalMinCodeSize,
        DiffRect rect,
        byte[] pixels) {
        stream.WriteByte(0x21); // Extension introducer
        stream.WriteByte(0xF9); // GCE label
        stream.WriteByte(4); // Block size

        var packed = (byte)((((int)frame.DisposalMethod) & 0x7) << 2);
        if (indexed.HasTransparency) {
            packed |= 0x01;
        }
        stream.WriteByte(packed);

        var delay = (frame.DurationMs + 5) / 10;
        if (delay < 0) delay = 0;
        if (delay > ushort.MaxValue) delay = ushort.MaxValue;
        WriteUInt16(stream, (ushort)delay);
        stream.WriteByte((byte)(indexed.HasTransparency ? indexed.TransparentIndex : 0));
        stream.WriteByte(0); // Block terminator

        stream.WriteByte(0x2C); // Image descriptor
        WriteUInt16(stream, (ushort)(frame.X + rect.X));
        WriteUInt16(stream, (ushort)(frame.Y + rect.Y));
        WriteUInt16(stream, (ushort)rect.Width);
        WriteUInt16(stream, (ushort)rect.Height);

        if (useGlobalPalette) {
            stream.WriteByte(0); // No local color table
        } else {
            var lctSize = Log2(indexed.PaletteSize) - 1;
            stream.WriteByte((byte)(0x80 | lctSize));
            stream.Write(indexed.Palette, 0, indexed.PaletteSize * 3);
        }

        var minCodeSize = useGlobalPalette ? globalMinCodeSize : indexed.MinCodeSize;
        stream.WriteByte((byte)minCodeSize);
        var lzwData = EncodeLzw(pixels, minCodeSize);
        WriteSubBlocks(stream, lzwData);
    }

    private static DiffRect ComputeDiffRectRgba(
        ReadOnlySpan<byte> current,
        int currentStride,
        ReadOnlySpan<byte> previous,
        int previousStride,
        int width,
        int height,
        bool previousIsBackground,
        uint backgroundRgba) {
        var minX = width;
        var minY = height;
        var maxX = -1;
        var maxY = -1;

        var bgR = (byte)((backgroundRgba >> 24) & 0xFF);
        var bgG = (byte)((backgroundRgba >> 16) & 0xFF);
        var bgB = (byte)((backgroundRgba >> 8) & 0xFF);

        for (var y = 0; y < height; y++) {
            var currRow = y * currentStride;
            var prevRow = y * previousStride;
            for (var x = 0; x < width; x++) {
                var currIdx = currRow + x * 4;
                var currA = current[currIdx + 3];
                if (currA < 128) {
                    continue;
                }

                var currR = current[currIdx];
                var currG = current[currIdx + 1];
                var currB = current[currIdx + 2];

                var changed = false;
                if (previousIsBackground) {
                    changed = currR != bgR || currG != bgG || currB != bgB;
                } else {
                    var prevIdx = prevRow + x * 4;
                    var prevA = previous[prevIdx + 3];
                    if (prevA < 128) {
                        changed = true;
                    } else {
                        changed = currR != previous[prevIdx] ||
                                  currG != previous[prevIdx + 1] ||
                                  currB != previous[prevIdx + 2];
                    }
                }

                if (changed) {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }

        if (maxX < minX || maxY < minY) {
            return default;
        }

        return new DiffRect(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    private static byte[] ExtractSubPixels(ReadOnlySpan<byte> pixels, int width, DiffRect rect) {
        var output = new byte[rect.Width * rect.Height];
        var dst = 0;
        for (var y = 0; y < rect.Height; y++) {
            var srcRow = (rect.Y + y) * width + rect.X;
            for (var x = 0; x < rect.Width; x++) {
                output[dst++] = pixels[srcRow + x];
            }
        }
        return output;
    }

    private static void WriteLoopExtension(Stream stream, int loopCount) {
        stream.WriteByte(0x21); // Extension introducer
        stream.WriteByte(0xFF); // Application extension
        stream.WriteByte(11); // Block size
        WriteAscii(stream, "NETSCAPE2.0");
        stream.WriteByte(3); // Sub-block size
        stream.WriteByte(1);
        WriteUInt16(stream, (ushort)loopCount);
        stream.WriteByte(0);
    }

    private static byte[] BuildBackgroundPalette(uint backgroundRgba) {
        var palette = new byte[6];
        palette[0] = (byte)((backgroundRgba >> 24) & 0xFF);
        palette[1] = (byte)((backgroundRgba >> 16) & 0xFF);
        palette[2] = (byte)((backgroundRgba >> 8) & 0xFF);
        return palette;
    }

    private static bool TryBuildGlobalPalette(
        ReadOnlySpan<GifAnimationFrame> frames,
        uint backgroundRgba,
        out GifIndexedFrame[] remappedFrames,
        out byte[] palette,
        out int paletteSize,
        out int minCodeSize,
        out int backgroundIndex) {
        if (TryBuildExactGlobalPalette(frames, backgroundRgba, out remappedFrames, out palette, out paletteSize, out minCodeSize, out backgroundIndex)) {
            return true;
        }

        return TryBuildQuantizedGlobalPalette(frames, backgroundRgba, out remappedFrames, out palette, out paletteSize, out minCodeSize, out backgroundIndex);
    }

    private static bool TryBuildExactGlobalPalette(
        ReadOnlySpan<GifAnimationFrame> frames,
        uint backgroundRgba,
        out GifIndexedFrame[] remappedFrames,
        out byte[] palette,
        out int paletteSize,
        out int minCodeSize,
        out int backgroundIndex) {
        remappedFrames = Array.Empty<GifIndexedFrame>();
        palette = Array.Empty<byte>();
        paletteSize = 0;
        minCodeSize = 0;
        backgroundIndex = 0;

        if (frames.Length == 0) return false;

        var colorMap = new Dictionary<int, int>(PaletteSize);
        var colors = new List<int>(PaletteSize);
        var hasTransparency = false;

        for (var i = 0; i < frames.Length; i++) {
            var frame = frames[i];
            var rgba = frame.Rgba;
            var width = frame.Width;
            var height = frame.Height;
            var stride = frame.Stride;
            for (var y = 0; y < height; y++) {
                var row = y * stride;
                for (var x = 0; x < width; x++) {
                    var idx = row + x * 4;
                    var a = rgba[idx + 3];
                    if (a < 128) {
                        if (!hasTransparency) {
                            hasTransparency = true;
                            if (colors.Count > PaletteSize - 1) return false;
                        }
                        continue;
                    }

                    var key = (rgba[idx + 0] << 16) | (rgba[idx + 1] << 8) | rgba[idx + 2];
                    if (!colorMap.ContainsKey(key)) {
                        var maxColors = hasTransparency ? PaletteSize - 1 : PaletteSize;
                        if (colors.Count >= maxColors) return false;
                        colorMap[key] = colors.Count;
                        colors.Add(key);
                    }
                }
            }
        }

        var backgroundKey = (int)(((backgroundRgba >> 24) & 0xFF) << 16 |
                                  ((backgroundRgba >> 16) & 0xFF) << 8 |
                                  ((backgroundRgba >> 8) & 0xFF));
        if (!colorMap.TryGetValue(backgroundKey, out backgroundIndex)) {
            var maxColors = hasTransparency ? PaletteSize - 1 : PaletteSize;
            if (colors.Count >= maxColors) return false;
            backgroundIndex = colors.Count;
            colorMap[backgroundKey] = backgroundIndex;
            colors.Add(backgroundKey);
        }

        var transparentIndex = -1;
        if (hasTransparency) {
            if (colors.Count > PaletteSize - 1) return false;
            transparentIndex = colors.Count;
        }

        var paletteCount = colors.Count + (hasTransparency ? 1 : 0);
        var paletteSizePower = NextPowerOfTwo(Math.Max(paletteCount, 2), PaletteSize);
        var localPalette = new byte[paletteSizePower * 3];
        for (var i = 0; i < colors.Count; i++) {
            var color = colors[i];
            localPalette[i * 3 + 0] = (byte)((color >> 16) & 0xFF);
            localPalette[i * 3 + 1] = (byte)((color >> 8) & 0xFF);
            localPalette[i * 3 + 2] = (byte)(color & 0xFF);
        }

        palette = localPalette;
        paletteSize = paletteSizePower;
        minCodeSize = Math.Max(2, Log2(paletteSizePower));

        remappedFrames = new GifIndexedFrame[frames.Length];
        for (var i = 0; i < frames.Length; i++) {
            var frame = frames[i];
            var rgba = frame.Rgba;
            var width = frame.Width;
            var height = frame.Height;
            var stride = frame.Stride;
            var pixels = new byte[width * height];
            var dst = 0;
            var frameHasTransparency = false;

            for (var y = 0; y < height; y++) {
                var row = y * stride;
                for (var x = 0; x < width; x++) {
                    var idx = row + x * 4;
                    var a = rgba[idx + 3];
                    if (a < 128) {
                        if (transparentIndex < 0) return false;
                        pixels[dst++] = (byte)transparentIndex;
                        frameHasTransparency = true;
                        continue;
                    }

                    var key = (rgba[idx + 0] << 16) | (rgba[idx + 1] << 8) | rgba[idx + 2];
                    if (!colorMap.TryGetValue(key, out var mapped)) return false;
                    pixels[dst++] = (byte)mapped;
                }
            }

            remappedFrames[i] = new GifIndexedFrame(
                pixels,
                localPalette,
                paletteSizePower,
                minCodeSize,
                frameHasTransparency,
                transparentIndex < 0 ? 0 : transparentIndex);
        }

        return true;
    }

    private static bool TryBuildQuantizedGlobalPalette(
        ReadOnlySpan<GifAnimationFrame> frames,
        uint backgroundRgba,
        out GifIndexedFrame[] remappedFrames,
        out byte[] palette,
        out int paletteSize,
        out int minCodeSize,
        out int backgroundIndex) {
        remappedFrames = Array.Empty<GifIndexedFrame>();
        palette = Array.Empty<byte>();
        paletteSize = 0;
        minCodeSize = 0;
        backgroundIndex = 0;

        if (frames.Length == 0) return false;

        var histogram = BuildHistogram(frames, out var hasTransparency);
        var maxColors = hasTransparency ? PaletteSize - 1 : PaletteSize;
        var paletteColors = BuildMedianCutPalette(histogram, maxColors, out var paletteCount);

        var paletteSizePower = NextPowerOfTwo(Math.Max(paletteCount + (hasTransparency ? 1 : 0), 2), PaletteSize);
        var localPalette = new byte[paletteSizePower * 3];
        Buffer.BlockCopy(paletteColors, 0, localPalette, 0, paletteCount * 3);

        var transparentIndex = hasTransparency ? paletteCount : -1;

        palette = localPalette;
        paletteSize = paletteSizePower;
        minCodeSize = Math.Max(2, Log2(paletteSizePower));

        backgroundIndex = FindNearestPaletteIndex(localPalette, paletteCount, backgroundRgba);

        var paletteMap = BuildPaletteMap(localPalette, paletteCount);
        remappedFrames = new GifIndexedFrame[frames.Length];
        for (var i = 0; i < frames.Length; i++) {
            var frame = frames[i];
            remappedFrames[i] = BuildIndexedFrameWithPaletteMap(
                frame.Rgba,
                frame.Width,
                frame.Height,
                frame.Stride,
                localPalette,
                paletteSizePower,
                minCodeSize,
                transparentIndex,
                paletteMap);
        }

        return true;
    }

    private static int FindNearestPaletteIndex(byte[] palette, int paletteCount, uint backgroundRgba) {
        var r = (byte)((backgroundRgba >> 24) & 0xFF);
        var g = (byte)((backgroundRgba >> 16) & 0xFF);
        var b = (byte)((backgroundRgba >> 8) & 0xFF);
        var best = 0;
        var bestDist = int.MaxValue;
        for (var i = 0; i < paletteCount; i++) {
            var baseIndex = i * 3;
            var dr = r - palette[baseIndex + 0];
            var dg = g - palette[baseIndex + 1];
            var db = b - palette[baseIndex + 2];
            var dist = dr * dr + dg * dg + db * db;
            if (dist < bestDist) {
                bestDist = dist;
                best = i;
                if (dist == 0) break;
            }
        }
        return best;
    }

    private static void WriteSubBlocks(Stream stream, byte[] data) {
        var offset = 0;
        while (offset < data.Length) {
            var count = Math.Min(255, data.Length - offset);
            stream.WriteByte((byte)count);
            stream.Write(data, offset, count);
            offset += count;
        }
        stream.WriteByte(0); // Terminator
    }

    private static byte[] EncodeLzw(ReadOnlySpan<byte> indices, int minCodeSize) {
        if (indices.Length == 0) return Array.Empty<byte>();

        var clearCode = 1 << minCodeSize;
        var endCode = clearCode + 1;
        var nextCode = endCode + 1;
        var codeSize = minCodeSize + 1;

        var dict = new Dictionary<int, int>(4096);
        var output = new List<byte>(indices.Length);
        var bitBuffer = 0;
        var bitCount = 0;

        void WriteCode(int code) {
            bitBuffer |= code << bitCount;
            bitCount += codeSize;
            while (bitCount >= 8) {
                output.Add((byte)(bitBuffer & 0xFF));
                bitBuffer >>= 8;
                bitCount -= 8;
            }
        }

        void ResetDictionary() {
            dict.Clear();
            codeSize = minCodeSize + 1;
            nextCode = endCode + 1;
            WriteCode(clearCode);
        }

        ResetDictionary();

        var prefix = indices[0];
        for (var i = 1; i < indices.Length; i++) {
            var c = indices[i];
            var key = (prefix << 8) | c;
            if (dict.TryGetValue(key, out var code)) {
                prefix = code;
                continue;
            }

            WriteCode(prefix);
            if (nextCode < (1 << MaxCodeSize)) {
                dict[key] = nextCode++;
                if (nextCode == (1 << codeSize) && codeSize < MaxCodeSize) {
                    codeSize++;
                }
            } else {
                ResetDictionary();
            }

            prefix = c;
        }

        WriteCode(prefix);
        WriteCode(endCode);
        if (bitCount > 0) {
            output.Add((byte)(bitBuffer & 0xFF));
        }
        return output.ToArray();
    }

    private static GifIndexedFrame BuildIndexedFrame(ReadOnlySpan<byte> rgba, int width, int height, int stride) {
        var hasTransparency = false;
        var colorCounts = new Dictionary<int, int>(256);
        var tooManyColors = false;
        for (var y = 0; y < height; y++) {
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                var idx = row + x * 4;
                var a = rgba[idx + 3];
                if (a < 128) {
                    hasTransparency = true;
                    continue;
                }
                if (tooManyColors) continue;
                var key = (rgba[idx + 0] << 16) | (rgba[idx + 1] << 8) | rgba[idx + 2];
                if (colorCounts.TryGetValue(key, out var count)) {
                    colorCounts[key] = count + 1;
                } else {
                    if (colorCounts.Count >= PaletteSize) {
                        tooManyColors = true;
                    } else {
                        colorCounts[key] = 1;
                    }
                }
            }
        }

        var useExactPalette = !tooManyColors && (!hasTransparency || colorCounts.Count <= PaletteSize - 1);
        Dictionary<int, byte>? colorIndex = null;
        int[]? paletteMap = null;
        var transparentIndex = 0;
        var paletteSizePower = PaletteSize;
        var paletteCount = 0;
        byte[] palette;
        var minCodeSize = 8;

        if (useExactPalette) {
            colorIndex = new Dictionary<int, byte>(colorCounts.Count);
            var paletteIndex = 0;
            var paletteEntries = colorCounts.Count + (hasTransparency ? 1 : 0);
            paletteSizePower = NextPowerOfTwo(Math.Max(paletteEntries, 2), PaletteSize);
            minCodeSize = Math.Max(2, Log2(paletteSizePower));
            palette = new byte[paletteSizePower * 3];
            foreach (var entry in colorCounts) {
                palette[paletteIndex * 3 + 0] = (byte)((entry.Key >> 16) & 0xFF);
                palette[paletteIndex * 3 + 1] = (byte)((entry.Key >> 8) & 0xFF);
                palette[paletteIndex * 3 + 2] = (byte)(entry.Key & 0xFF);
                colorIndex[entry.Key] = (byte)paletteIndex;
                paletteIndex++;
            }
            paletteCount = paletteIndex;
            if (hasTransparency) {
                transparentIndex = paletteIndex;
            }
        } else {
            var histogram = BuildHistogram(rgba, width, height, stride);
            palette = BuildMedianCutPalette(histogram, hasTransparency ? PaletteSize - 1 : PaletteSize, out paletteCount);
            paletteSizePower = NextPowerOfTwo(Math.Max(paletteCount + (hasTransparency ? 1 : 0), 2), PaletteSize);
            minCodeSize = Math.Max(2, Log2(paletteSizePower));
            if (palette.Length < paletteSizePower * 3) {
                var padded = new byte[paletteSizePower * 3];
                Buffer.BlockCopy(palette, 0, padded, 0, palette.Length);
                palette = padded;
            }
            if (hasTransparency) {
                transparentIndex = paletteCount;
            }
            paletteMap = BuildPaletteMap(palette, paletteCount);
        }

        var pixels = new byte[width * height];
        if (useExactPalette) {
            var dst = 0;
            for (var y = 0; y < height; y++) {
                var row = y * stride;
                for (var x = 0; x < width; x++) {
                    var idx = row + x * 4;
                    var a = rgba[idx + 3];
                    if (hasTransparency && a < 128) {
                        pixels[dst++] = (byte)transparentIndex;
                        continue;
                    }
                    var key = (rgba[idx + 0] << 16) | (rgba[idx + 1] << 8) | rgba[idx + 2];
                    pixels[dst++] = colorIndex![key];
                }
            }
        } else {
            var errR = new int[width + 1];
            var errG = new int[width + 1];
            var errB = new int[width + 1];
            var nextErrR = new int[width + 1];
            var nextErrG = new int[width + 1];
            var nextErrB = new int[width + 1];
            var dst = 0;

            for (var y = 0; y < height; y++) {
                Array.Clear(nextErrR, 0, nextErrR.Length);
                Array.Clear(nextErrG, 0, nextErrG.Length);
                Array.Clear(nextErrB, 0, nextErrB.Length);
                var row = y * stride;
                for (var x = 0; x < width; x++) {
                    var idx = row + x * 4;
                    var a = rgba[idx + 3];
                    if (hasTransparency && a < 128) {
                        pixels[dst++] = (byte)transparentIndex;
                        continue;
                    }

                    var r = ClampByte(rgba[idx + 0] + errR[x]);
                    var g = ClampByte(rgba[idx + 1] + errG[x]);
                    var b = ClampByte(rgba[idx + 2] + errB[x]);
                    var qIdx = ((r >> 3) << 10) | ((g >> 3) << 5) | (b >> 3);
                    var palIndex = paletteMap![qIdx];
                    pixels[dst++] = (byte)palIndex;

                    var baseIndex = palIndex * 3;
                    var pr = palette[baseIndex + 0];
                    var pg = palette[baseIndex + 1];
                    var pb = palette[baseIndex + 2];
                    var errRVal = r - pr;
                    var errGVal = g - pg;
                    var errBVal = b - pb;

                    if (x + 1 < width) {
                        errR[x + 1] += (errRVal * 7) / 16;
                        errG[x + 1] += (errGVal * 7) / 16;
                        errB[x + 1] += (errBVal * 7) / 16;
                    }
                    if (x > 0) {
                        nextErrR[x - 1] += (errRVal * 3) / 16;
                        nextErrG[x - 1] += (errGVal * 3) / 16;
                        nextErrB[x - 1] += (errBVal * 3) / 16;
                    }
                    nextErrR[x] += (errRVal * 5) / 16;
                    nextErrG[x] += (errGVal * 5) / 16;
                    nextErrB[x] += (errBVal * 5) / 16;
                    if (x + 1 < width) {
                        nextErrR[x + 1] += errRVal / 16;
                        nextErrG[x + 1] += errGVal / 16;
                        nextErrB[x + 1] += errBVal / 16;
                    }
                }

                var swapR = errR;
                var swapG = errG;
                var swapB = errB;
                errR = nextErrR;
                errG = nextErrG;
                errB = nextErrB;
                nextErrR = swapR;
                nextErrG = swapG;
                nextErrB = swapB;
            }
        }

        return new GifIndexedFrame(pixels, palette, paletteSizePower, minCodeSize, hasTransparency, transparentIndex);
    }

    private static GifIndexedFrame BuildIndexedFrameWithPaletteMap(
        ReadOnlySpan<byte> rgba,
        int width,
        int height,
        int stride,
        byte[] palette,
        int paletteSize,
        int minCodeSize,
        int transparentIndex,
        int[] paletteMap) {
        var pixels = new byte[width * height];
        var errR = new int[width + 1];
        var errG = new int[width + 1];
        var errB = new int[width + 1];
        var nextErrR = new int[width + 1];
        var nextErrG = new int[width + 1];
        var nextErrB = new int[width + 1];
        var dst = 0;
        var hasTransparency = false;

        for (var y = 0; y < height; y++) {
            Array.Clear(nextErrR, 0, nextErrR.Length);
            Array.Clear(nextErrG, 0, nextErrG.Length);
            Array.Clear(nextErrB, 0, nextErrB.Length);
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                var idx = row + x * 4;
                var a = rgba[idx + 3];
                if (a < 128) {
                    pixels[dst++] = (byte)(transparentIndex < 0 ? 0 : transparentIndex);
                    hasTransparency = true;
                    continue;
                }

                var r = ClampByte(rgba[idx + 0] + errR[x]);
                var g = ClampByte(rgba[idx + 1] + errG[x]);
                var b = ClampByte(rgba[idx + 2] + errB[x]);
                var qIdx = ((r >> 3) << 10) | ((g >> 3) << 5) | (b >> 3);
                var palIndex = paletteMap[qIdx];
                pixels[dst++] = (byte)palIndex;

                var baseIndex = palIndex * 3;
                var pr = palette[baseIndex + 0];
                var pg = palette[baseIndex + 1];
                var pb = palette[baseIndex + 2];
                var errRVal = r - pr;
                var errGVal = g - pg;
                var errBVal = b - pb;

                if (x + 1 < width) {
                    errR[x + 1] += (errRVal * 7) / 16;
                    errG[x + 1] += (errGVal * 7) / 16;
                    errB[x + 1] += (errBVal * 7) / 16;
                }
                if (x > 0) {
                    nextErrR[x - 1] += (errRVal * 3) / 16;
                    nextErrG[x - 1] += (errGVal * 3) / 16;
                    nextErrB[x - 1] += (errBVal * 3) / 16;
                }
                nextErrR[x] += (errRVal * 5) / 16;
                nextErrG[x] += (errGVal * 5) / 16;
                nextErrB[x] += (errBVal * 5) / 16;
                if (x + 1 < width) {
                    nextErrR[x + 1] += errRVal / 16;
                    nextErrG[x + 1] += errGVal / 16;
                    nextErrB[x + 1] += errBVal / 16;
                }
            }

            var swapR = errR;
            var swapG = errG;
            var swapB = errB;
            errR = nextErrR;
            errG = nextErrG;
            errB = nextErrB;
            nextErrR = swapR;
            nextErrG = swapG;
            nextErrB = swapB;
        }

        return new GifIndexedFrame(
            pixels,
            palette,
            paletteSize,
            minCodeSize,
            hasTransparency,
            transparentIndex < 0 ? 0 : transparentIndex);
    }

    private static int[] BuildHistogram(ReadOnlySpan<byte> rgba, int width, int height, int stride) {
        var histogram = new int[32 * 32 * 32];
        for (var y = 0; y < height; y++) {
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                var idx = row + x * 4;
                if (rgba[idx + 3] < 128) continue;
                var r5 = rgba[idx + 0] >> 3;
                var g5 = rgba[idx + 1] >> 3;
                var b5 = rgba[idx + 2] >> 3;
                var colorIndex = (r5 << 10) | (g5 << 5) | b5;
                histogram[colorIndex]++;
            }
        }
        return histogram;
    }

    private static int[] BuildHistogram(ReadOnlySpan<GifAnimationFrame> frames, out bool hasTransparency) {
        hasTransparency = false;
        var histogram = new int[32 * 32 * 32];
        for (var i = 0; i < frames.Length; i++) {
            var frame = frames[i];
            var rgba = frame.Rgba;
            var width = frame.Width;
            var height = frame.Height;
            var stride = frame.Stride;
            for (var y = 0; y < height; y++) {
                var row = y * stride;
                for (var x = 0; x < width; x++) {
                    var idx = row + x * 4;
                    if (rgba[idx + 3] < 128) {
                        hasTransparency = true;
                        continue;
                    }
                    var r5 = rgba[idx + 0] >> 3;
                    var g5 = rgba[idx + 1] >> 3;
                    var b5 = rgba[idx + 2] >> 3;
                    var colorIndex = (r5 << 10) | (g5 << 5) | b5;
                    histogram[colorIndex]++;
                }
            }
        }
        return histogram;
    }

    private static byte[] BuildMedianCutPalette(int[] histogram, int maxColors, out int paletteCount) {
        var colors = new List<int>();
        for (var i = 0; i < histogram.Length; i++) {
            if (histogram[i] > 0) {
                colors.Add(i);
            }
        }

        if (colors.Count == 0) {
            paletteCount = 1;
            return new byte[] { 0, 0, 0 };
        }

        var buckets = new List<ColorBucket> { ColorBucket.Create(colors.ToArray(), histogram) };
        while (buckets.Count < maxColors) {
            var splitIndex = -1;
            var bestRange = -1;
            for (var i = 0; i < buckets.Count; i++) {
                var bucket = buckets[i];
                if (bucket.Count < 2) continue;
                var range = bucket.MaxRange;
                if (range > bestRange) {
                    bestRange = range;
                    splitIndex = i;
                }
            }

            if (splitIndex < 0) break;
            var target = buckets[splitIndex];
            var channel = target.LongestChannel;
            Array.Sort(target.Colors, 0, target.Count, new ChannelComparer(channel));

            var total = target.TotalCount;
            var cumulative = 0;
            var cut = 0;
            while (cut < target.Count - 1 && cumulative < total / 2) {
                cumulative += histogram[target.Colors[cut]];
                cut++;
            }

            if (cut <= 0 || cut >= target.Count) break;

            var leftColors = new int[cut];
            var rightColors = new int[target.Count - cut];
            Array.Copy(target.Colors, 0, leftColors, 0, cut);
            Array.Copy(target.Colors, cut, rightColors, 0, rightColors.Length);

            buckets[splitIndex] = ColorBucket.Create(leftColors, histogram);
            buckets.Add(ColorBucket.Create(rightColors, histogram));
        }

        paletteCount = buckets.Count;
        var palette = new byte[paletteCount * 3];
        for (var i = 0; i < buckets.Count; i++) {
            var bucket = buckets[i];
            var total = bucket.TotalCount;
            if (total <= 0) total = 1;
            long sumR = 0;
            long sumG = 0;
            long sumB = 0;
            for (var j = 0; j < bucket.Count; j++) {
                var idx = bucket.Colors[j];
                var count = histogram[idx];
                var r5 = (idx >> 10) & 31;
                var g5 = (idx >> 5) & 31;
                var b5 = idx & 31;
                sumR += r5 * (long)count;
                sumG += g5 * (long)count;
                sumB += b5 * (long)count;
            }

            var avgR5 = (int)((sumR + total / 2) / total);
            var avgG5 = (int)((sumG + total / 2) / total);
            var avgB5 = (int)((sumB + total / 2) / total);
            palette[i * 3 + 0] = (byte)((avgR5 * 255 + 15) / 31);
            palette[i * 3 + 1] = (byte)((avgG5 * 255 + 15) / 31);
            palette[i * 3 + 2] = (byte)((avgB5 * 255 + 15) / 31);
        }

        return palette;
    }

    private static int[] BuildPaletteMap(byte[] palette, int paletteCount) {
        var map = new int[32 * 32 * 32];
        for (var idx = 0; idx < map.Length; idx++) {
            var r5 = (idx >> 10) & 31;
            var g5 = (idx >> 5) & 31;
            var b5 = idx & 31;
            var r8 = r5 * 255 / 31;
            var g8 = g5 * 255 / 31;
            var b8 = b5 * 255 / 31;

            var best = 0;
            var bestDist = int.MaxValue;
            for (var p = 0; p < paletteCount; p++) {
                var baseIndex = p * 3;
                var dr = r8 - palette[baseIndex + 0];
                var dg = g8 - palette[baseIndex + 1];
                var db = b8 - palette[baseIndex + 2];
                var dist = dr * dr + dg * dg + db * db;
                if (dist < bestDist) {
                    bestDist = dist;
                    best = p;
                    if (dist == 0) break;
                }
            }
            map[idx] = best;
        }
        return map;
    }

    private static int NextPowerOfTwo(int value, int max) {
        var result = 1;
        while (result < value && result < max) {
            result <<= 1;
        }
        return result > max ? max : result;
    }

    private static int Log2(int value) {
        var log = 0;
        while ((1 << log) < value) {
            log++;
        }
        return log;
    }

    private static byte ClampByte(int value) {
        if (value < 0) return 0;
        if (value > 255) return 255;
        return (byte)value;
    }

    private readonly struct GifIndexedFrame {
        public GifIndexedFrame(byte[] pixels, byte[] palette, int paletteSize, int minCodeSize, bool hasTransparency, int transparentIndex) {
            Pixels = pixels;
            Palette = palette;
            PaletteSize = paletteSize;
            MinCodeSize = minCodeSize;
            HasTransparency = hasTransparency;
            TransparentIndex = transparentIndex;
        }

        public byte[] Pixels { get; }
        public byte[] Palette { get; }
        public int PaletteSize { get; }
        public int MinCodeSize { get; }
        public bool HasTransparency { get; }
        public int TransparentIndex { get; }
    }

    private readonly struct ColorBucket {
        public ColorBucket(int[] colors, int count, int totalCount, byte minR, byte maxR, byte minG, byte maxG, byte minB, byte maxB) {
            Colors = colors;
            Count = count;
            TotalCount = totalCount;
            MinR = minR;
            MaxR = maxR;
            MinG = minG;
            MaxG = maxG;
            MinB = minB;
            MaxB = maxB;
        }

        public int[] Colors { get; }
        public int Count { get; }
        public int TotalCount { get; }
        public byte MinR { get; }
        public byte MaxR { get; }
        public byte MinG { get; }
        public byte MaxG { get; }
        public byte MinB { get; }
        public byte MaxB { get; }

        public int RangeR => MaxR - MinR;
        public int RangeG => MaxG - MinG;
        public int RangeB => MaxB - MinB;
        public int MaxRange => Math.Max(RangeR, Math.Max(RangeG, RangeB));

        public int LongestChannel {
            get {
                var r = RangeR;
                var g = RangeG;
                var b = RangeB;
                if (r >= g && r >= b) return 0;
                return g >= b ? 1 : 2;
            }
        }

        public static ColorBucket Create(int[] colors, int[] histogram) {
            var minR = (byte)31;
            var minG = (byte)31;
            var minB = (byte)31;
            var maxR = (byte)0;
            var maxG = (byte)0;
            var maxB = (byte)0;
            var total = 0;
            for (var i = 0; i < colors.Length; i++) {
                var idx = colors[i];
                var count = histogram[idx];
                total += count;
                var r5 = (byte)((idx >> 10) & 31);
                var g5 = (byte)((idx >> 5) & 31);
                var b5 = (byte)(idx & 31);
                if (r5 < minR) minR = r5;
                if (r5 > maxR) maxR = r5;
                if (g5 < minG) minG = g5;
                if (g5 > maxG) maxG = g5;
                if (b5 < minB) minB = b5;
                if (b5 > maxB) maxB = b5;
            }
            return new ColorBucket(colors, colors.Length, total, minR, maxR, minG, maxG, minB, maxB);
        }
    }

    private sealed class ChannelComparer : IComparer<int> {
        private readonly int _channel;

        public ChannelComparer(int channel) {
            _channel = channel;
        }

        public int Compare(int x, int y) {
            var xr = (x >> 10) & 31;
            var xg = (x >> 5) & 31;
            var xb = x & 31;
            var yr = (y >> 10) & 31;
            var yg = (y >> 5) & 31;
            var yb = y & 31;
            return _channel switch {
                0 => xr.CompareTo(yr),
                1 => xg.CompareTo(yg),
                _ => xb.CompareTo(yb),
            };
        }
    }

    private readonly struct DiffRect {
        public DiffRect(int x, int y, int width, int height) {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }
        public bool IsEmpty => Width <= 0 || Height <= 0;
    }

    private static void WriteAscii(Stream stream, string text) {
        for (var i = 0; i < text.Length; i++) {
            stream.WriteByte((byte)text[i]);
        }
    }

    private static void WriteUInt16(Stream stream, ushort value) {
        stream.WriteByte((byte)(value & 0xFF));
        stream.WriteByte((byte)((value >> 8) & 0xFF));
    }
}
