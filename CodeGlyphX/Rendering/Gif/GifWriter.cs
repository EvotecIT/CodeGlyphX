using System;
using System.Collections.Generic;
using System.IO;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Rendering.Gif;

/// <summary>
/// Writes GIF images from RGBA buffers.
/// </summary>
public static class GifWriter {
    /// <summary>
    /// Writes a GIF byte array from an RGBA buffer (single frame).
    /// </summary>
    public static byte[] WriteRgba32(int width, int height, ReadOnlySpan<byte> rgba, int stride) {
        using var ms = new MemoryStream();
        WriteRgba32(ms, width, height, rgba, stride);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a GIF to a stream from an RGBA buffer (single frame).
    /// </summary>
    public static void WriteRgba32(Stream stream, int width, int height, ReadOnlySpan<byte> rgba, int stride) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        _ = RenderGuards.EnsureOutputPixels(width, height, "GIF output exceeds size limits.");
        if (stride < width * 4) throw new ArgumentOutOfRangeException(nameof(stride));
        if (rgba.Length < (height - 1) * stride + width * 4) throw new ArgumentException("RGBA buffer is too small.", nameof(rgba));

        var indexBytes = RenderGuards.EnsureOutputBytes((long)width * height, "GIF output exceeds size limits.");
        var indices = new byte[indexBytes];
        BuildPalette(rgba, width, height, stride, indices, out var palette, out var hasTransparency, out var transparentIndex);

        var colorTableSize = 1;
        var colorTableBits = 0;
        while (colorTableSize < palette.Count) {
            colorTableSize <<= 1;
            colorTableBits++;
        }
        if (colorTableSize < 2) {
            colorTableSize = 2;
            colorTableBits = 1;
        }

        var gctSizeField = colorTableBits - 1;
        var colorResolution = colorTableBits - 1;
        if (colorResolution < 0) colorResolution = 0;
        if (colorResolution > 7) colorResolution = 7;
        var packed = (byte)(0x80 | (colorResolution << 4) | gctSizeField);

        stream.Write(GifHeader, 0, GifHeader.Length);
        WriteUInt16(stream, (ushort)width);
        WriteUInt16(stream, (ushort)height);
        stream.WriteByte(packed);
        stream.WriteByte(0); // background color index
        stream.WriteByte(0); // pixel aspect ratio

        WriteGlobalColorTable(stream, palette, colorTableSize);

        if (hasTransparency) {
            stream.WriteByte(0x21);
            stream.WriteByte(0xF9);
            stream.WriteByte(0x04);
            stream.WriteByte(0x01); // transparency flag
            WriteUInt16(stream, 0); // delay
            stream.WriteByte((byte)transparentIndex);
            stream.WriteByte(0);
        }

        stream.WriteByte(0x2C);
        WriteUInt16(stream, 0);
        WriteUInt16(stream, 0);
        WriteUInt16(stream, (ushort)width);
        WriteUInt16(stream, (ushort)height);
        stream.WriteByte(0x00); // no local color table, not interlaced

        var minCodeSize = Math.Max(2, colorTableBits);
        stream.WriteByte((byte)minCodeSize);
        var encoded = LzwEncode(indices, minCodeSize);
        WriteSubBlocks(stream, encoded);

        stream.WriteByte(0x3B);
    }

    /// <summary>
    /// Writes a GIF animation byte array from RGBA frame buffers.
    /// </summary>
    public static byte[] WriteAnimation(int canvasWidth, int canvasHeight, GifAnimationFrame[] frames, GifAnimationOptions options = default) {
        using var ms = new MemoryStream();
        WriteAnimation(ms, canvasWidth, canvasHeight, frames, options);
        return ms.ToArray();
    }

    /// <summary>
    /// Writes a GIF animation byte array from RGBA frame buffers (compatibility alias).
    /// </summary>
    public static byte[] WriteAnimationRgba32(int canvasWidth, int canvasHeight, GifAnimationFrame[] frames, GifAnimationOptions options = default) {
        return WriteAnimation(canvasWidth, canvasHeight, frames, options);
    }

    /// <summary>
    /// Writes a GIF animation to a stream from RGBA frame buffers.
    /// </summary>
    public static void WriteAnimation(Stream stream, int canvasWidth, int canvasHeight, GifAnimationFrame[] frames, GifAnimationOptions options = default) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (frames is null) throw new ArgumentNullException(nameof(frames));
        if (frames.Length == 0) throw new ArgumentException("At least one frame is required.", nameof(frames));
        if (canvasWidth <= 0) throw new ArgumentOutOfRangeException(nameof(canvasWidth));
        if (canvasHeight <= 0) throw new ArgumentOutOfRangeException(nameof(canvasHeight));
        _ = RenderGuards.EnsureOutputPixels(canvasWidth, canvasHeight, "GIF output exceeds size limits.");
        _ = RenderGuards.EnsureOutputBytes((long)canvasWidth * canvasHeight * 4, "GIF output exceeds size limits.");

        for (var i = 0; i < frames.Length; i++) {
            ValidateFrame(frames[i], canvasWidth, canvasHeight);
        }

        var backgroundRgba = options.BackgroundRgba;
        var bgR = (byte)(backgroundRgba & 0xFF);
        var bgG = (byte)((backgroundRgba >> 8) & 0xFF);
        var bgB = (byte)((backgroundRgba >> 16) & 0xFF);
        var bgA = (byte)((backgroundRgba >> 24) & 0xFF);

        var optimizedFrames = OptimizeFramesForEncoding(frames, canvasWidth, canvasHeight, bgR, bgG, bgB, bgA);
        var anyTransparency = FramesHaveTransparency(optimizedFrames);
        var useGlobal = TryBuildGlobalPalette(
            optimizedFrames,
            bgR,
            bgG,
            bgB,
            bgA,
            anyTransparency,
            out var globalPalette,
            out var globalMap,
            out var globalTransparentIndex,
            out var backgroundIndex,
            out var globalQuantized,
            out var globalRLevels,
            out var globalGLevels,
            out var globalBLevels);

        if (!useGlobal) {
            globalPalette = BuildBackgroundPalette(bgR, bgG, bgB);
            backgroundIndex = 0;
            globalTransparentIndex = -1;
            globalMap = null;
        }

        var gctSize = GetTableSize(globalPalette.Count);
        var gctBits = GetTableBits(gctSize);
        var packed = (byte)(0x80 | (ClampColorResolution(gctBits - 1) << 4) | (gctBits - 1));

        stream.Write(GifHeader, 0, GifHeader.Length);
        WriteUInt16(stream, (ushort)canvasWidth);
        WriteUInt16(stream, (ushort)canvasHeight);
        stream.WriteByte(packed);
        stream.WriteByte((byte)backgroundIndex);
        stream.WriteByte(0);

        WriteGlobalColorTable(stream, globalPalette, gctSize);

        if (optimizedFrames.Length > 1) {
            WriteLoopExtension(stream, options.LoopCount);
        }

        for (var i = 0; i < optimizedFrames.Length; i++) {
            var frame = optimizedFrames[i];
            var frameHasTransparency = FrameHasTransparency(frame);
            var paletteInfo = useGlobal
                ? new PaletteInfo(globalPalette, globalMap, gctSize, gctBits, anyTransparency, globalTransparentIndex, globalQuantized, globalRLevels, globalGLevels, globalBLevels)
                : BuildFramePalette(frame);

            var hasTransparency = useGlobal ? frameHasTransparency : paletteInfo.HasTransparency;
            var transparentIndex = paletteInfo.TransparentIndex;
            var disposal = ClampDisposal(frame.DisposalMethod);
            var delay = frame.DurationMs < 0 ? 0 : (frame.DurationMs + 5) / 10;
            if (delay > ushort.MaxValue) delay = ushort.MaxValue;

            stream.WriteByte(0x21);
            stream.WriteByte(0xF9);
            stream.WriteByte(0x04);
            var packedGce = (byte)(((int)disposal & 0x07) << 2);
            if (hasTransparency) packedGce |= 0x01;
            stream.WriteByte(packedGce);
            WriteUInt16(stream, (ushort)delay);
            stream.WriteByte((byte)(hasTransparency ? transparentIndex : 0));
            stream.WriteByte(0);

            stream.WriteByte(0x2C);
            WriteUInt16(stream, (ushort)frame.X);
            WriteUInt16(stream, (ushort)frame.Y);
            WriteUInt16(stream, (ushort)frame.Width);
            WriteUInt16(stream, (ushort)frame.Height);

            if (!useGlobal) {
                var lctPacked = (byte)(0x80 | (paletteInfo.TableBits - 1));
                stream.WriteByte(lctPacked);
                WriteGlobalColorTable(stream, paletteInfo.Colors, paletteInfo.TableSize);
            } else {
                stream.WriteByte(0x00);
            }

            var minCodeSize = Math.Max(2, paletteInfo.TableBits);
            stream.WriteByte((byte)minCodeSize);

            var indices = paletteInfo.IsQuantized
                ? BuildFrameIndicesQuantized(frame, paletteInfo)
                : BuildFrameIndices(frame, paletteInfo, useGlobal);
            var encoded = LzwEncode(indices, minCodeSize);
            WriteSubBlocks(stream, encoded);
        }

        stream.WriteByte(0x3B);
    }

    private static void BuildPalette(
        ReadOnlySpan<byte> rgba,
        int width,
        int height,
        int stride,
        byte[] indices,
        out List<int> palette,
        out bool hasTransparency,
        out int transparentIndex) {
        hasTransparency = false;
        transparentIndex = -1;
        palette = new List<int>(256);
        var lookup = new Dictionary<int, int>(256);
        var nextIndex = 0;
        var overflow = false;

        for (var y = 0; y < height && !overflow; y++) {
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                var p = row + x * 4;
                var r = rgba[p + 0];
                var g = rgba[p + 1];
                var b = rgba[p + 2];
                var a = rgba[p + 3];
                if (a < 128) {
                    if (!hasTransparency) {
                        hasTransparency = true;
                        transparentIndex = 0;
                        palette.Add(0);
                        nextIndex = 1;
                    }
                    continue;
                }

                var key = (r << 16) | (g << 8) | b;
                if (!lookup.TryGetValue(key, out _)) {
                    if (palette.Count >= 256) {
                        overflow = true;
                        break;
                    }
                    lookup[key] = nextIndex++;
                    palette.Add(key);
                }
            }
        }

        if (!overflow) {
            FillIndicesExact(rgba, width, height, stride, indices, lookup, hasTransparency, transparentIndex);
            if (palette.Count == 0) {
                palette.Add(0);
            }
            return;
        }

        hasTransparency = HasTransparency(rgba, width, height, stride);
        BuildUniformPalette(hasTransparency, out palette, out var rLevels, out var gLevels, out var bLevels, out transparentIndex);
        FillIndicesQuantizedDithered(rgba, width, height, stride, indices, hasTransparency, rLevels, gLevels, bLevels, transparentIndex);
    }

    private static void ValidateFrame(GifAnimationFrame frame, int canvasWidth, int canvasHeight) {
        if (frame.Width <= 0) throw new ArgumentOutOfRangeException(nameof(frame.Width));
        if (frame.Height <= 0) throw new ArgumentOutOfRangeException(nameof(frame.Height));
        _ = RenderGuards.EnsureOutputPixels(frame.Width, frame.Height, "GIF output exceeds size limits.");
        if (frame.Stride < frame.Width * 4) throw new ArgumentOutOfRangeException(nameof(frame.Stride));
        if (frame.Rgba.Length < (frame.Height - 1) * frame.Stride + frame.Width * 4) {
            throw new ArgumentException("RGBA buffer is too small.", nameof(frame.Rgba));
        }
        if (frame.X < 0 || frame.Y < 0) throw new ArgumentOutOfRangeException(nameof(frame.X));
        if (frame.X + frame.Width > canvasWidth || frame.Y + frame.Height > canvasHeight) {
            throw new ArgumentOutOfRangeException(nameof(frame.Width), "Frame exceeds canvas bounds.");
        }
    }

    private static GifAnimationFrame[] OptimizeFramesForEncoding(
        GifAnimationFrame[] frames,
        int canvasWidth,
        int canvasHeight,
        byte bgR,
        byte bgG,
        byte bgB,
        byte bgA) {
        var canvasBytes = RenderGuards.EnsureOutputBytes((long)canvasWidth * canvasHeight * 4, "GIF output exceeds size limits.");
        var canvas = new byte[canvasBytes];
        FillCanvas(canvas, bgR, bgG, bgB, bgA);
        var optimized = new GifAnimationFrame[frames.Length];

        for (var i = 0; i < frames.Length; i++) {
            var frame = frames[i];
            var disposal = ClampDisposal(frame.DisposalMethod);
            var allowCrop = disposal == GifDisposalMethod.None || disposal == GifDisposalMethod.DoNotDispose || disposal == GifDisposalMethod.RestoreBackground || disposal == GifDisposalMethod.RestorePrevious;

            GifAnimationFrame output;
            if (allowCrop && TryComputeDiffBounds(canvas, canvasWidth, canvasHeight, frame, disposal == GifDisposalMethod.RestoreBackground, bgR, bgG, bgB, bgA, out var minX, out var minY, out var maxX, out var maxY)) {
                output = CropFrame(frame, minX, minY, maxX, maxY);
            } else if (allowCrop) {
                output = disposal == GifDisposalMethod.RestoreBackground
                    ? frame
                    : new GifAnimationFrame(new byte[] { 0, 0, 0, 0 }, 1, 1, 4, frame.DurationMs, 0, 0, frame.DisposalMethod);
            } else {
                output = frame;
            }

            if (disposal != GifDisposalMethod.RestorePrevious) {
                ApplyFrameToCanvas(canvas, canvasWidth, canvasHeight, output);
                if (disposal == GifDisposalMethod.RestoreBackground) {
                    ClearRect(canvas, canvasWidth, canvasHeight, output.X, output.Y, output.Width, output.Height, bgR, bgG, bgB, bgA);
                }
            }

            optimized[i] = output;
        }

        return optimized;
    }

    private static bool TryComputeDiffBounds(
        byte[] canvas,
        int canvasWidth,
        int canvasHeight,
        GifAnimationFrame frame,
        bool considerBackgroundRestore,
        byte bgR,
        byte bgG,
        byte bgB,
        byte bgA,
        out int minX,
        out int minY,
        out int maxX,
        out int maxY) {
        minX = frame.Width;
        minY = frame.Height;
        maxX = -1;
        maxY = -1;

        for (var y = 0; y < frame.Height; y++) {
            var row = y * frame.Stride;
            var dstY = frame.Y + y;
            if ((uint)dstY >= (uint)canvasHeight) continue;
            var canvasRow = dstY * canvasWidth * 4;
            for (var x = 0; x < frame.Width; x++) {
                var p = row + x * 4;
                var a = frame.Rgba[p + 3];
                if (a < 128) continue;
                var dstX = frame.X + x;
                if ((uint)dstX >= (uint)canvasWidth) continue;
                var dst = canvasRow + dstX * 4;
                if (frame.Rgba[p + 0] == canvas[dst + 0]
                    && frame.Rgba[p + 1] == canvas[dst + 1]
                    && frame.Rgba[p + 2] == canvas[dst + 2]
                    && a == canvas[dst + 3]) {
                    continue;
                }
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }
        }

        if (!considerBackgroundRestore) {
            return maxX >= minX && maxY >= minY;
        }

        var rectMinX = maxX >= minX && maxY >= minY ? minX : frame.Width;
        var rectMinY = maxX >= minX && maxY >= minY ? minY : frame.Height;
        var rectMaxX = maxX;
        var rectMaxY = maxY;

        for (var y = 0; y < frame.Height; y++) {
            var row = y * frame.Stride;
            var dstY = frame.Y + y;
            if ((uint)dstY >= (uint)canvasHeight) continue;
            var canvasRow = dstY * canvasWidth * 4;
            for (var x = 0; x < frame.Width; x++) {
                var p = row + x * 4;
                var a = frame.Rgba[p + 3];
                if (a >= 128) continue;
                var dstX = frame.X + x;
                if ((uint)dstX >= (uint)canvasWidth) continue;
                var dst = canvasRow + dstX * 4;
                if (canvas[dst + 0] == bgR && canvas[dst + 1] == bgG && canvas[dst + 2] == bgB && canvas[dst + 3] == bgA) {
                    continue;
                }
                if (x < rectMinX) rectMinX = x;
                if (y < rectMinY) rectMinY = y;
                if (x > rectMaxX) rectMaxX = x;
                if (y > rectMaxY) rectMaxY = y;
            }
        }

        minX = rectMinX;
        minY = rectMinY;
        maxX = rectMaxX;
        maxY = rectMaxY;

        if (considerBackgroundRestore && maxX < minX && maxY < minY) {
            minX = 0;
            minY = 0;
            maxX = frame.Width - 1;
            maxY = frame.Height - 1;
        }

        return maxX >= minX && maxY >= minY;
    }

    private static GifAnimationFrame CropFrame(GifAnimationFrame frame, int minX, int minY, int maxX, int maxY) {
        var width = maxX - minX + 1;
        var height = maxY - minY + 1;
        if (width <= 0 || height <= 0) {
            return new GifAnimationFrame(new byte[] { 0, 0, 0, 0 }, 1, 1, 4, frame.DurationMs, 0, 0, frame.DisposalMethod);
        }
        if (width == frame.Width && height == frame.Height && minX == 0 && minY == 0) return frame;

        var cropped = new byte[width * height * 4];
        for (var y = 0; y < height; y++) {
            var srcRow = (minY + y) * frame.Stride;
            var srcOffset = srcRow + minX * 4;
            var dstRow = y * width * 4;
            Buffer.BlockCopy(frame.Rgba, srcOffset, cropped, dstRow, width * 4);
        }
        return new GifAnimationFrame(cropped, width, height, width * 4, frame.DurationMs, frame.X + minX, frame.Y + minY, frame.DisposalMethod);
    }

    private static void ApplyFrameToCanvas(byte[] canvas, int canvasWidth, int canvasHeight, GifAnimationFrame frame) {
        for (var y = 0; y < frame.Height; y++) {
            var srcRow = y * frame.Stride;
            var dstY = frame.Y + y;
            if ((uint)dstY >= (uint)canvasHeight) continue;
            var dstRow = dstY * canvasWidth * 4;
            for (var x = 0; x < frame.Width; x++) {
                var src = srcRow + x * 4;
                var a = frame.Rgba[src + 3];
                if (a < 128) continue;
                var dstX = frame.X + x;
                if ((uint)dstX >= (uint)canvasWidth) continue;
                var dst = dstRow + dstX * 4;
                canvas[dst + 0] = frame.Rgba[src + 0];
                canvas[dst + 1] = frame.Rgba[src + 1];
                canvas[dst + 2] = frame.Rgba[src + 2];
                canvas[dst + 3] = a;
            }
        }
    }

    private static bool FramesHaveTransparency(GifAnimationFrame[] frames) {
        for (var i = 0; i < frames.Length; i++) {
            if (FrameHasTransparency(frames[i])) return true;
        }
        return false;
    }

    private static bool FrameHasTransparency(GifAnimationFrame frame) {
        for (var y = 0; y < frame.Height; y++) {
            var row = y * frame.Stride;
            for (var x = 0; x < frame.Width; x++) {
                if (frame.Rgba[row + x * 4 + 3] < 128) return true;
            }
        }
        return false;
    }

    private static bool HasTransparency(ReadOnlySpan<byte> rgba, int width, int height, int stride) {
        for (var y = 0; y < height; y++) {
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                if (rgba[row + x * 4 + 3] < 128) return true;
            }
        }
        return false;
    }

    private static bool TryBuildGlobalPalette(
        GifAnimationFrame[] frames,
        byte bgR,
        byte bgG,
        byte bgB,
        byte bgA,
        bool anyTransparency,
        out List<int> palette,
        out Dictionary<int, int>? map,
        out int transparentIndex,
        out int backgroundIndex,
        out bool isQuantized,
        out int rLevels,
        out int gLevels,
        out int bLevels) {
        palette = new List<int>(256);
        map = new Dictionary<int, int>(256);
        transparentIndex = -1;
        isQuantized = false;
        rLevels = 0;
        gLevels = 0;
        bLevels = 0;

        var nextIndex = 0;
        if (anyTransparency) {
            transparentIndex = 0;
            palette.Add(0);
            nextIndex = 1;
        }

        backgroundIndex = 0;
        if (bgA >= 128) {
            var bgColor = (bgR << 16) | (bgG << 8) | bgB;
            if (!map.TryGetValue(bgColor, out var bgIdx)) {
                if (nextIndex >= 256) {
                    BuildUniformPalette(anyTransparency, out palette, out rLevels, out gLevels, out bLevels, out transparentIndex);
                    map = null;
                    isQuantized = true;
                    backgroundIndex = FindBackgroundIndex(palette, transparentIndex, bgR, bgG, bgB, bgA);
                    return true;
                }
                bgIdx = nextIndex++;
                map[bgColor] = bgIdx;
                palette.Add(bgColor);
            }
            backgroundIndex = bgIdx;
        } else if (anyTransparency) {
            backgroundIndex = transparentIndex;
        }

        for (var i = 0; i < frames.Length; i++) {
            var frame = frames[i];
            for (var y = 0; y < frame.Height; y++) {
                var row = y * frame.Stride;
                for (var x = 0; x < frame.Width; x++) {
                    var p = row + x * 4;
                    if (frame.Rgba[p + 3] < 128) continue;
                    var color = (frame.Rgba[p + 0] << 16) | (frame.Rgba[p + 1] << 8) | frame.Rgba[p + 2];
                    if (map.ContainsKey(color)) continue;
                    if (nextIndex >= 256) {
                        BuildUniformPalette(anyTransparency, out palette, out rLevels, out gLevels, out bLevels, out transparentIndex);
                        map = null;
                        isQuantized = true;
                        backgroundIndex = FindBackgroundIndex(palette, transparentIndex, bgR, bgG, bgB, bgA);
                        return true;
                    }
                    map[color] = nextIndex++;
                    palette.Add(color);
                }
            }
        }

        return true;
    }

    private static List<int> BuildBackgroundPalette(byte bgR, byte bgG, byte bgB) {
        var palette = new List<int>(2) {
            (bgR << 16) | (bgG << 8) | bgB
        };
        if (palette.Count < 2) palette.Add(0);
        return palette;
    }

    private static int FindBackgroundIndex(List<int> palette, int transparentIndex, byte bgR, byte bgG, byte bgB, byte bgA) {
        if (bgA < 128) {
            return transparentIndex >= 0 ? transparentIndex : 0;
        }
        var bgColor = (bgR << 16) | (bgG << 8) | bgB;
        for (var i = 0; i < palette.Count; i++) {
            if (palette[i] == bgColor) return i;
        }
        return 0;
    }

    private static PaletteInfo BuildFramePalette(GifAnimationFrame frame) {
        var palette = new List<int>(256);
        var map = new Dictionary<int, int>(256);
        var hasTransparency = FrameHasTransparency(frame);
        var transparentIndex = -1;
        var nextIndex = 0;
        var overflow = false;

        if (hasTransparency) {
            transparentIndex = 0;
            palette.Add(0);
            nextIndex = 1;
        }

        for (var y = 0; y < frame.Height && !overflow; y++) {
            var row = y * frame.Stride;
            for (var x = 0; x < frame.Width; x++) {
                var p = row + x * 4;
                if (frame.Rgba[p + 3] < 128) continue;
                var color = (frame.Rgba[p + 0] << 16) | (frame.Rgba[p + 1] << 8) | frame.Rgba[p + 2];
                if (map.ContainsKey(color)) continue;
                if (nextIndex >= 256) {
                    overflow = true;
                    break;
                }
                map[color] = nextIndex++;
                palette.Add(color);
            }
        }

        if (!overflow) {
            var tableSize = GetTableSize(palette.Count);
            var tableBits = GetTableBits(tableSize);
            return new PaletteInfo(palette, map, tableSize, tableBits, hasTransparency, transparentIndex, false, 0, 0, 0);
        }

        BuildUniformPalette(hasTransparency, out palette, out var rLevels, out var gLevels, out var bLevels, out transparentIndex);
        var uniformSize = GetTableSize(palette.Count);
        var uniformBits = GetTableBits(uniformSize);
        return new PaletteInfo(palette, null, uniformSize, uniformBits, hasTransparency, transparentIndex, true, rLevels, gLevels, bLevels);
    }

    private static byte[] BuildFrameIndices(GifAnimationFrame frame, PaletteInfo palette, bool useGlobal) {
        var indices = new byte[frame.Width * frame.Height];
        var dst = 0;
        for (var y = 0; y < frame.Height; y++) {
            var row = y * frame.Stride;
            for (var x = 0; x < frame.Width; x++) {
                var p = row + x * 4;
                if (palette.HasTransparency && frame.Rgba[p + 3] < 128) {
                    indices[dst++] = (byte)palette.TransparentIndex;
                    continue;
                }
                if (palette.IsQuantized) {
                    throw new InvalidOperationException("Quantized palettes must use the dithered path.");
                } else {
                    var color = (frame.Rgba[p + 0] << 16) | (frame.Rgba[p + 1] << 8) | frame.Rgba[p + 2];
                    if (!palette.ColorMap!.TryGetValue(color, out var index)) {
                        if (useGlobal) throw new ArgumentException("Frame color not found in global palette.");
                        throw new ArgumentException("Frame color not found in local palette.");
                    }
                    indices[dst++] = (byte)index;
                }
            }
        }
        return indices;
    }

    private static void WriteLoopExtension(Stream stream, int loopCount) {
        if (loopCount < 0) loopCount = 0;
        stream.WriteByte(0x21);
        stream.WriteByte(0xFF);
        stream.WriteByte(0x0B);
        stream.WriteByte((byte)'N');
        stream.WriteByte((byte)'E');
        stream.WriteByte((byte)'T');
        stream.WriteByte((byte)'S');
        stream.WriteByte((byte)'C');
        stream.WriteByte((byte)'A');
        stream.WriteByte((byte)'P');
        stream.WriteByte((byte)'E');
        stream.WriteByte((byte)'2');
        stream.WriteByte((byte)'.');
        stream.WriteByte((byte)'0');
        stream.WriteByte(0x03);
        stream.WriteByte(0x01);
        stream.WriteByte((byte)(loopCount & 0xFF));
        stream.WriteByte((byte)((loopCount >> 8) & 0xFF));
        stream.WriteByte(0x00);
    }

    private static GifDisposalMethod ClampDisposal(GifDisposalMethod disposal) {
        return disposal switch {
            GifDisposalMethod.DoNotDispose => GifDisposalMethod.DoNotDispose,
            GifDisposalMethod.RestoreBackground => GifDisposalMethod.RestoreBackground,
            GifDisposalMethod.RestorePrevious => GifDisposalMethod.RestorePrevious,
            _ => GifDisposalMethod.None
        };
    }

    private static int ClampColorResolution(int value) {
        if (value < 0) return 0;
        if (value > 7) return 7;
        return value;
    }

    private static int GetTableSize(int colorCount) {
        var size = 2;
        while (size < colorCount) size <<= 1;
        return size;
    }

    private static int GetTableBits(int tableSize) {
        var bits = 0;
        var size = 1;
        while (size < tableSize) {
            size <<= 1;
            bits++;
        }
        return bits;
    }

    private static void FillIndicesExact(
        ReadOnlySpan<byte> rgba,
        int width,
        int height,
        int stride,
        byte[] indices,
        Dictionary<int, int> map,
        bool hasTransparency,
        int transparentIndex) {
        var dst = 0;
        for (var y = 0; y < height; y++) {
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                var p = row + x * 4;
                if (hasTransparency && rgba[p + 3] < 128) {
                    indices[dst++] = (byte)transparentIndex;
                    continue;
                }
                var color = (rgba[p + 0] << 16) | (rgba[p + 1] << 8) | rgba[p + 2];
                if (!map.TryGetValue(color, out var index)) {
                    index = 0;
                }
                indices[dst++] = (byte)index;
            }
        }
    }

    private static byte[] BuildFrameIndicesQuantized(GifAnimationFrame frame, PaletteInfo palette) {
        var indices = new byte[frame.Width * frame.Height];
        FillIndicesQuantizedDithered(frame.Rgba, frame.Width, frame.Height, frame.Stride, indices, palette.HasTransparency, palette.RLevels, palette.GLevels, palette.BLevels, palette.TransparentIndex);
        return indices;
    }

    private static void FillIndicesQuantizedDithered(
        ReadOnlySpan<byte> rgba,
        int width,
        int height,
        int stride,
        byte[] indices,
        bool hasTransparency,
        int rLevels,
        int gLevels,
        int bLevels,
        int transparentIndex) {
        var dst = 0;
        var errR = new int[width + 2];
        var errG = new int[width + 2];
        var errB = new int[width + 2];
        var nextR = new int[width + 2];
        var nextG = new int[width + 2];
        var nextB = new int[width + 2];
        var offset = hasTransparency ? 1 : 0;

        for (var y = 0; y < height; y++) {
            Array.Clear(nextR, 0, nextR.Length);
            Array.Clear(nextG, 0, nextG.Length);
            Array.Clear(nextB, 0, nextB.Length);
            var row = y * stride;
            for (var x = 0; x < width; x++) {
                var p = row + x * 4;
                if (hasTransparency && rgba[p + 3] < 128) {
                    indices[dst++] = (byte)transparentIndex;
                    continue;
                }

                var idx = x + 1;
                var r = ClampToByte(rgba[p + 0] + ((errR[idx] + 8) >> 4));
                var g = ClampToByte(rgba[p + 1] + ((errG[idx] + 8) >> 4));
                var b = ClampToByte(rgba[p + 2] + ((errB[idx] + 8) >> 4));

                var rIndex = (r * rLevels) >> 8;
                var gIndex = (g * gLevels) >> 8;
                var bIndex = (b * bLevels) >> 8;

                var rQ = rLevels == 1 ? 0 : (rIndex * 255 / (rLevels - 1));
                var gQ = gLevels == 1 ? 0 : (gIndex * 255 / (gLevels - 1));
                var bQ = bLevels == 1 ? 0 : (bIndex * 255 / (bLevels - 1));

                var index = (rIndex * gLevels + gIndex) * bLevels + bIndex;
                indices[dst++] = (byte)(index + offset);

                var errValR = r - rQ;
                var errValG = g - gQ;
                var errValB = b - bQ;

                errR[idx + 1] += errValR * 7;
                errG[idx + 1] += errValG * 7;
                errB[idx + 1] += errValB * 7;

                nextR[idx - 1] += errValR * 3;
                nextG[idx - 1] += errValG * 3;
                nextB[idx - 1] += errValB * 3;

                nextR[idx] += errValR * 5;
                nextG[idx] += errValG * 5;
                nextB[idx] += errValB * 5;

                nextR[idx + 1] += errValR;
                nextG[idx + 1] += errValG;
                nextB[idx + 1] += errValB;
            }

            var swapR = errR;
            errR = nextR;
            nextR = swapR;

            var swapG = errG;
            errG = nextG;
            nextG = swapG;

            var swapB = errB;
            errB = nextB;
            nextB = swapB;
        }
    }

    private static void BuildUniformPalette(
        bool hasTransparency,
        out List<int> palette,
        out int rLevels,
        out int gLevels,
        out int bLevels,
        out int transparentIndex) {
        if (hasTransparency) {
            rLevels = 7;
            gLevels = 7;
            bLevels = 5;
            transparentIndex = 0;
            palette = new List<int>(1 + rLevels * gLevels * bLevels) { 0 };
        } else {
            rLevels = 8;
            gLevels = 8;
            bLevels = 4;
            transparentIndex = -1;
            palette = new List<int>(rLevels * gLevels * bLevels);
        }

        for (var r = 0; r < rLevels; r++) {
            var rVal = rLevels == 1 ? 0 : (r * 255 / (rLevels - 1));
            for (var g = 0; g < gLevels; g++) {
                var gVal = gLevels == 1 ? 0 : (g * 255 / (gLevels - 1));
                for (var b = 0; b < bLevels; b++) {
                    var bVal = bLevels == 1 ? 0 : (b * 255 / (bLevels - 1));
                    palette.Add((rVal << 16) | (gVal << 8) | bVal);
                }
            }
        }
    }

    private static int ClampToByte(int value) {
        if (value < 0) return 0;
        if (value > 255) return 255;
        return value;
    }

    private readonly struct PaletteInfo {
        public PaletteInfo(
            List<int> colors,
            Dictionary<int, int>? colorMap,
            int tableSize,
            int tableBits,
            bool hasTransparency,
            int transparentIndex,
            bool isQuantized,
            int rLevels,
            int gLevels,
            int bLevels) {
            Colors = colors;
            ColorMap = colorMap;
            TableSize = tableSize;
            TableBits = tableBits;
            HasTransparency = hasTransparency;
            TransparentIndex = transparentIndex;
            IsQuantized = isQuantized;
            RLevels = rLevels;
            GLevels = gLevels;
            BLevels = bLevels;
        }

        public List<int> Colors { get; }
        public Dictionary<int, int>? ColorMap { get; }
        public int TableSize { get; }
        public int TableBits { get; }
        public bool HasTransparency { get; }
        public int TransparentIndex { get; }
        public bool IsQuantized { get; }
        public int RLevels { get; }
        public int GLevels { get; }
        public int BLevels { get; }
    }

    private static void WriteGlobalColorTable(Stream stream, List<int> palette, int tableSize) {
        for (var i = 0; i < tableSize; i++) {
            if (i < palette.Count) {
                var color = palette[i];
                stream.WriteByte((byte)((color >> 16) & 0xFF));
                stream.WriteByte((byte)((color >> 8) & 0xFF));
                stream.WriteByte((byte)(color & 0xFF));
            } else {
                stream.WriteByte(0);
                stream.WriteByte(0);
                stream.WriteByte(0);
            }
        }
    }

    private static byte[] LzwEncode(ReadOnlySpan<byte> data, int minCodeSize) {
        if (data.Length == 0) return Array.Empty<byte>();
        if (minCodeSize < 2 || minCodeSize > 8) throw new ArgumentOutOfRangeException(nameof(minCodeSize));

        var clearCode = 1 << minCodeSize;
        var endCode = clearCode + 1;
        var codeSize = minCodeSize + 1;
        var nextCode = endCode + 1;
        var writer = new GifBitWriter(data.Length / 2 + 16);

        writer.Write(clearCode, codeSize);
        var hasOldCode = false;
        for (var i = 0; i < data.Length; i++) {
            if (nextCode == 4096) {
                writer.Write(clearCode, codeSize);
                codeSize = minCodeSize + 1;
                nextCode = endCode + 1;
                hasOldCode = false;
            }

            writer.Write(data[i], codeSize);
            if (hasOldCode) {
                nextCode++;
                if (nextCode == (1 << codeSize) && codeSize < 12) {
                    codeSize++;
                }
            } else {
                hasOldCode = true;
            }
        }

        writer.Write(endCode, codeSize);
        return writer.ToArray();
    }

    private static void FillCanvas(byte[] canvas, byte r, byte g, byte b, byte a) {
        for (var i = 0; i < canvas.Length; i += 4) {
            canvas[i + 0] = r;
            canvas[i + 1] = g;
            canvas[i + 2] = b;
            canvas[i + 3] = a;
        }
    }

    private static void ClearRect(byte[] canvas, int canvasW, int canvasH, int left, int top, int width, int height, byte r, byte g, byte b, byte a) {
        for (var y = 0; y < height; y++) {
            var dstY = top + y;
            if ((uint)dstY >= (uint)canvasH) continue;
            var dstRow = dstY * canvasW * 4;
            for (var x = 0; x < width; x++) {
                var dstX = left + x;
                if ((uint)dstX >= (uint)canvasW) continue;
                var dst = dstRow + dstX * 4;
                canvas[dst + 0] = r;
                canvas[dst + 1] = g;
                canvas[dst + 2] = b;
                canvas[dst + 3] = a;
            }
        }
    }

    private static void WriteSubBlocks(Stream stream, byte[] data) {
        var offset = 0;
        while (offset < data.Length) {
            var size = Math.Min(255, data.Length - offset);
            stream.WriteByte((byte)size);
            stream.Write(data, offset, size);
            offset += size;
        }
        stream.WriteByte(0);
    }

    private static void WriteUInt16(Stream stream, ushort value) {
        stream.WriteByte((byte)value);
        stream.WriteByte((byte)(value >> 8));
    }

    private static readonly byte[] GifHeader = { (byte)'G', (byte)'I', (byte)'F', (byte)'8', (byte)'9', (byte)'a' };

    private sealed class GifBitWriter {
        private readonly List<byte> _buffer;
        private int _bitPos;
        private byte _current;

        public GifBitWriter(int capacity) {
            _buffer = new List<byte>(capacity);
        }

        public void Write(int code, int size) {
            for (var i = 0; i < size; i++) {
                var bit = (code >> i) & 1;
                if (bit != 0) {
                    _current |= (byte)(1 << _bitPos);
                }
                _bitPos++;
                if (_bitPos == 8) {
                    _buffer.Add(_current);
                    _current = 0;
                    _bitPos = 0;
                }
            }
        }

        public byte[] ToArray() {
            if (_bitPos > 0) {
                _buffer.Add(_current);
                _current = 0;
                _bitPos = 0;
            }
            return _buffer.ToArray();
        }
    }
}
