using System;
using System.Buffers;
using System.Collections.Generic;
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
    private sealed class DataMatrixDecodeState {
        public bool CanBeGs1Header { get; set; } = true;
        public bool IsGs1 { get; set; }
        public DataMatrixStructuredAppend? StructuredAppend { get; set; }
        public bool ReaderProgramming { get; set; }
        public DataMatrixMacro Macro { get; set; }
        public List<int> EciAssignments { get; } = new List<int>();
    }

    private static bool TryDecodePixels(PixelSpan pixels, int width, int height, int stride, PixelFormat format, CancellationToken cancellationToken, out string value) {
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
        if (TryExtractModules(pixels, width, height, stride, format, cancellationToken, out var modules)) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
            if (TryDecodeWithRotations(modules, cancellationToken, out value)) return true;
            if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
            var mirror = MirrorX(modules);
            if (TryDecodeWithRotations(mirror, cancellationToken, out value)) return true;
        }
        value = string.Empty;
        return false;
    }

    private static bool TryDecodePixels(PixelSpan pixels, int width, int height, int stride, PixelFormat format, CancellationToken cancellationToken, out string value, DataMatrixDecodeDiagnostics diagnostics) {
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        if (TryExtractModules(pixels, width, height, stride, format, cancellationToken, out var modules)) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
            if (TryDecodeWithRotations(modules, cancellationToken, diagnostics, out value)) { diagnostics.Success = true; return true; }
            if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
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
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
        if (TryDecodeCore(modules, cancellationToken, out value)) return true;
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
        if (TryDecodeCore(Rotate90(modules), cancellationToken, out value)) return true;
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
        if (TryDecodeCore(Rotate180(modules), cancellationToken, out value)) return true;
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
        if (TryDecodeCore(Rotate270(modules), cancellationToken, out value)) return true;
        return false;
    }

    private static bool TryDecodeWithRotations(BitMatrix modules, CancellationToken cancellationToken, DataMatrixDecodeDiagnostics diagnostics, out string value) {
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        if (TryDecodeCore(modules, cancellationToken, diagnostics, out value)) return true;
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        if (TryDecodeCore(Rotate90(modules), cancellationToken, diagnostics, out value)) return true;
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        if (TryDecodeCore(Rotate180(modules), cancellationToken, diagnostics, out value)) return true;
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        if (TryDecodeCore(Rotate270(modules), cancellationToken, diagnostics, out value)) return true;
        return false;
    }

    private static bool TryDecodeCore(BitMatrix modules, CancellationToken cancellationToken, out string value) {
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
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
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
        if (!TryDecodeCodewords(codewords, symbol, cancellationToken, out value)) {
            value = string.Empty;
            return false;
        }

        return true;
    }

    private static bool TryDecodeDetailedCore(BitMatrix modules, CancellationToken cancellationToken, out DataMatrixDecoded decoded) {
        decoded = null!;
        if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
        if (!DataMatrixSymbolInfo.TryGetForSize(modules.Height, modules.Width, out var symbol)) return false;

        var dataRegion = ExtractDataRegion(modules, symbol, cancellationToken);
        if (dataRegion is null) return false;
        var codewords = DataMatrixPlacement.ReadCodewords(dataRegion, symbol.CodewordCount, cancellationToken);
        if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
        var state = new DataMatrixDecodeState();
        if (!TryDecodeCodewords(codewords, symbol, cancellationToken, out var value, state)) return false;

        decoded = new DataMatrixDecoded(
            value,
            state.IsGs1,
            state.StructuredAppend,
            state.ReaderProgramming,
            state.Macro,
            state.EciAssignments.ToArray(),
            symbol.SymbolRows,
            symbol.SymbolCols,
            symbol.IsDmre);
        return true;
    }

    private static bool TryDecodeCore(BitMatrix modules, CancellationToken cancellationToken, DataMatrixDecodeDiagnostics diagnostics, out string value) {
        diagnostics.AttemptCount++;
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
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
        if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; diagnostics.Failure = "Cancelled."; return false; }
        if (!TryDecodeCodewords(codewords, symbol, cancellationToken, out value)) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) { diagnostics.Failure = "Cancelled."; value = string.Empty; return false; }
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
            if (DecodeBudget.ShouldAbort(cancellationToken)) return null;
            for (var regionCol = 0; regionCol < regionCols; regionCol++) {
                var startRow = regionRow * regionTotalRows;
                var startCol = regionCol * regionTotalCols;
                for (var y = 0; y < regionDataRows; y++) {
                    if (DecodeBudget.ShouldAbort(cancellationToken)) return null;
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

    private static bool TryDecodeCodewords(
        byte[] codewords,
        DataMatrixSymbolInfo symbol,
        CancellationToken cancellationToken,
        out string value,
        DataMatrixDecodeState? decodeState = null) {
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
            if (DecodeBudget.ShouldAbort(cancellationToken)) return Fail(out value);
            for (var b = 0; b < blocks; b++) {
                if (i >= dataBlocks[b].Length) continue;
                if (offset >= codewords.Length) return Fail(out value);
                dataBlocks[b][i] = codewords[offset++];
            }
        }

        for (var i = 0; i < symbol.EccBlockSize; i++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return Fail(out value);
            for (var b = 0; b < blocks; b++) {
                if (offset >= codewords.Length) return Fail(out value);
                eccBlocks[b][i] = codewords[offset++];
            }
        }

        var data = new byte[symbol.DataCodewords];
        var dataOffset = 0;
        for (var b = 0; b < blocks; b++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return Fail(out value);
            var block = new byte[dataBlocks[b].Length + eccBlocks[b].Length];
            Buffer.BlockCopy(dataBlocks[b], 0, block, 0, dataBlocks[b].Length);
            Buffer.BlockCopy(eccBlocks[b], 0, block, dataBlocks[b].Length, eccBlocks[b].Length);
            if (!DataMatrixReedSolomonDecoder.TryCorrectInPlace(block, symbol.EccBlockSize)) return Fail(out value);
            Buffer.BlockCopy(block, 0, data, dataOffset, dataBlocks[b].Length);
            dataOffset += dataBlocks[b].Length;
        }

        value = DecodeData(data, cancellationToken, decodeState ?? new DataMatrixDecodeState());
        if (DecodeBudget.ShouldAbort(cancellationToken)) return Fail(out value);
        return true;
    }

    private static bool Fail(out string value) {
        value = string.Empty;
        return false;
    }

    internal static string DecodeDataCodewords(byte[] dataCodewords) {
        if (dataCodewords is null) throw new ArgumentNullException(nameof(dataCodewords));
        return DecodeData(dataCodewords, CancellationToken.None, new DataMatrixDecodeState());
    }

    private static string DecodeData(PixelSpan data, CancellationToken cancellationToken, DataMatrixDecodeState state) {
        var sb = new StringBuilder(data.Length);
        var mode = DataMatrixEncodation.Ascii;
        var index = 0;
        var upperShift = false;
        string? macroTrailer = null;
        int? activeEciAssignment = null;
        var asciiEciBytes = new List<byte>();

        while (index < data.Length) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) break;
            switch (mode) {
                case DataMatrixEncodation.Ascii:
                    mode = DecodeAsciiSegment(data, ref index, sb, asciiEciBytes, ref upperShift, ref macroTrailer, ref activeEciAssignment, state);
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
                    FlushAsciiEciBytes(sb, asciiEciBytes, activeEciAssignment);
                    DecodeBase256Segment(data, ref index, sb, activeEciAssignment);
                    mode = DataMatrixEncodation.Ascii;
                    break;
                default:
                    index = data.Length;
                    break;
            }
        }

        FlushAsciiEciBytes(sb, asciiEciBytes, activeEciAssignment);
        if (!string.IsNullOrEmpty(macroTrailer)) sb.Append(macroTrailer);
        return sb.ToString();
    }

    private static DataMatrixEncodation DecodeAsciiSegment(
        PixelSpan data,
        ref int index,
        StringBuilder sb,
        List<byte> asciiEciBytes,
        ref bool upperShift,
        ref string? macroTrailer,
        ref int? activeEciAssignment,
        DataMatrixDecodeState state) {
        if (index >= data.Length) return DataMatrixEncodation.Ascii;

        var cw = data[index++];

        if (cw == 129) {
            index = data.Length;
            return DataMatrixEncodation.Ascii;
        }

        if (cw <= 128) {
            state.CanBeGs1Header = false;
            AppendAsciiByte(sb, asciiEciBytes, (byte)(cw - 1), ref upperShift, activeEciAssignment);
            return DataMatrixEncodation.Ascii;
        }

        if (cw <= 229) {
            state.CanBeGs1Header = false;
            var val = cw - 130;
            AppendAsciiByte(sb, asciiEciBytes, (byte)('0' + (val / 10)), ref upperShift, activeEciAssignment);
            AppendAsciiByte(sb, asciiEciBytes, (byte)('0' + (val % 10)), ref upperShift, activeEciAssignment);
            return DataMatrixEncodation.Ascii;
        }

        switch (cw) {
            case 230:
                state.CanBeGs1Header = false;
                FlushAsciiEciBytes(sb, asciiEciBytes, activeEciAssignment);
                return DataMatrixEncodation.C40;
            case 231:
                state.CanBeGs1Header = false;
                FlushAsciiEciBytes(sb, asciiEciBytes, activeEciAssignment);
                return DataMatrixEncodation.Base256;
            case 232:
                FlushAsciiEciBytes(sb, asciiEciBytes, activeEciAssignment);
                if (state.CanBeGs1Header) {
                    state.IsGs1 = true;
                    state.CanBeGs1Header = false;
                } else {
                    sb.Append(Gs1.GroupSeparator);
                }
                return DataMatrixEncodation.Ascii;
            case 233:
                FlushAsciiEciBytes(sb, asciiEciBytes, activeEciAssignment);
                if (index + 2 < data.Length) {
                    var sequence = data[index++];
                    var fileId1 = data[index++];
                    var fileId2 = data[index++];
                    var metadata = new DataMatrixStructuredAppend(
                        (sequence >> 4) + 1,
                        17 - (sequence & 0x0F),
                        fileId1,
                        fileId2);
                    if (metadata.IsValid) state.StructuredAppend = metadata;
                } else {
                    index = data.Length;
                }
                return DataMatrixEncodation.Ascii;
            case 234:
                FlushAsciiEciBytes(sb, asciiEciBytes, activeEciAssignment);
                state.ReaderProgramming = true;
                state.CanBeGs1Header = false;
                return DataMatrixEncodation.Ascii;
            case 235:
                state.CanBeGs1Header = false;
                upperShift = true;
                return DataMatrixEncodation.Ascii;
            case 236:
                FlushAsciiEciBytes(sb, asciiEciBytes, activeEciAssignment);
                state.Macro = DataMatrixMacro.Macro05;
                state.CanBeGs1Header = false;
                sb.Append("[)>\u001E05\u001D");
                macroTrailer ??= "\u001E\u0004";
                return DataMatrixEncodation.Ascii;
            case 237:
                FlushAsciiEciBytes(sb, asciiEciBytes, activeEciAssignment);
                state.Macro = DataMatrixMacro.Macro06;
                state.CanBeGs1Header = false;
                sb.Append("[)>\u001E06\u001D");
                macroTrailer ??= "\u001E\u0004";
                return DataMatrixEncodation.Ascii;
            case 238:
                state.CanBeGs1Header = false;
                FlushAsciiEciBytes(sb, asciiEciBytes, activeEciAssignment);
                return DataMatrixEncodation.X12;
            case 239:
                state.CanBeGs1Header = false;
                FlushAsciiEciBytes(sb, asciiEciBytes, activeEciAssignment);
                return DataMatrixEncodation.Text;
            case 240:
                state.CanBeGs1Header = false;
                FlushAsciiEciBytes(sb, asciiEciBytes, activeEciAssignment);
                return DataMatrixEncodation.Edifact;
            case 241:
                state.CanBeGs1Header = false;
                if (TryReadEciAssignment(data, ref index, out var assignmentNumber)) {
                    FlushAsciiEciBytes(sb, asciiEciBytes, activeEciAssignment);
                    state.EciAssignments.Add(assignmentNumber);
                    activeEciAssignment = assignmentNumber;
                }
                return DataMatrixEncodation.Ascii;
            default:
                return DataMatrixEncodation.Ascii;
        }
    }

    private static void AppendAsciiByte(
        StringBuilder sb,
        List<byte> asciiEciBytes,
        byte value,
        ref bool upperShift,
        int? activeEciAssignment) {
        var resolved = (int)value;
        if (upperShift) {
            resolved += 128;
            upperShift = false;
        }
        if (activeEciAssignment.HasValue) {
            asciiEciBytes.Add((byte)resolved);
        } else {
            sb.Append((char)resolved);
        }
    }

    private static void FlushAsciiEciBytes(StringBuilder sb, List<byte> asciiEciBytes, int? activeEciAssignment) {
        if (asciiEciBytes.Count == 0) return;
        var bytes = asciiEciBytes.ToArray();
        sb.Append(DecodeBase256Bytes(bytes, bytes.Length, activeEciAssignment));
        asciiEciBytes.Clear();
    }

    private static bool TryReadEciAssignment(PixelSpan data, ref int index, out int assignmentNumber) {
        assignmentNumber = 0;
        if (index >= data.Length) return false;
        var first = data[index++];
        if (first is >= 1 and <= 127) {
            assignmentNumber = first - 1;
            return true;
        }
        if (first is >= 128 and <= 191) {
            if (index >= data.Length) return false;
            var second = data[index++];
            if (second is < 1 or > 254) return false;
            assignmentNumber = (first - 128) * 254 + second - 1 + 127;
            return true;
        }
        if (first is >= 192 and <= 254) {
            if (index + 1 >= data.Length) return false;
            var second = data[index++];
            var third = data[index++];
            if (second is < 1 or > 254 || third is < 1 or > 254) return false;
            assignmentNumber = (first - 192) * 64516 + (second - 1) * 254 + third - 1 + 16383;
            return assignmentNumber <= 999999;
        }
        return false;
    }

    private static bool TryDecodePixelsDetailed(
        PixelSpan pixels,
        int width,
        int height,
        int stride,
        PixelFormat format,
        CancellationToken cancellationToken,
        out DataMatrixDecoded decoded) {
        decoded = null!;
        if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
        if (!TryExtractModules(pixels, width, height, stride, format, cancellationToken, out var modules)) return false;
        if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
        if (TryDecodeDetailedWithRotations(modules, cancellationToken, out decoded)) return true;
        if (DecodeBudget.ShouldAbort(cancellationToken)) { decoded = null!; return false; }
        return TryDecodeDetailedWithRotations(MirrorX(modules), cancellationToken, out decoded);
    }

    private static bool TryDecodeDetailedWithRotations(BitMatrix modules, CancellationToken cancellationToken, out DataMatrixDecoded decoded) {
        if (DecodeBudget.ShouldAbort(cancellationToken)) { decoded = null!; return false; }
        if (TryDecodeDetailedCore(modules, cancellationToken, out decoded)) return true;
        if (DecodeBudget.ShouldAbort(cancellationToken)) { decoded = null!; return false; }
        if (TryDecodeDetailedCore(Rotate90(modules), cancellationToken, out decoded)) return true;
        if (DecodeBudget.ShouldAbort(cancellationToken)) { decoded = null!; return false; }
        if (TryDecodeDetailedCore(Rotate180(modules), cancellationToken, out decoded)) return true;
        if (DecodeBudget.ShouldAbort(cancellationToken)) { decoded = null!; return false; }
        return TryDecodeDetailedCore(Rotate270(modules), cancellationToken, out decoded);
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
