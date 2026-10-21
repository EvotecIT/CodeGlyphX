using System.Collections.Generic;
using CodeGlyphX.Internal;

namespace CodeGlyphX;

public static partial class SymbolScanner {
    private static void ScanCalibratedGrid(byte[] rgba, int width, int height, ImageRegion searchRegion,
        ScanOptions options, ScanDeadline deadline, SymbolFormat format, List<DetectedSymbol> results, HashSet<string>? seen) {
        using var attempt = deadline.CreateAttempt(1, options.Image?.RecognitionBudgetMilliseconds ?? 0);
        if (format == SymbolFormat.Ultracode) {
            ScanUltracodeCore(rgba, width, height, searchRegion, options, attempt, results, seen);
            return;
        }
        if (!SpecialistImageDecoder.TryPrepare(rgba, width, height, allowUniform: true, attempt.Token, out var context)) return;
        if (SpecialistImageDecoder.TryDecode(context, format, options.SamplingGrid, attempt.Token,
                out var decoded, out var geometry, out var inverted, out var mirrored, options.CompositeCarrier)
            && ApplyPostalInterpretation(decoded, options, out decoded))
            Add(results, seen, WithImageEvidence(decoded, searchRegion, geometry, width, height, inverted, mirrored));
    }

    private static void ScanSpecialists(byte[] rgba, int width, int height, ImageRegion searchRegion,
        ScanOptions options, ScanDeadline deadline, ISet<SymbolFormat> requested, List<DetectedSymbol> results,
        HashSet<string>? seen, ref int remainingAttempts) {
        SpecialistImageDecoder.ImageContext? context = null;
        foreach (var format in SpecialistImageDecoder.Formats) {
            if (!requested.Contains(format)) continue;
            if (ShouldStop(options, deadline, results)) return;
            using var attempt = deadline.CreateAttempt(remainingAttempts--, options.Image?.RecognitionBudgetMilliseconds ?? 0);
            if (context is null && !SpecialistImageDecoder.TryPrepare(rgba, width, height, options.SamplingGrid is not null,
                attempt.Token, out context)) continue;
            var success = SpecialistImageDecoder.TryDecode(context, format, options.SamplingGrid, attempt.Token,
                    out var decoded, out var geometry, out var inverted, out var mirrored, options.CompositeCarrier);
            if (context.ResourceLimitReached) deadline.MarkResourceLimitReached();
            if (!success || !ApplyPostalInterpretation(decoded, options, out decoded)) continue;
            Add(results, seen, WithImageEvidence(decoded, searchRegion, geometry, width, height, inverted, mirrored));
        }
    }

    private static bool ApplyPostalInterpretation(DetectedSymbol source, ScanOptions options, out DetectedSymbol selected) {
        selected = source;
        if (source.Format != SymbolFormat.AustraliaPost || (!options.AustraliaPostCustomerTable.HasValue && !options.AustraliaPostCustomerInfoLength.HasValue)) return true;
        if (source.Metadata is not AustraliaPostSymbolMetadata metadata
            || !metadata.Data.TrySelectCustomerInfo(options.AustraliaPostCustomerTable, options.AustraliaPostCustomerInfoLength, out var choice)) return false;
        selected = new DetectedSymbol(source.Format, choice.Value, metadata: new AustraliaPostSymbolMetadata(choice));
        return true;
    }

    private static DetectedSymbol WithImageEvidence(DetectedSymbol decoded, ImageRegion region, SymbolGeometry geometry,
        int width, int height, bool inverted, bool mirrored) {
        var metadata = decoded.Metadata is Gs1CompositeSymbolMetadata composite
            ? composite.MapImageGeometry(point => MapPointToSource(point, region, width, height)) : decoded.Metadata;
        return new DetectedSymbol(decoded.Format, decoded.Text,
            decoded.HasRawBytes ? decoded.RawBytes.ToArray() : null, metadata, decoded.PayloadProfile,
            region, MapGeometryToSource(geometry, region, width, height),
            isInverted: inverted, isMirrored: mirrored, symbologyIdentifier: decoded.SymbologyIdentifier);
    }
}
