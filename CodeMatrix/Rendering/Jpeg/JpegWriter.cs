using System;
using System.IO;

namespace CodeMatrix.Rendering.Jpeg;

internal static class JpegWriter {
    private static readonly byte[] ZigZag = {
        0, 1, 5, 6, 14, 15, 27, 28,
        2, 4, 7, 13, 16, 26, 29, 42,
        3, 8, 12, 17, 25, 30, 41, 43,
        9, 11, 18, 24, 31, 40, 44, 53,
        10, 19, 23, 32, 39, 45, 52, 54,
        20, 22, 33, 38, 46, 51, 55, 60,
        21, 34, 37, 47, 50, 56, 59, 61,
        35, 36, 48, 49, 57, 58, 62, 63,
    };

    private static readonly byte[] StdLumaQuant = {
        16, 11, 10, 16, 24, 40, 51, 61,
        12, 12, 14, 19, 26, 58, 60, 55,
        14, 13, 16, 24, 40, 57, 69, 56,
        14, 17, 22, 29, 51, 87, 80, 62,
        18, 22, 37, 56, 68, 109, 103, 77,
        24, 35, 55, 64, 81, 104, 113, 92,
        49, 64, 78, 87, 103, 121, 120, 101,
        72, 92, 95, 98, 112, 100, 103, 99,
    };

    private static readonly byte[] StdChromaQuant = {
        17, 18, 24, 47, 99, 99, 99, 99,
        18, 21, 26, 66, 99, 99, 99, 99,
        24, 26, 56, 99, 99, 99, 99, 99,
        47, 66, 99, 99, 99, 99, 99, 99,
        99, 99, 99, 99, 99, 99, 99, 99,
        99, 99, 99, 99, 99, 99, 99, 99,
        99, 99, 99, 99, 99, 99, 99, 99,
        99, 99, 99, 99, 99, 99, 99, 99,
    };

    private static readonly byte[] DcLumaBits = { 0, 1, 5, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0 };
    private static readonly byte[] DcChromaBits = { 0, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0 };
    private static readonly byte[] DcValues = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };

    private static readonly byte[] AcLumaBits = { 0, 2, 1, 3, 3, 2, 4, 3, 5, 5, 4, 4, 0, 0, 1, 0x7d };
    private static readonly byte[] AcChromaBits = { 0, 2, 1, 2, 4, 4, 3, 4, 7, 5, 4, 4, 0, 1, 2, 0x77 };

    private static readonly byte[] AcLumaValues = {
        0x01, 0x02, 0x03, 0x00, 0x04, 0x11, 0x05, 0x12, 0x21, 0x31, 0x41, 0x06, 0x13, 0x51, 0x61, 0x07,
        0x22, 0x71, 0x14, 0x32, 0x81, 0x91, 0xA1, 0x08, 0x23, 0x42, 0xB1, 0xC1, 0x15, 0x52, 0xD1, 0xF0,
        0x24, 0x33, 0x62, 0x72, 0x82, 0x09, 0x0A, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x25, 0x26, 0x27, 0x28,
        0x29, 0x2A, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49,
        0x4A, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5A, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69,
        0x6A, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7A, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89,
        0x8A, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9A, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7,
        0xA8, 0xA9, 0xAA, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xC2, 0xC3, 0xC4, 0xC5,
        0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xE1, 0xE2,
        0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8,
        0xF9, 0xFA,
    };

    private static readonly byte[] AcChromaValues = {
        0x00, 0x01, 0x02, 0x03, 0x11, 0x04, 0x05, 0x21, 0x31, 0x06, 0x12, 0x41, 0x51, 0x07, 0x61, 0x71,
        0x13, 0x22, 0x32, 0x81, 0x08, 0x14, 0x42, 0x91, 0xA1, 0xB1, 0xC1, 0x09, 0x23, 0x33, 0x52, 0xF0,
        0x15, 0x62, 0x72, 0xD1, 0x0A, 0x16, 0x24, 0x34, 0xE1, 0x25, 0xF1, 0x17, 0x18, 0x19, 0x1A, 0x26,
        0x27, 0x28, 0x29, 0x2A, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
        0x49, 0x4A, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5A, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
        0x69, 0x6A, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7A, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87,
        0x88, 0x89, 0x8A, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9A, 0xA2, 0xA3, 0xA4, 0xA5,
        0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xC2, 0xC3,
        0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA,
        0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8,
        0xF9, 0xFA,
    };

    private static readonly double[,] CosTable = BuildCosTable();

    public static byte[] WriteRgba(int width, int height, byte[] rgba, int stride, int quality) {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (rgba is null) throw new ArgumentNullException(nameof(rgba));
        if (stride < width * 4) throw new ArgumentOutOfRangeException(nameof(stride));
        if (rgba.Length < height * stride) throw new ArgumentException("RGBA buffer too small.", nameof(rgba));
        if (quality is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(quality));

        var qY = ScaleQuantTable(StdLumaQuant, quality);
        var qC = ScaleQuantTable(StdChromaQuant, quality);

        var dcLuma = BuildHuffmanTable(DcLumaBits, DcValues);
        var acLuma = BuildHuffmanTable(AcLumaBits, AcLumaValues);
        var dcChroma = BuildHuffmanTable(DcChromaBits, DcValues);
        var acChroma = BuildHuffmanTable(AcChromaBits, AcChromaValues);

        using var ms = new MemoryStream();
        WriteMarker(ms, 0xFFD8);
        WriteApp0(ms);
        WriteDqt(ms, 0, qY);
        WriteDqt(ms, 1, qC);
        WriteSof0(ms, width, height);
        WriteDht(ms, 0, 0, DcLumaBits, DcValues);
        WriteDht(ms, 1, 0, AcLumaBits, AcLumaValues);
        WriteDht(ms, 0, 1, DcChromaBits, DcValues);
        WriteDht(ms, 1, 1, AcChromaBits, AcChromaValues);
        WriteSos(ms);

        var bw = new BitWriter(ms);
        EncodeImage(bw, width, height, rgba, stride, qY, qC, dcLuma, acLuma, dcChroma, acChroma);
        bw.Flush();

        WriteMarker(ms, 0xFFD9);
        return ms.ToArray();
    }

    private static void EncodeImage(
        BitWriter bw,
        int width,
        int height,
        byte[] rgba,
        int stride,
        int[] qY,
        int[] qC,
        HuffmanTable dcY,
        HuffmanTable acY,
        HuffmanTable dcC,
        HuffmanTable acC) {
        var blockY = new int[64];
        var blockCb = new int[64];
        var blockCr = new int[64];
        var temp = new int[64];

        var prevY = 0;
        var prevCb = 0;
        var prevCr = 0;

        for (var by = 0; by < height; by += 8) {
            for (var bx = 0; bx < width; bx += 8) {
                LoadBlock(rgba, stride, width, height, bx, by, blockY, blockCb, blockCr);

                EncodeBlock(bw, blockY, qY, dcY, acY, ref prevY, temp);
                EncodeBlock(bw, blockCb, qC, dcC, acC, ref prevCb, temp);
                EncodeBlock(bw, blockCr, qC, dcC, acC, ref prevCr, temp);
            }
        }
    }

    private static void LoadBlock(
        byte[] rgba,
        int stride,
        int width,
        int height,
        int bx,
        int by,
        int[] yBlock,
        int[] cbBlock,
        int[] crBlock) {
        var i = 0;
        for (var y = 0; y < 8; y++) {
            var py = by + y;
            if (py >= height) py = height - 1;
            var row = py * stride;
            for (var x = 0; x < 8; x++) {
                var px = bx + x;
                if (px >= width) px = width - 1;
                var p = row + px * 4;
                var r = rgba[p + 0];
                var g = rgba[p + 1];
                var b = rgba[p + 2];
                var a = rgba[p + 3];
                if (a != 255) {
                    var inv = 255 - a;
                    r = (byte)((r * a + 255 * inv + 127) / 255);
                    g = (byte)((g * a + 255 * inv + 127) / 255);
                    b = (byte)((b * a + 255 * inv + 127) / 255);
                }

                var yv = (77 * r + 150 * g + 29 * b + 128) >> 8;
                var cb = ((-43 * r - 85 * g + 128 * b + 128) >> 8) + 128;
                var cr = ((128 * r - 107 * g - 21 * b + 128) >> 8) + 128;

                yBlock[i] = yv - 128;
                cbBlock[i] = cb - 128;
                crBlock[i] = cr - 128;
                i++;
            }
        }
    }

    private static void EncodeBlock(
        BitWriter bw,
        int[] input,
        int[] quant,
        HuffmanTable dcTable,
        HuffmanTable acTable,
        ref int prevDc,
        int[] temp) {
        ForwardDctQuantize(input, quant, temp);

        var dc = temp[0];
        var diff = dc - prevDc;
        prevDc = dc;
        var dcCat = BitCount(diff);
        bw.WriteBits(dcTable.Codes[dcCat], dcTable.Sizes[dcCat]);
        if (dcCat > 0) {
            bw.WriteBits(EncodeValue(diff, dcCat), dcCat);
        }

        var zeroRun = 0;
        for (var i = 1; i < 64; i++) {
            var v = temp[ZigZag[i]];
            if (v == 0) {
                zeroRun++;
                continue;
            }

            while (zeroRun >= 16) {
                bw.WriteBits(acTable.Codes[0xF0], acTable.Sizes[0xF0]);
                zeroRun -= 16;
            }

            var cat = BitCount(v);
            var symbol = (zeroRun << 4) | cat;
            bw.WriteBits(acTable.Codes[symbol], acTable.Sizes[symbol]);
            bw.WriteBits(EncodeValue(v, cat), cat);
            zeroRun = 0;
        }

        if (zeroRun > 0) {
            bw.WriteBits(acTable.Codes[0x00], acTable.Sizes[0x00]);
        }
    }

    private static void ForwardDctQuantize(int[] input, int[] quant, int[] output) {
        const double invSqrt2 = 0.7071067811865476;
        for (var u = 0; u < 8; u++) {
            var cu = u == 0 ? invSqrt2 : 1.0;
            for (var v = 0; v < 8; v++) {
                var cv = v == 0 ? invSqrt2 : 1.0;
                double sum = 0;
                for (var x = 0; x < 8; x++) {
                    for (var y = 0; y < 8; y++) {
                        sum += input[y * 8 + x] * CosTable[u, x] * CosTable[v, y];
                    }
                }
                var coeff = 0.25 * cu * cv * sum;
                var idx = u * 8 + v;
                output[idx] = (int)Math.Round(coeff / quant[idx]);
            }
        }
    }

    private static int BitCount(int value) {
        var v = value < 0 ? -value : value;
        var bits = 0;
        while (v != 0) {
            bits++;
            v >>= 1;
        }
        return bits;
    }

    private static uint EncodeValue(int value, int bits) {
        if (value >= 0) return (uint)value;
        return (uint)(value + (1 << bits) - 1);
    }

    private static int[] ScaleQuantTable(byte[] table, int quality) {
        var scale = quality < 50 ? 5000 / quality : 200 - quality * 2;
        var outTable = new int[64];
        for (var i = 0; i < 64; i++) {
            var val = (table[i] * scale + 50) / 100;
            if (val < 1) val = 1;
            if (val > 255) val = 255;
            outTable[i] = val;
        }
        return outTable;
    }

    private static void WriteApp0(Stream s) {
        WriteMarker(s, 0xFFE0);
        WriteUInt16(s, 16);
        s.WriteByte((byte)'J');
        s.WriteByte((byte)'F');
        s.WriteByte((byte)'I');
        s.WriteByte((byte)'F');
        s.WriteByte(0);
        s.WriteByte(1);
        s.WriteByte(1);
        s.WriteByte(0);
        WriteUInt16(s, 1);
        WriteUInt16(s, 1);
        s.WriteByte(0);
        s.WriteByte(0);
    }

    private static void WriteDqt(Stream s, int tableId, int[] table) {
        WriteMarker(s, 0xFFDB);
        WriteUInt16(s, 65);
        s.WriteByte((byte)tableId);
        for (var i = 0; i < 64; i++) {
            s.WriteByte((byte)table[ZigZag[i]]);
        }
    }

    private static void WriteSof0(Stream s, int width, int height) {
        WriteMarker(s, 0xFFC0);
        WriteUInt16(s, 17);
        s.WriteByte(8);
        WriteUInt16(s, (ushort)height);
        WriteUInt16(s, (ushort)width);
        s.WriteByte(3);
        s.WriteByte(1);
        s.WriteByte(0x11);
        s.WriteByte(0);
        s.WriteByte(2);
        s.WriteByte(0x11);
        s.WriteByte(1);
        s.WriteByte(3);
        s.WriteByte(0x11);
        s.WriteByte(1);
    }

    private static void WriteDht(Stream s, int tableClass, int tableId, byte[] bits, byte[] values) {
        WriteMarker(s, 0xFFC4);
        WriteUInt16(s, (ushort)(1 + 16 + values.Length));
        s.WriteByte((byte)((tableClass << 4) | tableId));
        for (var i = 0; i < 16; i++) s.WriteByte(bits[i]);
        s.Write(values, 0, values.Length);
    }

    private static void WriteSos(Stream s) {
        WriteMarker(s, 0xFFDA);
        WriteUInt16(s, 12);
        s.WriteByte(3);
        s.WriteByte(1);
        s.WriteByte(0x00);
        s.WriteByte(2);
        s.WriteByte(0x11);
        s.WriteByte(3);
        s.WriteByte(0x11);
        s.WriteByte(0);
        s.WriteByte(63);
        s.WriteByte(0);
    }

    private static HuffmanTable BuildHuffmanTable(byte[] bits, byte[] values) {
        var sizes = new byte[256];
        var codes = new ushort[256];
        var code = 0;
        var k = 0;
        for (var i = 1; i <= 16; i++) {
            var count = bits[i - 1];
            for (var j = 0; j < count; j++) {
                var val = values[k++];
                sizes[val] = (byte)i;
                codes[val] = (ushort)code;
                code++;
            }
            code <<= 1;
        }
        return new HuffmanTable(codes, sizes);
    }

    private static double[,] BuildCosTable() {
        var table = new double[8, 8];
        for (var u = 0; u < 8; u++) {
            for (var x = 0; x < 8; x++) {
                table[u, x] = Math.Cos(((2 * x + 1) * u * Math.PI) / 16.0);
            }
        }
        return table;
    }

    private static void WriteMarker(Stream s, int marker) {
        s.WriteByte(0xFF);
        s.WriteByte((byte)(marker & 0xFF));
    }

    private static void WriteUInt16(Stream s, ushort value) {
        s.WriteByte((byte)(value >> 8));
        s.WriteByte((byte)(value & 0xFF));
    }

    private readonly struct HuffmanTable {
        public readonly ushort[] Codes;
        public readonly byte[] Sizes;
        public HuffmanTable(ushort[] codes, byte[] sizes) {
            Codes = codes;
            Sizes = sizes;
        }
    }

    private sealed class BitWriter {
        private readonly Stream _stream;
        private uint _buffer;
        private int _bits;

        public BitWriter(Stream stream) {
            _stream = stream;
        }

        public void WriteBits(uint bits, int count) {
            _buffer = (_buffer << count) | (bits & ((1u << count) - 1));
            _bits += count;
            while (_bits >= 8) {
                var b = (byte)((_buffer >> (_bits - 8)) & 0xFF);
                WriteByte(b);
                _bits -= 8;
            }
        }

        public void Flush() {
            if (_bits <= 0) return;
            var b = (byte)((_buffer << (8 - _bits)) & 0xFF);
            WriteByte(b);
            _bits = 0;
        }

        private void WriteByte(byte b) {
            _stream.WriteByte(b);
            if (b == 0xFF) _stream.WriteByte(0x00);
        }
    }
}
