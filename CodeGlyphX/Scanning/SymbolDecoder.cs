using System;
using CodeGlyphX.Aztec;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.Pdf417;

namespace CodeGlyphX;

/// <summary>Decodes sampled module grids without image detection or raster conversion.</summary>
public static class SymbolDecoder {
    /// <summary>Decodes sampled Ultracode colour cells without losing their palette identity.</summary>
    public static bool TryDecode(UltracodeGrid cells, out DetectedSymbol symbol) {
        symbol = null!;
        if (!UltracodeDecoder.TryDecode(cells, out var decoded)) return false;
        symbol = SymbolResultFactory.From(decoded!);
        return true;
    }

    /// <summary>Decodes a colour-cell grid or throws when its structure or checked message is invalid.</summary>
    public static DetectedSymbol Decode(UltracodeGrid cells) {
        if (!TryDecode(cells, out var symbol)) throw new FormatException("The colour grid does not contain a decodable Ultracode symbol.");
        return symbol;
    }
    /// <summary>Decodes a linked GS1 Composite or standalone component, with an optional carrier interpretation.</summary>
    public static bool TryDecodeComposite(BitMatrix modules, out DetectedSymbol symbol, Gs1CompositeCarrier? carrier = null) {
        return TryDecodeCompositeCore(modules, out symbol, carrier, tolerateSamplingErrors: false);
    }

    internal static bool TryDecodeCompositeImage(BitMatrix modules, out DetectedSymbol symbol, Gs1CompositeCarrier? carrier) =>
        TryDecodeCompositeCore(modules, out symbol, carrier, tolerateSamplingErrors: true);

    private static bool TryDecodeCompositeCore(BitMatrix modules, out DetectedSymbol symbol, Gs1CompositeCarrier? carrier, bool tolerateSamplingErrors) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        symbol = null!;
        if (carrier.HasValue && !Enum.IsDefined(typeof(Gs1CompositeCarrier), carrier.Value)) throw new ArgumentOutOfRangeException(nameof(carrier));
        var complete = tolerateSamplingErrors ? Gs1CompositeDecoder.TryDecodeImage(modules, carrier, out var composite)
            : Gs1CompositeDecoder.TryDecode(modules, carrier, out composite);
        if (!complete) {
            var standalone = tolerateSamplingErrors ? Gs1CompositeComponents.TryDecodeImage(modules, out var componentText, out var componentType)
                : Gs1CompositeComponents.TryDecode(modules, out componentText, out componentType);
            if (!standalone) return false;
            composite = new Gs1CompositeDecoded(string.Empty, componentText, componentType, carrier: null,
                componentRegion: new ImageRegion(0, 0, modules.Width, modules.Height));
        }
        symbol = SymbolResultFactory.From(composite);
        return true;
    }

    private static readonly SymbolFormat[] DetailedFormats = {
        SymbolFormat.D3Aqr, SymbolFormat.Gs1Composite, SymbolFormat.RmQrCode, SymbolFormat.MaxiCode, SymbolFormat.DotCode,
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
            if (Array.IndexOf(DetailedFormats, capability.Format) >= 0 || !capability.CanDecodeModules
                || capability.Format == SymbolFormat.Daft || capability.Format == SymbolFormat.Flattermarken) continue;
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
            case SymbolFormat.AustraliaPost:
                if (!AustraliaPost.AustraliaPostDecoder.TryDecode(modules, null, out var australian)) return false;
                symbol = new DetectedSymbol(format, australian.Value, metadata: new AustraliaPostSymbolMetadata(australian)); return true;
            case SymbolFormat.D3Aqr:
                if (!D3Aqr.D3AqrDecoder.TryDecode(modules, out var d3)) return false;
                symbol = SymbolResultFactory.From(d3);
                return true;
            case SymbolFormat.PosiCode:
                if (!PosiCode.PosiCodeDecoder.TryDecode(modules, out var posi)) return false;
                symbol = new DetectedSymbol(format, posi.Text, metadata: new PosiCodeSymbolMetadata(posi),
                    payloadProfile: posi.IsGs1 ? SymbolPayloadProfile.Gs1 : SymbolPayloadProfile.None);
                return true;
            case SymbolFormat.CodablockF:
                if (!CodablockF.CodablockFDecoder.TryDecode(modules, out var block)) return false;
                symbol = new DetectedSymbol(format, block.Text, metadata: new CodablockFSymbolMetadata(block),
                    payloadProfile: block.IsGs1 ? SymbolPayloadProfile.Gs1 : SymbolPayloadProfile.None);
                return true;
            case SymbolFormat.Code16K:
                if (!Code16K.Code16KDecoder.TryDecode(modules, out var code16K)) return false;
                symbol = new DetectedSymbol(format, code16K.Text, metadata: new Code16KSymbolMetadata(code16K),
                    payloadProfile: code16K.IsGs1 ? SymbolPayloadProfile.Gs1 : SymbolPayloadProfile.None);
                return true;
            case SymbolFormat.Code49:
                if (!Code49.Code49Decoder.TryDecode(modules, out var code49)) return false;
                symbol = new DetectedSymbol(format, code49.Text, metadata: new Code49SymbolMetadata(code49),
                    payloadProfile: code49.IsGs1 ? SymbolPayloadProfile.Gs1 : SymbolPayloadProfile.None);
                return true;
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
                return TryDecodeComposite(modules, out symbol);
            case SymbolFormat.CodeOne:
                if (!CodeOneDecoder.TryDecode(modules, out var codeOne)) return false;
                symbol = CodeOneDecoder.Result(codeOne); return true;
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
            case SymbolFormat.AztecRune:
                if (!AztecRune.TryDecode(modules, out var rune)) return false;
                symbol = AztecRune.Result(rune); return true;
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
