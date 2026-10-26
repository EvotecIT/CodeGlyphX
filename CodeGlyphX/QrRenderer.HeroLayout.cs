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
            QrModuleShape.Leaf => QrLogoPlateShape.Leaf,
            QrModuleShape.Classy => QrLogoPlateShape.Classy,
            _ => QrLogoPlateShape.RoundedSquare
        };

    private static void ApplyHeroLayout(QrPngRenderOptions render, QrCode qr, QrHeroPreset hero) {
        var full = (qr.Size + render.QuietZone * 2) * render.ModuleSize;
        if (hero == QrHeroPreset.MinimalLine) {
            render.TimingThicknessModules = .2;
            if (qr.Version >= 5 && render.ModuleShape == QrModuleShape.Dot && render.ModuleScale == .6875) {
                render.ModuleScale = .825; // Dot's 0.8 factor gives a 66% diameter on dense codes.
            }
        }
        // Leaf bodies lose their shape when dense and reduced. Whole mirrored leaf finders remain.
        if (hero == QrHeroPreset.Botanical && qr.Version >= 5 && render.ModuleShape == QrModuleShape.Leaf) {
            render.ModuleShape = QrModuleShape.ConnectedRounded;
            render.ModuleCornerRadiusPx = Math.Max(1, (int)Math.Round(render.ModuleSize * .18));
        }
        if (hero == QrHeroPreset.Brand && render.Eyes?.OuterCornerRadiusPx == 16) render.Eyes.OuterCornerRadiusPx = render.ModuleSize;
        if (hero is QrHeroPreset.Brand or QrHeroPreset.Berry && render.ModuleShape == QrModuleShape.Rounded)
            render.ModuleCornerRadiusPx = Math.Max(1, (int)Math.Round(render.ModuleSize * .2));
        if (qr.Version >= 5 && hero is QrHeroPreset.Ink or QrHeroPreset.PaperCutout && render.ModuleShape == QrModuleShape.Liquid)
            render.ModuleCornerRadiusPx = Math.Max(1, (int)Math.Round(render.ModuleSize * .35));
        if (hero == QrHeroPreset.RetroPixel) {
            render.ModuleShadowOffsetPx = Math.Max(1, (int)Math.Ceiling(render.ModuleSize * .2));
            render.ModuleShadowColor = new Rgba32(255, 79, 109);
        }
        if (render.Canvas is null) return;
        var canvas = render.Canvas;
        if (canvas.Template != QrCanvasTemplate.None) return;
        var darkGround = canvas.Background.R + canvas.Background.G + canvas.Background.B < 300;
        // These small spacing differences preserve the silhouettes after reduction to 256 px.
        var darkPadding = hero switch {
            QrHeroPreset.DotMatrix => .19,
            QrHeroPreset.Botanical => .191,
            QrHeroPreset.MinimalLine => .184,
            QrHeroPreset.RetroPixel => .182,
            QrHeroPreset.Ocean or QrHeroPreset.Sunset => .18,
            _ => .181
        };
        canvas.PaddingPx = Math.Max(canvas.PaddingPx, (int)Math.Ceiling(full * (darkGround ? darkPadding : .065)));
        canvas.QrCornerRadiusPx = render.Eyes?.OuterShape switch {
            QrModuleShape.Square => 0,
            QrModuleShape.Circle => Math.Max(2, (int)Math.Round(full * .035)),
            QrModuleShape.Squircle => Math.Max(2, (int)Math.Round(full * .04)),
            _ => Math.Max(2, (int)Math.Round(full * .025))
        };
        if (darkGround) {
            canvas.PanelGlowRadiusPx = Math.Max(1, (int)Math.Round(full * .025));
        }
        if (hero == QrHeroPreset.Neon) {
            canvas.PanelGlowRadiusPx = Math.Max(1, (int)Math.Round(full * .03125));
            canvas.SecondaryPanelGlowRadiusPx = Math.Max(1, (int)Math.Round(full * .046875));
        }
        if (hero == QrHeroPreset.PaperCutout) {
            canvas.ShadowOnPanelsOnly = darkGround;
            canvas.StackedPanelOffsetPx = Math.Max(1, (int)Math.Round(full * .012));
            canvas.ShadowOffsetX = 0;
            canvas.ShadowOffsetY = Math.Max(1, (int)Math.Round(full * .012));
            canvas.ShadowBlurPx = Math.Max(1, (int)Math.Round(full * .03125));
        }
    }
}
