using System;
using System.Buffers;
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
    private static readonly BarcodeType[] AdditionalGuardedLinearTypes = {
        BarcodeType.Bc412, BarcodeType.ChannelCode, BarcodeType.PosiCode, BarcodeType.EAN2, BarcodeType.EAN5
    };
    private static bool TryDecodeWithTransforms(bool[] modules, BarcodeType? expectedType, BarcodeDecodeOptions? options, out BarcodeDecoded decoded) {
        return TryDecodeWithTransforms(modules, expectedType, options, CancellationToken.None, null, out decoded);
    }

    private static bool TryDecodeWithTransforms(bool[] modules, BarcodeType? expectedType, BarcodeDecodeOptions? options, CancellationToken cancellationToken, out BarcodeDecoded decoded) {
        return TryDecodeWithTransforms(modules, expectedType, options, cancellationToken, null, out decoded);
    }

    private static bool TryDecodeWithTransforms(bool[] modules, BarcodeType? expectedType, BarcodeDecodeOptions? options, CancellationToken cancellationToken, BarcodeDecodeDiagnostics? diagnostics, out BarcodeDecoded decoded) {
        return TryDecodeWithTransforms(modules, expectedType, options, cancellationToken, diagnostics, out decoded, out _);
    }

    private static bool TryDecodeWithTransforms(bool[] modules, BarcodeType? expectedType, BarcodeDecodeOptions? options, CancellationToken cancellationToken, BarcodeDecodeDiagnostics? diagnostics, out BarcodeDecoded decoded, out int priority, bool? backgroundIsDark = null) {
        priority = int.MaxValue;
        if (cancellationToken.IsCancellationRequested) { decoded = null!; return false; }
        if (expectedType == BarcodeType.Flattermarken) {
            decoded = null!;
            if (!Flattermarken.FlattermarkenCode.TryDecode(modules, out var marks)) return false;
            decoded = new BarcodeDecoded(BarcodeType.Flattermarken, marks);
            priority = 0;
            return true;
        }
        // Without guards or a checksum, Pharmacode can reinterpret spaces as
        // bars. Agreeing endpoints only propose a background polarity; the
        // Pharmacode decoder must validate it before excluding an interpretation.
        if (TryDecodeCoreTrimmed(modules, expectedType, options, cancellationToken, diagnostics, out decoded, out priority, modules, backgroundIsDark, allowPharmacode: backgroundIsDark != true)) return true;
        if (cancellationToken.IsCancellationRequested) return false;
        var inverted = InvertModules(modules);
        if (diagnostics is not null) diagnostics.InvertedTried = true;
        if (TryDecodeCoreTrimmed(inverted, expectedType, options, cancellationToken, diagnostics, out decoded, out priority, modules, backgroundIsDark, allowPharmacode: backgroundIsDark != false)) return true;
        if (cancellationToken.IsCancellationRequested) return false;
        var reversed = ReverseModules(modules);
        if (diagnostics is not null) diagnostics.ReversedTried = true;
        if (TryDecodeCoreTrimmed(reversed, expectedType, options, cancellationToken, diagnostics, out decoded, out priority, modules, backgroundIsDark, allowPharmacode: backgroundIsDark != true)) return true;
        if (cancellationToken.IsCancellationRequested) return false;
        var invertedReversed = InvertModules(reversed);
        if (diagnostics is not null) diagnostics.InvertedTried = true;
        return TryDecodeCoreTrimmed(invertedReversed, expectedType, options, cancellationToken, diagnostics, out decoded, out priority, modules, backgroundIsDark, allowPharmacode: backgroundIsDark != false);
    }

    private static bool TryDecodeCoreTrimmed(bool[] modules, BarcodeType? expectedType, BarcodeDecodeOptions? options, CancellationToken cancellationToken, BarcodeDecodeDiagnostics? diagnostics, out BarcodeDecoded decoded, out int priority, bool[] physicalModules, bool? backgroundIsDark, bool allowPharmacode) {
        decoded = null!;
        priority = int.MaxValue;
        if (cancellationToken.IsCancellationRequested) return false;
        var trimmed = TrimModules(modules);
        if (trimmed.Length == 0) return false;
        if (diagnostics is not null) diagnostics.AttemptCount++;
        return TryDecodeCore(trimmed, expectedType, options, cancellationToken, out decoded, out priority, physicalModules, backgroundIsDark, allowPharmacode);
    }

    private static bool TryDecodeCore(bool[] modules, BarcodeType? expectedType, BarcodeDecodeOptions? options, CancellationToken cancellationToken, out BarcodeDecoded decoded, out int priority, bool[] physicalModules, bool? backgroundIsDark, bool allowPharmacode) {
        var runWorkspace = new ModuleRunWorkspace(modules);
        try {
            return TryDecodeCoreWithRuns(ref runWorkspace, expectedType, options, cancellationToken, out decoded, out priority, physicalModules, backgroundIsDark, allowPharmacode);
        } finally {
            runWorkspace.Dispose();
        }
    }

    private static bool TryDecodeCoreWithRuns(ref ModuleRunWorkspace runWorkspace, BarcodeType? expectedType, BarcodeDecodeOptions? options, CancellationToken cancellationToken, out BarcodeDecoded decoded, out int priority, bool[] physicalModules, bool? backgroundIsDark, bool allowPharmacode) {
        var modules = runWorkspace.Modules;
        decoded = null!;
        // Report the successful decoder's existing order so competing sampling
        // hypotheses share this canonical priority rather than duplicating a type list.
        priority = 0;
        if (cancellationToken.IsCancellationRequested) return false;
        if (expectedType.HasValue) {
            if (expectedType == BarcodeType.Pharmacode) {
                if (!TryDecodePharmacodeWithBackground(ref runWorkspace, physicalModules, backgroundIsDark, allowPharmacode, out var hintedPharmacode)) return false;
                decoded = new BarcodeDecoded(BarcodeType.Pharmacode, hintedPharmacode);
                return true;
            }
            return TryDecodeType(expectedType.Value, ref runWorkspace, options, out decoded);
        }

        // Fixed-length symbologies first.
        if (cancellationToken.IsCancellationRequested) return false;
        priority++;
        if (TryDecodeEan8(modules, out var ean8)) {
            decoded = new BarcodeDecoded(BarcodeType.EAN, ean8);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        priority++;
        if (TryDecodeUpcA(modules, out var upca)) {
            decoded = new BarcodeDecoded(BarcodeType.UPCA, upca);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        priority++;
        if (TryDecodeEan13(modules, out var ean13)) {
            decoded = new BarcodeDecoded(BarcodeType.EAN, ean13);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        priority++;
        if (TryDecodeUpcE(modules, out var upce)) {
            decoded = new BarcodeDecoded(BarcodeType.UPCE, upce);
            return true;
        }
        foreach (var type in AdditionalGuardedLinearTypes) {
            if (cancellationToken.IsCancellationRequested) return false;
            priority++;
            if (TryDecodeType(type, ref runWorkspace, options, out decoded)) return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        priority++;
        if (TryDecodeItf14(ref runWorkspace, out var itf14)) {
            decoded = new BarcodeDecoded(BarcodeType.ITF14, itf14);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        priority++;
        if (TryDecodeItf(ref runWorkspace, out var itf) && ApplyTwoOfFiveCheck(ref itf, options)) {
            decoded = new BarcodeDecoded(BarcodeType.ITF, itf);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        priority++;
        if (TryDecodeIndustrial2of5(ref runWorkspace, out var industrial25) && ApplyTwoOfFiveCheck(ref industrial25, options)) {
            decoded = new BarcodeDecoded(BarcodeType.Industrial2of5, industrial25);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        priority++;
        if (TryDecodeMatrix2of5(ref runWorkspace, out var matrix25) && ApplyTwoOfFiveCheck(ref matrix25, options)) {
            decoded = new BarcodeDecoded(BarcodeType.Matrix2of5, matrix25);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        priority++;
        if (TryDecodeIata2of5(ref runWorkspace, out var iata25) && ApplyTwoOfFiveCheck(ref iata25, options)) {
            decoded = new BarcodeDecoded(BarcodeType.IATA2of5, iata25);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        priority++;
        if (TryDecodeType(BarcodeType.Coop2of5, ref runWorkspace, options, out decoded)) return true;
        if (cancellationToken.IsCancellationRequested) return false;
        priority++;
        if (TryDecodeType(BarcodeType.Datalogic2of5, ref runWorkspace, options, out decoded)) return true;

        if (cancellationToken.IsCancellationRequested) return false;
        priority++;
        if (TryDecodeCode128(ref runWorkspace, out var code128, out var isGs1, out var readerInitialization)) {
            decoded = new BarcodeDecoded(isGs1 ? BarcodeType.GS1_128 : BarcodeType.Code128, code128, readerInitialization);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        priority++;
        if (TryDecodeCode32(modules, options, out var code32)) {
            decoded = new BarcodeDecoded(BarcodeType.Code32, code32);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        priority++;
        if (TryDecodeCode39(modules, options, out var code39)) {
            decoded = new BarcodeDecoded(BarcodeType.Code39, code39);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        priority++;
        if (TryDecodeCode93(modules, out var code93)) {
            decoded = new BarcodeDecoded(BarcodeType.Code93, code93);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        priority++;
        if (TryDecodeCodabar(ref runWorkspace, options, out var codabar)) {
            decoded = new BarcodeDecoded(BarcodeType.Codabar, codabar);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        priority++;
        if (TryDecodeMsi(modules, options, out var msi)) {
            decoded = new BarcodeDecoded(BarcodeType.MSI, msi);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        priority++;
        if (TryDecodeCode11(ref runWorkspace, options, out var code11)) {
            decoded = new BarcodeDecoded(BarcodeType.Code11, code11);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        priority++;
        if (TryDecodePlessey(ref runWorkspace, options, out var plessey)) {
            decoded = new BarcodeDecoded(BarcodeType.Plessey, plessey);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        priority++;
        if (TryDecodePatchCode(ref runWorkspace, out var patch)) {
            decoded = new BarcodeDecoded(BarcodeType.PatchCode, patch);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        priority++;
        if (TryDecodeTelepen(ref runWorkspace, out var telepen)) {
            decoded = new BarcodeDecoded(BarcodeType.Telepen, telepen);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        priority++;
        if (TryDecodePharmacodeWithBackground(ref runWorkspace, physicalModules, backgroundIsDark, allowPharmacode, out var pharmacode)) {
            decoded = new BarcodeDecoded(BarcodeType.Pharmacode, pharmacode);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        priority++;
        if (TryDecodeGs1DataBarTruncated(modules, out var dataBarTruncated)) {
            decoded = new BarcodeDecoded(BarcodeType.GS1DataBarTruncated, dataBarTruncated);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        priority++;
        if (TryDecodeGs1DataBarLimited(modules, out var dataBarLimited)) {
            decoded = new BarcodeDecoded(BarcodeType.GS1DataBarLimited, dataBarLimited);
            return true;
        }
        if (cancellationToken.IsCancellationRequested) return false;
        priority++;
        if (TryDecodeGs1DataBarExpanded(modules, out var dataBarExpanded)) {
            decoded = new BarcodeDecoded(BarcodeType.GS1DataBarExpanded, dataBarExpanded);
            return true;
        }

        return false;
    }

    private static bool TryDecodeType(BarcodeType type, ref ModuleRunWorkspace runWorkspace, BarcodeDecodeOptions? options, out BarcodeDecoded decoded) {
        var modules = runWorkspace.Modules;
        decoded = null!;
        switch (type) {
            case BarcodeType.PosiCode:
                if (PosiCode.PosiCodeDecoder.TryDecode(modules, out var posi)) {
                    decoded = new BarcodeDecoded(type, posi.Text, readerInitialization: posi.ReaderInitialization,
                        metadata: new PosiCodeSymbolMetadata(posi));
                    return true;
                }
                return false;
            case BarcodeType.ChannelCode:
                if (ChannelCode.ChannelCodeDecoder.TryDecode(modules, out var channelNumber)) {
                    decoded = new BarcodeDecoded(type, channelNumber);
                    return true;
                }
                return false;
            case BarcodeType.FacingIdentificationMark:
                if (Postal.FacingIdentificationMark.TryDecode(modules, out var mark)) {
                    decoded = new BarcodeDecoded(type, mark);
                    return true;
                }
                return false;
            case BarcodeType.Bc412:
                if (Bc412.Bc412Decoder.TryDecode(modules, out var waferIdentifier, options?.Bc412Variant ?? Bc412.Bc412Variant.Semi)) {
                    decoded = new BarcodeDecoded(type, waferIdentifier);
                    return true;
                }
                return false;
            case BarcodeType.Coop2of5:
            case BarcodeType.Datalogic2of5:
                var discreteFormat = type == BarcodeType.Coop2of5 ? SymbolFormat.Coop2Of5 : SymbolFormat.Datalogic2Of5;
                if (Code25.Discrete2Of5Encoder.TryDecode(runWorkspace.Runs, discreteFormat, out var discreteText) && ApplyTwoOfFiveCheck(ref discreteText, options)) {
                    decoded = new BarcodeDecoded(type, discreteText);
                    return true;
                }
                return false;
            case BarcodeType.EAN2:
            case BarcodeType.EAN5:
                if (modules.Length == (type == BarcodeType.EAN2 ? 20 : 47)
                    && TryDecodeEanSupplement(modules, out var supplement)) {
                    decoded = new BarcodeDecoded(type, supplement);
                    return true;
                }
                return false;
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
            case BarcodeType.TelepenNumeric:
                if (TryDecodeTelepen(ref runWorkspace, out var compacted)
                    && TryExpandTelepenNumeric(compacted, out var numeric)) {
                    decoded = new BarcodeDecoded(type, numeric);
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
                if (TryDecodeItf14(ref runWorkspace, out var itf14)) {
                    decoded = new BarcodeDecoded(BarcodeType.ITF14, itf14);
                    return true;
                }
                return false;
            case BarcodeType.ITF:
                if (TryDecodeItf(ref runWorkspace, out var itf) && ApplyTwoOfFiveCheck(ref itf, options)) {
                    decoded = new BarcodeDecoded(BarcodeType.ITF, itf);
                    return true;
                }
                return false;
            case BarcodeType.Industrial2of5:
                if (TryDecodeIndustrial2of5(ref runWorkspace, out var industrial25) && ApplyTwoOfFiveCheck(ref industrial25, options)) {
                    decoded = new BarcodeDecoded(BarcodeType.Industrial2of5, industrial25);
                    return true;
                }
                return false;
            case BarcodeType.Matrix2of5:
                if (TryDecodeMatrix2of5(ref runWorkspace, out var matrix25) && ApplyTwoOfFiveCheck(ref matrix25, options)) {
                    decoded = new BarcodeDecoded(BarcodeType.Matrix2of5, matrix25);
                    return true;
                }
                return false;
            case BarcodeType.IATA2of5:
                if (TryDecodeIata2of5(ref runWorkspace, out var iata25) && ApplyTwoOfFiveCheck(ref iata25, options)) {
                    decoded = new BarcodeDecoded(BarcodeType.IATA2of5, iata25);
                    return true;
                }
                return false;
            case BarcodeType.Code128:
                if (TryDecodeCode128(ref runWorkspace, out var code128, out var isGs1, out var readerInitialization)) {
                    decoded = new BarcodeDecoded(isGs1 ? BarcodeType.GS1_128 : BarcodeType.Code128, code128, readerInitialization);
                    return true;
                }
                return false;
            case BarcodeType.GS1_128:
                if (TryDecodeCode128(ref runWorkspace, out var gs1, out var isGs1Only, out var gs1ReaderInitialization) && isGs1Only) {
                    decoded = new BarcodeDecoded(BarcodeType.GS1_128, gs1, gs1ReaderInitialization);
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
                if (TryDecodeCodabar(ref runWorkspace, options, out var codabar)) {
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
                if (TryDecodeCode11(ref runWorkspace, options, out var code11)) {
                    decoded = new BarcodeDecoded(BarcodeType.Code11, code11);
                    return true;
                }
                return false;
            case BarcodeType.Plessey:
                if (TryDecodePlessey(ref runWorkspace, options, out var plessey)) {
                    decoded = new BarcodeDecoded(BarcodeType.Plessey, plessey);
                    return true;
                }
                return false;
            case BarcodeType.PatchCode:
                if (TryDecodePatchCode(ref runWorkspace, out var patch)) {
                    decoded = new BarcodeDecoded(BarcodeType.PatchCode, patch);
                    return true;
                }
                return false;
            case BarcodeType.Telepen:
                if (TryDecodeTelepen(ref runWorkspace, out var telepen)) {
                    decoded = new BarcodeDecoded(BarcodeType.Telepen, telepen);
                    return true;
                }
                return false;
            case BarcodeType.Pharmacode:
                if (TryDecodePharmacode(ref runWorkspace, out var pharmacode)) {
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
            case BarcodeType.GS1DataBarOmni:
                if (TryDecodeGs1DataBarOmnidirectional(modules, out var dataBarOmnidirectional)) {
                    decoded = new BarcodeDecoded(BarcodeType.GS1DataBarOmni, dataBarOmnidirectional);
                    return true;
                }
                return false;
            case BarcodeType.GS1DataBarLimited:
                if (TryDecodeGs1DataBarLimited(modules, out var dataBarLimited)) {
                    decoded = new BarcodeDecoded(BarcodeType.GS1DataBarLimited, dataBarLimited);
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
        // Decoders only read these modules. Keep an already trimmed candidate
        // rather than copying it for each transform attempt.
        if (start == 0 && end == modules.Length - 1) return modules;
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

}
