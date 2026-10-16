using System.Collections.Generic;
using CodeGlyphX.Internal;

namespace CodeGlyphX;

public static partial class SymbolScanner {
    private static void ScanCalibratedGrid(byte[] rgba, int width, int height, ImageRegion searchRegion,
        ScanOptions options, ScanDeadline deadline, SymbolFormat format, List<DetectedSymbol> results, HashSet<string>? seen) {
        using var attempt = deadline.CreateAttempt(1, options.Image?.RecognitionBudgetMilliseconds ?? 0);
        if (!SpecialistImageDecoder.TryPrepare(rgba, width, height, allowUniform: true, attempt.Token, out var context)) return;
        if (SpecialistImageDecoder.TryDecode(context, format, options.SamplingGrid, attempt.Token,
                out var decoded, out var geometry, out var inverted, out var mirrored))
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
            if (!SpecialistImageDecoder.TryDecode(context, format, options.SamplingGrid, attempt.Token,
                    out var decoded, out var geometry, out var inverted, out var mirrored)) continue;
            Add(results, seen, WithImageEvidence(decoded, searchRegion, geometry, width, height, inverted, mirrored));
        }
    }

    private static DetectedSymbol WithImageEvidence(DetectedSymbol decoded, ImageRegion region, SymbolGeometry geometry,
        int width, int height, bool inverted, bool mirrored) => new(decoded.Format, decoded.Text,
            decoded.HasRawBytes ? decoded.RawBytes.ToArray() : null, decoded.Metadata, decoded.PayloadProfile,
            region, MapGeometryToSource(geometry, region, width, height),
            isInverted: inverted, isMirrored: mirrored, symbologyIdentifier: decoded.SymbologyIdentifier);
}
