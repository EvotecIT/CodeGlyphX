using System;
using System.Buffers;
using System.IO;

namespace CodeGlyphX.Rendering.Png;

internal static class PngWriter {
    private static readonly uint[] CrcTable = BuildCrcTable();
    internal delegate void RowWriter(int y, byte[] rowBuffer, int rowLength);

    public static byte[] WriteRgba8(int width, int height, byte[] scanlinesWithFilter) {
        using var ms = new MemoryStream();
        WriteRgba8(ms, width, height, scanlinesWithFilter, scanlinesWithFilter.Length);
        return ms.ToArray();
    }

    public static byte[] WriteRgba8(int width, int height, byte[] scanlinesWithFilter, int length) {
        using var ms = new MemoryStream();
        WriteRgba8(ms, width, height, scanlinesWithFilter, length);
        return ms.ToArray();
    }

    public static void WriteRgba8(Stream stream, int width, int height, byte[] scanlinesWithFilter) {
        WriteRgba8(stream, width, height, scanlinesWithFilter, scanlinesWithFilter.Length);
    }

    public static void WriteRgba8(Stream stream, int width, int height, byte[] scanlinesWithFilter, int length) {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (scanlinesWithFilter is null) throw new ArgumentNullException(nameof(scanlinesWithFilter));
        if (length < 0 || length > scanlinesWithFilter.Length) throw new ArgumentOutOfRangeException(nameof(length));
        var stride = width * 4;
        if (length != height * (stride + 1))
            throw new ArgumentException("Invalid scanline buffer length.", nameof(scanlinesWithFilter));

        if (stream is null) throw new ArgumentNullException(nameof(stream));

        stream.Write(Signature, 0, Signature.Length);
        WriteChunk(stream, "IHDR", BuildIHDR(width, height, bitDepth: 8, colorType: 6));

        var idatLength = GetZlibStoredLength(length);
        WriteChunk(stream, "IDAT", idatLength, (Stream s, ref uint crc) => WriteZlibStored(s, scanlinesWithFilter, length, ref crc));
        WriteChunk(stream, "IEND", Array.Empty<byte>());
    }

    public static void WriteRgba8(Stream stream, int width, int height, RowWriter fillRow) {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (fillRow is null) throw new ArgumentNullException(nameof(fillRow));
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        var stride = width * 4;
        var rowLength = stride + 1;
        var length = height * rowLength;

        stream.Write(Signature, 0, Signature.Length);
        WriteChunk(stream, "IHDR", BuildIHDR(width, height, bitDepth: 8, colorType: 6));

        var idatLength = GetZlibStoredLength(length);
        WriteChunk(stream, "IDAT", idatLength, (Stream s, ref uint crc) => WriteZlibStoredRows(s, height, rowLength, fillRow, ref crc));
        WriteChunk(stream, "IEND", Array.Empty<byte>());
    }

    public static byte[] WriteGray1(int width, int height, byte[] scanlinesWithFilter, int length) {
        using var ms = new MemoryStream();
        WriteGray1(ms, width, height, scanlinesWithFilter, length);
        return ms.ToArray();
    }

    public static void WriteGray1(Stream stream, int width, int height, byte[] scanlinesWithFilter, int length) {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (scanlinesWithFilter is null) throw new ArgumentNullException(nameof(scanlinesWithFilter));
        if (length < 0 || length > scanlinesWithFilter.Length) throw new ArgumentOutOfRangeException(nameof(length));
        var rowBytes = (width + 7) / 8;
        if (length != height * (rowBytes + 1))
            throw new ArgumentException("Invalid scanline buffer length.", nameof(scanlinesWithFilter));

        if (stream is null) throw new ArgumentNullException(nameof(stream));

        stream.Write(Signature, 0, Signature.Length);
        WriteChunk(stream, "IHDR", BuildIHDR(width, height, bitDepth: 1, colorType: 0));

        var idatLength = GetZlibStoredLength(length);
        WriteChunk(stream, "IDAT", idatLength, (Stream s, ref uint crc) => WriteZlibStored(s, scanlinesWithFilter, length, ref crc));
        WriteChunk(stream, "IEND", Array.Empty<byte>());
    }

    public static byte[] WriteIndexed1(int width, int height, byte[] scanlinesWithFilter, int length, byte[] palette) {
        using var ms = new MemoryStream();
        WriteIndexed1(ms, width, height, scanlinesWithFilter, length, palette);
        return ms.ToArray();
    }

    public static void WriteIndexed1(Stream stream, int width, int height, byte[] scanlinesWithFilter, int length, byte[] palette) {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (scanlinesWithFilter is null) throw new ArgumentNullException(nameof(scanlinesWithFilter));
        if (length < 0 || length > scanlinesWithFilter.Length) throw new ArgumentOutOfRangeException(nameof(length));
        if (palette is null) throw new ArgumentNullException(nameof(palette));
        if (palette.Length != 6) throw new ArgumentException("Palette must contain exactly two RGB entries (6 bytes).", nameof(palette));
        var rowBytes = (width + 7) / 8;
        if (length != height * (rowBytes + 1))
            throw new ArgumentException("Invalid scanline buffer length.", nameof(scanlinesWithFilter));

        if (stream is null) throw new ArgumentNullException(nameof(stream));

        stream.Write(Signature, 0, Signature.Length);
        WriteChunk(stream, "IHDR", BuildIHDR(width, height, bitDepth: 1, colorType: 3));
        WriteChunk(stream, "PLTE", palette);

        var idatLength = GetZlibStoredLength(length);
        WriteChunk(stream, "IDAT", idatLength, (Stream s, ref uint crc) => WriteZlibStored(s, scanlinesWithFilter, length, ref crc));
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
        var typeBytes = new byte[] { (byte)type[0], (byte)type[1], (byte)type[2], (byte)type[3] };
        s.Write(typeBytes, 0, typeBytes.Length);

        var crc = 0xFFFFFFFFu;
        crc = UpdateCrc(crc, typeBytes, 0, typeBytes.Length);
        if (dataLength != 0) {
            writeData(s, ref crc);
        }
        crc = ~crc;
        WriteUInt32BE(s, crc);
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

    private static void WriteZlibStored(Stream stream, byte[] uncompressed, int length, ref uint crc) {
        const uint mod = 65521;
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
            for (var i = 0; i < len; i++) {
                var value = uncompressed[offset + i];
                crc = (crc >> 8) ^ CrcTable[(crc ^ value) & 0xFF];
                a += value;
                if (a >= mod) a -= mod;
                b += a;
                b %= mod;
            }

            offset += len;
        }

        var adler = (b << 16) | a;
        WriteUInt32BEWithCrc(stream, adler, ref crc);
    }

    private static void WriteZlibStoredRows(Stream stream, int height, int rowLength, RowWriter fillRow, ref uint crc) {
        const uint mod = 65521;
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
                    for (var i = 0; i < len; i++) {
                        var value = rowBuffer[offset + i];
                        crc = (crc >> 8) ^ CrcTable[(crc ^ value) & 0xFF];
                        a += value;
                        if (a >= mod) a -= mod;
                        b += a;
                        b %= mod;
                    }
                    offset += len;
                }
            }
        } finally {
            ArrayPool<byte>.Shared.Return(rowBuffer);
        }

        var adler = (b << 16) | a;
        WriteUInt32BEWithCrc(stream, adler, ref crc);
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
