using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CodeGlyphX;

/// <summary>
/// Authoritative runtime catalog of CodeGlyphX symbol capabilities.
/// </summary>
public static class SymbolCapabilities {
    private const SymbolCapabilityFlags Standard = SymbolCapabilityFlags.Encode | SymbolCapabilityFlags.DecodeModules;
    private const SymbolCapabilityFlags Image = Standard | SymbolCapabilityFlags.ScanImage;
    private const SymbolCapabilityFlags ImageGeometry = Image | SymbolCapabilityFlags.ReportsGeometry;
    private const SymbolCapabilityFlags ImageMulti = Image | SymbolCapabilityFlags.ScanMultiple;

    private static readonly SymbolCapability[] Items = {
        Entry(SymbolFormat.QrCode, "QR Code", SymbolFamily.Matrix,
            ImageMulti | SymbolCapabilityFlags.EciEncode | SymbolCapabilityFlags.EciDecode |
            SymbolCapabilityFlags.Gs1Encode | SymbolCapabilityFlags.Gs1Decode |
            SymbolCapabilityFlags.StructuredAppendEncode | SymbolCapabilityFlags.StructuredAppendDecode),
        Entry(SymbolFormat.D3Aqr, "D3 AQR (provisional)", SymbolFamily.Matrix,
            ImageGeometry | SymbolCapabilityFlags.EciEncode | SymbolCapabilityFlags.EciDecode |
            SymbolCapabilityFlags.Gs1Encode | SymbolCapabilityFlags.Gs1Decode),
        Entry(SymbolFormat.MicroQrCode, "Micro QR Code", SymbolFamily.Matrix,
            ImageMulti | SymbolCapabilityFlags.ReportsGeometry),
        Entry(SymbolFormat.RmQrCode, "Rectangular Micro QR Code", SymbolFamily.Matrix,
            ImageGeometry | SymbolCapabilityFlags.EciEncode | SymbolCapabilityFlags.EciDecode |
            SymbolCapabilityFlags.Gs1Encode | SymbolCapabilityFlags.Gs1Decode),
        Entry(SymbolFormat.Aztec, "Aztec Code", SymbolFamily.Matrix, ImageMulti | SymbolCapabilityFlags.EciEncode | SymbolCapabilityFlags.EciDecode),
        Entry(SymbolFormat.AztecRune, "Aztec Rune", SymbolFamily.Matrix, ImageGeometry),
        Entry(SymbolFormat.CodeOne, "Code One", SymbolFamily.Matrix, ImageGeometry | SymbolCapabilityFlags.Gs1Encode
            | SymbolCapabilityFlags.Gs1Decode | SymbolCapabilityFlags.EciEncode | SymbolCapabilityFlags.EciDecode),
        Entry(SymbolFormat.Ultracode, "Ultracode", SymbolFamily.Matrix, SymbolCapabilityFlags.Encode
            | SymbolCapabilityFlags.DecodeColorCells | SymbolCapabilityFlags.ScanImage
            | SymbolCapabilityFlags.ReportsGeometry | SymbolCapabilityFlags.ScanMultiple),
        Legacy(SymbolFormat.Code128, "Code 128", SymbolFamily.Linear, BarcodeType.Code128, ImageMulti),
        Legacy(SymbolFormat.Gs1Code128, "GS1-128", SymbolFamily.Linear, BarcodeType.GS1_128,
            ImageMulti | SymbolCapabilityFlags.Gs1Encode | SymbolCapabilityFlags.Gs1Decode),
        Legacy(SymbolFormat.Code39, "Code 39", SymbolFamily.Linear, BarcodeType.Code39, ImageMulti),
        Legacy(SymbolFormat.Code93, "Code 93", SymbolFamily.Linear, BarcodeType.Code93, ImageMulti),
        Legacy(SymbolFormat.Ean, "EAN", SymbolFamily.Linear, BarcodeType.EAN, ImageMulti),
        Legacy(SymbolFormat.Ean2, "EAN-2", SymbolFamily.Linear, BarcodeType.EAN2, ImageMulti),
        Legacy(SymbolFormat.Ean5, "EAN-5", SymbolFamily.Linear, BarcodeType.EAN5, ImageMulti),
        Legacy(SymbolFormat.UpcA, "UPC-A", SymbolFamily.Linear, BarcodeType.UPCA, ImageMulti),
        Legacy(SymbolFormat.UpcE, "UPC-E", SymbolFamily.Linear, BarcodeType.UPCE, ImageMulti),
        Legacy(SymbolFormat.Itf14, "ITF-14", SymbolFamily.Linear, BarcodeType.ITF14, ImageMulti),
        Legacy(SymbolFormat.Itf, "Interleaved 2 of 5", SymbolFamily.Linear, BarcodeType.ITF, ImageMulti),
        Legacy(SymbolFormat.Industrial2Of5, "Industrial 2 of 5", SymbolFamily.Linear, BarcodeType.Industrial2of5, ImageMulti),
        Legacy(SymbolFormat.Matrix2Of5, "Matrix 2 of 5", SymbolFamily.Linear, BarcodeType.Matrix2of5, ImageMulti),
        Legacy(SymbolFormat.Iata2Of5, "IATA 2 of 5", SymbolFamily.Linear, BarcodeType.IATA2of5, ImageMulti),
        Legacy(SymbolFormat.Coop2Of5, "COOP 2 of 5", SymbolFamily.Linear, BarcodeType.Coop2of5, ImageMulti),
        Legacy(SymbolFormat.Datalogic2Of5, "Datalogic 2 of 5", SymbolFamily.Linear, BarcodeType.Datalogic2of5, ImageMulti),
        Legacy(SymbolFormat.PatchCode, "Patch Code", SymbolFamily.Linear, BarcodeType.PatchCode, ImageMulti),
        Legacy(SymbolFormat.Codabar, "Codabar", SymbolFamily.Linear, BarcodeType.Codabar, ImageMulti),
        Legacy(SymbolFormat.Msi, "MSI", SymbolFamily.Linear, BarcodeType.MSI, ImageMulti),
        Legacy(SymbolFormat.Code11, "Code 11", SymbolFamily.Linear, BarcodeType.Code11, ImageMulti),
        Legacy(SymbolFormat.Bc412, "BC412", SymbolFamily.Linear, BarcodeType.Bc412, ImageMulti),
        Legacy(SymbolFormat.FacingIdentificationMark, "Facing Identification Mark", SymbolFamily.Linear, BarcodeType.FacingIdentificationMark, ImageGeometry),
        Legacy(SymbolFormat.Daft, "DAFT", SymbolFamily.Postal, BarcodeType.Daft, ImageGeometry),
        Legacy(SymbolFormat.Flattermarken, "Flattermarken", SymbolFamily.Linear, BarcodeType.Flattermarken, ImageGeometry),
        Legacy(SymbolFormat.ChannelCode, "Channel Code", SymbolFamily.Linear, BarcodeType.ChannelCode, ImageMulti),
        Legacy(SymbolFormat.PosiCode, "PosiCode", SymbolFamily.Linear, BarcodeType.PosiCode,
            ImageMulti | SymbolCapabilityFlags.Gs1Encode | SymbolCapabilityFlags.Gs1Decode),
        Legacy(SymbolFormat.Mailmark4State, "Mailmark 4-State", SymbolFamily.Postal, BarcodeType.Mailmark4State, ImageGeometry),
        Legacy(SymbolFormat.Code16K, "Code 16K", SymbolFamily.Stacked, BarcodeType.Code16K,
            ImageGeometry | SymbolCapabilityFlags.Gs1Encode | SymbolCapabilityFlags.Gs1Decode |
            SymbolCapabilityFlags.StructuredAppendEncode | SymbolCapabilityFlags.StructuredAppendDecode),
        Legacy(SymbolFormat.Code49, "Code 49", SymbolFamily.Stacked, BarcodeType.Code49,
            ImageGeometry | SymbolCapabilityFlags.Gs1Encode | SymbolCapabilityFlags.Gs1Decode |
            SymbolCapabilityFlags.StructuredAppendEncode | SymbolCapabilityFlags.StructuredAppendDecode),
        Legacy(SymbolFormat.CodablockF, "Codablock F", SymbolFamily.Stacked, BarcodeType.CodablockF,
            ImageGeometry | SymbolCapabilityFlags.Gs1Encode | SymbolCapabilityFlags.Gs1Decode),
        Legacy(SymbolFormat.Plessey, "Plessey", SymbolFamily.Linear, BarcodeType.Plessey, ImageMulti),
        Legacy(SymbolFormat.Telepen, "Telepen", SymbolFamily.Linear, BarcodeType.Telepen, ImageMulti),
        Legacy(SymbolFormat.TelepenNumeric, "Telepen Numeric", SymbolFamily.Linear, BarcodeType.TelepenNumeric, ImageMulti),
        Legacy(SymbolFormat.Pharmacode, "Pharmacode", SymbolFamily.Linear, BarcodeType.Pharmacode, ImageMulti),
        Legacy(SymbolFormat.PharmacodeTwoTrack, "Two-track Pharmacode", SymbolFamily.Stacked, BarcodeType.PharmacodeTwoTrack, ImageGeometry),
        Legacy(SymbolFormat.Code32, "Code 32", SymbolFamily.Linear, BarcodeType.Code32, ImageMulti),
        Legacy(SymbolFormat.Postnet, "POSTNET", SymbolFamily.Postal, BarcodeType.Postnet, ImageGeometry),
        Legacy(SymbolFormat.Planet, "PLANET", SymbolFamily.Postal, BarcodeType.Planet, ImageGeometry),
        Legacy(SymbolFormat.RoyalMail4State, "Royal Mail 4-State", SymbolFamily.Postal, BarcodeType.RoyalMail4State, ImageGeometry),
        Legacy(SymbolFormat.AustraliaPost, "Australia Post", SymbolFamily.Postal, BarcodeType.AustraliaPost, ImageGeometry),
        Legacy(SymbolFormat.JapanPost, "Japan Post", SymbolFamily.Postal, BarcodeType.JapanPost, ImageGeometry),
        Legacy(SymbolFormat.Gs1DataBarTruncated, "GS1 DataBar Truncated", SymbolFamily.Linear, BarcodeType.GS1DataBarTruncated,
            ImageMulti | SymbolCapabilityFlags.Gs1Encode | SymbolCapabilityFlags.Gs1Decode),
        Legacy(SymbolFormat.Gs1DataBarOmnidirectional, "GS1 DataBar Omnidirectional", SymbolFamily.Linear, BarcodeType.GS1DataBarOmni,
            ImageMulti | SymbolCapabilityFlags.Gs1Encode | SymbolCapabilityFlags.Gs1Decode),
        Legacy(SymbolFormat.Gs1DataBarStacked, "GS1 DataBar Stacked", SymbolFamily.Stacked, BarcodeType.GS1DataBarStacked,
            Image | SymbolCapabilityFlags.Gs1Encode | SymbolCapabilityFlags.Gs1Decode),
        Legacy(SymbolFormat.Gs1DataBarExpanded, "GS1 DataBar Expanded", SymbolFamily.Linear, BarcodeType.GS1DataBarExpanded,
            ImageMulti | SymbolCapabilityFlags.Gs1Encode | SymbolCapabilityFlags.Gs1Decode),
        Legacy(SymbolFormat.Gs1DataBarExpandedStacked, "GS1 DataBar Expanded Stacked", SymbolFamily.Stacked, BarcodeType.GS1DataBarExpandedStacked,
            Image | SymbolCapabilityFlags.Gs1Encode | SymbolCapabilityFlags.Gs1Decode),
        Legacy(SymbolFormat.Gs1DataBarLimited, "GS1 DataBar Limited", SymbolFamily.Linear, BarcodeType.GS1DataBarLimited,
            ImageMulti | SymbolCapabilityFlags.Gs1Encode | SymbolCapabilityFlags.Gs1Decode),
        Legacy(SymbolFormat.Gs1DataBarStackedOmnidirectional, "GS1 DataBar Stacked Omnidirectional", SymbolFamily.Stacked, BarcodeType.GS1DataBarStackedOmni,
            Image | SymbolCapabilityFlags.Gs1Encode | SymbolCapabilityFlags.Gs1Decode),
        Legacy(SymbolFormat.MaxiCode, "MaxiCode", SymbolFamily.Matrix, BarcodeType.MaxiCode,
            ImageMulti | SymbolCapabilityFlags.EciEncode | SymbolCapabilityFlags.EciDecode |
            SymbolCapabilityFlags.StructuredAppendEncode | SymbolCapabilityFlags.StructuredAppendDecode),
        Legacy(SymbolFormat.DotCode, "DotCode", SymbolFamily.Matrix, BarcodeType.DotCode,
            ImageGeometry | SymbolCapabilityFlags.EciEncode | SymbolCapabilityFlags.EciDecode |
            SymbolCapabilityFlags.Gs1Encode | SymbolCapabilityFlags.Gs1Decode |
            SymbolCapabilityFlags.StructuredAppendEncode | SymbolCapabilityFlags.StructuredAppendDecode),
        Legacy(SymbolFormat.HanXin, "Han Xin Code", SymbolFamily.Matrix, BarcodeType.HanXin,
            ImageGeometry | SymbolCapabilityFlags.EciEncode | SymbolCapabilityFlags.EciDecode),
        Legacy(SymbolFormat.Gs1Composite, "GS1 Composite", SymbolFamily.Stacked, BarcodeType.GS1Composite,
            ImageGeometry | SymbolCapabilityFlags.Gs1Encode | SymbolCapabilityFlags.Gs1Decode),
        Legacy(SymbolFormat.UspsIntelligentMail, "USPS Intelligent Mail", SymbolFamily.Postal, BarcodeType.UspsImb, ImageGeometry),
        Legacy(SymbolFormat.KixCode, "KIX Code", SymbolFamily.Postal, BarcodeType.KixCode, ImageGeometry),
        Legacy(SymbolFormat.DataMatrix, "Data Matrix", SymbolFamily.Matrix, BarcodeType.DataMatrix,
            ImageMulti | SymbolCapabilityFlags.EciEncode | SymbolCapabilityFlags.EciDecode |
            SymbolCapabilityFlags.Gs1Encode | SymbolCapabilityFlags.Gs1Decode |
            SymbolCapabilityFlags.StructuredAppendEncode | SymbolCapabilityFlags.StructuredAppendDecode),
        Legacy(SymbolFormat.Pdf417, "PDF417", SymbolFamily.Stacked, BarcodeType.PDF417,
            ImageMulti | SymbolCapabilityFlags.EciEncode | SymbolCapabilityFlags.EciDecode),
        Legacy(SymbolFormat.MicroPdf417, "MicroPDF417", SymbolFamily.Stacked, BarcodeType.MicroPDF417,
            ImageGeometry | SymbolCapabilityFlags.EciEncode | SymbolCapabilityFlags.EciDecode)
    };

    private static readonly ReadOnlyCollection<SymbolCapability> ReadOnlyItems = Array.AsReadOnly(Items);
    private static readonly IReadOnlyDictionary<SymbolFormat, SymbolCapability> ByFormat = CreateLookup();
    private static readonly ReadOnlyCollection<SymbolFormat> ImageFormats = CreateImageFormats();

    /// <summary>Gets every known symbol capability.</summary>
    public static IReadOnlyList<SymbolCapability> All => ReadOnlyItems;
    /// <summary>Gets formats currently supported by the unified image scanner.</summary>
    public static IReadOnlyList<SymbolFormat> ImageScannableFormats => ImageFormats;

    /// <summary>Gets the capability record for a format.</summary>
    public static SymbolCapability Get(SymbolFormat format) {
        if (!ByFormat.TryGetValue(format, out var capability)) {
            throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown symbol format.");
        }
        return capability;
    }

    /// <summary>Attempts to get the capability record for a format.</summary>
    public static bool TryGet(SymbolFormat format, out SymbolCapability capability) {
        return ByFormat.TryGetValue(format, out capability!);
    }

    internal static bool TryFromLegacy(BarcodeType type, out SymbolFormat format) {
        for (var i = 0; i < Items.Length; i++) {
            if (Items[i].LegacyBarcodeType == type) {
                format = Items[i].Format;
                return true;
            }
        }
        format = default;
        return false;
    }

    internal static BarcodeType GetBarcodeType(SymbolFormat format) {
        var capability = Get(format);
        return capability.LegacyBarcodeType ?? throw new NotSupportedException($"{format} is not represented by the barcode encoder.");
    }

    private static SymbolCapability Entry(SymbolFormat format, string name, SymbolFamily family, SymbolCapabilityFlags operations) {
        return new SymbolCapability(format, name, family, operations);
    }

    private static SymbolCapability Legacy(SymbolFormat format, string name, SymbolFamily family, BarcodeType legacy, SymbolCapabilityFlags operations) {
        return new SymbolCapability(format, name, family, operations, legacy);
    }

    private static IReadOnlyDictionary<SymbolFormat, SymbolCapability> CreateLookup() {
        var result = new Dictionary<SymbolFormat, SymbolCapability>();
        for (var i = 0; i < Items.Length; i++) {
            result.Add(Items[i].Format, Items[i]);
        }
        return new ReadOnlyDictionary<SymbolFormat, SymbolCapability>(result);
    }

    private static ReadOnlyCollection<SymbolFormat> CreateImageFormats() {
        var result = new List<SymbolFormat>();
        for (var i = 0; i < Items.Length; i++) {
            if (Items[i].CanScanImages) result.Add(Items[i].Format);
        }
        return result.AsReadOnly();
    }
}
