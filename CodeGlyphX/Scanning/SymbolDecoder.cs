using System;
using CodeGlyphX.Aztec;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.Pdf417;

namespace CodeGlyphX;

/// <summary>Decodes sampled module grids without image detection or raster conversion.</summary>
public static class SymbolDecoder {
    private static readonly SymbolFormat[] DetailedFormats = {
        SymbolFormat.Gs1Composite, SymbolFormat.RmQrCode, SymbolFormat.MaxiCode, SymbolFormat.DotCode,
        SymbolFormat.HanXin, SymbolFormat.MicroQrCode, SymbolFormat.QrCode, SymbolFormat.Aztec,
        SymbolFormat.DataMatrix, SymbolFormat.Pdf417, SymbolFormat.MicroPdf417,
        SymbolFormat.Gs1DataBarOmnidirectional, SymbolFormat.Gs1DataBarStackedOmnidirectional,
        SymbolFormat.Gs1DataBarStacked, SymbolFormat.Gs1DataBarExpandedStacked
    };

    /// <summary>Attempts to decode a sampled grid, optionally restricted to one physical format.</summary>
    /// <remarks>Module results have no image search region or detected geometry.</remarks>
    public static bool TryDecode(BitMatrix modules, out DetectedSymbol symbol, SymbolFormat? format = null) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        symbol = null!;
        if (format.HasValue) {
            var capability = SymbolCapabilities.Get(format.Value);
            if (!capability.CanDecodeModules) throw new NotSupportedException($"{format.Value} does not support module decoding.");
            return TryDecodeFormat(modules, format.Value, out symbol);
        }
        for (var i = 0; i < DetailedFormats.Length; i++) {
            if (TryDecodeFormat(modules, DetailedFormats[i], out symbol)) return true;
        }
        for (var i = 0; i < SymbolCapabilities.All.Count; i++) {
            var capability = SymbolCapabilities.All[i];
            if (Array.IndexOf(DetailedFormats, capability.Format) >= 0 || !capability.CanDecodeModules) continue;
            if (TryDecodeFormat(modules, capability.Format, out symbol)) return true;
        }
        return false;
    }

    /// <summary>Decodes a sampled grid or throws when no requested symbol can be decoded.</summary>
    public static DetectedSymbol Decode(BitMatrix modules, SymbolFormat? format = null) {
        if (!TryDecode(modules, out var symbol, format)) throw new FormatException("The module grid does not contain a decodable symbol.");
        return symbol;
    }

    private static bool TryDecodeFormat(BitMatrix modules, SymbolFormat format, out DetectedSymbol symbol) {
        symbol = null!;
        switch (format) {
            case SymbolFormat.QrCode:
                if (!QrDecoder.TryDecode(modules, out var qr)) return false;
                symbol = SymbolResultFactory.From(qr); return true;
            case SymbolFormat.MicroQrCode:
                if (!MicroQrDecoder.TryDecode(modules, out var micro)) return false;
                symbol = SymbolResultFactory.From(micro); return true;
            case SymbolFormat.RmQrCode:
                if (!RmQrDecoder.TryDecode(modules, out var rm)) return false;
                symbol = SymbolResultFactory.From(rm); return true;
            case SymbolFormat.MaxiCode:
                if (!MaxiCodeDecoder.TryDecodeDetailed(modules, out var maxi)) return false;
                symbol = SymbolResultFactory.From(maxi); return true;
            case SymbolFormat.DotCode:
                if (!DotCodeDecoder.TryDecodeDetailed(modules, out var dot)) return false;
                symbol = SymbolResultFactory.From(dot); return true;
            case SymbolFormat.HanXin:
                if (!HanXinDecoder.TryDecodeDetailed(modules, out var han)) return false;
                symbol = SymbolResultFactory.From(han); return true;
            case SymbolFormat.Gs1Composite:
                if (!Gs1CompositeDecoder.TryDecode(modules, out var composite)) return false;
                symbol = SymbolResultFactory.From(composite); return true;
            case SymbolFormat.DataMatrix:
                if (!DataMatrixDecoder.TryDecodeDetailed(modules, out var matrix)) return false;
                symbol = SymbolResultFactory.From(matrix); return true;
            case SymbolFormat.Pdf417:
                if (!Pdf417Decoder.TryDecode(modules, out Pdf417Decoded pdf)) return false;
                symbol = SymbolResultFactory.From(pdf); return true;
            case SymbolFormat.MicroPdf417:
                if (!MicroPdf417Decoder.TryDecode(modules, out var microPdf)) return false;
                symbol = new DetectedSymbol(format, microPdf); return true;
            case SymbolFormat.Aztec:
                if (!AztecDecoder.TryDecode(modules, out var aztec)) return false;
                symbol = new DetectedSymbol(format, aztec); return true;
        }
        var capability = SymbolCapabilities.Get(format);
        if (!capability.LegacyBarcodeType.HasValue) return false;
        var barcodeType = capability.LegacyBarcodeType.Value;
        if (capability.Family == SymbolFamily.Linear) {
            if (modules.Height != 1) return false;
            var row = new bool[modules.Width];
            for (var x = 0; x < row.Length; x++) row[x] = modules[x, 0];
            if (!BarcodeDecoder.TryDecode(row, barcodeType, out var barcode)) return false;
            symbol = SymbolResultFactory.From(barcode);
            return true;
        }
        if (!MatrixBarcodeDecoder.TryDecode(barcodeType, modules, out var text)) return false;
        symbol = new DetectedSymbol(format, text,
            payloadProfile: capability.Has(SymbolCapabilityFlags.Gs1Decode) ? SymbolPayloadProfile.Gs1 : SymbolPayloadProfile.None);
        return true;
    }
}
