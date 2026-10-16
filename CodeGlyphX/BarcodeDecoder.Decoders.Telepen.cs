using System;
using System.Collections.Generic;
using CodeGlyphX.Telepen;

namespace CodeGlyphX;

public static partial class BarcodeDecoder {
    private static bool TryExpandTelepenNumeric(string compacted, out string text) {
        text = string.Empty;
        if (compacted.Length == 0) return false;
        var characters = new char[compacted.Length * 2];
        for (var i = 0; i < compacted.Length; i++) {
            var value = (int)compacted[i];
            if (value < 17 || value > 126) return false;
            if (value < 27) {
                characters[i * 2] = (char)('0' + value - 17);
                characters[i * 2 + 1] = 'X';
            } else {
                value -= 27;
                characters[i * 2] = (char)('0' + value / 10);
                characters[i * 2 + 1] = (char)('0' + value % 10);
            }
        }
        text = new string(characters);
        return true;
    }

    private static bool TryDecodeTelepen(ref ModuleRunWorkspace runWorkspace, out string text) {
        var modules = runWorkspace.Modules;
        text = string.Empty;
        if (modules.Length < 8) return false;
        if (!modules[0]) return false;

        var runs = runWorkspace.Runs;
        if (!TryNormalizeTelepenRuns(runs, out var normalized)) return false;
        if (!TryGetRunThreshold(normalized, startIndex: 0, step: 1, out var threshold)) return false;

        var pairKinds = BuildTelepenPairKinds(normalized, threshold);
        if (!TryBuildTelepenBits(pairKinds, out var bits)) return false;
        if (!TryBuildTelepenValues(bits, out var values)) return false;
        if (!TryValidateTelepenValues(values, out var dataLen)) return false;

        var chars = new char[dataLen];
        for (var i = 0; i < dataLen; i++) {
            chars[i] = (char)values[i + 1];
        }

        text = new string(chars);
        return true;
    }

    private static bool TryNormalizeTelepenRuns(ReadOnlySpan<int> runs, out ReadOnlySpan<int> normalized) {
        normalized = runs;
        if ((runs.Length & 1) == 0) return true;

        // Telepen ends with a space; TrimModules removes trailing spaces, so pad with a narrow space run.
        var min = int.MaxValue;
        for (var i = 0; i < runs.Length; i++) {
            if (runs[i] < min) min = runs[i];
        }
        if (min <= 0) return false;

        var padded = new int[runs.Length + 1];
        runs.CopyTo(padded);
        padded[padded.Length - 1] = min;
        normalized = padded;
        return (normalized.Length & 1) == 0;
    }

    private static TelepenPairKind[] BuildTelepenPairKinds(ReadOnlySpan<int> runs, double threshold) {
        var pairs = runs.Length / 2;
        var pairKinds = new TelepenPairKind[pairs];
        for (var i = 0; i < pairs; i++) {
            var barWide = runs[i * 2] > threshold;
            var spaceWide = runs[i * 2 + 1] > threshold;
            pairKinds[i] = GetTelepenPairKind(barWide, spaceWide);
        }
        return pairKinds;
    }

    private static bool TryBuildTelepenBits(TelepenPairKind[] pairKinds, out List<bool> bits) {
        bits = new List<bool>(pairKinds.Length * 2);
        var skipUntil = 0;
        for (var i = 0; i < pairKinds.Length; i++) {
            if (i < skipUntil) continue;
            switch (pairKinds[i]) {
                case TelepenPairKind.NarrowNarrow:
                    bits.Add(true);
                    break;
                case TelepenPairKind.WideNarrow:
                    bits.Add(false);
                    bits.Add(false);
                    break;
                case TelepenPairKind.WideWide:
                    bits.Add(false);
                    bits.Add(true);
                    bits.Add(false);
                    break;
                case TelepenPairKind.NarrowWide: {
                    var j = i + 1;
                    var middle = 0;
                    while (j < pairKinds.Length && pairKinds[j] == TelepenPairKind.NarrowNarrow) {
                        middle++;
                        j++;
                    }
                    if (j >= pairKinds.Length || pairKinds[j] != TelepenPairKind.NarrowWide) return false;
                    bits.Add(false);
                    for (var k = 0; k < middle + 2; k++) bits.Add(true);
                    bits.Add(false);
                    skipUntil = j + 1;
                    break;
                }
                default:
                    return false;
            }
        }
        return true;
    }

    private static bool TryBuildTelepenValues(List<bool> bits, out byte[] values) {
        values = Array.Empty<byte>();
        if ((bits.Count & 7) != 0) return false;
        var byteCount = bits.Count / 8;
        if (byteCount < 3) return false;

        var result = new byte[byteCount];
        var bitIndex = 0;
        for (var i = 0; i < byteCount; i++) {
            var value = 0;
            for (var b = 0; b < 8; b++) {
                if (bits[bitIndex++]) value |= 1 << b;
            }
            var full = (byte)value;
            if (!TelepenTables.HasEvenParity(full)) return false;
            result[i] = (byte)(full & 0x7F);
        }

        values = result;
        return true;
    }

    private static bool TryValidateTelepenValues(byte[] values, out int dataLen) {
        dataLen = 0;
        if (values[0] != TelepenTables.StartValue) return false;
        if (values[values.Length - 1] != TelepenTables.StopValue) return false;

        var checkIndex = values.Length - 2;
        if (checkIndex <= 0) return false;

        var checksum = TelepenTables.CalcChecksum(values, 1, checkIndex - 1);
        if (values[checkIndex] != checksum) return false;

        dataLen = checkIndex - 1;
        return true;
    }

    private enum TelepenPairKind {
        NarrowNarrow,
        WideNarrow,
        WideWide,
        NarrowWide
    }

    private static TelepenPairKind GetTelepenPairKind(bool barWide, bool spaceWide) {
        if (barWide) return spaceWide ? TelepenPairKind.WideWide : TelepenPairKind.WideNarrow;
        return spaceWide ? TelepenPairKind.NarrowWide : TelepenPairKind.NarrowNarrow;
    }

}
