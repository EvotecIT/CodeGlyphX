using System;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Printing;

public static partial class BarcodePrint {
    /// <summary>Positions a standalone CC-A/B/C component with its required row aspect ratio.</summary>
    /// <remarks>A standalone component has no linked linear payload or carrier text.</remarks>
    public static BarcodePrintLayout Layout(Gs1CompositeComponentSymbol symbol, BarcodePrintOptions? options = null) {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));
        var b = new PrintLayoutBuilder(SymbolFormat.Gs1Composite, options, .330);
        var q = b.Options.IncludeQuietZones ? 2 * b.X : 0;
        var rowHeight = b.X * (symbol.Component == Gs1CompositeComponent.CcC ? 3 : 2);
        DrawRows(b, symbol.Modules, 0, symbol.Height, q, q, rowHeight);
        var bounds = new BarcodeBounds(0, 0, symbol.Width * b.X + 2 * q, symbol.Height * rowHeight + 2 * q);
        b.OrdinaryText(bounds);
        return b.Finish(bounds);
    }

    private static BarcodePrintLayout Composite(Gs1CompositeSymbol symbol, BarcodePrintOptions? options) {
        var retail = symbol.Carrier is Gs1CompositeCarrier.Ean13 or Gs1CompositeCarrier.Ean8 or Gs1CompositeCarrier.UpcA or Gs1CompositeCarrier.UpcE;
        var nominal = symbol.Carrier == Gs1CompositeCarrier.Gs1_128 ? .495 : .330;
        var b = new PrintLayoutBuilder(SymbolFormat.Gs1Composite, options, nominal, retail ? .264 : 0, retail ? .660 : double.PositiveInfinity);
        var qx = b.Options.IncludeQuietZones ? (retail ? 11 : symbol.Carrier == Gs1CompositeCarrier.Gs1_128 ? 10 : 2) * b.X : 0;
        var qy = b.Options.IncludeQuietZones ? 2 * b.X : 0;
        var componentHeight = symbol.ComponentRows * b.X * (symbol.Component == Gs1CompositeComponent.CcC ? 3 : 2);
        DrawRows(b, symbol.Modules, 0, symbol.ComponentRows, qx, qy,
            b.X * (symbol.Component == Gs1CompositeComponent.CcC ? 3 : 2));
        var carrierTop = qy + componentHeight;
        BarcodeBounds bounds;
        if (retail) {
            DrawRows(b, symbol.Modules, symbol.ComponentRows, 3, qx, carrierTop, 2 * b.X);
            var first = 0;
            while (first < symbol.Width && !symbol.Modules[first, symbol.ComponentRows]) first++;
            var format = symbol.Carrier switch {
                Gs1CompositeCarrier.UpcA => SymbolFormat.UpcA,
                Gs1CompositeCarrier.UpcE => SymbolFormat.UpcE,
                _ => SymbolFormat.Ean
            };
            var suffix = symbol.LinearText.IndexOf('+');
            var addOnWidth = suffix < 0 ? 0 : symbol.LinearText.Length - suffix - 1 == 2 ? 20 : 47;
            var mainWidth = symbol.Carrier == Gs1CompositeCarrier.UpcE ? 51 : symbol.Carrier == Gs1CompositeCarrier.Ean8 ? 67 : 95;
            var linear = RetailRuns(symbol.Modules, symbol.ComponentRows + 3, first, mainWidth + (addOnWidth > 0 ? 9 + addOnWidth : 0), format);
            if (!TryRetail(linear, format, out var data)) throw new ArgumentException("The Composite retail carrier cannot be represented.", nameof(symbol));
            var o = b.Options.Snapshot();
            o.Format = null; o.XDimensionMm = b.X; o.SnapToPrinterPixels = false; o.Caption = null;
            var layout = Retail(linear, data, o);
            var retailLeftQuiet = b.Options.IncludeQuietZones ? (format is SymbolFormat.UpcA or SymbolFormat.UpcE ? 9 : symbol.Carrier == Gs1CompositeCarrier.Ean8 ? 7 : 11) * b.X : 0;
            var dx = qx + first * b.X - retailLeftQuiet;
            var dy = carrierTop + 6 * b.X;
            b.Append(layout, dx, dy);
            bounds = new BarcodeBounds(0, 0, symbol.Width * b.X + 2 * qx, qy + componentHeight + 6 * b.X)
                .Union(layout.SymbolBounds.Translate(dx, dy));
            // The retail layout owns both derived digit groups and explicit text overrides.
            b.Options.HumanReadableText = null;
        } else {
            DrawRows(b, symbol.Modules, symbol.ComponentRows, 1, qx, carrierTop, b.X);
            var bodyRows = symbol.Height - symbol.ComponentRows - 1;
            var format = symbol.Carrier switch {
                Gs1CompositeCarrier.Gs1_128 => SymbolFormat.Gs1Code128,
                Gs1CompositeCarrier.DataBarExpandedStacked => SymbolFormat.Gs1DataBarExpandedStacked,
                Gs1CompositeCarrier.DataBarStacked => SymbolFormat.Gs1DataBarStacked,
                Gs1CompositeCarrier.DataBarStackedOmnidirectional => SymbolFormat.Gs1DataBarStackedOmnidirectional,
                Gs1CompositeCarrier.DataBarLimited => SymbolFormat.Gs1DataBarLimited,
                Gs1CompositeCarrier.DataBarTruncated => SymbolFormat.Gs1DataBarTruncated,
                Gs1CompositeCarrier.DataBarExpanded => SymbolFormat.Gs1DataBarExpanded,
                _ => SymbolFormat.Gs1DataBarOmnidirectional
            };
            var grid = new BitMatrix(symbol.Width, bodyRows);
            for (var y = 0; y < bodyRows; y++) for (var x = 0; x < symbol.Width; x++) grid[x, y] = symbol.Modules[x, y + symbol.ComponentRows + 1];
            var heights = MatrixRowHeights(grid, format, b);
            if (bodyRows == 1) heights[0] = b.Options.BarHeightMm ?? LinearHeight(format, symbol.Width, b.X);
            var height = carrierTop + b.X;
            for (var y = 0; y < bodyRows; y++) {
                DrawRows(b, grid, y, 1, qx, height, heights[y]);
                height += heights[y];
            }
            bounds = new BarcodeBounds(0, 0, symbol.Width * b.X + 2 * qx, height + qy);
        }
        b.OrdinaryText(bounds);
        return b.Finish(bounds);
    }

    private static Barcode1D RetailRuns(BitMatrix modules, int row, int left, int width, SymbolFormat format) {
        var runs = new System.Collections.Generic.List<BarSegment>();
        for (var x = 0; x < width;) {
            var bar = modules[left + x, row];
            var start = x++;
            while (x < width && modules[left + x, row] == bar) x++;
            runs.Add(new BarSegment(bar, x - start));
        }
        return new Barcode1D(runs, format);
    }

    private static void DrawRows(PrintLayoutBuilder b, BitMatrix modules, int startRow, int count,
        double left, double top, double rowHeight) {
        for (var y = 0; y < count; y++) for (var x = 0; x < modules.Width;) {
            if (!modules[x, y + startRow]) { x++; continue; }
            var first = x++;
            while (x < modules.Width && modules[x, y + startRow]) x++;
            b.Rect(left + first * b.X, top + y * rowHeight, (x - first) * b.X, rowHeight);
        }
    }
}
