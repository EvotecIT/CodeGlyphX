using CodeGlyphX;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Art;

namespace CodeGlyphX.Examples;

/// <summary>
/// The appearance of every look in the website QR gallery. The text inside each <c>#region recipe</c>
/// block is published on the gallery page exactly as written, so each block uses public API only and
/// stands on its own: no helpers, no constants from elsewhere. <see cref="QrGalleryLooks"/> renders these
/// methods, and the scan tests decode what they render.
/// </summary>
internal static class QrGalleryRecipes {
    internal static QrRenderOptions Sunset() {
        #region recipe sunset
        var options = QrArt.Hero(QrHeroPreset.Sunset);
        #endregion
        return options;
    }


    internal static QrRenderOptions MinimalLine() {
        #region recipe minimal-line
        var options = QrArt.Hero(QrHeroPreset.MinimalLine);
        // Fuller dots than the preset keep a logo readable on long URLs at small sizes.
        options.ModuleScale = .9;
        #endregion
        return options;
    }


    internal static QrRenderOptions Botanical() {
        #region recipe botanical
        var options = QrArt.Hero(QrHeroPreset.Botanical);
        #endregion
        return options;
    }

    internal static QrRenderOptions BotanicalDark() {
        #region recipe botanical-dark
        var options = QrArt.Hero(QrHeroPreset.Botanical, darkPanel: true);
        #endregion
        return options;
    }

    internal static QrRenderOptions NeonDark() {
        #region recipe neon-dark
        var options = QrArt.Hero(QrHeroPreset.Neon, darkPanel: true);
        #endregion
        return options;
    }

    internal static QrRenderOptions Neon() {
        #region recipe neon
        var options = QrArt.Hero(QrHeroPreset.Neon);
        #endregion
        return options;
    }

    internal static QrRenderOptions Ink() {
        #region recipe ink
        var options = QrArt.Hero(QrHeroPreset.Ink);
        #endregion
        return options;
    }

    internal static QrRenderOptions InkDark() {
        #region recipe ink-dark
        var options = QrArt.Hero(QrHeroPreset.Ink, darkPanel: true);
        #endregion
        return options;
    }

    internal static QrRenderOptions RetroPixel() {
        #region recipe retro-pixel
        var options = QrArt.Hero(QrHeroPreset.RetroPixel);
        #endregion
        return options;
    }

    internal static QrRenderOptions RetroPixelDark() {
        #region recipe retro-pixel-dark
        var options = QrArt.Hero(QrHeroPreset.RetroPixel, darkPanel: true);
        #endregion
        return options;
    }

    internal static QrRenderOptions MonoClassyDark() {
        #region recipe mono-classy-dark
        var options = QrArt.Hero(QrHeroPreset.MonoClassy, darkPanel: true);
        #endregion
        return options;
    }

    internal static QrRenderOptions MonoClassy() {
        #region recipe mono-classy
        var options = QrArt.Hero(QrHeroPreset.MonoClassy);
        #endregion
        return options;
    }

    internal static QrRenderOptions Ocean() {
        #region recipe ocean
        var options = QrArt.Hero(QrHeroPreset.Ocean);
        options.Background = new Rgba32(226, 247, 245);
        #endregion
        return options;
    }


    internal static QrRenderOptions Berry() {
        #region recipe berry
        var options = QrArt.Hero(QrHeroPreset.Berry);
        #endregion
        return options;
    }

    internal static QrRenderOptions BerryDark() {
        #region recipe berry-dark
        var options = QrArt.Hero(QrHeroPreset.Berry, darkPanel: true);
        #endregion
        return options;
    }

    internal static QrRenderOptions PaperCutout() {
        #region recipe paper-cutout
        var options = QrArt.Hero(QrHeroPreset.PaperCutout);
        #endregion
        return options;
    }

    internal static QrRenderOptions PaperCutoutDark() {
        #region recipe paper-cutout-dark
        var options = QrArt.Hero(QrHeroPreset.PaperCutout, darkPanel: true);
        #endregion
        return options;
    }

    internal static QrRenderOptions DotMatrix() {
        #region recipe dot-matrix
        var options = QrArt.Hero(QrHeroPreset.DotMatrix);
        options.Background = new Rgba32(232, 240, 253);
        #endregion
        return options;
    }

    internal static QrRenderOptions DotMatrixDark() {
        #region recipe dot-matrix-dark
        var options = QrArt.Hero(QrHeroPreset.DotMatrix, darkPanel: true);
        #endregion
        return options;
    }

    internal static QrRenderOptions BrandLogo() {
        #region recipe brand-logo
        var options = QrArt.Hero(QrHeroPreset.Brand);
        #endregion
        return options;
    }

    internal static QrRenderOptions BrandLogoDark() {
        #region recipe brand-logo-dark
        var options = QrArt.Hero(QrHeroPreset.Brand, darkPanel: true);
        #endregion
        return options;
    }

    internal static QrRenderOptions GoldSparkle() {
        #region recipe gold-sparkle
        var options = new QrRenderOptions {
            QuietZone = 4,
            ModuleShape = QrModuleShape.ConnectedRounded,
            Foreground = new Rgba32(92, 60, 4),
            Background = new Rgba32(255, 250, 232),
            Eyes = QrEyePresets.NeonSparkle(),
            Canvas = new QrCanvasOptions {
                PaddingPx = 44,
                CornerRadiusPx = 28,
                QrCornerRadiusPx = 16,
                BackgroundGradient = new QrGradientOptions {
                    Type = QrGradientType.DiagonalDown,
                    StartColor = new Rgba32(14, 12, 8),
                    EndColor = new Rgba32(70, 50, 12),
                },
            },
        };
        options.Eyes.OuterColor = new Rgba32(150, 98, 0);
        options.Eyes.InnerColor = new Rgba32(60, 38, 0);
        options.Eyes.SparkleColor = new Rgba32(255, 222, 140, 210);
        #endregion
        return options;
    }

    internal static QrRenderOptions Ticket() {
        #region recipe ticket
        var options = new QrRenderOptions {
            QuietZone = 4,
            ModuleShape = QrModuleShape.ConnectedRounded,
            Foreground = new Rgba32(24, 28, 52),
            Background = Rgba32.White,
            Eyes = new QrEyeOptions { OuterShape = QrModuleShape.Rounded, InnerShape = QrModuleShape.Circle },
            Canvas = new QrCanvasOptions {
                Template = QrCanvasTemplate.TicketStub,
                Background = new Rgba32(255, 92, 72),
                Caption = new QrCanvasCaptionOptions { Text = "ADMIT ONE", Weight = QrCanvasCaptionWeight.Bold },
            },
        };
        #endregion
        return options;
    }

    internal static QrRenderOptions Confetti() {
        #region recipe confetti
        var options = new QrRenderOptions {
            QuietZone = 4,
            ModuleShape = QrModuleShape.Rounded,
            Foreground = new Rgba32(204, 34, 84),
            Background = new Rgba32(255, 246, 236),
            Eyes = new QrEyeOptions { OuterShape = QrModuleShape.Rounded, InnerShape = QrModuleShape.Circle },
            ForegroundPalette = new QrPaletteOptions {
                Mode = QrPaletteMode.Random,
                Seed = 7,
                RingSize = 2,
                ApplyToEyes = false,
                Colors = new[] { new Rgba32(204, 34, 84), new Rgba32(112, 38, 196), new Rgba32(0, 112, 160) },
            },
        };
        #endregion
        return options;
    }

    internal static QrRenderOptions ScanMe() {
        #region recipe scan-me
        var options = new QrRenderOptions {
            QuietZone = 4,
            ModuleShape = QrModuleShape.Rounded,
            Foreground = new Rgba32(30, 34, 96),
            Background = Rgba32.White,
            Eyes = new QrEyeOptions { OuterShape = QrModuleShape.Rounded, InnerShape = QrModuleShape.Circle },
            Canvas = new QrCanvasOptions {
                Template = QrCanvasTemplate.ScanMePill,
                Background = new Rgba32(255, 210, 80),
                Caption = new QrCanvasCaptionOptions { Text = "SCAN ME", Weight = QrCanvasCaptionWeight.Bold },
            },
        };
        #endregion
        return options;
    }

    internal static QrRenderOptions GlowFrame() {
        #region recipe glow-frame
        var options = new QrRenderOptions {
            Art = QrArt.Theme(QrArtTheme.ConnectedSquircleGlow, QrArtVariant.Pastel),
        };
        #endregion
        return options;
    }


    internal static QrRenderOptions WifiCard() {
        #region recipe wi-fi-card
        var options = new QrRenderOptions {
            QuietZone = 4,
            ModuleShape = QrModuleShape.ConnectedSquircle,
            Foreground = new Rgba32(18, 52, 120),
            Background = Rgba32.White,
            Eyes = new QrEyeOptions { OuterShape = QrModuleShape.Rounded, InnerShape = QrModuleShape.Circle },
            Canvas = new QrCanvasOptions {
                Template = QrCanvasTemplate.RoundedCardHeader,
                Background = new Rgba32(36, 99, 235),
                Caption = new QrCanvasCaptionOptions { Text = "Join the Wi-Fi", Weight = QrCanvasCaptionWeight.Bold },
            },
        };
        #endregion
        return options;
    }

    internal static QrRenderOptions Viewfinder() {
        #region recipe viewfinder
        var options = new QrRenderOptions {
            QuietZone = 4,
            ModuleShape = QrModuleShape.ExtraRounded,
            Foreground = new Rgba32(168, 18, 52),
            Background = Rgba32.White,
            Eyes = new QrEyeOptions { OuterShape = QrModuleShape.Rounded, InnerShape = QrModuleShape.Circle },
            Canvas = new QrCanvasOptions {
                Template = QrCanvasTemplate.CornerViewfinder,
                StrokeColor = new Rgba32(168, 18, 52),
                Caption = new QrCanvasCaptionOptions { Text = "Point your camera", Weight = QrCanvasCaptionWeight.Bold },
            },
        };
        #endregion
        return options;
    }

    internal static QrRenderOptions MintLogo() {
        #region recipe mint-logo
        var options = new QrRenderOptions {
            QuietZone = 4,
            ModuleShape = QrModuleShape.Squircle,
            Foreground = new Rgba32(0, 112, 86),
            Background = new Rgba32(234, 251, 243),
            ForegroundPalette = new QrPaletteOptions {
                Mode = QrPaletteMode.Checker,
                Seed = 7,
                RingSize = 2,
                ApplyToEyes = false,
                Colors = new[] { new Rgba32(0, 112, 86), new Rgba32(0, 84, 104) },
            },
            Eyes = new QrEyeOptions {
                OuterShape = QrModuleShape.Squircle,
                InnerShape = QrModuleShape.Squircle,
                OuterColor = new Rgba32(0, 112, 86),
                InnerColor = new Rgba32(0, 84, 104),
            },
            Canvas = new QrCanvasOptions {
                PaddingPx = 28,
                CornerRadiusPx = 28,
                Background = new Rgba32(234, 251, 243),
                BorderPx = 6,
                BorderColor = new Rgba32(0, 112, 86),
            },
        };
        #endregion
        return options;
    }

    internal static QrImageCompositionOptions PhotoHalftone() {
        #region recipe photo-halftone
        var options = new QrImageCompositionOptions {
            Style = QrImageCompositionStyle.PhotoHalftone,
            ModuleSize = 25,
            Halftone = new QrImageHalftoneOptions { SubCells = 5, CenterSize = 0.46 },
        };
        #endregion
        return options;
    }

    internal static QrImageCompositionOptions PhotoHalftoneBold() {
        #region recipe photo-halftone-bold
        var options = new QrImageCompositionOptions {
            Style = QrImageCompositionStyle.PhotoHalftone,
            ModuleSize = 24,
            Halftone = new QrImageHalftoneOptions { SubCells = 3, CenterSize = 0.55, Contrast = 1.3 },
        };
        #endregion
        return options;
    }

    internal static QrImageCompositionOptions RisoDuotone() {
        #region recipe riso-duotone
        var options = new QrImageCompositionOptions {
            Style = QrImageCompositionStyle.PhotoHalftone,
            ModuleSize = 25,
            Duotone = new QrImageDuotoneOptions(),
            Halftone = new QrImageHalftoneOptions { SubCells = 5 },
        };
        #endregion
        return options;
    }

    internal static QrImageCompositionOptions RisoNewsprint() {
        #region recipe riso-newsprint
        var options = new QrImageCompositionOptions {
            Style = QrImageCompositionStyle.PhotoHalftone,
            ModuleSize = 25,
            Duotone = new QrImageDuotoneOptions {
                Ink = new Rgba32(28, 26, 24),
                Paper = new Rgba32(240, 236, 226),
            },
            Halftone = new QrImageHalftoneOptions { SubCells = 5 },
        };
        #endregion
        return options;
    }

    internal static QrImageCompositionOptions PixelDither() {
        #region recipe pixel-dither
        var options = new QrImageCompositionOptions {
            Style = QrImageCompositionStyle.PhotoHalftone,
            ModuleSize = 24,
            Halftone = new QrImageHalftoneOptions {
                SubCells = 4,
                Screen = QrHalftoneScreen.ErrorDiffusion,
                DotShape = QrHalftoneDotShape.Square,
            },
        };
        #endregion
        return options;
    }
}