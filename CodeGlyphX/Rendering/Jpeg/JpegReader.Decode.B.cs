using System;

namespace CodeGlyphX.Rendering.Jpeg;

public static partial class JpegReader {
    private static byte[] ComposeRgba(JpegFrame frame, BaselineComponentState[] states, int? adobeTransform) {
        var rgba = new byte[frame.Width * frame.Height * 4];
        var maxH = frame.MaxH;
        var maxV = frame.MaxV;

        if (frame.ComponentCount == 4) {
            var cIndex = FindComponentIndex(frame.Components, (byte)'C');
            var mIndex = FindComponentIndex(frame.Components, (byte)'M');
            var yIndex = FindComponentIndex(frame.Components, (byte)'Y');
            var kIndex = FindComponentIndex(frame.Components, (byte)'K');
            if (cIndex < 0 || mIndex < 0 || yIndex < 0 || kIndex < 0) {
                cIndex = FindComponentIndex(frame.Components, 1);
                mIndex = FindComponentIndex(frame.Components, 2);
                yIndex = FindComponentIndex(frame.Components, 3);
                kIndex = FindComponentIndex(frame.Components, 4);
                if (cIndex < 0 || mIndex < 0 || yIndex < 0 || kIndex < 0) {
                    cIndex = 0;
                    mIndex = 1;
                    yIndex = 2;
                    kIndex = 3;
                }
            }

            var isYcck = adobeTransform == 2;
            var ycckY = FindComponentIndex(frame.Components, 1);
            var ycckCb = FindComponentIndex(frame.Components, 2);
            var ycckCr = FindComponentIndex(frame.Components, 3);
            var ycckK = FindComponentIndex(frame.Components, 4);
            if (isYcck && (ycckY < 0 || ycckCb < 0 || ycckCr < 0 || ycckK < 0)) {
                ycckY = cIndex;
                ycckCb = mIndex;
                ycckCr = yIndex;
                ycckK = kIndex;
            }

            for (var y = 0; y < frame.Height; y++) {
                for (var x = 0; x < frame.Width; x++) {
                    byte r;
                    byte g;
                    byte b;
                    var kVal = SampleComponent(states, isYcck ? ycckK : kIndex, x, y, maxH, maxV, 0);

                    if (isYcck) {
                        var yVal = SampleComponent(states, ycckY, x, y, maxH, maxV, 128);
                        var cbVal = SampleComponent(states, ycckCb, x, y, maxH, maxV, 128);
                        var crVal = SampleComponent(states, ycckCr, x, y, maxH, maxV, 128);
                        r = ClampToByte(yVal + (1.402 * (crVal - 128)));
                        g = ClampToByte(yVal - (0.344136 * (cbVal - 128)) - (0.714136 * (crVal - 128)));
                        b = ClampToByte(yVal + (1.772 * (cbVal - 128)));
                    } else {
                        var c = SampleComponent(states, cIndex, x, y, maxH, maxV, 0);
                        var m = SampleComponent(states, mIndex, x, y, maxH, maxV, 0);
                        var y0 = SampleComponent(states, yIndex, x, y, maxH, maxV, 0);
                        r = ApplyCmyk(c, kVal);
                        g = ApplyCmyk(m, kVal);
                        b = ApplyCmyk(y0, kVal);
                    }

                    if (isYcck) {
                        var c = (byte)(255 - r);
                        var m = (byte)(255 - g);
                        var y0 = (byte)(255 - b);
                        r = ApplyCmyk(c, kVal);
                        g = ApplyCmyk(m, kVal);
                        b = ApplyCmyk(y0, kVal);
                    }

                    var p = (y * frame.Width + x) * 4;
                    rgba[p + 0] = r;
                    rgba[p + 1] = g;
                    rgba[p + 2] = b;
                    rgba[p + 3] = 255;
                }
            }

            return rgba;
        }

        if (frame.ComponentCount == 1) {
            var grayIndex = FindComponentIndex(frame.Components, 1);
            if (grayIndex < 0) grayIndex = 0;
            for (var y = 0; y < frame.Height; y++) {
                for (var x = 0; x < frame.Width; x++) {
                    var v = SampleComponent(states, grayIndex, x, y, maxH, maxV, 0);
                    var p = (y * frame.Width + x) * 4;
                    rgba[p + 0] = (byte)v;
                    rgba[p + 1] = (byte)v;
                    rgba[p + 2] = (byte)v;
                    rgba[p + 3] = 255;
                }
            }
            return rgba;
        }

        var rIndex = FindComponentIndex(frame.Components, (byte)'R');
        var gIndex = FindComponentIndex(frame.Components, (byte)'G');
        var bIndex = FindComponentIndex(frame.Components, (byte)'B');
        var rgb = rIndex >= 0 && gIndex >= 0 && bIndex >= 0;

        var yIndex2 = FindComponentIndex(frame.Components, 1);
        if (yIndex2 < 0) yIndex2 = 0;
        var cbIndex = frame.ComponentCount > 1 ? FindComponentIndex(frame.Components, 2) : -1;
        var crIndex = frame.ComponentCount > 1 ? FindComponentIndex(frame.Components, 3) : -1;
        if (frame.ComponentCount == 3) {
            if (cbIndex < 0) cbIndex = yIndex2 == 0 ? 1 : 0;
            if (crIndex < 0) crIndex = yIndex2 == 2 ? 1 : 2;
        }

        for (var y = 0; y < frame.Height; y++) {
            for (var x = 0; x < frame.Width; x++) {
                byte r;
                byte g;
                byte b;
                if (rgb) {
                    r = (byte)SampleComponent(states, rIndex, x, y, maxH, maxV, 0);
                    g = (byte)SampleComponent(states, gIndex, x, y, maxH, maxV, 0);
                    b = (byte)SampleComponent(states, bIndex, x, y, maxH, maxV, 0);
                } else {
                    var yVal = SampleComponent(states, yIndex2, x, y, maxH, maxV, 128);
                    var cbVal = SampleComponent(states, cbIndex, x, y, maxH, maxV, 128);
                    var crVal = SampleComponent(states, crIndex, x, y, maxH, maxV, 128);

                    r = ClampToByte(yVal + (1.402 * (crVal - 128)));
                    g = ClampToByte(yVal - (0.344136 * (cbVal - 128)) - (0.714136 * (crVal - 128)));
                    b = ClampToByte(yVal + (1.772 * (cbVal - 128)));
                }

                var p = (y * frame.Width + x) * 4;
                rgba[p + 0] = r;
                rgba[p + 1] = g;
                rgba[p + 2] = b;
                rgba[p + 3] = 255;
            }
        }

        return rgba;
    }

    private static int SampleComponent(
        BaselineComponentState[] states,
        int index,
        int x,
        int y,
        int maxH,
        int maxV,
        int fallback) {
        if (index < 0 || index >= states.Length) return fallback;
        var state = states[index];
        var sx = x * state.Component.H / maxH;
        var sy = y * state.Component.V / maxV;
        var stride = state.Stride;
        return state.Buffer[sy * stride + sx];
    }

    private static void DecodeBlock(
        JpegBitReader reader,
        HuffmanTable dcTable,
        HuffmanTable acTable,
        int[] quant,
        ref int prevDc,
        int[] coeffs,
        int[] pixels) {
        Array.Clear(coeffs, 0, 64);

        var t = DecodeHuffman(reader, dcTable);
        var diff = t == 0 ? 0 : Extend(reader.ReadBits(t), t);
        var dc = prevDc + diff;
        prevDc = dc;
        coeffs[0] = dc * quant[0];

        var k = 1;
        while (k < 64) {
            var rs = DecodeHuffman(reader, acTable);
            if (rs == 0) break;
            var r = rs >> 4;
            var s = rs & 0x0F;
            if (s == 0) {
                if (r == 15) {
                    k += 16;
                    continue;
                }
                break;
            }

            k += r;
            if (k >= 64) break;
            var ac = Extend(reader.ReadBits(s), s);
            var zig = ZigZag[k];
            coeffs[zig] = ac * quant[zig];
            k++;
        }

        InverseDct(coeffs, pixels);
    }

    private static int DecodeHuffman(JpegBitReader reader, HuffmanTable table) {
        var code = 0;
        for (var i = 1; i <= 16; i++) {
            code = (code << 1) | reader.ReadBit();
            if (table.MaxCode[i] < 0) continue;
            if (code <= table.MaxCode[i]) {
                var index = table.ValPtr[i] + (code - table.MinCode[i]);
                return table.Values[index];
            }
        }
        throw new FormatException("Invalid JPEG Huffman code.");
    }

    private static int Extend(int value, int bits) {
        if (bits == 0) return 0;
        var limit = 1 << (bits - 1);
        if (value < limit) value -= (1 << bits) - 1;
        return value;
    }

    private static void WriteBlock(int[] buffer, int stride, int blockX, int blockY, int[] pixels) {
        var baseX = blockX * 8;
        var baseY = blockY * 8;
        for (var y = 0; y < 8; y++) {
            var row = (baseY + y) * stride + baseX;
            var src = y * 8;
            for (var x = 0; x < 8; x++) {
                buffer[row + x] = pixels[src + x];
            }
        }
    }

    private static void InverseDct(int[] input, int[] output) {
        Span<double> temp = stackalloc double[64];
        for (var y = 0; y < 8; y++) {
            var row = y * 8;
            for (var x = 0; x < 8; x++) {
                double sum = 0;
                for (var u = 0; u < 8; u++) {
                    var cu = u == 0 ? 0.7071067811865476 : 1.0;
                    sum += cu * input[row + u] * IdctCos[x, u];
                }
                temp[row + x] = sum * 0.5;
            }
        }

        for (var x = 0; x < 8; x++) {
            for (var y = 0; y < 8; y++) {
                double sum = 0;
                for (var v = 0; v < 8; v++) {
                    var cv = v == 0 ? 0.7071067811865476 : 1.0;
                    sum += cv * temp[v * 8 + x] * IdctCos[y, v];
                }
                output[y * 8 + x] = ClampToInt(sum * 0.5 + 128.0);
            }
        }
    }

    private static byte ClampToByte(double value) {
        if (value <= 0) return 0;
        if (value >= 255) return 255;
        return (byte)(value + 0.5);
    }

    private static int ClampToInt(double value) {
        if (value <= 0) return 0;
        if (value >= 255) return 255;
        return (int)(value + 0.5);
    }

    private static JpegFrame ParseFrameHeader(ReadOnlySpan<byte> data) {
        var precision = data[0];
        if (precision != 8) throw new FormatException("Unsupported JPEG precision.");
        var height = ReadUInt16BE(data, 1);
        var width = ReadUInt16BE(data, 3);
        var components = data[5];
        if (width == 0 || height == 0) throw new FormatException("Invalid JPEG dimensions.");
        if (components == 0) throw new FormatException("Invalid JPEG component count.");
        if (data.Length < 6 + components * 3) throw new FormatException("Invalid JPEG SOF segment.");

        var frame = new JpegFrame {
            Width = width,
            Height = height,
            ComponentCount = components,
            Components = new Component[components]
        };

        var offset = 6;
        var maxH = 0;
        var maxV = 0;
        for (var i = 0; i < components; i++) {
            var id = data[offset++];
            var sampling = data[offset++];
            var h = sampling >> 4;
            var v = sampling & 0x0F;
            var qt = data[offset++];
            if (h == 0 || v == 0) throw new FormatException("Invalid JPEG sampling factors.");
            if (qt >= 4) throw new FormatException("Unsupported JPEG quantization table.");
            frame.Components[i] = new Component {
                Id = id,
                H = h,
                V = v,
                QuantId = qt
            };
            if (h > maxH) maxH = h;
            if (v > maxV) maxV = v;
        }
        frame.MaxH = maxH;
        frame.MaxV = maxV;
        return frame;
    }

    private static ScanHeader ParseScanHeader(ReadOnlySpan<byte> data, ref JpegFrame frame) {
        var components = data[0];
        if (components == 0) throw new FormatException("Invalid JPEG scan component count.");
        if (data.Length < 1 + components * 2 + 3) throw new FormatException("Invalid JPEG scan header.");

        var indices = new int[components];
        var offset = 1;
        for (var i = 0; i < components; i++) {
            var id = data[offset++];
            var table = data[offset++];
            var dc = table >> 4;
            var ac = table & 0x0F;
            var index = FindComponentIndex(frame.Components, id);
            if (index < 0) throw new FormatException("Unknown JPEG component in scan.");
            frame.Components[index].DcTable = (byte)dc;
            frame.Components[index].AcTable = (byte)ac;
            indices[i] = index;
        }

        var ss = data[offset++];
        var se = data[offset++];
        var ahal = data[offset++];

        return new ScanHeader {
            ComponentIndices = indices,
            Ss = ss,
            Se = se,
            Ah = (byte)(ahal >> 4),
            Al = (byte)(ahal & 0x0F)
        };
    }

    private static int FindComponentIndex(Component[] components, int id) {
        for (var i = 0; i < components.Length; i++) {
            if (components[i].Id == id) return i;
        }
        return -1;
    }

    private static int FindScanEnd(ReadOnlySpan<byte> data, int start) {
        var i = start;
        while (i + 1 < data.Length) {
            if (data[i] == 0xFF) {
                var next = data[i + 1];
                if (next == 0x00) {
                    i += 2;
                    continue;
                }
                if (next >= 0xD0 && next <= 0xD7) {
                    i += 2;
                    continue;
                }
                return i;
            }
            i++;
        }
        return data.Length;
    }

    private static bool TryReadExifOrientation(ReadOnlySpan<byte> data, out int orientation) {
        orientation = 1;
        if (data.Length < 6) return false;
        if (data[0] != (byte)'E' || data[1] != (byte)'x' || data[2] != (byte)'i' || data[3] != (byte)'f' || data[4] != 0 || data[5] != 0) {
            return false;
        }

        var tiff = data.Slice(6);
        if (tiff.Length < 8) return false;
        var little = tiff[0] == (byte)'I' && tiff[1] == (byte)'I';
        var big = tiff[0] == (byte)'M' && tiff[1] == (byte)'M';
        if (!little && !big) return false;
        if (ReadUInt16(tiff, 2, little) != 0x2A) return false;
        var ifdOffset = ReadUInt32(tiff, 4, little);
        if (ifdOffset > (uint)(tiff.Length - 2)) return false;
        var ifd = tiff.Slice((int)ifdOffset);
        var count = ReadUInt16(ifd, 0, little);
        var entriesOffset = 2;
        for (var i = 0; i < count; i++) {
            var entryOffset = entriesOffset + i * 12;
            if (entryOffset + 12 > ifd.Length) break;
            var tag = ReadUInt16(ifd, entryOffset, little);
            if (tag != 0x0112) continue;
            var type = ReadUInt16(ifd, entryOffset + 2, little);
            var entryCount = ReadUInt32(ifd, entryOffset + 4, little);
            if (type != 3 || entryCount != 1) break;
            var value = ReadUInt16(ifd, entryOffset + 8, little);
            if (value is >= 1 and <= 8) {
                orientation = value;
                return true;
            }
            break;
        }

        return false;
    }

    private static bool TryReadAdobeTransform(ReadOnlySpan<byte> data, out int transform) {
        transform = 0;
        if (data.Length < 12) return false;
        if (data[0] != (byte)'A' || data[1] != (byte)'d' || data[2] != (byte)'o' || data[3] != (byte)'b' || data[4] != (byte)'e') {
            return false;
        }
        transform = data[11];
        return true;
    }

    private static ushort ReadUInt16(ReadOnlySpan<byte> data, int offset, bool little) {
        return little
            ? (ushort)(data[offset] | (data[offset + 1] << 8))
            : (ushort)((data[offset] << 8) | data[offset + 1]);
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset, bool little) {
        return little
            ? (uint)(data[offset]
                     | (data[offset + 1] << 8)
                     | (data[offset + 2] << 16)
                     | (data[offset + 3] << 24))
            : (uint)((data[offset] << 24)
                     | (data[offset + 1] << 16)
                     | (data[offset + 2] << 8)
                     | data[offset + 3]);
    }

    private static byte[] ApplyOrientation(byte[] rgba, ref int width, ref int height, int orientation) {
        if (orientation <= 1) return rgba;
        var srcWidth = width;
        var srcHeight = height;
        var destWidth = (orientation >= 5 && orientation <= 8) ? srcHeight : srcWidth;
        var destHeight = (orientation >= 5 && orientation <= 8) ? srcWidth : srcHeight;
        var result = new byte[destWidth * destHeight * 4];

        for (var y = 0; y < destHeight; y++) {
            for (var x = 0; x < destWidth; x++) {
                int sx;
                int sy;
                switch (orientation) {
                    case 2:
                        sx = srcWidth - 1 - x;
                        sy = y;
                        break;
                    case 3:
                        sx = srcWidth - 1 - x;
                        sy = srcHeight - 1 - y;
                        break;
                    case 4:
                        sx = x;
                        sy = srcHeight - 1 - y;
                        break;
                    case 5:
                        sx = y;
                        sy = x;
                        break;
                    case 6:
                        sx = y;
                        sy = srcHeight - 1 - x;
                        break;
                    case 7:
                        sx = srcWidth - 1 - y;
                        sy = srcHeight - 1 - x;
                        break;
                    case 8:
                        sx = srcWidth - 1 - y;
                        sy = x;
                        break;
                    default:
                        sx = x;
                        sy = y;
                        break;
                }

                var srcIndex = (sy * srcWidth + sx) * 4;
                var dstIndex = (y * destWidth + x) * 4;
                result[dstIndex + 0] = rgba[srcIndex + 0];
                result[dstIndex + 1] = rgba[srcIndex + 1];
                result[dstIndex + 2] = rgba[srcIndex + 2];
                result[dstIndex + 3] = rgba[srcIndex + 3];
            }
        }

        width = destWidth;
        height = destHeight;
        return result;
    }

    private static double[,] BuildCosTable() {
        var table = new double[8, 8];
        for (var x = 0; x < 8; x++) {
            for (var u = 0; u < 8; u++) {
                table[x, u] = Math.Cos(((2 * x + 1) * u * Math.PI) / 16.0);
            }
        }
        return table;
    }

    private static ushort ReadUInt16BE(ReadOnlySpan<byte> data, int offset) {
        return (ushort)((data[offset] << 8) | data[offset + 1]);
    }

    private struct Component {
        public byte Id;
        public int H;
        public int V;
        public byte QuantId;
        public byte DcTable;
        public byte AcTable;
    }

    private struct JpegFrame {
        public int Width;
        public int Height;
        public int ComponentCount;
        public Component[] Components;
        public int MaxH;
        public int MaxV;
    }

    private struct ScanHeader {
        public int[] ComponentIndices;
        public byte Ss;
        public byte Se;
        public byte Ah;
        public byte Al;
    }

    private sealed class BaselineComponentState {
        public Component Component;
        public int[] Buffer;
        public int[] BlockCoeffs;
        public int[] BlockPixels;
        public int Stride;
        public int BlocksPerRow;
        public int BlocksPerCol;
        public int PrevDc;

        public BaselineComponentState(Component component, int blocksPerRow, int blocksPerCol) {
            Component = component;
            BlocksPerRow = blocksPerRow;
            BlocksPerCol = blocksPerCol;
            Stride = blocksPerRow * 8;
            Buffer = new int[Stride * (blocksPerCol * 8)];
            BlockCoeffs = new int[64];
            BlockPixels = new int[64];
            PrevDc = 0;
        }
    }

    private sealed class ProgressiveState {
        public ProgressiveComponentState[] Components = Array.Empty<ProgressiveComponentState>();
        public int McuCols;
        public int McuRows;

        public static ProgressiveState Create(JpegFrame frame, int[][] quantTables) {
            var maxH = frame.MaxH;
            var maxV = frame.MaxV;
            var mcuWidth = maxH * 8;
            var mcuHeight = maxV * 8;
            var mcuCols = (frame.Width + mcuWidth - 1) / mcuWidth;
            var mcuRows = (frame.Height + mcuHeight - 1) / mcuHeight;

            var components = new ProgressiveComponentState[frame.ComponentCount];
            for (var i = 0; i < frame.ComponentCount; i++) {
                var comp = frame.Components[i];
                if (comp.QuantId >= quantTables.Length || quantTables[comp.QuantId] is null) {
                    throw new FormatException("Missing JPEG quantization table.");
                }
                components[i] = new ProgressiveComponentState(comp, mcuCols * comp.H, mcuRows * comp.V);
            }

            return new ProgressiveState {
                Components = components,
                McuCols = mcuCols,
                McuRows = mcuRows
            };
        }

        public byte[] RenderRgba(JpegFrame frame, int? adobeTransform) {
            for (var i = 0; i < Components.Length; i++) {
                var compState = Components[i];
                for (var by = 0; by < compState.BlocksPerCol; by++) {
                    for (var bx = 0; bx < compState.BlocksPerRow; bx++) {
                        var baseIndex = (by * compState.BlocksPerRow + bx) * 64;
                        Array.Copy(compState.Coeffs, baseIndex, compState.BlockCoeffs, 0, 64);
                        InverseDct(compState.BlockCoeffs, compState.BlockPixels);
                        WriteBlock(compState.Buffer, compState.Stride, bx, by, compState.BlockPixels);
                    }
                }
            }

            var baselineStates = new BaselineComponentState[Components.Length];
            for (var i = 0; i < Components.Length; i++) {
                var compState = Components[i];
                var baseline = new BaselineComponentState(compState.Component, compState.BlocksPerRow, compState.BlocksPerCol) {
                    Buffer = compState.Buffer,
                    Stride = compState.Stride
                };
                baselineStates[i] = baseline;
            }

            return ComposeRgba(frame, baselineStates, adobeTransform);
        }
    }

    private static byte ApplyCmyk(int c, int k) {
        var v = c + k;
        if (v > 255) v = 255;
        return (byte)(255 - v);
    }

    private sealed class ProgressiveComponentState {
        public Component Component;
        public int BlocksPerRow;
        public int BlocksPerCol;
        public int[] Coeffs;
        public int[] Buffer;
        public int[] BlockCoeffs;
        public int[] BlockPixels;
        public int Stride;
        public int PrevDc;

        public ProgressiveComponentState(Component component, int blocksPerRow, int blocksPerCol) {
            Component = component;
            BlocksPerRow = blocksPerRow;
            BlocksPerCol = blocksPerCol;
            Stride = blocksPerRow * 8;
            Coeffs = new int[BlocksPerRow * BlocksPerCol * 64];
            Buffer = new int[Stride * (blocksPerCol * 8)];
            BlockCoeffs = new int[64];
            BlockPixels = new int[64];
            PrevDc = 0;
        }
    }

    private struct HuffmanTable {
        public int[] MinCode;
        public int[] MaxCode;
        public int[] ValPtr;
        public byte[] Values;
        public bool IsValid;

        public static HuffmanTable Build(ReadOnlySpan<byte> counts, byte[] values) {
            var minCode = new int[17];
            var maxCode = new int[17];
            var valPtr = new int[17];
            for (var i = 0; i < 17; i++) {
                minCode[i] = -1;
                maxCode[i] = -1;
            }

            var code = 0;
            var k = 0;
            for (var i = 1; i <= 16; i++) {
                var count = counts[i - 1];
                if (count != 0) {
                    minCode[i] = code;
                    valPtr[i] = k;
                    code += count;
                    maxCode[i] = code - 1;
                    k += count;
                }
                code <<= 1;
            }

            return new HuffmanTable {
                MinCode = minCode,
                MaxCode = maxCode,
                ValPtr = valPtr,
                Values = values,
                IsValid = true
            };
        }
    }

    private ref struct JpegBitReader {
        private readonly ReadOnlySpan<byte> _data;
        private int _pos;
        private int _bitBuffer;
        private int _bitCount;

        public bool RestartMarkerSeen;

        public JpegBitReader(ReadOnlySpan<byte> data) {
            _data = data;
            _pos = 0;
            _bitBuffer = 0;
            _bitCount = 0;
            RestartMarkerSeen = false;
        }

        public int ReadBit() {
            EnsureBits(1);
            var bit = (_bitBuffer >> (_bitCount - 1)) & 1;
            _bitCount--;
            return bit;
        }

        public int ReadBits(int count) {
            if (count == 0) return 0;
            EnsureBits(count);
            var value = (_bitBuffer >> (_bitCount - count)) & ((1 << count) - 1);
            _bitCount -= count;
            return value;
        }

        public void ExpectRestartMarker() {
            _bitBuffer = 0;
            _bitCount = 0;
            while (_pos < _data.Length) {
                var b = _data[_pos++];
                if (b != 0xFF) continue;
                while (_pos < _data.Length && _data[_pos] == 0xFF) _pos++;
                if (_pos >= _data.Length) throw new FormatException("Unexpected JPEG end.");
                var marker = _data[_pos++];
                if (marker >= 0xD0 && marker <= 0xD7) {
                    RestartMarkerSeen = false;
                    return;
                }
                if (marker == 0x00) continue;
                throw new FormatException("Unexpected JPEG marker in scan.");
            }
            throw new FormatException("Missing JPEG restart marker.");
        }

        private void EnsureBits(int count) {
            while (_bitCount < count) {
                var b = ReadByte();
                _bitBuffer = (_bitBuffer << 8) | b;
                _bitCount += 8;
            }
        }

        private int ReadByte() {
            while (_pos < _data.Length) {
                var b = _data[_pos++];
                if (b != 0xFF) return b;
                if (_pos >= _data.Length) throw new FormatException("Unexpected JPEG end.");
                var marker = _data[_pos++];
                if (marker == 0x00) return 0xFF;
                if (marker >= 0xD0 && marker <= 0xD7) {
                    RestartMarkerSeen = true;
                    continue;
                }
                throw new FormatException("Unexpected JPEG marker in scan.");
            }
            throw new FormatException("Unexpected JPEG end.");
        }
    }

}
