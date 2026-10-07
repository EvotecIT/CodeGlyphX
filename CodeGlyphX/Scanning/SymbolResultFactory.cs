using CodeGlyphX.DataMatrix;
using CodeGlyphX.Pdf417;

namespace CodeGlyphX;

/// <summary>Projects specialist decoder results once into the stable common result contract.</summary>
internal static class SymbolResultFactory {
    internal static DetectedSymbol From(QrDecoded value, ImageRegion? region = null) =>
        new(SymbolFormat.QrCode, value.Text, value.Bytes, new QrSymbolMetadata(value),
            value.Fnc1Mode == QrFnc1Mode.None ? SymbolPayloadProfile.None : SymbolPayloadProfile.Gs1, region);
    internal static DetectedSymbol From(MicroQrDecoded value, ImageRegion? region = null,
        SymbolGeometry? geometry = null, bool? inverted = null, bool? mirrored = null) =>
        new(SymbolFormat.MicroQrCode, value.Text, value.Bytes, new MicroQrSymbolMetadata(value),
            searchRegion: region, geometry: geometry, isInverted: inverted, isMirrored: mirrored);
    internal static DetectedSymbol From(RmQrDecoded value) =>
        new(SymbolFormat.RmQrCode, value.Text, value.Bytes, new RmQrSymbolMetadata(value),
            value.IsGs1 ? SymbolPayloadProfile.Gs1 : SymbolPayloadProfile.None);
    internal static DetectedSymbol From(DataMatrixDecoded value, ImageRegion? region = null,
        DirectPartMarkProfile? directPartMarkProfile = null) =>
        new(SymbolFormat.DataMatrix, value.Text, metadata: new DataMatrixSymbolMetadata(value),
            payloadProfile: value.IsGs1 ? SymbolPayloadProfile.Gs1 : SymbolPayloadProfile.None,
            searchRegion: region, directPartMarkProfile: directPartMarkProfile);
    internal static DetectedSymbol From(Pdf417Decoded value, ImageRegion? region = null) =>
        new(SymbolFormat.Pdf417, value.Text, metadata: new Pdf417SymbolMetadata(value), searchRegion: region);
    internal static DetectedSymbol From(MaxiCodeDecoded value) =>
        new(SymbolFormat.MaxiCode, value.Text, value.Bytes, new MaxiCodeSymbolMetadata(value), symbologyIdentifier: value.SymbologyIdentifier);
    internal static DetectedSymbol From(DotCodeDecoded value) =>
        new(SymbolFormat.DotCode, value.Text, value.Bytes, new DotCodeSymbolMetadata(value),
            value.HasFnc1 ? SymbolPayloadProfile.Gs1 : SymbolPayloadProfile.None, symbologyIdentifier: value.SymbologyIdentifier);
    internal static DetectedSymbol From(HanXinDecoded value) =>
        new(SymbolFormat.HanXin, value.Text, value.Bytes, new HanXinSymbolMetadata(value), symbologyIdentifier: value.SymbologyIdentifier);
    internal static DetectedSymbol From(Gs1CompositeDecoded value) =>
        new(SymbolFormat.Gs1Composite, value.Text, metadata: new Gs1CompositeSymbolMetadata(value), payloadProfile: SymbolPayloadProfile.Gs1);
    internal static DetectedSymbol From(BarcodeDecoded value, ImageRegion? region = null) {
        if (!SymbolCapabilities.TryFromLegacy(value.Type, out var format))
            throw new System.InvalidOperationException("The decoder returned an unknown physical format.");
        return new DetectedSymbol(format, value.Text, payloadProfile: IsGs1(format) ? SymbolPayloadProfile.Gs1 : SymbolPayloadProfile.None,
            searchRegion: region);
    }
    private static bool IsGs1(SymbolFormat format) => format == SymbolFormat.Gs1Code128 ||
        format == SymbolFormat.Gs1DataBarTruncated || format == SymbolFormat.Gs1DataBarOmnidirectional ||
        format == SymbolFormat.Gs1DataBarStacked || format == SymbolFormat.Gs1DataBarExpanded ||
        format == SymbolFormat.Gs1DataBarExpandedStacked || format == SymbolFormat.Gs1DataBarLimited ||
        format == SymbolFormat.Gs1DataBarStackedOmnidirectional;
}
