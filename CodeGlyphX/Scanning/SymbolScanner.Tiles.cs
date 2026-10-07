using System;
using System.Collections.Generic;

namespace CodeGlyphX;

public static partial class SymbolScanner {
    // Relocated from the Playground: bounded overlapping tiles improve matrix recognition without a second host pipeline.
    private static void ScanMatrixTiles(byte[] rgba, int width, int height, ImageRegion sourceRegion,
        ScanOptions options, ScanDeadline deadline, ISet<SymbolFormat> requested,
        List<DetectedSymbol> results, HashSet<string>? seen) {
        if (!requested.Contains(SymbolFormat.DataMatrix) && !requested.Contains(SymbolFormat.Aztec) &&
            !requested.Contains(SymbolFormat.Pdf417)) return;
        var grid = options.TileGrid == 0 ? (Math.Max(width, height) >= 720 ? 3 : 2) : options.TileGrid;
        var padding = Math.Max(8, Math.Min(width, height) / 40);
        for (var ty = 0; ty < grid; ty++) {
            for (var tx = 0; tx < grid; tx++) {
                if (ShouldStop(options, deadline, results)) return;
                var x0 = Math.Max(0, tx * width / grid - padding);
                var y0 = Math.Max(0, ty * height / grid - padding);
                var x1 = Math.Min(width, (tx + 1) * width / grid + padding);
                var y1 = Math.Min(height, (ty + 1) * height / grid + padding);
                var tileWidth = x1 - x0;
                var tileHeight = y1 - y0;
                if (tileWidth < 48 || tileHeight < 48) continue;
                var tile = new byte[checked(tileWidth * tileHeight * 4)];
                for (var y = 0; y < tileHeight; y++) {
                    if (deadline.ShouldStop) return;
                    Buffer.BlockCopy(rgba, ((y0 + y) * width + x0) * 4, tile, y * tileWidth * 4, tileWidth * 4);
                }
                var sx = sourceRegion.X + (int)((long)x0 * sourceRegion.Width / width);
                var sy = sourceRegion.Y + (int)((long)y0 * sourceRegion.Height / height);
                var right = sourceRegion.X + (int)(((long)x1 * sourceRegion.Width + width - 1) / width);
                var bottom = sourceRegion.Y + (int)(((long)y1 * sourceRegion.Height + height - 1) / height);
                var region = new ImageRegion(sx, sy, right - sx, bottom - sy);
                ScanDataMatrix(tile, tileWidth, tileHeight, region, options, deadline, requested, results, seen);
                if (!ShouldStop(options, deadline, results)) ScanPdf417(tile, tileWidth, tileHeight, region, deadline, requested, results, seen);
                if (!ShouldStop(options, deadline, results)) ScanAztec(tile, tileWidth, tileHeight, region, deadline, requested, results, seen);
            }
        }
    }
}
