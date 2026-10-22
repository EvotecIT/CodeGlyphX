using System;
using System.Threading;

namespace CodeGlyphX.Internal;

internal static partial class SpecialistImageDecoder {
    /// <summary>Normalizes only the defined retail guard extensions below a complete Composite carrier.</summary>
    /// <remarks>The retained rows still pass the full component/linkage/separator/carrier checks. Every removed
    /// or merged band must match the expected guard or supplement pattern exactly; this is image sampling,
    /// not tolerance in the public sampled-module decoder.</remarks>
    private static bool TryRetailCompositeGuardRows(BitMatrix ink, Gs1CompositeCarrier? hint,
        CancellationToken token, out DetectedSymbol symbol) {
        symbol = null!;
        if (hint.HasValue && hint is not (Gs1CompositeCarrier.Ean13 or Gs1CompositeCarrier.Ean8 or Gs1CompositeCarrier.UpcA or Gs1CompositeCarrier.UpcE)) return false;
        if (ink.Height < 8 || ink.Height > 50) return false;
        var first = hint.HasValue ? (int)hint.Value : (int)Gs1CompositeCarrier.Ean13;
        var last = hint.HasValue ? first : (int)Gs1CompositeCarrier.UpcE;
        for (var kind = first; kind <= last; kind++) for (var addon = 0; addon <= 1; addon++) {
            if (DecodeBudget.ShouldAbort(token)) return false;
            var carrier = (Gs1CompositeCarrier)kind;
            var componentRows = ink.Height - (addon == 0 ? 5 : 6);
            if (componentRows < 3 || componentRows > 44) continue;
            var mainWidth = carrier == Gs1CompositeCarrier.UpcE ? 51 : carrier == Gs1CompositeCarrier.Ean8 ? 67 : 95;
            var start = 0;
            while (start < ink.Width && !ink[start, componentRows]) start++;
            if (start + mainWidth > ink.Width) continue;
            var candidate = new BitMatrix(ink.Width, ink.Height - 1);
            for (var y = 0; y < componentRows + 3; y++) for (var x = 0; x < ink.Width; x++) candidate[x, y] = ink[x, y];
            var dataRow = componentRows + 3 + addon;
            var tailRow = ink.Height - 1;
            var matches = true;
            for (var x = 0; x < ink.Width; x++) {
                var relative = x - start;
                var inMain = relative >= 0 && relative < mainWidth;
                var guard = inMain && RetailGuardColumn(carrier, mainWidth, relative);
                var data = ink[x, dataRow];
                var tail = data && (guard || addon != 0 && relative >= mainWidth);
                if (ink[x, tailRow] != tail || addon != 0 && ink[x, componentRows + 3] != (data && inMain)) { matches = false; break; }
                candidate[x, componentRows + 3] = data;
                if (addon != 0) candidate[x, componentRows + 4] = data && relative >= mainWidth;
            }
            if (!matches || !Gs1CompositeDecoder.TryDecodeImage(candidate, carrier, out var decoded)
                || decoded.LinearRegion is not { } linear || decoded.LinearText.IndexOf('+') >= 0 != (addon != 0)) continue;
            // Metadata continues to use the original sampled row coordinates, including the guard descent.
            var physicalLinear = new ImageRegion(linear.X, componentRows + 3, linear.Width, addon == 0 ? 2 : 3);
            symbol = SymbolResultFactory.From(new Gs1CompositeDecoded(decoded.LinearText, decoded.CompositeText, decoded.Component,
                decoded.Carrier, decoded.ComponentRegion, physicalLinear));
            return true;
        }
        return false;
    }

    private static bool RetailGuardColumn(Gs1CompositeCarrier carrier, int width, int x) => carrier switch {
        Gs1CompositeCarrier.UpcE => x < 3 || x >= 45,
        Gs1CompositeCarrier.UpcA => x < 10 || x >= 85 || x is >= 45 and < 50,
        _ => x < 3 || x >= width - 3 || x >= width / 2 - 2 && x <= width / 2 + 2
    };
}
