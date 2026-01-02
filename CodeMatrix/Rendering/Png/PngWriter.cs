using System;
using System.IO;

namespace CodeMatrix.Rendering.Png;

internal static class PngWriter {
    private static readonly uint[] CrcTable = BuildCrcTable();

    public static byte[] WriteRgba8(int width, int height, byte[] scanlinesWithFilter) {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (scanlinesWithFilter is null) throw new ArgumentNullException(nameof(scanlinesWithFilter));
        var stride = width * 4;
        if (scanlinesWithFilter.Length != height * (stride + 1))
            throw new ArgumentException("Invalid scanline buffer length.", nameof(scanlinesWithFilter));

        var idat = BuildZlibStored(scanlinesWithFilter);

        using var ms = new MemoryStream();
        ms.Write(Signature, 0, Signature.Length);
        WriteChunk(ms, "IHDR", BuildIHDR(width, height));
        WriteChunk(ms, "IDAT", idat);
        WriteChunk(ms, "IEND", Array.Empty<byte>());
        return ms.ToArray();
    }

    private static readonly byte[] Signature = { 137, 80, 78, 71, 13, 10, 26, 10 };

    private static byte[] BuildIHDR(int width, int height) {
        var data = new byte[13];
        WriteUInt32BE(data, 0, (uint)width);
        WriteUInt32BE(data, 4, (uint)height);
        data[8] = 8;  // bit depth
        data[9] = 6;  // color type: RGBA
        data[10] = 0; // compression
        data[11] = 0; // filter
        data[12] = 0; // interlace
        return data;
    }

    private static void WriteChunk(Stream s, string type, byte[] data) {
        if (type.Length != 4) throw new ArgumentOutOfRangeException(nameof(type));
        data ??= Array.Empty<byte>();

        WriteUInt32BE(s, (uint)data.Length);
        var typeBytes = new byte[] { (byte)type[0], (byte)type[1], (byte)type[2], (byte)type[3] };
        s.Write(typeBytes, 0, typeBytes.Length);
        if (data.Length != 0) s.Write(data, 0, data.Length);

        var crc = ComputeCrc(typeBytes, data);
        WriteUInt32BE(s, crc);
    }

    private static uint ComputeCrc(byte[] type, byte[] data) {
        var crc = 0xFFFFFFFFu;
        for (var i = 0; i < type.Length; i++) crc = (crc >> 8) ^ CrcTable[(crc ^ type[i]) & 0xFF];
        for (var i = 0; i < data.Length; i++) crc = (crc >> 8) ^ CrcTable[(crc ^ data[i]) & 0xFF];
        return ~crc;
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

    private static byte[] BuildZlibStored(byte[] uncompressed) {
        var adler = Adler32(uncompressed);

        using var ms = new MemoryStream();
        ms.WriteByte(0x78);
        ms.WriteByte(0x01);

        var offset = 0;
        while (offset < uncompressed.Length) {
            var remaining = uncompressed.Length - offset;
            var len = Math.Min(65535, remaining);
            var final = offset + len >= uncompressed.Length;

            ms.WriteByte(final ? (byte)0x01 : (byte)0x00); // BFINAL + BTYPE=00

            // LEN + NLEN (little endian)
            ms.WriteByte((byte)(len & 0xFF));
            ms.WriteByte((byte)((len >> 8) & 0xFF));
            var nlen = (~len) & 0xFFFF;
            ms.WriteByte((byte)(nlen & 0xFF));
            ms.WriteByte((byte)((nlen >> 8) & 0xFF));

            ms.Write(uncompressed, offset, len);
            offset += len;
        }

        WriteUInt32BE(ms, adler);
        return ms.ToArray();
    }

    private static uint Adler32(byte[] data) {
        const uint mod = 65521;
        uint a = 1;
        uint b = 0;
        for (var i = 0; i < data.Length; i++) {
            a += data[i];
            if (a >= mod) a -= mod;
            b += a;
            b %= mod;
        }
        return (b << 16) | a;
    }

    private static void WriteUInt32BE(Stream s, uint value) {
        s.WriteByte((byte)((value >> 24) & 0xFF));
        s.WriteByte((byte)((value >> 16) & 0xFF));
        s.WriteByte((byte)((value >> 8) & 0xFF));
        s.WriteByte((byte)(value & 0xFF));
    }

    private static void WriteUInt32BE(byte[] buffer, int offset, uint value) {
        buffer[offset + 0] = (byte)((value >> 24) & 0xFF);
        buffer[offset + 1] = (byte)((value >> 16) & 0xFF);
        buffer[offset + 2] = (byte)((value >> 8) & 0xFF);
        buffer[offset + 3] = (byte)(value & 0xFF);
    }
}

