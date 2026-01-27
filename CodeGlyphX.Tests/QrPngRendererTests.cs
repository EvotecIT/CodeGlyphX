using CodeGlyphX;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Tests.TestHelpers;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class QrPngRendererTests {
    [Fact]
    public void Render_With_Logo_Overlays_Center_Pixels() {
        var qr = QrCodeEncoder.EncodeText("HELLO", QrErrorCorrectionLevel.H);

        var logoRgba = new byte[4 * 4 * 4];
        for (var i = 0; i < logoRgba.Length; i += 4) {
            logoRgba[i + 0] = 255;
            logoRgba[i + 1] = 0;
            logoRgba[i + 2] = 0;
            logoRgba[i + 3] = 255;
        }

        var opts = new QrPngRenderOptions {
            ModuleSize = 4,
            QuietZone = 4,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            Logo = new QrPngLogoOptions(logoRgba, 4, 4) {
                Scale = 0.3,
                PaddingPx = 0,
                DrawBackground = false,
            },
        };

        var png = QrPngRenderer.Render(qr.Modules, opts);
        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);

        var cx = width / 2;
        var cy = height / 2;
        var p = cy * stride + cx * 4;
        Assert.Equal(255, rgba[p + 0]);
        Assert.Equal(0, rgba[p + 1]);
        Assert.Equal(0, rgba[p + 2]);
        Assert.Equal(255, rgba[p + 3]);
    }

    [Fact]
    public void Render_With_Circle_Modules_Leaves_Corners_Light() {
        var matrix = new BitMatrix(1, 1);
        matrix[0, 0] = true;

        var opts = new QrPngRenderOptions {
            ModuleSize = 6,
            QuietZone = 0,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            ModuleShape = QrPngModuleShape.Circle,
            ModuleScale = 0.8,
        };

        var png = QrPngRenderer.Render(matrix, opts);
        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);

        var corner = 0;
        Assert.Equal(255, rgba[corner + 0]);
        Assert.Equal(255, rgba[corner + 1]);
        Assert.Equal(255, rgba[corner + 2]);

        var center = (height / 2) * stride + (width / 2) * 4;
        Assert.Equal(0, rgba[center + 0]);
        Assert.Equal(0, rgba[center + 1]);
        Assert.Equal(0, rgba[center + 2]);
    }

    [Fact]
    public void Render_With_Gradient_Changes_Color_Across_Module() {
        var matrix = new BitMatrix(1, 1);
        matrix[0, 0] = true;

        var opts = new QrPngRenderOptions {
            ModuleSize = 6,
            QuietZone = 0,
            Background = Rgba32.White,
            Foreground = Rgba32.Black,
            ForegroundGradient = new QrPngGradientOptions {
                Type = QrPngGradientType.Horizontal,
                StartColor = new Rgba32(255, 0, 0),
                EndColor = new Rgba32(0, 0, 255),
            },
        };

        var png = QrPngRenderer.Render(matrix, opts);
        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);

        var left = (height / 2) * stride + 0 * 4;
        var right = (height / 2) * stride + (width - 1) * 4;

        Assert.True(rgba[left + 0] > rgba[left + 2]);
        Assert.True(rgba[right + 2] > rgba[right + 0]);
    }

    [Fact]
    public void Render_With_Background_Pattern_Draws_Overlay() {
        var matrix = new BitMatrix(1, 1);
        matrix[0, 0] = true;

        var opts = new QrPngRenderOptions {
            ModuleSize = 4,
            QuietZone = 2,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            ProtectQuietZone = false,
            BackgroundPattern = new QrPngBackgroundPatternOptions {
                Type = QrPngBackgroundPatternType.Grid,
                SizePx = 4,
                ThicknessPx = 2,
                Color = Rgba32.Black,
            },
        };

        var png = QrPngRenderer.Render(matrix, opts);
        var (rgba, _, _, stride) = PngTestDecoder.DecodeRgba32(png);

        var p0 = 0 * stride + 0 * 4;
        Assert.Equal(0, rgba[p0 + 0]);
        Assert.Equal(0, rgba[p0 + 1]);
        Assert.Equal(0, rgba[p0 + 2]);

        var p1 = 3 * stride + 3 * 4;
        Assert.Equal(255, rgba[p1 + 0]);
        Assert.Equal(255, rgba[p1 + 1]);
        Assert.Equal(255, rgba[p1 + 2]);
    }

    [Fact]
    public void Render_With_QuietZone_Protection_Skips_Pattern_In_QuietZone() {
        var matrix = new BitMatrix(1, 1);
        matrix[0, 0] = true;

        var opts = new QrPngRenderOptions {
            ModuleSize = 4,
            QuietZone = 2,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            ProtectQuietZone = true,
            BackgroundPattern = new QrPngBackgroundPatternOptions {
                Type = QrPngBackgroundPatternType.Grid,
                SizePx = 4,
                ThicknessPx = 2,
                Color = Rgba32.Black,
            },
        };

        var png = QrPngRenderer.Render(matrix, opts);
        var (rgba, _, _, stride) = PngTestDecoder.DecodeRgba32(png);

        var quietZonePixel = 0 * stride + 0 * 4;
        Assert.Equal(255, rgba[quietZonePixel + 0]);
        Assert.Equal(255, rgba[quietZonePixel + 1]);
        Assert.Equal(255, rgba[quietZonePixel + 2]);
    }

    [Fact]
    public void Render_With_ConnectedRounded_Connects_Adjacent_Modules() {
        var matrix = new BitMatrix(2, 2);
        matrix[0, 0] = true;
        matrix[1, 0] = true;

        const int moduleSize = 12;
        const int sampleY = moduleSize / 2;
        const int sampleX = moduleSize - 1;

        var rounded = new QrPngRenderOptions {
            ModuleSize = moduleSize,
            QuietZone = 0,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            ModuleShape = QrPngModuleShape.Rounded,
            ModuleScale = 0.6,
        };

        var connected = new QrPngRenderOptions {
            ModuleSize = moduleSize,
            QuietZone = 0,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            ModuleShape = QrPngModuleShape.ConnectedRounded,
            ModuleScale = 0.6,
        };

        var roundedPng = QrPngRenderer.Render(matrix, rounded);
        var (roundedRgba, _, _, roundedStride) = PngTestDecoder.DecodeRgba32(roundedPng);
        var connectedPng = QrPngRenderer.Render(matrix, connected);
        var (connectedRgba, _, _, connectedStride) = PngTestDecoder.DecodeRgba32(connectedPng);

        var roundedIndex = sampleY * roundedStride + sampleX * 4;
        var connectedIndex = sampleY * connectedStride + sampleX * 4;

        Assert.Equal(255, roundedRgba[roundedIndex + 0]);
        Assert.Equal(255, roundedRgba[roundedIndex + 1]);
        Assert.Equal(255, roundedRgba[roundedIndex + 2]);

        Assert.Equal(0, connectedRgba[connectedIndex + 0]);
        Assert.Equal(0, connectedRgba[connectedIndex + 1]);
        Assert.Equal(0, connectedRgba[connectedIndex + 2]);
    }

    [Fact]
    public void Render_With_ConnectedSquircle_Connects_Adjacent_Modules() {
        var matrix = new BitMatrix(2, 2);
        matrix[0, 0] = true;
        matrix[1, 0] = true;

        const int moduleSize = 12;
        const int sampleY = moduleSize / 2;
        const int sampleX = moduleSize - 1;

        var squircle = new QrPngRenderOptions {
            ModuleSize = moduleSize,
            QuietZone = 0,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            ModuleShape = QrPngModuleShape.Squircle,
            ModuleScale = 0.6,
        };

        var connected = new QrPngRenderOptions {
            ModuleSize = moduleSize,
            QuietZone = 0,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            ModuleShape = QrPngModuleShape.ConnectedSquircle,
            ModuleScale = 0.6,
        };

        var squirclePng = QrPngRenderer.Render(matrix, squircle);
        var (squircleRgba, _, _, squircleStride) = PngTestDecoder.DecodeRgba32(squirclePng);
        var connectedPng = QrPngRenderer.Render(matrix, connected);
        var (connectedRgba, _, _, connectedStride) = PngTestDecoder.DecodeRgba32(connectedPng);

        var squircleIndex = sampleY * squircleStride + sampleX * 4;
        var connectedIndex = sampleY * connectedStride + sampleX * 4;

        Assert.Equal(255, squircleRgba[squircleIndex + 0]);
        Assert.Equal(255, squircleRgba[squircleIndex + 1]);
        Assert.Equal(255, squircleRgba[squircleIndex + 2]);

        Assert.Equal(0, connectedRgba[connectedIndex + 0]);
        Assert.Equal(0, connectedRgba[connectedIndex + 1]);
        Assert.Equal(0, connectedRgba[connectedIndex + 2]);
    }

    [Fact]
    public void Render_With_Foreground_Palette_Checker_Alternates_Modules() {
        var matrix = new BitMatrix(2, 2);
        matrix[0, 0] = true;
        matrix[1, 0] = true;
        matrix[0, 1] = true;
        matrix[1, 1] = true;

        var opts = new QrPngRenderOptions {
            ModuleSize = 3,
            QuietZone = 0,
            Background = Rgba32.White,
            Foreground = Rgba32.Black,
            ForegroundPalette = new QrPngPaletteOptions {
                Mode = QrPngPaletteMode.Checker,
                Colors = new[] {
                    new Rgba32(255, 0, 0),
                    new Rgba32(0, 0, 255),
                }
            }
        };

        var png = QrPngRenderer.Render(matrix, opts);
        var (rgba, _, _, stride) = PngTestDecoder.DecodeRgba32(png);

        var moduleCenter = opts.ModuleSize / 2;
        var p00 = moduleCenter * stride + moduleCenter * 4;
        var p10 = moduleCenter * stride + (opts.ModuleSize + moduleCenter) * 4;

        Assert.Equal(255, rgba[p00 + 0]);
        Assert.Equal(0, rgba[p00 + 1]);
        Assert.Equal(0, rgba[p00 + 2]);

        Assert.Equal(0, rgba[p10 + 0]);
        Assert.Equal(0, rgba[p10 + 1]);
        Assert.Equal(255, rgba[p10 + 2]);
    }

    [Fact]
    public void LogoOptions_FromPng_Decodes_Rgba() {
        var matrix = new BitMatrix(1, 1);
        matrix[0, 0] = true;

        var opts = new QrPngRenderOptions {
            ModuleSize = 1,
            QuietZone = 0,
            Foreground = new Rgba32(10, 20, 30),
            Background = new Rgba32(200, 200, 200),
        };

        var png = QrPngRenderer.Render(matrix, opts);
        var logo = QrPngLogoOptions.FromPng(png);

        Assert.Equal(1, logo.Width);
        Assert.Equal(1, logo.Height);
        Assert.Equal(10, logo.Rgba[0]);
        Assert.Equal(20, logo.Rgba[1]);
        Assert.Equal(30, logo.Rgba[2]);
        Assert.Equal(255, logo.Rgba[3]);
    }

    [Fact]
    public void Render_With_Eye_Colors_Overrides_Foreground() {
        var qr = QrCodeEncoder.EncodeText("HELLO", QrErrorCorrectionLevel.H);

        var opts = new QrPngRenderOptions {
            ModuleSize = 4,
            QuietZone = 4,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            Eyes = new QrPngEyeOptions {
                OuterColor = new Rgba32(0, 255, 0),
                InnerColor = new Rgba32(0, 0, 255),
            },
        };

        var png = QrPngRenderer.Render(qr.Modules, opts);
        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);

        var px = opts.QuietZone * opts.ModuleSize + opts.ModuleSize / 2;
        var py = opts.QuietZone * opts.ModuleSize + opts.ModuleSize / 2;
        var outer = py * stride + px * 4;
        Assert.Equal(0, rgba[outer + 0]);
        Assert.Equal(255, rgba[outer + 1]);
        Assert.Equal(0, rgba[outer + 2]);

        var ix = opts.QuietZone * opts.ModuleSize + 3 * opts.ModuleSize + opts.ModuleSize / 2;
        var iy = opts.QuietZone * opts.ModuleSize + 3 * opts.ModuleSize + opts.ModuleSize / 2;
        var inner = iy * stride + ix * 4;
        Assert.Equal(0, rgba[inner + 0]);
        Assert.Equal(0, rgba[inner + 1]);
        Assert.Equal(255, rgba[inner + 2]);
    }

    [Fact]
    public void Render_With_Eye_Frame_Draws_Ring_And_Dot() {
        var qr = QrCodeEncoder.EncodeText("HELLO", QrErrorCorrectionLevel.H);

        var opts = new QrPngRenderOptions {
            ModuleSize = 4,
            QuietZone = 4,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            Eyes = new QrPngEyeOptions {
                UseFrame = true,
                OuterColor = new Rgba32(255, 0, 0),
                InnerColor = new Rgba32(0, 0, 255),
            },
        };

        var png = QrPngRenderer.Render(qr.Modules, opts);
        var (rgba, _, _, stride) = PngTestDecoder.DecodeRgba32(png);

        var baseX = opts.QuietZone * opts.ModuleSize;
        var baseY = opts.QuietZone * opts.ModuleSize;

        var ring = (baseY + 2) * stride + (baseX + 2) * 4;
        Assert.Equal(255, rgba[ring + 0]);
        Assert.Equal(0, rgba[ring + 1]);
        Assert.Equal(0, rgba[ring + 2]);

        var clear = (baseY + 6) * stride + (baseX + 6) * 4;
        Assert.Equal(255, rgba[clear + 0]);
        Assert.Equal(255, rgba[clear + 1]);
        Assert.Equal(255, rgba[clear + 2]);

        var dot = (baseY + 10) * stride + (baseX + 10) * 4;
        Assert.Equal(0, rgba[dot + 0]);
        Assert.Equal(0, rgba[dot + 1]);
        Assert.Equal(255, rgba[dot + 2]);
    }

    [Fact]
    public void Render_With_Eye_Frame_Gradient_Varies_Color() {
        var qr = QrCodeEncoder.EncodeText("HELLO", QrErrorCorrectionLevel.H);

        var opts = new QrPngRenderOptions {
            ModuleSize = 4,
            QuietZone = 4,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            Eyes = new QrPngEyeOptions {
                UseFrame = true,
                OuterGradient = new QrPngGradientOptions {
                    Type = QrPngGradientType.Horizontal,
                    StartColor = new Rgba32(255, 0, 0),
                    EndColor = new Rgba32(0, 255, 0),
                },
            },
        };

        var png = QrPngRenderer.Render(qr.Modules, opts);
        var (rgba, _, _, stride) = PngTestDecoder.DecodeRgba32(png);

        var baseX = opts.QuietZone * opts.ModuleSize;
        var baseY = opts.QuietZone * opts.ModuleSize;
        var left = (baseY + 2) * stride + (baseX + 2) * 4;
        var right = (baseY + 2) * stride + (baseX + 26) * 4;

        Assert.True(rgba[left + 0] > rgba[left + 1]);
        Assert.True(rgba[right + 1] > rgba[right + 0]);
    }

    [Fact]
    public void Render_With_Eye_Module_Gradient_Varies_Color() {
        var qr = QrCodeEncoder.EncodeText("HELLO", QrErrorCorrectionLevel.H);

        var opts = new QrPngRenderOptions {
            ModuleSize = 4,
            QuietZone = 4,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            Eyes = new QrPngEyeOptions {
                OuterGradient = new QrPngGradientOptions {
                    Type = QrPngGradientType.Horizontal,
                    StartColor = new Rgba32(255, 0, 0),
                    EndColor = new Rgba32(0, 255, 0),
                },
            },
        };

        var png = QrPngRenderer.Render(qr.Modules, opts);
        var (rgba, _, _, stride) = PngTestDecoder.DecodeRgba32(png);

        var baseX = opts.QuietZone * opts.ModuleSize;
        var baseY = opts.QuietZone * opts.ModuleSize;
        var left = (baseY + 2) * stride + (baseX + 2) * 4;
        var right = (baseY + 2) * stride + (baseX + 26) * 4;

        Assert.True(rgba[left + 0] > rgba[left + 1]);
        Assert.True(rgba[right + 1] > rgba[right + 0]);
    }

    [Fact]
    public void Render_With_Eye_Glow_Adds_Halo_On_Light_Modules_Near_Eye() {
        var qr = QrCodeEncoder.EncodeText("HELLO", QrErrorCorrectionLevel.H);
        var size = qr.Size;

        var baseEyes = new QrPngEyeOptions {
            UseFrame = true,
            FrameStyle = QrPngEyeFrameStyle.Single,
            OuterShape = QrPngModuleShape.Rounded,
            InnerShape = QrPngModuleShape.Circle,
            OuterColor = new Rgba32(0, 220, 255),
            InnerColor = new Rgba32(0, 220, 255),
            OuterCornerRadiusPx = 6,
            InnerCornerRadiusPx = 4,
        };

        var glowEyes = new QrPngEyeOptions {
            UseFrame = true,
            FrameStyle = QrPngEyeFrameStyle.Glow,
            OuterShape = baseEyes.OuterShape,
            InnerShape = baseEyes.InnerShape,
            OuterColor = baseEyes.OuterColor,
            InnerColor = baseEyes.InnerColor,
            OuterCornerRadiusPx = baseEyes.OuterCornerRadiusPx,
            InnerCornerRadiusPx = baseEyes.InnerCornerRadiusPx,
            GlowRadiusPx = 40,
            GlowAlpha = 140,
        };

        var optsBase = new QrPngRenderOptions {
            ModuleSize = 8,
            QuietZone = 4,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            Eyes = baseEyes,
        };

        var optsGlow = new QrPngRenderOptions {
            ModuleSize = optsBase.ModuleSize,
            QuietZone = optsBase.QuietZone,
            Foreground = optsBase.Foreground,
            Background = optsBase.Background,
            Eyes = glowEyes,
        };

        var pngBase = QrPngRenderer.Render(qr.Modules, optsBase);
        var (rgbaBase, _, _, strideBase) = PngTestDecoder.DecodeRgba32(pngBase);
        var pngGlow = QrPngRenderer.Render(qr.Modules, optsGlow);
        var (rgbaGlow, _, _, strideGlow) = PngTestDecoder.DecodeRgba32(pngGlow);

        var found = false;
        var mx = 0;
        var my = 0;
        var searchMax = Math.Min(size - 1, 14);
        for (var y = 0; y <= searchMax && !found; y++) {
            for (var x = 7; x <= searchMax; x++) {
                if (qr.Modules[x, y]) continue;
                mx = x;
                my = y;
                found = true;
                break;
            }
        }
        Assert.True(found);

        var px = (optsBase.QuietZone + mx) * optsBase.ModuleSize + optsBase.ModuleSize / 2;
        var py = (optsBase.QuietZone + my) * optsBase.ModuleSize + optsBase.ModuleSize / 2;

        var pBase = py * strideBase + px * 4;
        var pGlow = py * strideGlow + px * 4;

        var baseR = rgbaBase[pBase + 0];
        var baseG = rgbaBase[pBase + 1];
        var baseB = rgbaBase[pBase + 2];
        var glowR = rgbaGlow[pGlow + 0];
        var glowG = rgbaGlow[pGlow + 1];
        var glowB = rgbaGlow[pGlow + 2];

        var baseIsWhite = baseR == 255 && baseG == 255 && baseB == 255;
        var glowDiffers = baseR != glowR || baseG != glowG || baseB != glowB;
        var glowNotWhite = glowR != 255 || glowG != 255 || glowB != 255;

        Assert.True(baseIsWhite);
        Assert.True(glowDiffers && glowNotWhite);
    }

    [Fact]
    public void Render_With_Eye_InsetRing_Leaves_Center_Light_And_Ring_Dark() {
        var qr = QrCodeEncoder.EncodeText("HELLO", QrErrorCorrectionLevel.H);

        var opts = new QrPngRenderOptions {
            ModuleSize = 6,
            QuietZone = 4,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            Eyes = new QrPngEyeOptions {
                UseFrame = true,
                FrameStyle = QrPngEyeFrameStyle.InsetRing,
                OuterShape = QrPngModuleShape.Rounded,
                InnerShape = QrPngModuleShape.Rounded,
                OuterColor = Rgba32.Black,
                InnerColor = Rgba32.Black,
                OuterCornerRadiusPx = 6,
                InnerCornerRadiusPx = 4,
                InnerScale = 0.92,
            },
        };

        var png = QrPngRenderer.Render(qr.Modules, opts);
        var (rgba, _, _, stride) = PngTestDecoder.DecodeRgba32(png);

        var baseX = opts.QuietZone * opts.ModuleSize;
        var baseY = opts.QuietZone * opts.ModuleSize;

        var centerPx = baseX + 3 * opts.ModuleSize + opts.ModuleSize / 2;
        var centerPy = baseY + 3 * opts.ModuleSize + opts.ModuleSize / 2;
        var center = centerPy * stride + centerPx * 4;

        var ringPx = baseX + 3 * opts.ModuleSize + opts.ModuleSize / 2;
        var ringPy = baseY + opts.ModuleSize / 2;
        var ring = ringPy * stride + ringPx * 4;

        Assert.Equal(255, rgba[center + 0]);
        Assert.Equal(255, rgba[center + 1]);
        Assert.Equal(255, rgba[center + 2]);

        Assert.Equal(0, rgba[ring + 0]);
        Assert.Equal(0, rgba[ring + 1]);
        Assert.Equal(0, rgba[ring + 2]);
    }

    [Fact]
    public void Render_With_Eye_CutCorner_Clears_Corner_Pixels() {
        var qr = QrCodeEncoder.EncodeText("HELLO", QrErrorCorrectionLevel.H);

        var opts = new QrPngRenderOptions {
            ModuleSize = 6,
            QuietZone = 4,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            Eyes = new QrPngEyeOptions {
                UseFrame = true,
                FrameStyle = QrPngEyeFrameStyle.CutCorner,
                OuterShape = QrPngModuleShape.Square,
                InnerShape = QrPngModuleShape.Rounded,
                OuterColor = Rgba32.Black,
                InnerColor = Rgba32.Black,
                OuterCornerRadiusPx = 0,
                InnerCornerRadiusPx = 4,
            },
        };

        var png = QrPngRenderer.Render(qr.Modules, opts);
        var (rgba, _, _, stride) = PngTestDecoder.DecodeRgba32(png);

        var baseX = opts.QuietZone * opts.ModuleSize;
        var baseY = opts.QuietZone * opts.ModuleSize;

        var cornerX = baseX + 1;
        var cornerY = baseY + 1;
        var corner = cornerY * stride + cornerX * 4;

        var edgeX = baseX + 3 * opts.ModuleSize + opts.ModuleSize / 2;
        var edgeY = baseY + opts.ModuleSize / 2;
        var edge = edgeY * stride + edgeX * 4;

        Assert.Equal(255, rgba[corner + 0]);
        Assert.Equal(255, rgba[corner + 1]);
        Assert.Equal(255, rgba[corner + 2]);

        Assert.Equal(0, rgba[edge + 0]);
        Assert.Equal(0, rgba[edge + 1]);
        Assert.Equal(0, rgba[edge + 2]);
    }

    [Fact]
    public void Render_With_ScaleMap_ApplyToEyes_Affects_Eye_Modules() {
        var qr = QrCodeEncoder.EncodeText("HELLO", QrErrorCorrectionLevel.H);

        var scaleMap = new QrPngModuleScaleMapOptions {
            Mode = QrPngModuleScaleMode.Checker,
            MinScale = 0.4,
            MaxScale = 0.4,
            ApplyToEyes = false,
        };

        var optsNoEyes = new QrPngRenderOptions {
            ModuleSize = 10,
            QuietZone = 4,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            ModuleScaleMap = scaleMap,
        };

        var pngNoEyes = QrPngRenderer.Render(qr.Modules, optsNoEyes);
        var (rgbaNoEyes, _, _, strideNoEyes) = PngTestDecoder.DecodeRgba32(pngNoEyes);

        scaleMap.ApplyToEyes = true;
        var optsEyes = new QrPngRenderOptions {
            ModuleSize = 10,
            QuietZone = 4,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            ModuleScaleMap = scaleMap,
        };

        var pngEyes = QrPngRenderer.Render(qr.Modules, optsEyes);
        var (rgbaEyes, _, _, strideEyes) = PngTestDecoder.DecodeRgba32(pngEyes);

        var moduleX = optsEyes.QuietZone + 1;
        var moduleY = optsEyes.QuietZone + 0;
        var px = moduleX * optsEyes.ModuleSize + 1;
        var py = moduleY * optsEyes.ModuleSize + 1;

        var pNoEyes = py * strideNoEyes + px * 4;
        var pEyes = py * strideEyes + px * 4;

        Assert.Equal(0, rgbaNoEyes[pNoEyes + 0]);
        Assert.Equal(0, rgbaNoEyes[pNoEyes + 1]);
        Assert.Equal(0, rgbaNoEyes[pNoEyes + 2]);

        Assert.Equal(255, rgbaEyes[pEyes + 0]);
        Assert.Equal(255, rgbaEyes[pEyes + 1]);
        Assert.Equal(255, rgbaEyes[pEyes + 2]);
    }

    [Fact]
    public void Render_With_Functional_Protection_Preserves_Dark_Module_Color_And_Coverage() {
        var qr = QrCodeEncoder.EncodeText("HELLO", QrErrorCorrectionLevel.H);
        var size = qr.Size;

        const int darkModuleX = 8;
        var darkModuleY = size - 8;
        Assert.True(qr.Modules[darkModuleX, darkModuleY]);

        var opts = new QrPngRenderOptions {
            ModuleSize = 6,
            QuietZone = 4,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            ModuleShape = QrPngModuleShape.Circle,
            ModuleScale = 0.6,
            ProtectFunctionalPatterns = true,
            ForegroundPalette = new QrPngPaletteOptions {
                Mode = QrPngPaletteMode.Random,
                Seed = 42,
                Colors = new[] {
                    new Rgba32(255, 0, 0),
                    new Rgba32(0, 0, 255),
                },
            },
        };

        var png = QrPngRenderer.Render(qr.Modules, opts);
        var (rgba, _, _, stride) = PngTestDecoder.DecodeRgba32(png);

        var px = (opts.QuietZone + darkModuleX) * opts.ModuleSize;
        var py = (opts.QuietZone + darkModuleY) * opts.ModuleSize;
        var p = py * stride + px * 4;

        Assert.Equal(0, rgba[p + 0]);
        Assert.Equal(0, rgba[p + 1]);
        Assert.Equal(0, rgba[p + 2]);
    }

    [Fact]
    public void Render_With_Functional_Protection_Preserves_Gradient_On_Eyes_When_Eyes_Not_Configured() {
        var qr = QrCodeEncoder.EncodeText("HELLO", QrErrorCorrectionLevel.H);
        var size = qr.Size;

        var opts = new QrPngRenderOptions {
            ModuleSize = 6,
            QuietZone = 4,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            ProtectFunctionalPatterns = true,
            ForegroundGradient = new QrPngGradientOptions {
                Type = QrPngGradientType.Horizontal,
                StartColor = new Rgba32(255, 0, 0),
                EndColor = new Rgba32(0, 255, 0),
            },
        };

        var png = QrPngRenderer.Render(qr.Modules, opts);
        var (rgba, _, _, stride) = PngTestDecoder.DecodeRgba32(png);

        var moduleY = opts.QuietZone + 0;
        var leftModuleX = opts.QuietZone + 0;
        var rightModuleX = opts.QuietZone + (size - 1);

        var leftPx = leftModuleX * opts.ModuleSize + opts.ModuleSize / 2;
        var rightPx = rightModuleX * opts.ModuleSize + opts.ModuleSize / 2;
        var py = moduleY * opts.ModuleSize + opts.ModuleSize / 2;

        var left = py * stride + leftPx * 4;
        var right = py * stride + rightPx * 4;

        Assert.True(rgba[left + 0] > rgba[left + 1]);
        Assert.True(rgba[right + 1] > rgba[right + 0]);
    }

    [Theory]
    [InlineData(QrPngModuleShape.Diamond)]
    [InlineData(QrPngModuleShape.Squircle)]
    [InlineData(QrPngModuleShape.Dot)]
    [InlineData(QrPngModuleShape.DotGrid)]
    public void Render_With_Fancy_Module_Shapes_Draws_Module(QrPngModuleShape shape) {
        var qr = QrCodeEncoder.EncodeText("HELLO", QrErrorCorrectionLevel.H);

        var opts = new QrPngRenderOptions {
            ModuleSize = 6,
            QuietZone = 2,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            ModuleShape = shape,
        };

        var png = QrPngRenderer.Render(qr.Modules, opts);
        var (rgba, _, _, stride) = PngTestDecoder.DecodeRgba32(png);

        var mx = 0;
        var my = 0;
        var found = false;
        for (var y = 0; y < qr.Modules.Height && !found; y++) {
            for (var x = 0; x < qr.Modules.Width; x++) {
                if (!qr.Modules[x, y]) continue;
                mx = x;
                my = y;
                found = true;
                break;
            }
        }
        Assert.True(found);

        var px = (opts.QuietZone + mx) * opts.ModuleSize + opts.ModuleSize / 2;
        var py = (opts.QuietZone + my) * opts.ModuleSize + opts.ModuleSize / 2;
        var p = py * stride + px * 4;
        Assert.Equal(0, rgba[p + 0]);
        Assert.Equal(0, rgba[p + 1]);
        Assert.Equal(0, rgba[p + 2]);
    }
}
