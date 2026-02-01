using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Rendering.Png;

internal static class PngWriter {
    private static readonly uint[] CrcTable = BuildCrcTable();
    private const uint AdlerMod = 65521;
    private const int AdlerNmax = 5552;
    private const string PngOutputLimitMessage = "PNG output exceeds size limits.";
    private static readonly byte[] ChunkIHDR = { (byte)'I', (byte)'H', (byte)'D', (byte)'R' };
    private static readonly byte[] ChunkIDAT = { (byte)'I', (byte)'D', (byte)'A', (byte)'T' };
    private static readonly byte[] ChunkIEND = { (byte)'I', (byte)'E', (byte)'N', (byte)'D' };
    private static readonly byte[] ChunkPLTE = { (byte)'P', (byte)'L', (byte)'T', (byte)'E' };
    internal delegate void RowWriter(int y, byte[] rowBuffer, int rowLength);

    public static byte[] WriteRgba8(int width, int height, byte[] scanlinesWithFilter) {
        var idatLength = GetZlibStoredLength(scanlinesWithFilter.Length);
        using var ms = new MemoryStream(EstimateTotalLength(idatLength, hasPlte: false));
        WriteRgba8(ms, width, height, scanlinesWithFilter, scanlinesWithFilter.Length, compressionLevel: 0);
        return ms.ToArray();
    }

    public static byte[] WriteRgba8(int width, int height, byte[] scanlinesWithFilter, int length) {
        var idatLength = GetZlibStoredLength(length);
        using var ms = new MemoryStream(EstimateTotalLength(idatLength, hasPlte: false));
        WriteRgba8(ms, width, height, scanlinesWithFilter, length, compressionLevel: 0);
        return ms.ToArray();
    }

    public static byte[] WriteRgba8(int width, int height, byte[] scanlinesWithFilter, int length, int compressionLevel) {
        var normalizedLevel = NormalizeCompressionLevel(compressionLevel);
        var idatLength = normalizedLevel <= 0 ? GetZlibStoredLength(length) : Math.Max(length, 0);
        using var ms = new MemoryStream(EstimateTotalLength(idatLength, hasPlte: false));
        WriteRgba8(ms, width, height, scanlinesWithFilter, length, normalizedLevel);
        return ms.ToArray();
    }

    public static void WriteRgba8(Stream stream, int width, int height, byte[] scanlinesWithFilter) {
        WriteRgba8(stream, width, height, scanlinesWithFilter, scanlinesWithFilter.Length, compressionLevel: 0);
    }

    public static void WriteRgba8(Stream stream, int width, int height, byte[] scanlinesWithFilter, int length) {
        WriteRgba8(stream, width, height, scanlinesWithFilter, length, compressionLevel: 0);
    }

    public static void WriteRgba8(Stream stream, int width, int height, byte[] scanlinesWithFilter, int length, int compressionLevel) {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        _ = RenderGuards.EnsureOutputPixels(width, height, PngOutputLimitMessage);
        if (scanlinesWithFilter is null) throw new ArgumentNullException(nameof(scanlinesWithFilter));
        if (length < 0 || length > scanlinesWithFilter.Length) throw new ArgumentOutOfRangeException(nameof(length));
        var stride = RenderGuards.EnsureOutputBytes((long)width * 4, PngOutputLimitMessage);
        var expectedLength = RenderGuards.EnsureOutputBytes((long)height * (stride + 1), PngOutputLimitMessage);
        if (length != expectedLength)
            throw new ArgumentException("Invalid scanline buffer length.", nameof(scanlinesWithFilter));

        if (stream is null) throw new ArgumentNullException(nameof(stream));

        stream.Write(Signature, 0, Signature.Length);
        WriteChunk(stream, "IHDR", BuildIHDR(width, height, bitDepth: 8, colorType: 6));

        var normalizedLevel = NormalizeCompressionLevel(compressionLevel);
        if (normalizedLevel <= 0) {
            var idatLength = GetZlibStoredLength(length);
            WriteChunk(stream, "IDAT", idatLength, (Stream s, ref uint crc) => WriteZlibStored(s, scanlinesWithFilter, length, ref crc));
        } else {
            var compressed = ZlibCompress(scanlinesWithFilter, length, normalizedLevel);
            WriteChunk(stream, "IDAT", compressed);
        }
        WriteChunk(stream, "IEND", Array.Empty<byte>());
    }

    public static void WriteRgba8(Stream stream, int width, int height, RowWriter fillRow) {
        WriteRgba8(stream, width, height, fillRow, compressionLevel: 0);
    }

    public static void WriteRgba8(Stream stream, int width, int height, RowWriter fillRow, int compressionLevel) {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        _ = RenderGuards.EnsureOutputPixels(width, height, PngOutputLimitMessage);
        if (fillRow is null) throw new ArgumentNullException(nameof(fillRow));
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var stride = RenderGuards.EnsureOutputBytes((long)width * 4, PngOutputLimitMessage);
        var rowLength = RenderGuards.EnsureOutputBytes((long)stride + 1, PngOutputLimitMessage);
        var length = RenderGuards.EnsureOutputBytes((long)height * rowLength, PngOutputLimitMessage);

        stream.Write(Signature, 0, Signature.Length);
        WriteChunk(stream, "IHDR", BuildIHDR(width, height, bitDepth: 8, colorType: 6));

        var normalizedLevel = NormalizeCompressionLevel(compressionLevel);
        if (normalizedLevel <= 0) {
            var idatLength = GetZlibStoredLength(length);
            WriteChunk(stream, "IDAT", idatLength, (Stream s, ref uint crc) => WriteZlibStoredRows(s, height, rowLength, fillRow, ref crc));
        } else {
            var scanlines = ArrayPool<byte>.Shared.Rent(length);
            try {
                for (var y = 0; y < height; y++) {
                    fillRow(y, scanlines, rowLength);
                }
                // Apply a lightweight adaptive filter pass before deflate to improve compression.
                PngRenderHelpers.ApplyAdaptiveFilterHeuristic(scanlines, height, stride);
                var compressed = ZlibCompress(scanlines, length, normalizedLevel);
                WriteChunk(stream, "IDAT", compressed);
            } finally {
                ArrayPool<byte>.Shared.Return(scanlines);
            }
        }
        WriteChunk(stream, "IEND", Array.Empty<byte>());
    }
    public static byte[] WriteGray1(int width, int height, byte[] scanlinesWithFilter, int length) {
        var idatLength = GetZlibStoredLength(length);
        using var ms = new MemoryStream(EstimateTotalLength(idatLength, hasPlte: false));
        WriteGray1(ms, width, height, scanlinesWithFilter, length, compressionLevel: 0);
        return ms.ToArray();
    }

    public static byte[] WriteGray1(int width, int height, byte[] scanlinesWithFilter, int length, int compressionLevel) {
        var normalizedLevel = NormalizeCompressionLevel(compressionLevel);
        var idatLength = normalizedLevel <= 0 ? GetZlibStoredLength(length) : Math.Max(length, 0);
        using var ms = new MemoryStream(EstimateTotalLength(idatLength, hasPlte: false));
        WriteGray1(ms, width, height, scanlinesWithFilter, length, normalizedLevel);
        return ms.ToArray();
    }

    public static void WriteGray1(Stream stream, int width, int height, byte[] scanlinesWithFilter, int length) {
        WriteGray1(stream, width, height, scanlinesWithFilter, length, compressionLevel: 0);
    }

    public static void WriteGray1(Stream stream, int width, int height, byte[] scanlinesWithFilter, int length, int compressionLevel) {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        _ = RenderGuards.EnsureOutputPixels(width, height, PngOutputLimitMessage);
        if (scanlinesWithFilter is null) throw new ArgumentNullException(nameof(scanlinesWithFilter));
        if (length < 0 || length > scanlinesWithFilter.Length) throw new ArgumentOutOfRangeException(nameof(length));
        var rowBytes = RenderGuards.EnsureOutputBytes(((long)width + 7) / 8, PngOutputLimitMessage);
        var expectedLength = RenderGuards.EnsureOutputBytes((long)height * (rowBytes + 1), PngOutputLimitMessage);
        if (length != expectedLength)
            throw new ArgumentException("Invalid scanline buffer length.", nameof(scanlinesWithFilter));

        if (stream is null) throw new ArgumentNullException(nameof(stream));

        stream.Write(Signature, 0, Signature.Length);
        WriteChunk(stream, "IHDR", BuildIHDR(width, height, bitDepth: 1, colorType: 0));

        var normalizedLevel = NormalizeCompressionLevel(compressionLevel);
        if (normalizedLevel <= 0) {
            var idatLength = GetZlibStoredLength(length);
            WriteChunk(stream, "IDAT", idatLength, (Stream s, ref uint crc) => WriteZlibStored(s, scanlinesWithFilter, length, ref crc));
        } else {
            var compressed = ZlibCompress(scanlinesWithFilter, length, normalizedLevel);
            WriteChunk(stream, "IDAT", compressed);
        }
        WriteChunk(stream, "IEND", Array.Empty<byte>());
    }

    public static byte[] WriteIndexed1(int width, int height, byte[] scanlinesWithFilter, int length, byte[] palette) {
        var idatLength = GetZlibStoredLength(length);
        using var ms = new MemoryStream(EstimateTotalLength(idatLength, hasPlte: true));
        WriteIndexed1(ms, width, height, scanlinesWithFilter, length, palette, compressionLevel: 0);
        return ms.ToArray();
    }

    public static byte[] WriteIndexed1(int width, int height, byte[] scanlinesWithFilter, int length, byte[] palette, int compressionLevel) {
        var normalizedLevel = NormalizeCompressionLevel(compressionLevel);
        var idatLength = normalizedLevel <= 0 ? GetZlibStoredLength(length) : Math.Max(length, 0);
        using var ms = new MemoryStream(EstimateTotalLength(idatLength, hasPlte: true));
        WriteIndexed1(ms, width, height, scanlinesWithFilter, length, palette, normalizedLevel);
        return ms.ToArray();
    }

    public static void WriteIndexed1(Stream stream, int width, int height, byte[] scanlinesWithFilter, int length, byte[] palette) {
        WriteIndexed1(stream, width, height, scanlinesWithFilter, length, palette, compressionLevel: 0);
    }

    public static void WriteIndexed1(Stream stream, int width, int height, byte[] scanlinesWithFilter, int length, byte[] palette, int compressionLevel) {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        _ = RenderGuards.EnsureOutputPixels(width, height, PngOutputLimitMessage);
        if (scanlinesWithFilter is null) throw new ArgumentNullException(nameof(scanlinesWithFilter));
        if (length < 0 || length > scanlinesWithFilter.Length) throw new ArgumentOutOfRangeException(nameof(length));
        if (palette is null) throw new ArgumentNullException(nameof(palette));
        if (palette.Length != 6) throw new ArgumentException("Palette must contain exactly two RGB entries (6 bytes).", nameof(palette));
        var rowBytes = RenderGuards.EnsureOutputBytes(((long)width + 7) / 8, PngOutputLimitMessage);
        var expectedLength = RenderGuards.EnsureOutputBytes((long)height * (rowBytes + 1), PngOutputLimitMessage);
        if (length != expectedLength)
            throw new ArgumentException("Invalid scanline buffer length.", nameof(scanlinesWithFilter));

        if (stream is null) throw new ArgumentNullException(nameof(stream));

        stream.Write(Signature, 0, Signature.Length);
        WriteChunk(stream, "IHDR", BuildIHDR(width, height, bitDepth: 1, colorType: 3));
        WriteChunk(stream, "PLTE", palette);

        var normalizedLevel = NormalizeCompressionLevel(compressionLevel);
        if (normalizedLevel <= 0) {
            var idatLength = GetZlibStoredLength(length);
            WriteChunk(stream, "IDAT", idatLength, (Stream s, ref uint crc) => WriteZlibStored(s, scanlinesWithFilter, length, ref crc));
        } else {
            var compressed = ZlibCompress(scanlinesWithFilter, length, normalizedLevel);
            WriteChunk(stream, "IDAT", compressed);
        }
        WriteChunk(stream, "IEND", Array.Empty<byte>());
    }

    private static readonly byte[] Signature = { 137, 80, 78, 71, 13, 10, 26, 10 };

    private static byte[] BuildIHDR(int width, int height, byte bitDepth, byte colorType) {
        var data = new byte[13];
        WriteUInt32BE(data, 0, (uint)width);
        WriteUInt32BE(data, 4, (uint)height);
        data[8] = bitDepth;
        data[9] = colorType;
        data[10] = 0; // compression
        data[11] = 0; // filter
        data[12] = 0; // interlace
        return data;
    }

    private static void WriteChunk(Stream s, string type, byte[] data) {
        data ??= Array.Empty<byte>();
        WriteChunk(s, type, data.Length, (Stream stream, ref uint crc) => {
            if (data.Length == 0) return;
            stream.Write(data, 0, data.Length);
            crc = UpdateCrc(crc, data, 0, data.Length);
        });
    }

    private delegate void ChunkWriter(Stream stream, ref uint crc);

    private static void WriteChunk(Stream s, string type, int dataLength, ChunkWriter writeData) {
        if (type.Length != 4) throw new ArgumentOutOfRangeException(nameof(type));

        WriteUInt32BE(s, (uint)dataLength);
        var typeBytes = GetChunkTypeBytes(type);
        s.Write(typeBytes, 0, typeBytes.Length);

        var crc = 0xFFFFFFFFu;
        crc = UpdateCrc(crc, typeBytes, 0, typeBytes.Length);
        if (dataLength != 0) {
            writeData(s, ref crc);
        }
        crc = ~crc;
        WriteUInt32BE(s, crc);
    }

    private static byte[] GetChunkTypeBytes(string type) {
        return type switch {
            "IHDR" => ChunkIHDR,
            "IDAT" => ChunkIDAT,
            "IEND" => ChunkIEND,
            "PLTE" => ChunkPLTE,
            _ => new byte[] { (byte)type[0], (byte)type[1], (byte)type[2], (byte)type[3] }
        };
    }

    private static uint UpdateCrc(uint crc, byte[] data, int offset, int count) {
        for (var i = 0; i < count; i++) {
            crc = (crc >> 8) ^ CrcTable[(crc ^ data[offset + i]) & 0xFF];
        }
        return crc;
    }

    private static uint[] BuildCrcTable() {
        var table = new uint[256];
        for (uint i = 0; i < 256; i++) {
            var c = i;
            for (var k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : (c >> 1);
            table[i] = c;
        }
        return table;
    }

    private static int GetZlibStoredLength(int dataLength) {
        if (dataLength < 0) throw new ArgumentOutOfRangeException(nameof(dataLength));
        var blocks = (dataLength + 65534) / 65535;
        return 2 + 4 + dataLength + blocks * 5;
    }

    private static int EstimateTotalLength(int idatLength, bool hasPlte) {
        // PNG layout: signature (8) + IHDR (25) + [PLTE (18)] + IDAT (12 + data) + IEND (12)
        var total = 57 + idatLength;
        if (hasPlte) total += 18;
        return total;
    }

    private static void WriteZlibStored(Stream stream, byte[] uncompressed, int length, ref uint crc) {
        uint a = 1;
        uint b = 0;

        WriteByteWithCrc(stream, 0x78, ref crc);
        WriteByteWithCrc(stream, 0x01, ref crc);

        var offset = 0;
        while (offset < length) {
            var remaining = length - offset;
            var len = Math.Min(65535, remaining);
            var final = offset + len >= length;

            WriteByteWithCrc(stream, final ? (byte)0x01 : (byte)0x00, ref crc);

            WriteByteWithCrc(stream, (byte)(len & 0xFF), ref crc);
            WriteByteWithCrc(stream, (byte)((len >> 8) & 0xFF), ref crc);
            var nlen = (~len) & 0xFFFF;
            WriteByteWithCrc(stream, (byte)(nlen & 0xFF), ref crc);
            WriteByteWithCrc(stream, (byte)((nlen >> 8) & 0xFF), ref crc);

            stream.Write(uncompressed, offset, len);
            UpdateCrcAndAdler(uncompressed, offset, len, ref crc, ref a, ref b);

            offset += len;
        }

        var adler = (b << 16) | a;
        WriteUInt32BEWithCrc(stream, adler, ref crc);
    }

    private static void WriteZlibStoredRows(Stream stream, int height, int rowLength, RowWriter fillRow, ref uint crc) {
        uint a = 1;
        uint b = 0;

        WriteByteWithCrc(stream, 0x78, ref crc);
        WriteByteWithCrc(stream, 0x01, ref crc);

        var rowBuffer = ArrayPool<byte>.Shared.Rent(rowLength);
        try {
            for (var y = 0; y < height; y++) {
                fillRow(y, rowBuffer, rowLength);
                var offset = 0;
                while (offset < rowLength) {
                    var remaining = rowLength - offset;
                    var len = Math.Min(65535, remaining);
                    var final = y == height - 1 && offset + len >= rowLength;

                    WriteByteWithCrc(stream, final ? (byte)0x01 : (byte)0x00, ref crc);
                    WriteByteWithCrc(stream, (byte)(len & 0xFF), ref crc);
                    WriteByteWithCrc(stream, (byte)((len >> 8) & 0xFF), ref crc);
                    var nlen = (~len) & 0xFFFF;
                    WriteByteWithCrc(stream, (byte)(nlen & 0xFF), ref crc);
                    WriteByteWithCrc(stream, (byte)((nlen >> 8) & 0xFF), ref crc);

                    stream.Write(rowBuffer, offset, len);
                    UpdateCrcAndAdler(rowBuffer, offset, len, ref crc, ref a, ref b);
                    offset += len;
                }
            }
        } finally {
            ArrayPool<byte>.Shared.Return(rowBuffer);
        }

        var adler = (b << 16) | a;
        WriteUInt32BEWithCrc(stream, adler, ref crc);
    }

    private static void UpdateCrcAndAdler(byte[] buffer, int offset, int count, ref uint crc, ref uint a, ref uint b) {
        var table = CrcTable;
        var end = offset + count;
        while (offset < end) {
            var n = Math.Min(AdlerNmax, end - offset);
            var limit = offset + n;
            while (offset < limit) {
                var value = buffer[offset++];
                crc = (crc >> 8) ^ table[(crc ^ value) & 0xFF];
                a += value;
                b += a;
            }
            a %= AdlerMod;
            b %= AdlerMod;
        }
    }

    private static int NormalizeCompressionLevel(int level) {
        if (level < 0) return 0;
        if (level > 9) return 9;
        return level;
    }

    private static CompressionLevel MapCompressionLevel(int level) {
#if NET8_0_OR_GREATER
        if (level <= 3) return CompressionLevel.Fastest;
        if (level >= 8) return CompressionLevel.SmallestSize;
        return CompressionLevel.Optimal;
#else
        return level <= 3 ? CompressionLevel.Fastest : CompressionLevel.Optimal;
#endif
    }

    private static byte[] ZlibCompress(byte[] buffer, int length, int level) {
        var normalized = NormalizeCompressionLevel(level);
        if (normalized <= 0) {
            throw new ArgumentOutOfRangeException(nameof(level));
        }

        using var ms = new MemoryStream();
#if NET8_0_OR_GREATER
        using (var z = new ZLibStream(ms, MapCompressionLevel(normalized), leaveOpen: true)) {
            z.Write(buffer, 0, length);
        }
#else
        WriteZlibHeader(ms, normalized);
        using (var deflate = new DeflateStream(ms, MapCompressionLevel(normalized), leaveOpen: true)) {
            deflate.Write(buffer, 0, length);
        }
        var adler = ComputeAdler32(buffer, length);
        WriteUInt32BE(ms, adler);
#endif
        return ms.ToArray();
    }

#if !NET8_0_OR_GREATER
    private static void WriteZlibHeader(Stream stream, int level) {
        stream.WriteByte(0x78);
        var flg = level <= 3 ? (byte)0x01 : level >= 7 ? (byte)0xDA : (byte)0x9C;
        stream.WriteByte(flg);
    }
#endif

    private static uint ComputeAdler32(byte[] buffer, int length) {
        uint a = 1;
        uint b = 0;
        var offset = 0;
        while (offset < length) {
            var n = Math.Min(AdlerNmax, length - offset);
            var limit = offset + n;
            while (offset < limit) {
                a += buffer[offset++];
                b += a;
            }
            a %= AdlerMod;
            b %= AdlerMod;
        }
        return (b << 16) | a;
    }

    private static void WriteUInt32BE(Stream s, uint value) {
        s.WriteByte((byte)((value >> 24) & 0xFF));
        s.WriteByte((byte)((value >> 16) & 0xFF));
        s.WriteByte((byte)((value >> 8) & 0xFF));
        s.WriteByte((byte)(value & 0xFF));
    }

    private static void WriteUInt32BEWithCrc(Stream s, uint value, ref uint crc) {
        WriteByteWithCrc(s, (byte)((value >> 24) & 0xFF), ref crc);
        WriteByteWithCrc(s, (byte)((value >> 16) & 0xFF), ref crc);
        WriteByteWithCrc(s, (byte)((value >> 8) & 0xFF), ref crc);
        WriteByteWithCrc(s, (byte)(value & 0xFF), ref crc);
    }

    private static void WriteByteWithCrc(Stream s, byte value, ref uint crc) {
        s.WriteByte(value);
        crc = (crc >> 8) ^ CrcTable[(crc ^ value) & 0xFF];
    }

    private static void WriteUInt32BE(byte[] buffer, int offset, uint value) {
        buffer[offset + 0] = (byte)((value >> 24) & 0xFF);
        buffer[offset + 1] = (byte)((value >> 16) & 0xFF);
        buffer[offset + 2] = (byte)((value >> 8) & 0xFF);
        buffer[offset + 3] = (byte)(value & 0xFF);
    }
}
