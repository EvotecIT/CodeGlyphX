using System;
using System.Collections.Generic;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Printing;

public static partial class BarcodePrint {
    private static double[] MatrixRowHeights(BitMatrix modules, SymbolFormat format, PrintLayoutBuilder b) {
        if (modules.Width < 1 || modules.Height < 1 || (long)modules.Width * modules.Height > 1048576)
            throw new ArgumentException("Print module grids are limited to 1048576 cells.", nameof(modules));
        var rows = new double[modules.Height];
        var height = b.Options.BarHeightMm;
        for (var i = 0; i < rows.Length; i++) rows[i] = b.X;
        if (format is SymbolFormat.Pdf417 or SymbolFormat.MicroPdf417) {
            for (var i = 0; i < rows.Length; i++) rows[i] = 3 * b.X;
        } else if (format == SymbolFormat.Gs1DataBarStacked && rows.Length == 3) {
            rows[0] = 5 * b.X; rows[1] = b.X; rows[2] = 7 * b.X;
        } else if (format == SymbolFormat.Gs1DataBarStackedOmnidirectional && rows.Length == 5) {
            rows[0] = rows[4] = 33 * b.X;
        } else if (format == SymbolFormat.Gs1DataBarExpandedStacked) {
            // Encoded data rows alternate with the three one-module separator rows.
            for (var i = 0; i < rows.Length; i += 4) rows[i] = 34 * b.X;
        }
        if (height.HasValue) {
            if (format is SymbolFormat.QrCode or SymbolFormat.MicroQrCode or SymbolFormat.RmQrCode or SymbolFormat.DataMatrix
                or SymbolFormat.Aztec or SymbolFormat.AztecRune or SymbolFormat.HanXin or SymbolFormat.DotCode or SymbolFormat.CodeOne or SymbolFormat.D3Aqr)
                b.Finding("SquareModuleHeight", true, "The requested bar height does not apply to a two-dimensional module grid; its module aspect ratio was preserved.");
            else {
                GrowStackedRows(modules, format, rows, height.Value, b);
            }
        }
        return rows;
    }

    private static void GrowStackedRows(BitMatrix modules, SymbolFormat format, double[] rows, double requested, PrintLayoutBuilder b) {
        var data = new bool[rows.Length];
        if (format is SymbolFormat.Gs1DataBarStacked or SymbolFormat.Gs1DataBarStackedOmnidirectional) data[0] = data[data.Length - 1] = true;
        else if (format == SymbolFormat.Gs1DataBarExpandedStacked) for (var y = 0; y < data.Length; y += 4) data[y] = true;
        else if (format is SymbolFormat.Code16K or SymbolFormat.Code49 or SymbolFormat.CodablockF or SymbolFormat.PosiCode) {
            for (var y = 0; y < rows.Length;) {
                var end = y + 1;
                while (end < rows.Length && EqualRows(modules, y, end)) end++;
                if (end - y >= 2 && RowHasInk(modules, y)) for (var at = y; at < end; at++) data[at] = true;
                y = end;
            }
        } else for (var y = 0; y < data.Length; y++) data[y] = true;
        var fixedHeight = 0D; var dataHeight = 0D;
        for (var y = 0; y < rows.Length; y++) if (data[y]) dataHeight += rows[y]; else fixedHeight += rows[y];
        if (requested < fixedHeight + dataHeight - 1e-7 || dataHeight == 0) {
            b.Finding("StackedRowHeight", true, "The requested height is below this layout's row minimum. Its data rows and separators retain their minimum geometry.");
            return;
        }
        var factor = (requested - fixedHeight) / dataHeight;
        for (var y = 0; y < rows.Length; y++) if (data[y]) rows[y] *= factor;
    }

    private static bool EqualRows(BitMatrix grid, int first, int second) {
        for (var x = 0; x < grid.Width; x++) if (grid[x, first] != grid[x, second]) return false;
        return true;
    }
    private static bool RowHasInk(BitMatrix grid, int row) {
        for (var x = 0; x < grid.Width; x++) if (grid[x, row]) return true;
        return false;
    }

    private static bool IsFourState(SymbolFormat f) => f is SymbolFormat.UspsIntelligentMail or SymbolFormat.RoyalMail4State
        or SymbolFormat.AustraliaPost or SymbolFormat.JapanPost or SymbolFormat.KixCode or SymbolFormat.Daft or SymbolFormat.Mailmark4State;

    private static void Circle(PrintLayoutBuilder b, double x, double y, double radius, BarcodePaint paint, string role, double inner = 0) {
        var contours = new List<IEnumerable<BarcodePoint>> { Ring(x, y, radius, false) };
        if (inner > 0) contours.Add(Ring(x, y, inner, true));
        b.Path(contours, paint, role);
    }

    private static BarcodePoint[] Ring(double x, double y, double radius, bool reverse) {
        const int count = 96;
        var points = new BarcodePoint[count];
        for (var i = 0; i < count; i++) {
            var angle = (reverse ? -i : i) * Math.PI * 2 / count;
            points[i] = new BarcodePoint(x + Math.Cos(angle) * radius, y + Math.Sin(angle) * radius);
        }
        return points;
    }

    /// <summary>Preserves MaxiCode's staggered hexagons and finder rings within its permitted physical size range.</summary>
    public static BarcodePrintLayout Layout(MaxiCodeSymbol symbol, BarcodePrintOptions? options = null) {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));
        // ISO/IEC 16023 lattice: short hexagon diameter X, row pitch sqrt(3)/2 X.
        var pitch = options?.XDimensionMm ?? .88;
        var b = new PrintLayoutBuilder(SymbolFormat.MaxiCode, options, pitch, 24D / 29, 27D / 29, fixedSize: true);
        var q = b.Options.IncludeQuietZones ? pitch : 0;
        var rowStep = Math.Sqrt(3) / 2 * pitch;
        var radius = pitch / Math.Sqrt(3);
        var height = 32 * rowStep + 2 * radius;
        for (var y = 0; y < 33; y++) for (var x = 0; x < 30 - (y & 1); x++) {
            if (!symbol.Modules[x, y]) continue;
            var cx = q + (x + .5 + (y & 1) * .5) * pitch;
            var cy = q + y * rowStep + radius;
            var hex = new BarcodePoint[6];
            for (var i = 0; i < 6; i++) {
                var angle = Math.PI / 6 + i * Math.PI / 3;
                hex[i] = new BarcodePoint(cx + Math.Cos(angle) * radius, cy + Math.Sin(angle) * radius);
            }
            b.Path(new[] { hex }, b.Options.Foreground, "hexagons");
        }
        var firstDiameter = radius * 2;
        var increment = (9 * pitch - firstDiameter) / 5;
        var stroke = increment / 2;
        for (var i = 1; i <= 5; i += 2) {
            var outerRadius = (firstDiameter + increment * i) / 2;
            Circle(b, q + 14.5 * pitch, q + height / 2, outerRadius, b.Options.Foreground, "finder", outerRadius - stroke);
        }
        var bounds = new BarcodeBounds(0, 0, pitch * 30 + q * 2, height + q * 2);
        b.OrdinaryText(bounds);
        return b.Finish(bounds);
    }

    /// <summary>Preserves the 46 mm Swiss QR data area and its 7 mm recognition cross.</summary>
    public static BarcodePrintLayout Layout(SwissQrSymbol symbol, BarcodePrintOptions? options = null) {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));
        var b = new PrintLayoutBuilder(SymbolFormat.QrCode, options, 46D / symbol.Modules.Width, fixedSize: true);
        var q = b.Options.IncludeQuietZones ? 4 * b.X : 0;
        var clear = q + (46 - 7) / 2;
        // Clip the original modules away from the overlay so alpha does not repaint hidden data.
        for (var y = 0; y < symbol.Modules.Height; y++) for (var x = 0; x < symbol.Modules.Width; x++) {
            if (!symbol.Modules[x, y]) continue;
            var cell = new BarcodeBounds(q + x * b.X, q + y * b.X, b.X, b.X);
            var overlay = new BarcodeBounds(clear, clear, 7, 7);
            RectOutside(b, cell, overlay);
        }
        var white = BarcodePaint.Solid(Rgba32.White);
        var black = BarcodePaint.Solid(Rgba32.Black);
        b.Rect(clear, clear, 7, 7, white, "swiss-cross");
        var unit = 7D / 83;
        b.Rect(clear + 6 * unit, clear + 6 * unit, 71 * unit, 71 * unit, black, "swiss-cross");
        b.Rect(clear + 34 * unit, clear + 18 * unit, 15 * unit, 47 * unit, white, "swiss-cross");
        b.Rect(clear + 18 * unit, clear + 34 * unit, 47 * unit, 15 * unit, white, "swiss-cross");
        var bounds = new BarcodeBounds(0, 0, 46 + 2 * q, 46 + 2 * q);
        b.OrdinaryText(bounds);
        return b.Finish(bounds);
    }

    private static void RectOutside(PrintLayoutBuilder b, BarcodeBounds cell, BarcodeBounds hole) {
        var left = Math.Max(cell.X, hole.X); var right = Math.Min(cell.Right, hole.Right);
        var top = Math.Max(cell.Y, hole.Y); var bottom = Math.Min(cell.Bottom, hole.Bottom);
        if (left >= right || top >= bottom) { b.Rect(cell.X, cell.Y, cell.Width, cell.Height); return; }
        b.Rect(cell.X, cell.Y, cell.Width, top - cell.Y);
        b.Rect(cell.X, bottom, cell.Width, cell.Bottom - bottom);
        b.Rect(cell.X, top, left - cell.X, bottom - top);
        b.Rect(right, top, cell.Right - right, bottom - top);
    }

    /// <summary>Positions fixed Ultracode data colours without replacing them with decorative paints.</summary>
    public static BarcodePrintLayout Layout(UltracodeSymbol symbol, BarcodePrintOptions? options = null) {
        if (symbol is null) throw new ArgumentNullException(nameof(symbol));
        var b = new PrintLayoutBuilder(SymbolFormat.Ultracode, options, .495);
        if (!IsColor(b.Options.Foreground, Rgba32.Black) || !IsColor(b.Options.Background, Rgba32.White))
            throw new ArgumentException("Ultracode print requires its opaque black, white and fixed data palette.", nameof(options));
        var q = b.Options.IncludeQuietZones ? b.X : 0;
        for (var y = 0; y < symbol.Cells.Height; y++) for (var x = 0; x < symbol.Cells.Width; x++) {
            var color = symbol.Cells[x, y] switch {
                UltracodeColor.White => Rgba32.White, UltracodeColor.Black => Rgba32.Black,
                UltracodeColor.Cyan => new Rgba32(0, 255, 255), UltracodeColor.Magenta => new Rgba32(255, 0, 255),
                UltracodeColor.Yellow => new Rgba32(255, 255, 0), UltracodeColor.Green => new Rgba32(0, 255, 0),
                _ => throw new ArgumentException("Unknown Ultracode cells cannot be printed.", nameof(symbol))
            };
            if (symbol.Cells[x, y] != UltracodeColor.White) b.Rect(q + x * b.X, q + y * b.X, b.X, b.X, BarcodePaint.Solid(color), "colour-cells");
        }
        var bounds = new BarcodeBounds(0, 0, (symbol.Cells.Width + (b.Options.IncludeQuietZones ? 2 : 0)) * b.X,
            (symbol.Cells.Height + (b.Options.IncludeQuietZones ? 2 : 0)) * b.X);
        b.OrdinaryText(bounds);
        return b.Finish(bounds);
    }

    private static bool IsColor(BarcodePaint p, Rgba32 c) => p.Kind == BarcodePaint.PaintKind.Solid
        && p.StartColor.R == c.R && p.StartColor.G == c.G && p.StartColor.B == c.B && p.StartColor.A == c.A;
}
