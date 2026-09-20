using System;
using System.Collections.Generic;
using CodeGlyphX.Rendering.Art;
using CodeGlyphX.Rendering;

namespace CodeGlyphX;

public static partial class QrArt {
    private static bool CanRenderLayout(QrCode code, QrImageCompositionOptions options) {
        var side = (code.Size + 2 * options.QuietZone + 2 * (options.Canvas?.PaddingModules ?? 0)) * options.ModuleSize;
        try {
            RenderGuards.EnsureOutputPixels(side, side, "Artwork search output exceeds limits.");
            RenderGuards.EnsureOutputBytes((long)side * side * 4, "Artwork search output exceeds limits.");
            return true;
        } catch (ArgumentException) { return false; }
    }

    private static IReadOnlyList<QrImageCompositionOptions> BuildImageLayouts(QrImageSearchOptions options) {
        var source = options.Composition;
        var layouts = new List<QrImageCompositionOptions> { source.WithModuleSize(source.ModuleSize) };
        if (!options.ExploreLayouts) return layouts;
        // Keep the search bounded. Each alternative changes crop, placement and relative QR size;
        // source fidelity and actual recognition choose the winner rather than an assumed safe corner.
        for (var i = 0; i < 4; i++) {
            var layout = source.WithModuleSize(source.ModuleSize);
            layout.ImagePositionX = i % 2 == 0 ? 0.25 : 0.75;
            layout.ImagePositionY = i / 2 == 0 ? 0.25 : 0.75;
            layout.ImageZoom = Math.Min(4, source.ImageZoom * (i % 2 == 0 ? 1 : 1.15));
            layout.Canvas = new QrImageCanvasOptions {
                PaddingModules = Math.Max(source.Canvas?.PaddingModules ?? 0, i % 2 == 0 ? 8 : 12),
                PositionX = i % 2 == 0 ? 0.15 : 0.85,
                PositionY = i / 2 == 0 ? 0.15 : 0.85
            };
            layouts.Add(layout);
        }
        return layouts;
    }
}
