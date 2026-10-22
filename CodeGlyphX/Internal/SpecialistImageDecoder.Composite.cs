using System;
using System.Threading;

namespace CodeGlyphX.Internal;

internal static partial class SpecialistImageDecoder {
    internal sealed class CompositeImageCandidate {
        internal readonly DetectedSymbol Symbol;
        internal readonly SymbolGeometry Geometry;
        internal readonly bool Inverted, Mirrored;
        internal CompositeImageCandidate(DetectedSymbol symbol, SymbolGeometry geometry, bool inverted, bool mirrored) {
            Symbol = symbol; Geometry = geometry; Inverted = inverted; Mirrored = mirrored;
        }
    }

    private static bool UseCompositeFallback(ImageContext context, SymbolFormat format, out DetectedSymbol symbol,
        out SymbolGeometry geometry, out bool inverted, out bool mirrored) {
        var fallback = format == SymbolFormat.Gs1Composite ? context.CompositeFallback : null;
        symbol = fallback?.Symbol!; geometry = fallback?.Geometry!;
        inverted = fallback?.Inverted ?? false; mirrored = fallback?.Mirrored ?? false;
        return fallback is not null;
    }

    private static CompositeImageCandidate MapCompositeCandidate(CompositeImageCandidate candidate, Func<SymbolPoint, SymbolPoint> map, bool invert = false) {
        var geometry = candidate.Geometry;
        var mapped = new SymbolGeometry(map(geometry.TopLeft), map(geometry.TopRight), map(geometry.BottomRight), map(geometry.BottomLeft));
        var symbol = candidate.Symbol;
        if (symbol.Metadata is Gs1CompositeSymbolMetadata metadata) symbol = symbol.WithMetadata(metadata.MapImageGeometry(map));
        return new CompositeImageCandidate(symbol, mapped, candidate.Inverted ^ invert, candidate.Mirrored);
    }

    private static bool UseCompositeGridFallback((DetectedSymbol Symbol, int Rotation, bool Mirrored)? fallback,
        out DetectedSymbol symbol, out int rotation, out bool mirrored) {
        symbol = fallback?.Symbol!; rotation = fallback?.Rotation ?? 0; mirrored = fallback?.Mirrored ?? false;
        return fallback.HasValue;
    }

    private static bool TryDecodeCompositeImageGrid(BitMatrix ink, Gs1CompositeCarrier? carrier,
        CancellationToken token, out DetectedSymbol symbol) {
        DetectedSymbol? partial = null;
        if (SymbolDecoder.TryDecodeCompositeImage(ink, out symbol, carrier)) {
            if (symbol.Metadata is not Gs1CompositeSymbolMetadata { IsStandalone: true }) return true;
            partial = symbol;
        }
        if (ink.Width > 600 || ink.Height > 90) { symbol = partial!; return partial is not null; }
        if (TryRetailCompositeGuardRows(ink, carrier, token, out symbol)) return true;
        // Projection bounds omit light guards. Expanded has a one-module guard at each end;
        // Limited has a one-module leading guard and a five-module trailing guard. CC-B can retain
        // the left edge through its wider component. Restore only these defined carrier margins.
        foreach (var trailing in new[] { 0, 1, 5 }) for (var leading = 0; leading <= 1; leading++) {
            if (trailing == 0 && leading == 0) continue;
            if (DecodeBudget.ShouldAbort(token)) { symbol = partial!; return partial is not null; }
            var modules = new BitMatrix(ink.Width + leading + trailing, ink.Height);
            for (var y = 0; y < ink.Height; y++) for (var x = 0; x < ink.Width; x++) modules[x + leading, y] = ink[x, y];
            var found = SymbolDecoder.TryDecodeCompositeImage(modules, out symbol, carrier);
            if (found && symbol.Metadata is Gs1CompositeSymbolMetadata { IsStandalone: true }) {
                var metadata = (Gs1CompositeSymbolMetadata)symbol.Metadata;
                partial ??= symbol.WithMetadata(metadata.WithLeadingModules(leading));
                found = false;
            }
            if (found || TryRetailCompositeGuardRows(modules, carrier, token, out symbol)) {
                if (symbol.Metadata is Gs1CompositeSymbolMetadata metadata)
                    symbol = symbol.WithMetadata(metadata.WithLeadingModules(leading));
                return true;
            }
        }
        if (partial is not null) { symbol = partial; return true; }
        // A damaged or cropped carrier must not hide a complete, checked upper component.
        // Recover only the component's own rectangle; never infer a missing linear message.
        for (var rows = 3; rows < ink.Height && rows <= 90; rows++) {
            if (DecodeBudget.ShouldAbort(token)) return false;
            var left = ink.Width;
            var right = -1;
            for (var y = 0; y < rows; y++) for (var x = 0; x < ink.Width; x++) {
                if (!ink[x, y]) continue;
                if (x < left) left = x;
                if (x > right) right = x;
            }
            if (right < left) continue;
            var component = new BitMatrix(right - left + 1, rows);
            for (var y = 0; y < rows; y++) for (var x = left; x <= right; x++) component[x - left, y] = ink[x, y];
            if (!Gs1CompositeComponents.TryDecodeImage(component, out var text, out var type)) continue;
            symbol = SymbolResultFactory.From(new Gs1CompositeDecoded(string.Empty, text, type, carrier: null,
                componentRegion: new ImageRegion(left, 0, component.Width, rows)));
            return true;
        }
        return false;
    }
}
