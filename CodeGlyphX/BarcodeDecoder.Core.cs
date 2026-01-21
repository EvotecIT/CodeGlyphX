using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CodeGlyphX.Code11;
using CodeGlyphX.Code128;
using CodeGlyphX.Code39;
using CodeGlyphX.Code93;
using CodeGlyphX.Codabar;
using CodeGlyphX.DataBar;
using CodeGlyphX.Ean;
using CodeGlyphX.Itf;
using CodeGlyphX.Internal;
using CodeGlyphX.Msi;
using CodeGlyphX.Plessey;
using CodeGlyphX.UpcA;
using CodeGlyphX.UpcE;

namespace CodeGlyphX;

public static partial class BarcodeDecoder {
    private static bool TryDecodeWithTransforms(bool[] modules, BarcodeType? expectedType, BarcodeDecodeOptions? options, out BarcodeDecoded decoded) {
        return TryDecodeWithTransforms(modules, expectedType, options, CancellationToken.None, null, out decoded);
    }

    private static bool TryDecodeWithTransforms(bool[] modules, BarcodeType? expectedType, BarcodeDecodeOptions? options, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        return TryDecodeWithTransforms(modules, expectedType, options, cancellationToken, null, out decoded);
    }

    private static bool TryDecodeWithTransforms(bool[] modules, BarcodeType? expectedType, BarcodeDecodeOptions? options, CancellationToken cancellationToken, BarcodeDecodeDiagnostics? diagnostics, out BarcodeDecoded decoded) {
        if (cancellationToken.IsCancellationRequested) { decoded = null!; return false; }
        if (TryDecodeCoreTrimmed(modules, expectedType, options, cancellationToken, diagnostics, out decoded)) return true;
        if (cancellationToken.IsCancellationRequested) return false;
        var inverted = InvertModules(modules);
        if (diagnostics is not null) diagnostics.InvertedTried = true;
        if (TryDecodeCoreTrimmed(inverted, expectedType, options, cancellationToken, diagnostics, out decoded)) return true;
        if (cancellationToken.IsCancellationRequested) return false;
        var reversed = ReverseModules(modules);
        if (diagnostics is not null) diagnostics.ReversedTried = true;
        if (TryDecodeCoreTrimmed(reversed, expectedType, options, cancellationToken, diagnostics, out decoded)) return true;
        if (cancellationToken.IsCancellationRequested) return false;
        var invertedReversed = InvertModules(reversed);
        if (diagnostics is not null) diagnostics.InvertedTried = true;
        return TryDecodeCoreTrimmed(invertedReversed, expectedType, options, cancellationToken, diagnostics, out decoded);
    }

    private static bool TryDecodeCoreTrimmed(bool[] modules, BarcodeType? expectedType, BarcodeDecodeOptions? options, CancellationToken cancellationToken, BarcodeDecodeDiagnostics? diagnostics, out BarcodeDecoded decoded) {
        decoded = null!;
        if (cancellationToken.IsCancellationRequested) return false;
        var trimmed = TrimModules(modules);
        if (trimmed.Length == 0) return false;
        if (diagnostics is not null) diagnostics.AttemptCount++;
        return TryDecodeCore(trimmed, expectedType, options, cancellationToken, out decoded);
    }

    private static bool TryDecodeCore(bool[] modules, BarcodeType? expectedType, BarcodeDecodeOptions? options, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        decoded = null!;
        if (cancellationToken.IsCancellationRequested) return false;
        if (expectedType.HasValue) {
            return TryDecodeType(expectedType.Value, modules, options, out decoded);
        }

        // Fixed-length symbologies first.
        if (cancellationToken.IsCancellationRequested) return false;
        if (TryDecodeEan8(modules, out var ean8)) {
            decoded = new BarcodeDecoded(BarcodeType.EAN, ean8);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (TryDecodeUpcA(modules, out var upca)) {
            decoded = new BarcodeDecoded(BarcodeType.UPCA, upca);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (TryDecodeEan13(modules, out var ean13)) {
            decoded = new BarcodeDecoded(BarcodeType.EAN, ean13);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (TryDecodeUpcE(modules, out var upce)) {
            decoded = new BarcodeDecoded(BarcodeType.UPCE, upce);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (TryDecodeItf14(modules, out var itf14)) {
            decoded = new BarcodeDecoded(BarcodeType.ITF14, itf14);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (TryDecodeItf(modules, out var itf)) {
            decoded = new BarcodeDecoded(BarcodeType.ITF, itf);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (TryDecodeIndustrial2of5(modules, out var industrial25)) {
            decoded = new BarcodeDecoded(BarcodeType.Industrial2of5, industrial25);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (TryDecodeMatrix2of5(modules, out var matrix25)) {
            decoded = new BarcodeDecoded(BarcodeType.Matrix2of5, matrix25);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (TryDecodeIata2of5(modules, out var iata25)) {
            decoded = new BarcodeDecoded(BarcodeType.IATA2of5, iata25);
            return true;
        }

        if (cancellationToken.IsCancellationRequested) return false;
        if (TryDecodeCode128(modules, out var code128, out var isGs1)) {
            decoded = new BarcodeDecoded(isGs1 ? BarcodeType.GS1_128 : BarcodeType.Code128, code128);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (TryDecodeCode32(modules, options, out var code32)) {
            decoded = new BarcodeDecoded(BarcodeType.Code32, code32);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (TryDecodeCode39(modules, options, out var code39)) {
            decoded = new BarcodeDecoded(BarcodeType.Code39, code39);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (TryDecodeCode93(modules, out var code93)) {
            decoded = new BarcodeDecoded(BarcodeType.Code93, code93);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (TryDecodeCodabar(modules, out var codabar)) {
            decoded = new BarcodeDecoded(BarcodeType.Codabar, codabar);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (TryDecodeMsi(modules, options, out var msi)) {
            decoded = new BarcodeDecoded(BarcodeType.MSI, msi);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (TryDecodeCode11(modules, options, out var code11)) {
            decoded = new BarcodeDecoded(BarcodeType.Code11, code11);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (TryDecodePlessey(modules, options, out var plessey)) {
            decoded = new BarcodeDecoded(BarcodeType.Plessey, plessey);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (TryDecodePatchCode(modules, out var patch)) {
            decoded = new BarcodeDecoded(BarcodeType.PatchCode, patch);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (TryDecodeTelepen(modules, out var telepen)) {
            decoded = new BarcodeDecoded(BarcodeType.Telepen, telepen);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (TryDecodePharmacode(modules, out var pharmacode)) {
            decoded = new BarcodeDecoded(BarcodeType.Pharmacode, pharmacode);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (TryDecodeGs1DataBarTruncated(modules, out var dataBarTruncated)) {
            decoded = new BarcodeDecoded(BarcodeType.GS1DataBarTruncated, dataBarTruncated);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        if (TryDecodeGs1DataBarExpanded(modules, out var dataBarExpanded)) {
            decoded = new BarcodeDecoded(BarcodeType.GS1DataBarExpanded, dataBarExpanded);
            return true;
        }

        return false;
    }

    private static bool TryDecodeType(BarcodeType type, bool[] modules, BarcodeDecodeOptions? options, out BarcodeDecoded decoded) {
        decoded = null!;
        switch (type) {
            case BarcodeType.EAN:
                if (TryDecodeEan8(modules, out var ean8)) {
                    decoded = new BarcodeDecoded(BarcodeType.EAN, ean8);
                    return true;
                }
                if (TryDecodeEan13(modules, out var ean13)) {
                    decoded = new BarcodeDecoded(BarcodeType.EAN, ean13);
                    return true;
                }
                return false;
            case BarcodeType.UPCA:
                if (TryDecodeUpcA(modules, out var upca)) {
                    decoded = new BarcodeDecoded(BarcodeType.UPCA, upca);
                    return true;
                }
                return false;
            case BarcodeType.UPCE:
                if (TryDecodeUpcE(modules, out var upce)) {
                    decoded = new BarcodeDecoded(BarcodeType.UPCE, upce);
                    return true;
                }
                return false;
            case BarcodeType.ITF14:
                if (TryDecodeItf14(modules, out var itf14)) {
                    decoded = new BarcodeDecoded(BarcodeType.ITF14, itf14);
                    return true;
                }
                return false;
            case BarcodeType.ITF:
                if (TryDecodeItf(modules, out var itf)) {
                    decoded = new BarcodeDecoded(BarcodeType.ITF, itf);
                    return true;
                }
                return false;
            case BarcodeType.Industrial2of5:
                if (TryDecodeIndustrial2of5(modules, out var industrial25)) {
                    decoded = new BarcodeDecoded(BarcodeType.Industrial2of5, industrial25);
                    return true;
                }
                return false;
            case BarcodeType.Matrix2of5:
                if (TryDecodeMatrix2of5(modules, out var matrix25)) {
                    decoded = new BarcodeDecoded(BarcodeType.Matrix2of5, matrix25);
                    return true;
                }
                return false;
            case BarcodeType.IATA2of5:
                if (TryDecodeIata2of5(modules, out var iata25)) {
                    decoded = new BarcodeDecoded(BarcodeType.IATA2of5, iata25);
                    return true;
                }
                return false;
            case BarcodeType.Code128:
                if (TryDecodeCode128(modules, out var code128, out var isGs1)) {
                    decoded = new BarcodeDecoded(isGs1 ? BarcodeType.GS1_128 : BarcodeType.Code128, code128);
                    return true;
                }
                return false;
            case BarcodeType.GS1_128:
                if (TryDecodeCode128(modules, out var gs1, out var isGs1Only) && isGs1Only) {
                    decoded = new BarcodeDecoded(BarcodeType.GS1_128, gs1);
                    return true;
                }
                return false;
            case BarcodeType.Code32:
                if (TryDecodeCode32(modules, options, out var code32)) {
                    decoded = new BarcodeDecoded(BarcodeType.Code32, code32);
                    return true;
                }
                return false;
            case BarcodeType.Code39:
                if (TryDecodeCode39(modules, options, out var code39)) {
                    decoded = new BarcodeDecoded(BarcodeType.Code39, code39);
                    return true;
                }
                return false;
            case BarcodeType.Code93:
                if (TryDecodeCode93(modules, out var code93)) {
                    decoded = new BarcodeDecoded(BarcodeType.Code93, code93);
                    return true;
                }
                return false;
            case BarcodeType.Codabar:
                if (TryDecodeCodabar(modules, out var codabar)) {
                    decoded = new BarcodeDecoded(BarcodeType.Codabar, codabar);
                    return true;
                }
                return false;
            case BarcodeType.MSI:
                if (TryDecodeMsi(modules, options, out var msi)) {
                    decoded = new BarcodeDecoded(BarcodeType.MSI, msi);
                    return true;
                }
                return false;
            case BarcodeType.Code11:
                if (TryDecodeCode11(modules, options, out var code11)) {
                    decoded = new BarcodeDecoded(BarcodeType.Code11, code11);
                    return true;
                }
                return false;
            case BarcodeType.Plessey:
                if (TryDecodePlessey(modules, options, out var plessey)) {
                    decoded = new BarcodeDecoded(BarcodeType.Plessey, plessey);
                    return true;
                }
                return false;
            case BarcodeType.PatchCode:
                if (TryDecodePatchCode(modules, out var patch)) {
                    decoded = new BarcodeDecoded(BarcodeType.PatchCode, patch);
                    return true;
                }
                return false;
            case BarcodeType.Telepen:
                if (TryDecodeTelepen(modules, out var telepen)) {
                    decoded = new BarcodeDecoded(BarcodeType.Telepen, telepen);
                    return true;
                }
                return false;
            case BarcodeType.Pharmacode:
                if (TryDecodePharmacode(modules, out var pharmacode)) {
                    decoded = new BarcodeDecoded(BarcodeType.Pharmacode, pharmacode);
                    return true;
                }
                return false;
            case BarcodeType.GS1DataBarTruncated:
                if (TryDecodeGs1DataBarTruncated(modules, out var dataBarTruncated)) {
                    decoded = new BarcodeDecoded(BarcodeType.GS1DataBarTruncated, dataBarTruncated);
                    return true;
                }
                return false;
            case BarcodeType.GS1DataBarExpanded:
                if (TryDecodeGs1DataBarExpanded(modules, out var dataBarExpanded)) {
                    decoded = new BarcodeDecoded(BarcodeType.GS1DataBarExpanded, dataBarExpanded);
                    return true;
                }
                return false;
            default:
                return false;
        }
    }

    private static bool[] TrimModules(bool[] modules) {
        var start = 0;
        while (start < modules.Length && !modules[start]) start++;
        var end = modules.Length - 1;
        while (end >= start && !modules[end]) end--;
        if (start > end) return Array.Empty<bool>();
        var trimmed = new bool[end - start + 1];
        Array.Copy(modules, start, trimmed, 0, trimmed.Length);
        return trimmed;
    }

    private static bool[] InvertModules(bool[] modules) {
        var output = new bool[modules.Length];
        for (var i = 0; i < modules.Length; i++) output[i] = !modules[i];
        return output;
    }

    private static bool[] ReverseModules(bool[] modules) {
        var output = new bool[modules.Length];
        for (var i = 0; i < modules.Length; i++) output[i] = modules[modules.Length - 1 - i];
        return output;
    }

    private static int[] GetRuns(bool[] modules) {
        var runs = new List<int>(modules.Length / 2);
        var current = modules[0];
        var len = 1;
        for (var i = 1; i < modules.Length; i++) {
            if (modules[i] == current) {
                len++;
            } else {
                runs.Add(len);
                current = modules[i];
                len = 1;
            }
        }
        runs.Add(len);
        return runs.ToArray();
    }

}
