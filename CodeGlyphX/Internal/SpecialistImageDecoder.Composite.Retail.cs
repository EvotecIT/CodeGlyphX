using System;
using System.Threading;

namespace CodeGlyphX.Internal;

internal static partial class SpecialistImageDecoder {
    /// <summary>Uses a checked upper component as a physical lattice anchor when retail lettering splits bar bands.</summary>
    /// <remarks>Samples the three linkage separators and corroborated middle bar rows at that same lattice.
    /// Every result still passes the complete Composite decoder. Text and guard descent are not payload rows.</remarks>
    private static bool TryRetailCompositeFromComponent(ImageContext context, DetectedSymbol partial, bool inverted,
        Gs1CompositeCarrier? hint, CancellationToken token, out DetectedSymbol symbol, out SymbolGeometry geometry) {
        symbol = null!; geometry = null!;
        if (partial.Metadata is not Gs1CompositeSymbolMetadata { IsStandalone: true, ComponentGeometry: { } anchor, ComponentModuleRegion: { } region } metadata
            || metadata.Component == Gs1CompositeComponent.CcC || region.Width > 99 || region.Height > 44
            || hint.HasValue && hint is not (Gs1CompositeCarrier.Ean13 or Gs1CompositeCarrier.Ean8 or Gs1CompositeCarrier.UpcA or Gs1CompositeCarrier.UpcE)) return false;
        foreach (var previous in context.RetailCompositeAnchors) {
            if (Math.Abs(previous.TopLeft.X - anchor.TopLeft.X) + Math.Abs(previous.TopLeft.Y - anchor.TopLeft.Y)
                + Math.Abs(previous.BottomRight.X - anchor.BottomRight.X) + Math.Abs(previous.BottomRight.Y - anchor.BottomRight.Y) < 2) return false;
        }
        if (context.RetailCompositeAnchors.Count >= 16) return false;
        context.RetailCompositeAnchors.Add(anchor);
        var ux = (anchor.TopRight.X - anchor.TopLeft.X) / region.Width;
        var uy = (anchor.TopRight.Y - anchor.TopLeft.Y) / region.Width;
        var vx = (anchor.BottomLeft.X - anchor.TopLeft.X) / (region.Height * 2);
        var vy = (anchor.BottomLeft.Y - anchor.TopLeft.Y) / (region.Height * 2);
        var verticalPitch = Math.Sqrt(vx * vx + vy * vy);
        if (verticalPitch < .75 || verticalPitch > 128) return false;
        var origin = anchor.TopLeft;
        RefineCompositeHorizontal(context, region, inverted, vx, vy, token, ref origin, ref ux, ref uy);
        SymbolPoint Point(double x, double y) => new(origin.X + x * ux + y * vx, origin.Y + x * uy + y * vy);
        bool Sample(double x, double y, out bool dark) {
            var p = Point(x, y);
            var px = (int)Math.Floor(p.X); var py = (int)Math.Floor(p.Y);
            dark = false;
            if ((uint)px >= (uint)context.Width || (uint)py >= (uint)context.Height) return false;
            dark = ((context.Mask[py * context.Width + px] & 1) != 0) != inverted;
            return true;
        }
        var component = new BitMatrix(region.Width, region.Height);
        for (var y = 0; y < region.Height; y++) for (var x = 0; x < region.Width; x++) {
            if (!Sample(x + .5, y * 2 + 1, out var dark)) return false;
            component[x, y] = dark;
        }
        if (!Gs1CompositeComponents.TryDecodeImage(component, out var checkedText, out var type)
            || checkedText != metadata.CompositeText || type != metadata.Component) return false;
        var first = hint.HasValue ? (int)hint.Value : (int)Gs1CompositeCarrier.Ean13;
        var last = hint.HasValue ? first : (int)Gs1CompositeCarrier.UpcE;
        for (var kind = first; kind <= last; kind++) for (var addon = 2; addon >= 0; addon--) {
            if (DecodeBudget.ShouldAbort(token)) return false;
            var carrier = (Gs1CompositeCarrier)kind;
            var main = carrier == Gs1CompositeCarrier.UpcE ? 51 : carrier == Gs1CompositeCarrier.Ean8 ? 67 : 95;
            var offset = Gs1Composite.CompositeCarrierCodec.RetailOffset(carrier, type);
            var bodyWidth = main + (addon == 0 ? 2 : 10 + (addon == 1 ? 20 : 47));
            var width = Math.Max(component.Width, bodyWidth + offset);
            var dataLeft = offset + 1;
            var bodyTop = region.Height * 2 + 6;
            var candidate = new BitMatrix(width, region.Height + (addon == 0 ? 4 : 5));
            for (var y = 0; y < region.Height; y++) for (var x = 0; x < component.Width; x++) candidate[x, y] = component[x, y];
            var valid = true;
            for (var y = 0; y < 3 && valid; y++) for (var x = 0; x < width; x++) {
                if (!Sample(x + .5, region.Height * 2 + y * 2 + 1, out var dark)) { valid = false; break; }
                candidate[x, region.Height + y] = dark;
            }
            if (!valid) continue;
            // These middle-bar positions lie below a standard retail supplement's text and above its baseline.
            // Corroboration four modules apart rejects a thin text stroke mistaken for a data row.
            foreach (var distance in new[] { 16, 32, 48 }) {
                if (DecodeBudget.ShouldAbort(token)) return false;
                valid = true;
                for (var x = 0; x < bodyWidth; x++) {
                    if (!Sample(offset + x + .5, bodyTop + distance, out var dark)
                        || !Sample(offset + x + .5, bodyTop + distance + 4, out var corroborated) || dark != corroborated) { valid = false; break; }
                    candidate[offset + x, region.Height + 3] = dark;
                    if (addon != 0) candidate[offset + x, region.Height + 4] = x > main && dark;
                }
                // A five-digit add-on may have a valid two-digit prefix. Require the actual trailing
                // clear zone before selecting a shorter supplement, and try supplements before the main alone.
                if (addon != 0) for (var x = 0; x < 5 && valid; x++) {
                    if (!Sample(offset + bodyWidth + x + .5, bodyTop + distance, out var ink) || ink) valid = false;
                }
                if (!valid || !Gs1CompositeDecoder.TryDecodeImage(candidate, carrier, out var decoded)
                    || decoded.CompositeText != metadata.CompositeText || decoded.LinearRegion is not { } linear) continue;
                var bottom = FindGuardBottom(bodyTop, distance, dataLeft, main, carrier, candidate, region.Height + 3,
                    verticalPitch, Sample, token);
                if (bottom <= bodyTop + distance) continue;
                SymbolGeometry Quad(double left, double top, double right, double end) => new(
                    Point(left, top), Point(right, top), Point(right, end), Point(left, end));
                var linearGeometry = Quad(linear.X, bodyTop, linear.X + linear.Width, bottom);
                var result = SymbolResultFactory.From(decoded);
                var componentGeometry = Quad(0, 0, component.Width, component.Height * 2);
                symbol = result.WithMetadata(new Gs1CompositeSymbolMetadata(decoded, componentGeometry: componentGeometry, linearGeometry: linearGeometry));
                geometry = Quad(0, 0, width, bottom);
                return true;
            }
        }
        return false;
    }

    private static void RefineCompositeHorizontal(ImageContext context, ImageRegion region, bool inverted, double vx, double vy,
        CancellationToken token, ref SymbolPoint origin, ref double ux, ref double uy) {
        var pitch = Math.Sqrt(ux * ux + uy * uy);
        if (pitch < .75 || pitch > 128) return;
        var left = double.PositiveInfinity; var right = double.NegativeInfinity;
        // Lettering can enlarge a whole-image projection by a fractional module. Measure the checked
        // component's own left/right printed edges before extrapolating its lattice to a wider add-on.
        for (var row = 0; row < region.Height; row++) {
            if (DecodeBudget.ShouldAbort(token)) return;
            var limit = (int)Math.Ceiling((region.Width + 2) * pitch);
            for (var at = -(int)Math.Ceiling(2 * pitch); at < limit; at++) {
                if ((at & 1023) == 0 && DecodeBudget.ShouldAbort(token)) return;
                var along = (at + .5) / pitch;
                var px = (int)Math.Floor(origin.X + along * ux + (row * 2 + 1) * vx);
                var py = (int)Math.Floor(origin.Y + along * uy + (row * 2 + 1) * vy);
                if ((uint)px >= (uint)context.Width || (uint)py >= (uint)context.Height) continue;
                if (((context.Mask[py * context.Width + px] & 1) != 0) == inverted) continue;
                left = Math.Min(left, at); right = Math.Max(right, at + 1);
            }
        }
        var measured = (right - left) / region.Width;
        if (!BarcodeFinite(measured) || measured / pitch is < .95 or > 1.05 || Math.Abs(left) > pitch * 2) return;
        origin = new SymbolPoint(origin.X + left / pitch * ux, origin.Y + left / pitch * uy);
        ux *= measured / pitch; uy *= measured / pitch;
    }

    private static bool BarcodeFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    private delegate bool CompositePixelSample(double x, double y, out bool dark);

    private static double FindGuardBottom(double top, int distance, int left, int width, Gs1CompositeCarrier carrier,
        BitMatrix data, int row, double pitch, CompositePixelSample sample, CancellationToken token) {
        var step = 1 / pitch;
        var bottom = top + distance;
        for (var index = 0; index < 4096 && bottom < top + 512; index++, bottom += step) {
            if ((index & 31) == 0 && DecodeBudget.ShouldAbort(token)) return 0;
            for (var x = 0; x < width; x++) {
                if (!RetailGuardColumn(carrier, width, x) || !data[left + x, row]) continue;
                if (!sample(left + x + .5, bottom + step / 2, out var dark) || !dark) return bottom;
            }
        }
        return 0;
    }
}
