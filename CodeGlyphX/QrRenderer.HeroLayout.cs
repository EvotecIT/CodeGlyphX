using System;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX;

internal static partial class QrRenderer {
    private static QrLogoPlateShape ResolvePlateShape(QrRenderOptions options) => options.LogoPlateShape != QrLogoPlateShape.MatchEyes
        ? options.LogoPlateShape : options.Eyes?.OuterShape switch {
            QrModuleShape.Circle or QrModuleShape.Dot => QrLogoPlateShape.Circle,
            QrModuleShape.Squircle => QrLogoPlateShape.Squircle,
            QrModuleShape.Square => QrLogoPlateShape.Square,
            _ => QrLogoPlateShape.RoundedSquare
        };

    private static void ApplyHeroLayout(QrPngRenderOptions render, QrCode qr, QrHeroPreset hero) {
        var full = (qr.Size + render.QuietZone * 2) * render.ModuleSize;
        // Leaf bodies lose their shape when dense and reduced. Whole mirrored leaf finders remain.
        if (hero == QrHeroPreset.MinimalLine) render.TimingThicknessModules = .2;
        if (hero == QrHeroPreset.Botanical && qr.Version >= 5 && render.ModuleShape == QrModuleShape.Leaf) render.ModuleShape = QrModuleShape.Liquid;
        if (hero == QrHeroPreset.RetroPixel) {
            render.ModuleShadowOffsetPx = Math.Max(1, (int)Math.Round(full * 2 / 1024.0));
            render.ModuleShadowColor = new Rgba32(255, 79, 109);
        }
        if (render.Canvas is null) return;
        var canvas = render.Canvas;
        canvas.PaddingPx = Math.Max(canvas.PaddingPx, (int)Math.Round(full * .065));
        canvas.QrCornerRadiusPx = hero == QrHeroPreset.RetroPixel ? 0 : Math.Max(2, (int)Math.Round(full * .024));
        if (hero == QrHeroPreset.Neon) canvas.PanelGlowRadiusPx = Math.Max(1, (int)Math.Round(full * .022));
        if (hero == QrHeroPreset.PaperCutout) {
            canvas.StackedPanelOffsetPx = Math.Max(1, (int)Math.Round(full * .012));
            canvas.ShadowOffsetX = 0;
            canvas.ShadowOffsetY = Math.Max(1, (int)Math.Round(full * .012));
            canvas.ShadowBlurPx = Math.Max(1, (int)Math.Round(full * .03125));
        }
    }
}
