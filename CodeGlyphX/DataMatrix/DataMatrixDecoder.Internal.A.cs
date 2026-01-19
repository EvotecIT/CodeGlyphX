using System;
using System.Buffers;
using System.Text;
using System.Threading;
using CodeGlyphX.Internal;
using CodeGlyphX;

#if NET8_0_OR_GREATER
using PixelSpan = System.ReadOnlySpan<byte>;
#else
using PixelSpan = byte[];
#endif

namespace CodeGlyphX.DataMatrix;

public static partial class DataMatrixDecoder {
    private static bool TryDecodePixels(PixelSpan pixels, int width, int height, int stride, PixelFormat format, CancellationToken cancellationToken, out string value) {
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; return false; }
        if (TryExtractModules(pixels, width, height, stride, format, out var modules)) {
            if (TryDecodeWithRotations(modules, cancellationToken, out value)) return true;
            if (cancellationToken.IsCancellationRequested) { value = string.Empty; return false; }
            var mirror = MirrorX(modules);
            if (TryDecodeWithRotations(mirror, cancellationToken, out value)) return true;
        }
        value = string.Empty;
        return false;
    }

    private static bool TryDecodePixels(PixelSpan pixels, int width, int height, int stride, PixelFormat format, CancellationToken cancellationToken, out string value, DataMatrixDecodeDiagnostics diagnostics) {
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        if (TryExtractModules(pixels, width, height, stride, format, out var modules)) {
            if (TryDecodeWithRotations(modules, cancellationToken, diagnostics, out value)) { diagnostics.Success = true; return true; }
            if (cancellationToken.IsCancellationRequested) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
            diagnostics.MirroredTried = true;
            var mirror = MirrorX(modules);
            if (TryDecodeWithRotations(mirror, cancellationToken, diagnostics, out value)) { diagnostics.Success = true; return true; }
        } else {
            diagnostics.Failure ??= "Failed to extract Data Matrix modules.";
        }
        value = string.Empty;
        diagnostics.Failure ??= "No Data Matrix decoded.";
        return false;
    }

    private static bool TryDecodeWithRotations(BitMatrix modules, CancellationToken cancellationToken, out string value) {
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; return false; }
        if (TryDecodeCore(modules, cancellationToken, out value)) return true;
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; return false; }
        if (TryDecodeCore(Rotate90(modules), cancellationToken, out value)) return true;
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; return false; }
        if (TryDecodeCore(Rotate180(modules), cancellationToken, out value)) return true;
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; return false; }
        if (TryDecodeCore(Rotate270(modules), cancellationToken, out value)) return true;
        return false;
    }

    private static bool TryDecodeWithRotations(BitMatrix modules, CancellationToken cancellationToken, DataMatrixDecodeDiagnostics diagnostics, out string value) {
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        if (TryDecodeCore(modules, cancellationToken, diagnostics, out value)) return true;
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        if (TryDecodeCore(Rotate90(modules), cancellationToken, diagnostics, out value)) return true;
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        if (TryDecodeCore(Rotate180(modules), cancellationToken, diagnostics, out value)) return true;
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        if (TryDecodeCore(Rotate270(modules), cancellationToken, diagnostics, out value)) return true;
        return false;
    }

    private static bool TryDecodeCore(BitMatrix modules, CancellationToken cancellationToken, out string value) {
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; return false; }
        if (!DataMatrixSymbolInfo.TryGetForSize(modules.Height, modules.Width, out var symbol)) {
            value = string.Empty;
            return false;
        }

        var dataRegion = ExtractDataRegion(modules, symbol, cancellationToken);
        if (dataRegion is null) {
            value = string.Empty;
            return false;
        }
        var codewords = DataMatrixPlacement.ReadCodewords(dataRegion, symbol.CodewordCount, cancellationToken);
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; return false; }
        if (!TryDecodeCodewords(codewords, symbol, cancellationToken, out value)) {
            value = string.Empty;
            return false;
        }

        return true;
    }

    private static bool TryDecodeCore(BitMatrix modules, CancellationToken cancellationToken, DataMatrixDecodeDiagnostics diagnostics, out string value) {
        diagnostics.AttemptCount++;
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        if (!DataMatrixSymbolInfo.TryGetForSize(modules.Height, modules.Width, out var symbol)) {
            value = string.Empty;
            diagnostics.Failure ??= "Unsupported symbol size.";
            return false;
        }

        var dataRegion = ExtractDataRegion(modules, symbol, cancellationToken);
        if (dataRegion is null) {
            value = string.Empty;
            diagnostics.Failure ??= "Failed to extract data region.";
            return false;
        }
        var codewords = DataMatrixPlacement.ReadCodewords(dataRegion, symbol.CodewordCount, cancellationToken);
        if (cancellationToken.IsCancellationRequested) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        if (!TryDecodeCodewords(codewords, symbol, cancellationToken, out value)) {
            if (cancellationToken.IsCancellationRequested) { diagnostics.Failure = "Cancelled."; value = string.Empty; return false; }
            diagnostics.Failure ??= "Failed to decode codewords.";
            value = string.Empty;
            return false;
        }

        return true;
    }

    private static BitMatrix? ExtractDataRegion(BitMatrix modules, DataMatrixSymbolInfo symbol, CancellationToken cancellationToken) {
        var dataRegion = new BitMatrix(symbol.DataRegionCols, symbol.DataRegionRows);
        var regionRows = symbol.RegionRows;
        var regionCols = symbol.RegionCols;
        var regionTotalRows = symbol.RegionTotalRows;
        var regionTotalCols = symbol.RegionTotalCols;
        var regionDataRows = symbol.RegionDataRows;
        var regionDataCols = symbol.RegionDataCols;

        for (var regionRow = 0; regionRow < regionRows; regionRow++) {
            if (cancellationToken.IsCancellationRequested) return null;
            for (var regionCol = 0; regionCol < regionCols; regionCol++) {
                var startRow = regionRow * regionTotalRows;
                var startCol = regionCol * regionTotalCols;
                for (var y = 0; y < regionDataRows; y++) {
                    if (cancellationToken.IsCancellationRequested) return null;
                    for (var x = 0; x < regionDataCols; x++) {
                        var dataRow = regionRow * regionDataRows + y;
                        var dataCol = regionCol * regionDataCols + x;
                        dataRegion[dataCol, dataRow] = modules[startCol + 1 + x, startRow + 1 + y];
                    }
                }
            }
        }

        return dataRegion;
    }

    private static bool TryDecodeCodewords(byte[] codewords, DataMatrixSymbolInfo symbol, CancellationToken cancellationToken, out string value) {
        var blocks = symbol.BlockCount;
        var maxDataBlock = 0;
        for (var i = 0; i < blocks; i++) {
            if (symbol.DataBlockSizes[i] > maxDataBlock) maxDataBlock = symbol.DataBlockSizes[i];
        }

        var dataBlocks = new byte[blocks][];
        var eccBlocks = new byte[blocks][];
        for (var i = 0; i < blocks; i++) {
            dataBlocks[i] = new byte[symbol.DataBlockSizes[i]];
            eccBlocks[i] = new byte[symbol.EccBlockSize];
        }

        var offset = 0;
        for (var i = 0; i < maxDataBlock; i++) {
            if (cancellationToken.IsCancellationRequested) return Fail(out value);
            for (var b = 0; b < blocks; b++) {
                if (i >= dataBlocks[b].Length) continue;
                if (offset >= codewords.Length) return Fail(out value);
                dataBlocks[b][i] = codewords[offset++];
            }
        }

        for (var i = 0; i < symbol.EccBlockSize; i++) {
            if (cancellationToken.IsCancellationRequested) return Fail(out value);
            for (var b = 0; b < blocks; b++) {
                if (offset >= codewords.Length) return Fail(out value);
                eccBlocks[b][i] = codewords[offset++];
            }
        }

        var data = new byte[symbol.DataCodewords];
        var dataOffset = 0;
        for (var b = 0; b < blocks; b++) {
            if (cancellationToken.IsCancellationRequested) return Fail(out value);
            var block = new byte[dataBlocks[b].Length + eccBlocks[b].Length];
            Buffer.BlockCopy(dataBlocks[b], 0, block, 0, dataBlocks[b].Length);
            Buffer.BlockCopy(eccBlocks[b], 0, block, dataBlocks[b].Length, eccBlocks[b].Length);
            if (!DataMatrixReedSolomonDecoder.TryCorrectInPlace(block, symbol.EccBlockSize)) return Fail(out value);
            Buffer.BlockCopy(block, 0, data, dataOffset, dataBlocks[b].Length);
            dataOffset += dataBlocks[b].Length;
        }

        value = DecodeData(data, cancellationToken);
        if (cancellationToken.IsCancellationRequested) return Fail(out value);
        return true;
    }

    private static bool Fail(out string value) {
        value = string.Empty;
        return false;
    }

    internal static string DecodeDataCodewords(byte[] dataCodewords) {
        if (dataCodewords is null) throw new ArgumentNullException(nameof(dataCodewords));
        return DecodeData(dataCodewords, CancellationToken.None);
    }

    private static string DecodeData(PixelSpan data, CancellationToken cancellationToken) {
        var sb = new StringBuilder(data.Length);
        var mode = DataMatrixEncodation.Ascii;
        var index = 0;
        var upperShift = false;
        string? macroTrailer = null;
        Encoding? base256Encoding = null;

        while (index < data.Length) {
            if (cancellationToken.IsCancellationRequested) break;
            switch (mode) {
                case DataMatrixEncodation.Ascii:
                    mode = DecodeAsciiSegment(data, ref index, sb, ref upperShift, ref macroTrailer, ref base256Encoding);
                    break;
                case DataMatrixEncodation.C40:
                    mode = DecodeC40TextSegment(data, ref index, sb, isText: false, ref upperShift);
                    break;
                case DataMatrixEncodation.Text:
                    mode = DecodeC40TextSegment(data, ref index, sb, isText: true, ref upperShift);
                    break;
                case DataMatrixEncodation.X12:
                    mode = DecodeX12Segment(data, ref index, sb, ref upperShift);
                    break;
                case DataMatrixEncodation.Edifact:
                    mode = DecodeEdifactSegment(data, ref index, sb, ref upperShift);
                    break;
                case DataMatrixEncodation.Base256:
                    DecodeBase256Segment(data, ref index, sb, base256Encoding);
                    mode = DataMatrixEncodation.Ascii;
                    break;
                default:
                    index = data.Length;
                    break;
            }
        }

        if (!string.IsNullOrEmpty(macroTrailer)) sb.Append(macroTrailer);
        return sb.ToString();
    }

    private static DataMatrixEncodation DecodeAsciiSegment(
        PixelSpan data,
        ref int index,
        StringBuilder sb,
        ref bool upperShift,
        ref string? macroTrailer,
        ref Encoding? base256Encoding) {
        if (index >= data.Length) return DataMatrixEncodation.Ascii;

        var cw = data[index++];

        if (cw == 129) {
            index = data.Length;
            return DataMatrixEncodation.Ascii;
        }

        if (cw <= 128) {
            AppendChar(sb, (char)(cw - 1), ref upperShift);
            return DataMatrixEncodation.Ascii;
        }

        if (cw <= 229) {
            var val = cw - 130;
            AppendChar(sb, (char)('0' + (val / 10)), ref upperShift);
            AppendChar(sb, (char)('0' + (val % 10)), ref upperShift);
            return DataMatrixEncodation.Ascii;
        }

        switch (cw) {
            case 230:
                return DataMatrixEncodation.C40;
            case 231:
                return DataMatrixEncodation.Base256;
            case 232:
                sb.Append(Gs1.GroupSeparator);
                return DataMatrixEncodation.Ascii;
            case 233:
                if (index + 1 < data.Length) index += 2;
                return DataMatrixEncodation.Ascii;
            case 234:
                return DataMatrixEncodation.Ascii;
            case 235:
                upperShift = true;
                return DataMatrixEncodation.Ascii;
            case 236:
                sb.Append("[)>\u001E05\u001D");
                macroTrailer ??= "\u001E\u0004";
                return DataMatrixEncodation.Ascii;
            case 237:
                sb.Append("[)>\u001E06\u001D");
                macroTrailer ??= "\u001E\u0004";
                return DataMatrixEncodation.Ascii;
            case 238:
                return DataMatrixEncodation.X12;
            case 239:
                return DataMatrixEncodation.Text;
            case 240:
                return DataMatrixEncodation.Edifact;
            case 241:
                if (index < data.Length) {
                    // Best-effort ECI: skip one codeword for now.
                    index++;
                }
                return DataMatrixEncodation.Ascii;
            default:
                return DataMatrixEncodation.Ascii;
        }
    }

    private static DataMatrixEncodation DecodeC40TextSegment(
        PixelSpan data,
        ref int index,
        StringBuilder sb,
        bool isText,
        ref bool upperShift) {
        var shift = 0;

        while (index < data.Length) {
            var cw1 = data[index];
            if (cw1 == 254) {
                index++;
                return DataMatrixEncodation.Ascii;
            }

            if (index + 1 >= data.Length) {
                index = data.Length;
                return DataMatrixEncodation.Ascii;
            }

            var cw2 = data[index + 1];
            index += 2;

            ParseTwoBytes(cw1, cw2, out var c1, out var c2, out var c3);
            DecodeC40TextValue(c1, sb, isText, ref shift, ref upperShift);
            DecodeC40TextValue(c2, sb, isText, ref shift, ref upperShift);
            DecodeC40TextValue(c3, sb, isText, ref shift, ref upperShift);
        }

        return DataMatrixEncodation.Ascii;
    }

}
