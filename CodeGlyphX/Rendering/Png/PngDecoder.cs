using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Rendering.Png;

internal static class PngDecoder {
    private static readonly byte[] Signature = { 137, 80, 78, 71, 13, 10, 26, 10 };
    private const string PngPayloadLimitMessage = "PNG payload exceeds size limits.";
    private const string PngIdatLimitMessage = "PNG IDAT payload exceeds size limits.";
    private const string PngDimensionsLimitMessage = "PNG dimensions exceed size limits.";

    public static byte[] DecodeRgba32(byte[] png, out int width, out int height) {
        if (png is null) throw new ArgumentNullException(nameof(png));
        return DecodeRgba32(png, 0, png.Length, out width, out height);
    }

    public static byte[] DecodeRgba32(byte[] png, int offset, int length, out int width, out int height) {
        if (png is null) throw new ArgumentNullException(nameof(png));
        if (offset < 0 || length < 0 || offset + length > png.Length) throw new ArgumentOutOfRangeException(nameof(length));
        if (length < Signature.Length) throw new FormatException("Invalid PNG signature.");
        DecodeGuards.EnsurePayloadWithinLimits(length, PngPayloadLimitMessage);

        for (var i = 0; i < Signature.Length; i++) {
            if (png[offset + i] != Signature[i]) throw new FormatException("Invalid PNG signature.");
        }

        width = 0;
        height = 0;
        var bitDepth = 0;
        var colorType = 0;
        var compression = 0;
        var filter = 0;
        var interlace = 0;

        var idatCount = 0;
        var idatTotal = 0;
        var singleIdatOffset = 0;
        var singleIdatLength = 0;
        List<(int Offset, int Length)>? idatSegments = null;
        byte[]? palette = null;
        byte[]? transparency = null;
        var localOffset = Signature.Length;
        var end = length;

        while (localOffset + 8 <= end) {
            var len = ReadUInt32BE(png, offset + localOffset);
            if (len > int.MaxValue) throw new FormatException("Invalid PNG chunk length.");
            var chunkLength = (int)len;
            localOffset += 4;
            if (localOffset + 4 > end) throw new FormatException("Invalid PNG chunk.");
            var typeOffset = offset + localOffset;
            localOffset += 4;
            if (localOffset + chunkLength + 4 > end) throw new FormatException("Invalid PNG chunk length.");
            var dataOffset = offset + localOffset;
            localOffset += chunkLength;
            localOffset += 4; // CRC

            if (MatchType(png, typeOffset, "IHDR")) {
                if (chunkLength < 13) throw new FormatException("Invalid IHDR chunk.");
                width = (int)ReadUInt32BE(png, dataOffset);
                height = (int)ReadUInt32BE(png, dataOffset + 4);
                bitDepth = png[dataOffset + 8];
                colorType = png[dataOffset + 9];
                compression = png[dataOffset + 10];
                filter = png[dataOffset + 11];
                interlace = png[dataOffset + 12];
            } else if (MatchType(png, typeOffset, "PLTE")) {
                if (chunkLength <= 0 || chunkLength > 256 * 3 || chunkLength % 3 != 0) {
                    throw new FormatException("Invalid PNG palette.");
                }
                palette = new byte[chunkLength];
                Buffer.BlockCopy(png, dataOffset, palette, 0, chunkLength);
            } else if (MatchType(png, typeOffset, "tRNS")) {
                if (chunkLength < 0 || chunkLength > 256) {
                    throw new FormatException("Invalid PNG transparency data.");
                }
                transparency = new byte[chunkLength];
                Buffer.BlockCopy(png, dataOffset, transparency, 0, chunkLength);
            } else if (MatchType(png, typeOffset, "IDAT")) {
                if (chunkLength > 0) {
                    idatTotal = checked(idatTotal + chunkLength);
                    DecodeGuards.EnsurePayloadWithinLimits(idatTotal, PngIdatLimitMessage);
                }
                if (idatCount == 0) {
                    singleIdatOffset = dataOffset;
                    singleIdatLength = chunkLength;
                } else {
                    idatSegments ??= new List<(int, int)>(4) { (singleIdatOffset, singleIdatLength) };
                    idatSegments.Add((dataOffset, chunkLength));
                }
                idatCount++;
            } else if (MatchType(png, typeOffset, "IEND")) {
                break;
            }
        }

        if (width <= 0 || height <= 0) throw new FormatException("Missing IHDR.");
        _ = DecodeGuards.EnsurePixelCount(width, height, PngDimensionsLimitMessage);
        if (compression != 0 || filter != 0) throw new FormatException("Unsupported PNG compression/filter method.");
        if (interlace != 0 && interlace != 1) throw new FormatException("Unsupported PNG interlace method.");

        var channels = colorType switch {
            0 => 1,
            2 => 3,
            3 => 1,
            4 => 2,
            6 => 4,
            _ => throw new FormatException("Unsupported PNG color type."),
        };
        var bitDepthOk = colorType switch {
            0 => bitDepth == 1 || bitDepth == 2 || bitDepth == 4 || bitDepth == 8 || bitDepth == 16,
            2 => bitDepth == 8 || bitDepth == 16,
            3 => bitDepth == 1 || bitDepth == 2 || bitDepth == 4 || bitDepth == 8,
            4 => bitDepth == 8 || bitDepth == 16,
            6 => bitDepth == 8 || bitDepth == 16,
            _ => false
        };
        if (!bitDepthOk) throw new FormatException("Unsupported PNG bit depth.");
        if (colorType == 3 && (palette is null || palette.Length < 3)) {
            throw new FormatException("Missing PNG palette.");
        }
        if (palette is not null && palette.Length % 3 != 0) {
            throw new FormatException("Invalid PNG palette.");
        }

        var rowBytes = bitDepth < 8
            ? DecodeGuards.EnsureByteCount(((long)width * bitDepth + 7) / 8, PngDimensionsLimitMessage)
            : DecodeGuards.EnsureByteCount((long)width * channels * (bitDepth / 8), PngDimensionsLimitMessage);
        var bytesPerPixel = (bitDepth * channels + 7) / 8;
        var expected = interlace == 0
            ? DecodeGuards.EnsureByteCount((long)height * (rowBytes + 1), PngDimensionsLimitMessage)
            : GetAdam7ExpectedSize(width, height, bitDepth, channels);
        var scanlines = ArrayPool<byte>.Shared.Rent(expected);

        if (idatCount == 0 || idatTotal == 0) throw new FormatException("Missing IDAT.");

        byte[]? idatBuffer = null;
        Stream? idatStream = null;
        try {
            if (idatCount == 1) {
                idatStream = new MemoryStream(png, singleIdatOffset, singleIdatLength, writable: false);
            } else {
                idatBuffer = ArrayPool<byte>.Shared.Rent(idatTotal);
                var writeOffset = 0;
                if (idatSegments is not null) {
                    for (var i = 0; i < idatSegments.Count; i++) {
                        var segment = idatSegments[i];
                        Buffer.BlockCopy(png, segment.Offset, idatBuffer, writeOffset, segment.Length);
                        writeOffset += segment.Length;
                    }
                } else if (singleIdatLength > 0) {
                    Buffer.BlockCopy(png, singleIdatOffset, idatBuffer, 0, singleIdatLength);
                    writeOffset = singleIdatLength;
                }
                idatStream = new MemoryStream(idatBuffer, 0, idatTotal, writable: false);
            }

            try {
                using (var z = CreateZLibStream(idatStream)) {
                    ReadExact(z, scanlines, expected);
                }
            } catch (InvalidDataException) {
                // Some PNGs in the wild (and a few of our stylized samples) decode fine with raw DEFLATE but fail
                // strict zlib checksum validation. Fall back to a raw-DEFLATE stream when available.
                if (!TryReadDeflateFallback(idatStream, scanlines, expected)) {
                    throw;
                }
            }

            var raw = interlace == 0
                ? DecodeNonInterlaced(scanlines.AsSpan(0, expected), width, height, rowBytes, bytesPerPixel)
                : DecodeAdam7(scanlines.AsSpan(0, expected), width, height, rowBytes, bitDepth, channels, bytesPerPixel);

            return ExpandToRgba(raw, width, height, colorType, bitDepth, palette, transparency);
        } finally {
            idatStream?.Dispose();
            if (idatBuffer is not null) {
                ArrayPool<byte>.Shared.Return(idatBuffer);
            }
            ArrayPool<byte>.Shared.Return(scanlines);
        }
    }

    private static void ReadExact(Stream s, byte[] buffer, int length) {
        var offset = 0;
        while (offset < length) {
            var read = s.Read(buffer, offset, length - offset);
            if (read <= 0) throw new FormatException("Truncated PNG data.");
            offset += read;
        }
    }

    private static bool TryReadDeflateFallback(Stream idatStream, byte[] scanlines, int expected) {
        try {
            // Best effort rewind. All callers currently pass MemoryStream.
            if (idatStream.CanSeek) idatStream.Position = 0;

            var data = idatStream is MemoryStream ms ? ms.ToArray() : ReadAllBytes(idatStream);
            if (data.Length < 6) return false;

            // zlib stream: 2 byte header + deflate payload + 4 byte Adler32 checksum.
            using var deflate = new DeflateStream(new MemoryStream(data, 2, data.Length - 6, writable: false), CompressionMode.Decompress, leaveOpen: false);
            ReadExact(deflate, scanlines, expected);
            return true;
        } catch {
            return false;
        }
    }

    private static void Unfilter(ReadOnlySpan<byte> scanlines, byte[] raw, int rowBytes, int height, int bpp) {
        var src = 0;
        var dst = 0;

        for (var y = 0; y < height; y++) {
            var filter = scanlines[src++];
            for (var x = 0; x < rowBytes; x++) {
                var cur = scanlines[src++];
                var a = x >= bpp ? raw[dst + x - bpp] : 0;
                var b = y > 0 ? raw[dst - rowBytes + x] : 0;
                var c = y > 0 && x >= bpp ? raw[dst - rowBytes + x - bpp] : 0;
                int val = filter switch {
                    0 => cur,
                    1 => cur + a,
                    2 => cur + b,
                    3 => cur + ((a + b) >> 1),
                    4 => cur + Paeth(a, b, c),
                    _ => throw new FormatException("Unsupported PNG filter."),
                };
                raw[dst + x] = (byte)(val & 0xFF);
            }
            dst += rowBytes;
        }
    }

    private static byte[] DecodeNonInterlaced(ReadOnlySpan<byte> scanlines, int width, int height, int rowBytes, int bytesPerPixel) {
        var raw = new byte[DecodeGuards.EnsureByteCount((long)height * rowBytes, PngDimensionsLimitMessage)];
        Unfilter(scanlines, raw, rowBytes, height, bytesPerPixel);
        return raw;
    }

    private static byte[] DecodeAdam7(ReadOnlySpan<byte> scanlines, int width, int height, int rowBytes, int bitDepth, int channels, int bytesPerPixel) {
        var raw = new byte[DecodeGuards.EnsureByteCount((long)height * rowBytes, PngDimensionsLimitMessage)];
        var offset = 0;
        var passes = Adam7Passes;
        for (var i = 0; i < passes.Length; i++) {
            var pass = passes[i];
            var passWidth = GetAdam7Size(width, pass.StartX, pass.StepX);
            var passHeight = GetAdam7Size(height, pass.StartY, pass.StepY);
            if (passWidth == 0 || passHeight == 0) {
                continue;
            }

            var passRowBytes = bitDepth < 8
                ? DecodeGuards.EnsureByteCount(((long)passWidth * bitDepth + 7) / 8, PngDimensionsLimitMessage)
                : DecodeGuards.EnsureByteCount((long)passWidth * channels * (bitDepth / 8), PngDimensionsLimitMessage);
            var passExpected = DecodeGuards.EnsureByteCount((long)passHeight * (passRowBytes + 1), PngDimensionsLimitMessage);
            if (offset + passExpected > scanlines.Length) {
                throw new FormatException("Invalid Adam7 scanline data.");
            }

            var passRawLength = DecodeGuards.EnsureByteCount((long)passHeight * passRowBytes, PngDimensionsLimitMessage);
            var passRaw = ArrayPool<byte>.Shared.Rent(passRawLength);
            try {
                Unfilter(scanlines.Slice(offset, passExpected), passRaw, passRowBytes, passHeight, bytesPerPixel);
                offset += passExpected;

                if (bitDepth >= 8) {
                    for (var y = 0; y < passHeight; y++) {
                        var srcRow = y * passRowBytes;
                        var dstY = pass.StartY + y * pass.StepY;
                        var dstRow = dstY * rowBytes;
                        for (var x = 0; x < passWidth; x++) {
                            var src = srcRow + x * bytesPerPixel;
                            var dstX = pass.StartX + x * pass.StepX;
                            var dst = dstRow + dstX * bytesPerPixel;
                            Buffer.BlockCopy(passRaw, src, raw, dst, bytesPerPixel);
                        }
                    }
                } else {
                    for (var y = 0; y < passHeight; y++) {
                        var srcRow = y * passRowBytes;
                        var dstY = pass.StartY + y * pass.StepY;
                        var dstRow = dstY * rowBytes;
                        for (var x = 0; x < passWidth; x++) {
                            var sample = ReadPackedSample(passRaw, srcRow, x, bitDepth);
                            var dstX = pass.StartX + x * pass.StepX;
                            WritePackedSample(raw, dstRow, dstX, bitDepth, sample);
                        }
                    }
                }
            } finally {
                ArrayPool<byte>.Shared.Return(passRaw);
            }
        }

        if (offset != scanlines.Length) {
            throw new FormatException("Invalid Adam7 scanline length.");
        }

        return raw;
    }

    private static int GetAdam7ExpectedSize(int width, int height, int bitDepth, int channels) {
        var total = 0L;
        var passes = Adam7Passes;
        for (var i = 0; i < passes.Length; i++) {
            var pass = passes[i];
            var passWidth = GetAdam7Size(width, pass.StartX, pass.StepX);
            var passHeight = GetAdam7Size(height, pass.StartY, pass.StepY);
            if (passWidth == 0 || passHeight == 0) {
                continue;
            }
            var passRowBytes = bitDepth < 8
                ? DecodeGuards.EnsureByteCount(((long)passWidth * bitDepth + 7) / 8, PngDimensionsLimitMessage)
                : DecodeGuards.EnsureByteCount((long)passWidth * channels * (bitDepth / 8), PngDimensionsLimitMessage);
            total += (long)passHeight * (passRowBytes + 1);
            if (total > int.MaxValue) throw new FormatException(PngDimensionsLimitMessage);
        }
        return (int)total;
    }

    private static int GetAdam7Size(int length, int start, int step) {
        if (length <= start) return 0;
        return (length - start + step - 1) / step;
    }

    private static byte[] ExpandToRgba(byte[] raw, int width, int height, int colorType, int bitDepth, byte[]? palette, byte[]? transparency) {
        if (colorType == 6 && bitDepth == 8 && transparency is null) return raw;

        var pixelCount = DecodeGuards.EnsurePixelCount(width, height, PngDimensionsLimitMessage);
        var rgba = new byte[DecodeGuards.EnsureByteCount((long)pixelCount * 4, PngDimensionsLimitMessage)];
        if (colorType == 2 && bitDepth == 8) {
            var tr = transparency is { Length: >= 6 } ? ReadUInt16BE(transparency, 0) >> 8 : -1;
            var tg = transparency is { Length: >= 6 } ? ReadUInt16BE(transparency, 2) >> 8 : -1;
            var tb = transparency is { Length: >= 6 } ? ReadUInt16BE(transparency, 4) >> 8 : -1;
            for (var i = 0; i < pixelCount; i++) {
                var src = i * 3;
                var dst = i * 4;
                rgba[dst + 0] = raw[src + 0];
                rgba[dst + 1] = raw[src + 1];
                rgba[dst + 2] = raw[src + 2];
                rgba[dst + 3] = (raw[src + 0] == tr && raw[src + 1] == tg && raw[src + 2] == tb) ? (byte)0 : (byte)255;
            }
            return rgba;
        }

        if (colorType == 2 && bitDepth == 16) {
            var tr = transparency is { Length: >= 6 } ? ReadUInt16BE(transparency, 0) : -1;
            var tg = transparency is { Length: >= 6 } ? ReadUInt16BE(transparency, 2) : -1;
            var tb = transparency is { Length: >= 6 } ? ReadUInt16BE(transparency, 4) : -1;
            for (var i = 0; i < pixelCount; i++) {
                var src = i * 6;
                var r16 = ReadUInt16BE(raw, src);
                var g16 = ReadUInt16BE(raw, src + 2);
                var b16 = ReadUInt16BE(raw, src + 4);
                var dst = i * 4;
                rgba[dst + 0] = Sample16To8(r16);
                rgba[dst + 1] = Sample16To8(g16);
                rgba[dst + 2] = Sample16To8(b16);
                rgba[dst + 3] = (r16 == tr && g16 == tg && b16 == tb) ? (byte)0 : (byte)255;
            }
            return rgba;
        }

        if (colorType == 6 && bitDepth == 16) {
            for (var i = 0; i < pixelCount; i++) {
                var src = i * 8;
                var dst = i * 4;
                rgba[dst + 0] = Sample16To8(ReadUInt16BE(raw, src));
                rgba[dst + 1] = Sample16To8(ReadUInt16BE(raw, src + 2));
                rgba[dst + 2] = Sample16To8(ReadUInt16BE(raw, src + 4));
                rgba[dst + 3] = Sample16To8(ReadUInt16BE(raw, src + 6));
            }
            return rgba;
        }

        if (colorType == 4 && bitDepth == 8) {
            for (var i = 0; i < pixelCount; i++) {
                var src = i * 2;
                var dst = i * 4;
                var v = raw[src + 0];
                rgba[dst + 0] = v;
                rgba[dst + 1] = v;
                rgba[dst + 2] = v;
                rgba[dst + 3] = raw[src + 1];
            }
            return rgba;
        }

        if (colorType == 4 && bitDepth == 16) {
            for (var i = 0; i < pixelCount; i++) {
                var src = i * 4;
                var dst = i * 4;
                var v = Sample16To8(ReadUInt16BE(raw, src));
                rgba[dst + 0] = v;
                rgba[dst + 1] = v;
                rgba[dst + 2] = v;
                rgba[dst + 3] = Sample16To8(ReadUInt16BE(raw, src + 2));
            }
            return rgba;
        }

        if (colorType == 0 && bitDepth == 16) {
            var transparent = transparency is { Length: >= 2 } ? ReadUInt16BE(transparency, 0) : -1;
            for (var i = 0; i < pixelCount; i++) {
                var src = i * 2;
                var dst = i * 4;
                var v16 = ReadUInt16BE(raw, src);
                var v8 = Sample16To8(v16);
                rgba[dst + 0] = v8;
                rgba[dst + 1] = v8;
                rgba[dst + 2] = v8;
                rgba[dst + 3] = v16 == transparent ? (byte)0 : (byte)255;
            }
            return rgba;
        }

        if (colorType == 0 && bitDepth == 8) {
            var transparent = transparency is { Length: >= 2 } ? ReadUInt16BE(transparency, 0) >> 8 : -1;
            for (var i = 0; i < pixelCount; i++) {
                var v = raw[i];
                var dst = i * 4;
                rgba[dst + 0] = v;
                rgba[dst + 1] = v;
                rgba[dst + 2] = v;
                rgba[dst + 3] = v == transparent ? (byte)0 : (byte)255;
            }
            return rgba;
        }

        if (colorType == 0 && bitDepth < 8) {
            var rowBytes = DecodeGuards.EnsureByteCount(((long)width * bitDepth + 7) / 8, PngDimensionsLimitMessage);
            var max = (1 << bitDepth) - 1;
            var transparent = transparency is { Length: >= 2 }
                ? ReadUInt16BE(transparency, 0) >> (16 - bitDepth)
                : -1;
            for (var y = 0; y < height; y++) {
                var rowStart = y * rowBytes;
                for (var x = 0; x < width; x++) {
                    var sample = ReadPackedSample(raw, rowStart, x, bitDepth);
                    var v = (byte)(sample * 255 / max);
                    var dst = (y * width + x) * 4;
                    rgba[dst + 0] = v;
                    rgba[dst + 1] = v;
                    rgba[dst + 2] = v;
                    rgba[dst + 3] = sample == transparent ? (byte)0 : (byte)255;
                }
            }
            return rgba;
        }

        if (colorType == 3 && palette is not null) {
            var entryCount = palette.Length / 3;
            byte[]? paletteAlpha = null;
            if (transparency is { Length: > 0 }) {
                paletteAlpha = new byte[entryCount];
                for (var i = 0; i < paletteAlpha.Length; i++) paletteAlpha[i] = 255;
                Buffer.BlockCopy(transparency, 0, paletteAlpha, 0, Math.Min(transparency.Length, paletteAlpha.Length));
            }
            if (bitDepth == 8) {
                for (var i = 0; i < pixelCount; i++) {
                    var idx = raw[i];
                    if (idx >= entryCount) throw new FormatException("Palette index out of range.");
                    var p = idx * 3;
                    var dst = i * 4;
                    rgba[dst + 0] = palette[p + 0];
                    rgba[dst + 1] = palette[p + 1];
                    rgba[dst + 2] = palette[p + 2];
                    rgba[dst + 3] = paletteAlpha is null ? (byte)255 : paletteAlpha[idx];
                }
                return rgba;
            }
            if (bitDepth < 8) {
                var rowBytes = DecodeGuards.EnsureByteCount(((long)width * bitDepth + 7) / 8, PngDimensionsLimitMessage);
                for (var y = 0; y < height; y++) {
                    var rowStart = y * rowBytes;
                    for (var x = 0; x < width; x++) {
                        var idx = ReadPackedSample(raw, rowStart, x, bitDepth);
                        if (idx >= entryCount) throw new FormatException("Palette index out of range.");
                        var p = idx * 3;
                        var dst = (y * width + x) * 4;
                        rgba[dst + 0] = palette[p + 0];
                        rgba[dst + 1] = palette[p + 1];
                        rgba[dst + 2] = palette[p + 2];
                        rgba[dst + 3] = paletteAlpha is null ? (byte)255 : paletteAlpha[idx];
                    }
                }
                return rgba;
            }
        }

        throw new FormatException("Unsupported PNG color type.");
    }

    private static int ReadPackedSample(byte[] raw, int rowStart, int x, int bitDepth) {
        var bitIndex = x * bitDepth;
        var byteIndex = rowStart + (bitIndex >> 3);
        var shift = 8 - bitDepth - (bitIndex & 7);
        var mask = (1 << bitDepth) - 1;
        return (raw[byteIndex] >> shift) & mask;
    }

    private static void WritePackedSample(byte[] raw, int rowStart, int x, int bitDepth, int sample) {
        var bitIndex = x * bitDepth;
        var byteIndex = rowStart + (bitIndex >> 3);
        var shift = 8 - bitDepth - (bitIndex & 7);
        var mask = ((1 << bitDepth) - 1) << shift;
        raw[byteIndex] = (byte)((raw[byteIndex] & ~mask) | ((sample << shift) & mask));
    }

    private static byte Sample16To8(ushort sample) {
        return (byte)(sample >> 8);
    }

    private static int Paeth(int a, int b, int c) {
        var p = a + b - c;
        var pa = Math.Abs(p - a);
        var pb = Math.Abs(p - b);
        var pc = Math.Abs(p - c);
        if (pa <= pb && pa <= pc) return a;
        return pb <= pc ? b : c;
    }

    private static uint ReadUInt32BE(byte[] buffer, int offset) {
        return ((uint)buffer[offset] << 24) |
               ((uint)buffer[offset + 1] << 16) |
               ((uint)buffer[offset + 2] << 8) |
               buffer[offset + 3];
    }

    private static ushort ReadUInt16BE(byte[] buffer, int offset) {
        return (ushort)((buffer[offset] << 8) | buffer[offset + 1]);
    }

    private static bool MatchType(byte[] buffer, int offset, string type) {
        return offset + 4 <= buffer.Length
               && buffer[offset] == (byte)type[0]
               && buffer[offset + 1] == (byte)type[1]
               && buffer[offset + 2] == (byte)type[2]
               && buffer[offset + 3] == (byte)type[3];
    }

    private readonly record struct Adam7Pass(int StartX, int StartY, int StepX, int StepY);

    private static readonly Adam7Pass[] Adam7Passes = {
        new Adam7Pass(0, 0, 8, 8),
        new Adam7Pass(4, 0, 8, 8),
        new Adam7Pass(0, 4, 4, 8),
        new Adam7Pass(2, 0, 4, 4),
        new Adam7Pass(0, 2, 2, 4),
        new Adam7Pass(1, 0, 2, 2),
        new Adam7Pass(0, 1, 1, 2)
    };

    private static Stream CreateZLibStream(Stream source) {
#if NET8_0_OR_GREATER
        return new ZLibStream(source, CompressionMode.Decompress, leaveOpen: true);
#else
        var data = source is MemoryStream ms ? ms.ToArray() : ReadAllBytes(source);
        if (data.Length < 6) throw new FormatException("Invalid zlib stream.");
        return new DeflateStream(new MemoryStream(data, 2, data.Length - 6, writable: false), CompressionMode.Decompress, leaveOpen: true);
#endif
    }

    private static byte[] ReadAllBytes(Stream source) {
        using var ms = new MemoryStream();
        source.CopyTo(ms);
        return ms.ToArray();
    }
}
