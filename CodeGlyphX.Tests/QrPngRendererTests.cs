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
    public void Render_With_SpecklePattern_Changes_With_Seed() {
        var matrix = new BitMatrix(1, 1);
        matrix[0, 0] = true;

        static QrPngRenderOptions OptionsForSeed(int seed) => new() {
            ModuleSize = 72,
            QuietZone = 0,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            ForegroundPattern = new QrPngForegroundPatternOptions {
                Type = QrPngForegroundPatternType.SpeckleDots,
                Color = new Rgba32(255, 255, 255, 110),
                Seed = seed,
                Variation = 0.9,
                Density = 1.0,
                SizePx = 10,
                ThicknessPx = 3,
                ApplyToModules = true,
                ApplyToEyes = true,
            },
        };

        static int Fingerprint(byte[] rgba, int width, int height, int stride) {
            var hash = unchecked((int)2166136261);
            var length = height * stride;
            for (var i = 0; i < length; i++) {
                hash ^= rgba[i];
                hash = unchecked(hash * 16777619);
            }
            return hash;
        }

        var pngA = QrPngRenderer.Render(matrix, OptionsForSeed(123));
        var (rgbaA, widthA, heightA, strideA) = PngTestDecoder.DecodeRgba32(pngA);
        var pngB = QrPngRenderer.Render(matrix, OptionsForSeed(456));
        var (rgbaB, widthB, heightB, strideB) = PngTestDecoder.DecodeRgba32(pngB);

        Assert.Equal(widthA, widthB);
        Assert.Equal(heightA, heightB);

        var fpA = Fingerprint(rgbaA, widthA, heightA, strideA);
        var fpB = Fingerprint(rgbaB, widthB, heightB, strideB);
        Assert.NotEqual(fpA, fpB);
    }

    [Fact]
    public void Render_With_HalftonePattern_Is_Stronger_Near_Center() {
        var matrix = new BitMatrix(1, 1);
        matrix[0, 0] = true;

        var opts = new QrPngRenderOptions {
            ModuleSize = 200,
            QuietZone = 0,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            ForegroundPattern = new QrPngForegroundPatternOptions {
                Type = QrPngForegroundPatternType.HalftoneDots,
                Color = new Rgba32(255, 255, 255, 140),
                Seed = 2026,
                Variation = 1.0,
                Density = 1.0,
                SizePx = 16,
                ThicknessPx = 5,
                ApplyToModules = true,
                ApplyToEyes = true,
            },
        };

        var png = QrPngRenderer.Render(matrix, opts);
        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);

        static long SumLuma(byte[] rgba, int stride, int x0, int y0, int size) {
            long sum = 0;
            var x1 = x0 + size;
            var y1 = y0 + size;
            for (var y = y0; y < y1; y++) {
                var row = y * stride;
                for (var x = x0; x < x1; x++) {
                    var idx = row + x * 4;
                    sum += rgba[idx + 0];
                    sum += rgba[idx + 1];
                    sum += rgba[idx + 2];
                }
            }
            return sum;
        }

        var regionSize = 48;
        var corner = SumLuma(rgba, stride, 0, 0, regionSize);
        var centerX = Math.Max(0, (width - regionSize) / 2);
        var centerY = Math.Max(0, (height - regionSize) / 2);
        var center = SumLuma(rgba, stride, centerX, centerY, regionSize);

        Assert.True(center > corner, "Expected halftone to brighten the center more than the corners.");
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
    public void Render_With_PerEye_Colors_Applies_Different_Colors_Per_Finder() {
        var qr = QrCodeEncoder.EncodeText("HELLO", QrErrorCorrectionLevel.H);
        var size = qr.Size;

        var opts = new QrPngRenderOptions {
            ModuleSize = 8,
            QuietZone = 4,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            Eyes = new QrPngEyeOptions {
                OuterColors = new[] {
                    new Rgba32(255, 0, 0),
                    new Rgba32(0, 255, 0),
                    new Rgba32(0, 0, 255),
                },
            },
        };

        var png = QrPngRenderer.Render(qr.Modules, opts);
        var (rgba, _, _, stride) = PngTestDecoder.DecodeRgba32(png);

        var sampleX0 = (opts.QuietZone + 0) * opts.ModuleSize + opts.ModuleSize / 2;
        var sampleY0 = (opts.QuietZone + 0) * opts.ModuleSize + opts.ModuleSize / 2;
        var p0 = sampleY0 * stride + sampleX0 * 4;

        var sampleX1 = (opts.QuietZone + (size - 7)) * opts.ModuleSize + opts.ModuleSize / 2;
        var sampleY1 = (opts.QuietZone + 0) * opts.ModuleSize + opts.ModuleSize / 2;
        var p1 = sampleY1 * stride + sampleX1 * 4;

        var sampleX2 = (opts.QuietZone + 0) * opts.ModuleSize + opts.ModuleSize / 2;
        var sampleY2 = (opts.QuietZone + (size - 7)) * opts.ModuleSize + opts.ModuleSize / 2;
        var p2 = sampleY2 * stride + sampleX2 * 4;

        Assert.True(rgba[p0 + 0] > rgba[p0 + 1] && rgba[p0 + 0] > rgba[p0 + 2], "Top-left eye should be red-dominant.");
        Assert.True(rgba[p1 + 1] > rgba[p1 + 0] && rgba[p1 + 1] > rgba[p1 + 2], "Top-right eye should be green-dominant.");
        Assert.True(rgba[p2 + 2] > rgba[p2 + 0] && rgba[p2 + 2] > rgba[p2 + 1], "Bottom-left eye should be blue-dominant.");
    }

    [Fact]
    public void Render_With_PerEye_Gradients_Uses_PerEye_Outer_Gradients() {
        var qr = QrCodeEncoder.EncodeText("HELLO", QrErrorCorrectionLevel.H);
        var size = qr.Size;

        static QrPngGradientOptions SolidGradient(byte r, byte g, byte b) => new() {
            Type = QrPngGradientType.Horizontal,
            StartColor = new Rgba32(r, g, b),
            EndColor = new Rgba32(r, g, b),
        };

        var opts = new QrPngRenderOptions {
            ModuleSize = 8,
            QuietZone = 4,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            Eyes = new QrPngEyeOptions {
                OuterGradients = new[] {
                    SolidGradient(255, 0, 0),
                    SolidGradient(0, 255, 0),
                    SolidGradient(0, 0, 255),
                },
            },
        };

        var png = QrPngRenderer.Render(qr.Modules, opts);
        var (rgba, _, _, stride) = PngTestDecoder.DecodeRgba32(png);

        var sampleX0 = (opts.QuietZone + 0) * opts.ModuleSize + opts.ModuleSize / 2;
        var sampleY0 = (opts.QuietZone + 0) * opts.ModuleSize + opts.ModuleSize / 2;
        var p0 = sampleY0 * stride + sampleX0 * 4;

        var sampleX1 = (opts.QuietZone + (size - 7)) * opts.ModuleSize + opts.ModuleSize / 2;
        var sampleY1 = (opts.QuietZone + 0) * opts.ModuleSize + opts.ModuleSize / 2;
        var p1 = sampleY1 * stride + sampleX1 * 4;

        var sampleX2 = (opts.QuietZone + 0) * opts.ModuleSize + opts.ModuleSize / 2;
        var sampleY2 = (opts.QuietZone + (size - 7)) * opts.ModuleSize + opts.ModuleSize / 2;
        var p2 = sampleY2 * stride + sampleX2 * 4;

        Assert.True(rgba[p0 + 0] > rgba[p0 + 1] && rgba[p0 + 0] > rgba[p0 + 2], "Top-left eye should be red-dominant.");
        Assert.True(rgba[p1 + 1] > rgba[p1 + 0] && rgba[p1 + 1] > rgba[p1 + 2], "Top-right eye should be green-dominant.");
        Assert.True(rgba[p2 + 2] > rgba[p2 + 0] && rgba[p2 + 2] > rgba[p2 + 1], "Bottom-left eye should be blue-dominant.");
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
    public void Render_With_Eye_Sparkles_Draws_On_Canvas_Outside_Qr() {
        var qr = QrCodeEncoder.EncodeText("HELLO", QrErrorCorrectionLevel.H);
        var moduleSize = 8;
        var quietZone = 4;
        var padding = 26;

        var opts = new QrPngRenderOptions {
            ModuleSize = moduleSize,
            QuietZone = quietZone,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            Canvas = new QrPngCanvasOptions {
                PaddingPx = padding,
                CornerRadiusPx = 0,
                Background = Rgba32.White,
            },
            Eyes = new QrPngEyeOptions {
                SparkleCount = 20,
                SparkleRadiusPx = 3,
                SparkleSpreadPx = 28,
                SparkleSeed = 4242,
                SparkleColor = new Rgba32(0, 0, 0, 200),
            },
        };

        var png = QrPngRenderer.Render(qr.Modules, opts);
        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);

        var qrFullPx = (qr.Size + quietZone * 2) * moduleSize;
        var qrX0 = padding;
        var qrY0 = padding;
        var qrX1 = qrX0 + qrFullPx - 1;
        var qrY1 = qrY0 + qrFullPx - 1;

        var foundSparkle = false;
        for (var y = 0; y < height && !foundSparkle; y++) {
            for (var x = 0; x < width; x++) {
                if (x >= qrX0 && x <= qrX1 && y >= qrY0 && y <= qrY1) continue;
                var p = y * stride + x * 4;
                var r = rgba[p + 0];
                var g = rgba[p + 1];
                var b = rgba[p + 2];
                if (r != 255 || g != 255 || b != 255) {
                    foundSparkle = true;
                    break;
                }
            }
        }

        Assert.True(foundSparkle, "Expected at least one sparkle-colored pixel outside the QR area.");
    }

    [Fact]
    public void Render_With_Eye_AccentRings_Draws_On_Canvas_Outside_Qr() {
        var qr = QrCodeEncoder.EncodeText("HELLO", QrErrorCorrectionLevel.H);
        var moduleSize = 8;
        var quietZone = 4;
        var padding = 26;

        var opts = new QrPngRenderOptions {
            ModuleSize = moduleSize,
            QuietZone = quietZone,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            Canvas = new QrPngCanvasOptions {
                PaddingPx = padding,
                CornerRadiusPx = 0,
                Background = Rgba32.White,
            },
            Eyes = new QrPngEyeOptions {
                AccentRingCount = 6,
                AccentRingThicknessPx = 5,
                AccentRingSpreadPx = 36,
                AccentRingJitterPx = 4,
                AccentRingSeed = 20260127,
                AccentRingColor = new Rgba32(0, 0, 0, 180),
            },
        };

        var png = QrPngRenderer.Render(qr.Modules, opts);
        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);

        var qrFullPx = (qr.Size + quietZone * 2) * moduleSize;
        var qrX0 = padding;
        var qrY0 = padding;
        var qrX1 = qrX0 + qrFullPx - 1;
        var qrY1 = qrY0 + qrFullPx - 1;

        var foundAccent = false;
        for (var y = 0; y < height && !foundAccent; y++) {
            for (var x = 0; x < width; x++) {
                if (x >= qrX0 && x <= qrX1 && y >= qrY0 && y <= qrY1) continue;
                var p = y * stride + x * 4;
                var r = rgba[p + 0];
                var g = rgba[p + 1];
                var b = rgba[p + 2];
                if (r != 255 || g != 255 || b != 255) {
                    foundAccent = true;
                    break;
                }
            }
        }

        Assert.True(foundAccent, "Expected accent ring pixels outside the QR area.");
    }

    [Fact]
    public void Render_With_Eye_AccentRays_Draws_On_Canvas_Outside_Qr() {
        var qr = QrCodeEncoder.EncodeText("HELLO", QrErrorCorrectionLevel.H);
        var moduleSize = 8;
        var quietZone = 4;
        var padding = 26;

        var opts = new QrPngRenderOptions {
            ModuleSize = moduleSize,
            QuietZone = quietZone,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            Canvas = new QrPngCanvasOptions {
                PaddingPx = padding,
                CornerRadiusPx = 0,
                Background = Rgba32.White,
            },
            Eyes = new QrPngEyeOptions {
                AccentRayCount = 10,
                AccentRayLengthPx = 52,
                AccentRayThicknessPx = 6,
                AccentRaySpreadPx = 44,
                AccentRayJitterPx = 6,
                AccentRayLengthJitterPx = 12,
                AccentRaySeed = 20260128,
                AccentRayColor = new Rgba32(0, 0, 0, 190),
            },
        };

        var png = QrPngRenderer.Render(qr.Modules, opts);
        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);

        var qrFullPx = (qr.Size + quietZone * 2) * moduleSize;
        var qrX0 = padding;
        var qrY0 = padding;
        var qrX1 = qrX0 + qrFullPx - 1;
        var qrY1 = qrY0 + qrFullPx - 1;

        var foundAccent = false;
        for (var y = 0; y < height && !foundAccent; y++) {
            for (var x = 0; x < width; x++) {
                if (x >= qrX0 && x <= qrX1 && y >= qrY0 && y <= qrY1) continue;
                var p = y * stride + x * 4;
                var r = rgba[p + 0];
                var g = rgba[p + 1];
                var b = rgba[p + 2];
                if (r != 255 || g != 255 || b != 255) {
                    foundAccent = true;
                    break;
                }
            }
        }

        Assert.True(foundAccent, "Expected accent ray pixels outside the QR area.");
    }

    [Fact]
    public void Render_With_Canvas_Vignette_Draws_Outside_Qr_Bounds() {
        var qr = QrCodeEncoder.EncodeText("HELLO", QrErrorCorrectionLevel.H);
        var moduleSize = 8;
        var quietZone = 4;
        var padding = 28;

        var opts = new QrPngRenderOptions {
            ModuleSize = moduleSize,
            QuietZone = quietZone,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            Canvas = new QrPngCanvasOptions {
                PaddingPx = padding,
                CornerRadiusPx = 0,
                Background = Rgba32.White,
                Vignette = new QrPngCanvasVignetteOptions {
                    Color = new Rgba32(0, 0, 0, 200),
                    BandPx = 64,
                    Strength = 1.0,
                    ProtectQrArea = true,
                },
            },
        };

        var png = QrPngRenderer.Render(qr.Modules, opts);
        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);

        var qrFullPx = (qr.Size + quietZone * 2) * moduleSize;
        var qrX0 = padding;
        var qrY0 = padding;
        var qrX1 = qrX0 + qrFullPx - 1;
        var qrY1 = qrY0 + qrFullPx - 1;

        var foundVignette = false;
        for (var y = 0; y < height && !foundVignette; y++) {
            for (var x = 0; x < width; x++) {
                if (x >= qrX0 && x <= qrX1 && y >= qrY0 && y <= qrY1) continue;
                var p = y * stride + x * 4;
                var r = rgba[p + 0];
                var g = rgba[p + 1];
                var b = rgba[p + 2];
                if (r != 255 || g != 255 || b != 255) {
                    foundVignette = true;
                    break;
                }
            }
        }

        Assert.True(foundVignette, "Expected vignette pixels outside the QR bounds.");
    }

    [Fact]
    public void Render_With_Canvas_Grain_Draws_Outside_Qr_Bounds() {
        var qr = QrCodeEncoder.EncodeText("HELLO", QrErrorCorrectionLevel.H);
        var moduleSize = 8;
        var quietZone = 4;
        var padding = 28;

        var opts = new QrPngRenderOptions {
            ModuleSize = moduleSize,
            QuietZone = quietZone,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            Canvas = new QrPngCanvasOptions {
                PaddingPx = padding,
                CornerRadiusPx = 0,
                Background = Rgba32.White,
                Grain = new QrPngCanvasGrainOptions {
                    Color = new Rgba32(0, 0, 0, 140),
                    Density = 0.35,
                    PixelSizePx = 2,
                    AlphaJitter = 0.6,
                    Seed = 20260129,
                    ProtectQrArea = true,
                },
            },
        };

        var png = QrPngRenderer.Render(qr.Modules, opts);
        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);

        var qrFullPx = (qr.Size + quietZone * 2) * moduleSize;
        var qrX0 = padding;
        var qrY0 = padding;
        var qrX1 = qrX0 + qrFullPx - 1;
        var qrY1 = qrY0 + qrFullPx - 1;

        var foundGrain = false;
        for (var y = 0; y < height && !foundGrain; y++) {
            for (var x = 0; x < width; x++) {
                if (x >= qrX0 && x <= qrX1 && y >= qrY0 && y <= qrY1) continue;
                var p = y * stride + x * 4;
                var r = rgba[p + 0];
                var g = rgba[p + 1];
                var b = rgba[p + 2];
                if (r != 255 || g != 255 || b != 255) {
                    foundGrain = true;
                    break;
                }
            }
        }

        Assert.True(foundGrain, "Expected grain pixels outside the QR bounds.");
    }

    [Fact]
    public void Render_With_Canvas_Pattern_DiagonalStripes_Draws_Outside_Qr_Bounds() {
        var qr = QrCodeEncoder.EncodeText("HELLO", QrErrorCorrectionLevel.H);
        var moduleSize = 8;
        var quietZone = 4;
        var padding = 28;

        var opts = new QrPngRenderOptions {
            ModuleSize = moduleSize,
            QuietZone = quietZone,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            Canvas = new QrPngCanvasOptions {
                PaddingPx = padding,
                CornerRadiusPx = 0,
                Background = Rgba32.White,
                Pattern = new QrPngBackgroundPatternOptions {
                    Type = QrPngBackgroundPatternType.DiagonalStripes,
                    Color = new Rgba32(0, 0, 0, 56),
                    SizePx = 16,
                    ThicknessPx = 2,
                    SnapToModuleSize = false,
                    ModuleStep = 2,
                },
            },
        };

        var png = QrPngRenderer.Render(qr.Modules, opts);
        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);

        var qrFullPx = (qr.Size + quietZone * 2) * moduleSize;
        var qrX0 = padding;
        var qrY0 = padding;
        var qrX1 = qrX0 + qrFullPx - 1;
        var qrY1 = qrY0 + qrFullPx - 1;

        var foundPattern = false;
        for (var y = 0; y < height && !foundPattern; y++) {
            for (var x = 0; x < width; x++) {
                if (x >= qrX0 && x <= qrX1 && y >= qrY0 && y <= qrY1) continue;
                var p = y * stride + x * 4;
                var r = rgba[p + 0];
                var g = rgba[p + 1];
                var b = rgba[p + 2];
                if (r != 255 || g != 255 || b != 255) {
                    foundPattern = true;
                    break;
                }
            }
        }

        Assert.True(foundPattern, "Expected diagonal stripe pattern pixels outside the QR bounds.");
    }

    [Fact]
    public void Render_With_Canvas_Splash_CanvasEdges_Draws_Outside_Qr_Bounds() {
        var qr = QrCodeEncoder.EncodeText("HELLO", QrErrorCorrectionLevel.H);
        var moduleSize = 8;
        var quietZone = 4;
        var padding = 32;

        var opts = new QrPngRenderOptions {
            ModuleSize = moduleSize,
            QuietZone = quietZone,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            Canvas = new QrPngCanvasOptions {
                PaddingPx = padding,
                CornerRadiusPx = 0,
                Background = Rgba32.White,
                Splash = new QrPngCanvasSplashOptions {
                    Color = new Rgba32(0, 0, 0, 128),
                    Count = 14,
                    MinRadiusPx = 16,
                    MaxRadiusPx = 46,
                    SpreadPx = 26,
                    Placement = QrPngCanvasSplashPlacement.CanvasEdges,
                    EdgeBandPx = 104,
                    DripChance = 0.6,
                    DripLengthPx = 44,
                    DripWidthPx = 10,
                    Seed = 20260131,
                    ProtectQrArea = true,
                },
            },
        };

        var png = QrPngRenderer.Render(qr.Modules, opts);
        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);

        var qrFullPx = (qr.Size + quietZone * 2) * moduleSize;
        var qrX0 = padding;
        var qrY0 = padding;
        var qrX1 = qrX0 + qrFullPx - 1;
        var qrY1 = qrY0 + qrFullPx - 1;

        var foundSplash = false;
        for (var y = 0; y < height && !foundSplash; y++) {
            for (var x = 0; x < width; x++) {
                if (x >= qrX0 && x <= qrX1 && y >= qrY0 && y <= qrY1) continue;
                var p = y * stride + x * 4;
                var r = rgba[p + 0];
                var g = rgba[p + 1];
                var b = rgba[p + 2];
                if (r != 255 || g != 255 || b != 255) {
                    foundSplash = true;
                    break;
                }
            }
        }

        Assert.True(foundSplash, "Expected canvas-edge splash pixels outside the QR bounds.");
    }

    [Fact]
    public void Render_With_Eye_Accent_Stripes_Draws_Outside_Qr_Bounds() {
        var qr = QrCodeEncoder.EncodeText("HELLO", QrErrorCorrectionLevel.H);
        var moduleSize = 8;
        var quietZone = 4;
        var padding = 32;

        var opts = new QrPngRenderOptions {
            ModuleSize = moduleSize,
            QuietZone = quietZone,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            Canvas = new QrPngCanvasOptions {
                PaddingPx = padding,
                CornerRadiusPx = 0,
                Background = Rgba32.White,
            },
            Eyes = new QrPngEyeOptions {
                UseFrame = true,
                FrameStyle = QrPngEyeFrameStyle.Single,
                OuterColor = new Rgba32(0, 0, 0),
                InnerColor = new Rgba32(0, 0, 0),
                AccentStripeCount = 26,
                AccentStripeLengthPx = 28,
                AccentStripeThicknessPx = 4,
                AccentStripeSpreadPx = 34,
                AccentStripeJitterPx = 6,
                AccentStripeLengthJitterPx = 8,
                AccentStripeSeed = 20260130,
                AccentStripeColor = new Rgba32(0, 0, 0, 140),
                AccentStripeProtectQrArea = true,
            },
        };

        var png = QrPngRenderer.Render(qr.Modules, opts);
        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);

        var qrFullPx = (qr.Size + quietZone * 2) * moduleSize;
        var qrX0 = padding;
        var qrY0 = padding;
        var qrX1 = qrX0 + qrFullPx - 1;
        var qrY1 = qrY0 + qrFullPx - 1;

        var foundAccents = false;
        for (var y = 0; y < height && !foundAccents; y++) {
            for (var x = 0; x < width; x++) {
                if (x >= qrX0 && x <= qrX1 && y >= qrY0 && y <= qrY1) continue;
                var p = y * stride + x * 4;
                var r = rgba[p + 0];
                var g = rgba[p + 1];
                var b = rgba[p + 2];
                if (r != 255 || g != 255 || b != 255) {
                    foundAccents = true;
                    break;
                }
            }
        }

        Assert.True(foundAccents, "Expected eye accent stripe pixels outside the QR bounds.");
    }

    [Fact]
    public void Render_With_Canvas_Halo_Draws_Outside_Qr_Bounds() {
        var qr = QrCodeEncoder.EncodeText("HELLO", QrErrorCorrectionLevel.H);
        var moduleSize = 8;
        var quietZone = 4;
        var padding = 26;
        var haloRadius = 28;

        var opts = new QrPngRenderOptions {
            ModuleSize = moduleSize,
            QuietZone = quietZone,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            Canvas = new QrPngCanvasOptions {
                PaddingPx = padding,
                CornerRadiusPx = 0,
                Background = Rgba32.White,
                Halo = new QrPngCanvasHaloOptions {
                    Color = new Rgba32(0, 0, 0, 200),
                    RadiusPx = haloRadius,
                    ProtectQrArea = true,
                },
            },
        };

        var png = QrPngRenderer.Render(qr.Modules, opts);
        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);

        var qrFullPx = (qr.Size + quietZone * 2) * moduleSize;
        var qrX0 = padding;
        var qrY0 = padding;
        var qrX1 = qrX0 + qrFullPx - 1;
        var qrY1 = qrY0 + qrFullPx - 1;

        var minX = Math.Max(0, qrX0 - haloRadius);
        var minY = Math.Max(0, qrY0 - haloRadius);
        var maxX = Math.Min(width - 1, qrX1 + haloRadius);
        var maxY = Math.Min(height - 1, qrY1 + haloRadius);

        var foundHalo = false;
        for (var y = minY; y <= maxY && !foundHalo; y++) {
            for (var x = minX; x <= maxX; x++) {
                if (x >= qrX0 && x <= qrX1 && y >= qrY0 && y <= qrY1) continue;
                var p = y * stride + x * 4;
                var r = rgba[p + 0];
                var g = rgba[p + 1];
                var b = rgba[p + 2];
                if (r != 255 || g != 255 || b != 255) {
                    foundHalo = true;
                    break;
                }
            }
        }

        Assert.True(foundHalo, "Expected halo pixels outside the QR bounds.");
    }

    [Fact]
    public void Render_With_Canvas_Frame_Draws_Outside_Qr_Bounds() {
        var qr = QrCodeEncoder.EncodeText("HELLO", QrErrorCorrectionLevel.H);
        var moduleSize = 8;
        var quietZone = 4;
        var padding = 36;

        var opts = new QrPngRenderOptions {
            ModuleSize = moduleSize,
            QuietZone = quietZone,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            Canvas = new QrPngCanvasOptions {
                PaddingPx = padding,
                CornerRadiusPx = 24,
                Background = Rgba32.White,
                Frame = new QrPngCanvasFrameOptions {
                    ThicknessPx = 14,
                    GapPx = 10,
                    RadiusPx = 24,
                    Color = new Rgba32(20, 40, 120, 220),
                    InnerThicknessPx = 4,
                    InnerGapPx = 4,
                    InnerColor = new Rgba32(255, 120, 40, 220),
                },
            },
        };

        var png = QrPngRenderer.Render(qr.Modules, opts);
        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);

        var qrFullPx = (qr.Size + quietZone * 2) * moduleSize;
        var qrX0 = padding;
        var qrY0 = padding;
        var qrX1 = qrX0 + qrFullPx - 1;
        var qrY1 = qrY0 + qrFullPx - 1;

        var foundFrame = false;
        for (var y = 0; y < height && !foundFrame; y++) {
            for (var x = 0; x < width; x++) {
                if (x >= qrX0 && x <= qrX1 && y >= qrY0 && y <= qrY1) continue;
                var p = y * stride + x * 4;
                var r = rgba[p + 0];
                var g = rgba[p + 1];
                var b = rgba[p + 2];
                if (r != 255 || g != 255 || b != 255) {
                    foundFrame = true;
                    break;
                }
            }
        }

        Assert.True(foundFrame, "Expected frame pixels outside the QR bounds.");
    }

    [Fact]
    public void Render_With_Canvas_Frame_Clamps_To_Padding_And_Stays_Outside_Qr() {
        var matrix = new BitMatrix(21, 21);
        var moduleSize = 6;
        var quietZone = 4;
        var padding = 16;

        var opts = new QrPngRenderOptions {
            ModuleSize = moduleSize,
            QuietZone = quietZone,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            Canvas = new QrPngCanvasOptions {
                PaddingPx = padding,
                CornerRadiusPx = 18,
                Background = Rgba32.White,
                Frame = new QrPngCanvasFrameOptions {
                    ThicknessPx = 40,
                    GapPx = 40,
                    RadiusPx = 18,
                    Color = new Rgba32(40, 80, 160, 220),
                },
            },
        };

        var png = QrPngRenderer.Render(matrix, opts);
        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);

        var qrFullPx = (matrix.Width + quietZone * 2) * moduleSize;
        var qrX0 = padding;
        var qrY0 = padding;
        var qrX1 = qrX0 + qrFullPx - 1;
        var qrY1 = qrY0 + qrFullPx - 1;

        var foundFrame = false;
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                var p = y * stride + x * 4;
                var r = rgba[p + 0];
                var g = rgba[p + 1];
                var b = rgba[p + 2];

                var insideQr = x >= qrX0 && x <= qrX1 && y >= qrY0 && y <= qrY1;
                if (insideQr) {
                    Assert.True(r == 255 && g == 255 && b == 255, "Frame should not draw inside the QR bounds.");
                } else if (r != 255 || g != 255 || b != 255) {
                    foundFrame = true;
                }
            }
        }

        Assert.True(foundFrame, "Expected frame pixels outside the QR bounds.");
    }

    [Fact]
    public void Render_With_Canvas_Badge_Draws_Outside_Qr_Bounds() {
        var qr = QrCodeEncoder.EncodeText("HELLO", QrErrorCorrectionLevel.H);
        var moduleSize = 8;
        var quietZone = 4;
        var padding = 30;

        var opts = new QrPngRenderOptions {
            ModuleSize = moduleSize,
            QuietZone = quietZone,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            Canvas = new QrPngCanvasOptions {
                PaddingPx = padding,
                CornerRadiusPx = 20,
                Background = Rgba32.White,
                Badge = new QrPngCanvasBadgeOptions {
                    Shape = QrPngCanvasBadgeShape.Badge,
                    Position = QrPngCanvasBadgePosition.Top,
                    WidthPx = 120,
                    HeightPx = 30,
                    GapPx = 10,
                    CornerRadiusPx = 14,
                    Color = new Rgba32(80, 40, 180, 220),
                },
            },
        };

        var png = QrPngRenderer.Render(qr.Modules, opts);
        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);

        var qrFullPx = (qr.Size + quietZone * 2) * moduleSize;
        var qrX0 = padding;
        var qrY0 = padding;
        var qrX1 = qrX0 + qrFullPx - 1;
        var qrY1 = qrY0 + qrFullPx - 1;

        var foundBadge = false;
        for (var y = 0; y < height && !foundBadge; y++) {
            for (var x = 0; x < width; x++) {
                if (x >= qrX0 && x <= qrX1 && y >= qrY0 && y <= qrY1) continue;
                var p = y * stride + x * 4;
                var r = rgba[p + 0];
                var g = rgba[p + 1];
                var b = rgba[p + 2];
                if (r != 255 || g != 255 || b != 255) {
                    foundBadge = true;
                    break;
                }
            }
        }

        Assert.True(foundBadge, "Expected badge pixels outside the QR bounds.");
    }

    [Fact]
    public void Render_With_Canvas_Ribbon_Clamps_To_Padding_And_Stays_Outside_Qr() {
        var matrix = new BitMatrix(21, 21);
        var moduleSize = 6;
        var quietZone = 4;
        var padding = 18;

        var opts = new QrPngRenderOptions {
            ModuleSize = moduleSize,
            QuietZone = quietZone,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            Canvas = new QrPngCanvasOptions {
                PaddingPx = padding,
                CornerRadiusPx = 16,
                Background = Rgba32.White,
                Badge = new QrPngCanvasBadgeOptions {
                    Shape = QrPngCanvasBadgeShape.Ribbon,
                    Position = QrPngCanvasBadgePosition.Bottom,
                    WidthPx = 140,
                    HeightPx = 28,
                    GapPx = 20,
                    TailPx = 16,
                    CornerRadiusPx = 8,
                    Color = new Rgba32(30, 120, 90, 220),
                },
            },
        };

        var png = QrPngRenderer.Render(matrix, opts);
        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);

        var qrFullPx = (matrix.Width + quietZone * 2) * moduleSize;
        var qrX0 = padding;
        var qrY0 = padding;
        var qrX1 = qrX0 + qrFullPx - 1;
        var qrY1 = qrY0 + qrFullPx - 1;

        var foundRibbon = false;
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                var p = y * stride + x * 4;
                var r = rgba[p + 0];
                var g = rgba[p + 1];
                var b = rgba[p + 2];

                var insideQr = x >= qrX0 && x <= qrX1 && y >= qrY0 && y <= qrY1;
                if (insideQr) {
                    Assert.True(r == 255 && g == 255 && b == 255, "Ribbon should not draw inside the QR bounds.");
                } else if (r != 255 || g != 255 || b != 255) {
                    foundRibbon = true;
                }
            }
        }

        Assert.True(foundRibbon, "Expected ribbon pixels outside the QR bounds.");
    }

    [Fact]
    public void Render_With_Canvas_Band_Draws_Outside_Qr_Bounds() {
        var qr = QrCodeEncoder.EncodeText("HELLO", QrErrorCorrectionLevel.H);
        var moduleSize = 8;
        var quietZone = 4;
        var padding = 28;

        var opts = new QrPngRenderOptions {
            ModuleSize = moduleSize,
            QuietZone = quietZone,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            Canvas = new QrPngCanvasOptions {
                PaddingPx = padding,
                CornerRadiusPx = 20,
                Background = Rgba32.White,
                Band = new QrPngCanvasBandOptions {
                    BandPx = 12,
                    GapPx = 0,
                    RadiusPx = 18,
                    Color = new Rgba32(30, 80, 160, 200),
                },
            },
        };

        var png = QrPngRenderer.Render(qr.Modules, opts);
        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);

        var qrFullPx = (qr.Size + quietZone * 2) * moduleSize;
        var qrX0 = padding;
        var qrY0 = padding;
        var qrX1 = qrX0 + qrFullPx - 1;
        var qrY1 = qrY0 + qrFullPx - 1;

        var foundBand = false;
        for (var y = 0; y < height && !foundBand; y++) {
            for (var x = 0; x < width; x++) {
                if (x >= qrX0 && x <= qrX1 && y >= qrY0 && y <= qrY1) continue;
                var p = y * stride + x * 4;
                var r = rgba[p + 0];
                var g = rgba[p + 1];
                var b = rgba[p + 2];
                if (r != 255 || g != 255 || b != 255) {
                    foundBand = true;
                    break;
                }
            }
        }

        Assert.True(foundBand, "Expected band pixels outside the QR bounds.");
    }

    [Fact]
    public void Render_With_Canvas_Band_Clamps_To_Padding_And_Stays_Outside_Qr() {
        var matrix = new BitMatrix(21, 21);
        var moduleSize = 6;
        var quietZone = 4;
        var padding = 12;

        var opts = new QrPngRenderOptions {
            ModuleSize = moduleSize,
            QuietZone = quietZone,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            Canvas = new QrPngCanvasOptions {
                PaddingPx = padding,
                CornerRadiusPx = 16,
                Background = Rgba32.White,
                Band = new QrPngCanvasBandOptions {
                    BandPx = 40,
                    GapPx = 12,
                    RadiusPx = 16,
                    Color = new Rgba32(80, 120, 200, 200),
                },
            },
        };

        var png = QrPngRenderer.Render(matrix, opts);
        var (rgba, width, height, stride) = PngTestDecoder.DecodeRgba32(png);

        var qrFullPx = (matrix.Width + quietZone * 2) * moduleSize;
        var qrX0 = padding;
        var qrY0 = padding;
        var qrX1 = qrX0 + qrFullPx - 1;
        var qrY1 = qrY0 + qrFullPx - 1;

        var foundBand = false;
        for (var y = 0; y < height; y++) {
            for (var x = 0; x < width; x++) {
                var p = y * stride + x * 4;
                var r = rgba[p + 0];
                var g = rgba[p + 1];
                var b = rgba[p + 2];

                var insideQr = x >= qrX0 && x <= qrX1 && y >= qrY0 && y <= qrY1;
                if (insideQr) {
                    Assert.True(r == 255 && g == 255 && b == 255, "Band should not draw inside the QR bounds.");
                } else if (r != 255 || g != 255 || b != 255) {
                    foundBand = true;
                }
            }
        }

        Assert.True(foundBand, "Expected band pixels outside the QR bounds.");
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

    [Fact]
    public void Render_With_PatternMask_Reduces_Dark_Pixels() {
        var matrix = new BitMatrix(1, 1);
        matrix[0, 0] = true;

        var solidOpts = new QrPngRenderOptions {
            ModuleSize = 18,
            QuietZone = 0,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
        };

        var maskOpts = new QrPngRenderOptions {
            ModuleSize = 18,
            QuietZone = 0,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            ForegroundPattern = new QrPngForegroundPatternOptions {
                Type = QrPngForegroundPatternType.StippleDots,
                SizePx = 4,
                ThicknessPx = 2,
                BlendMode = QrPngForegroundPatternBlendMode.Mask,
                ApplyToModules = true,
            },
        };

        var pngSolid = QrPngRenderer.Render(matrix, solidOpts);
        var (rgbaSolid, widthSolid, heightSolid, strideSolid) = PngTestDecoder.DecodeRgba32(pngSolid);
        var pngMasked = QrPngRenderer.Render(matrix, maskOpts);
        var (rgbaMasked, widthMasked, heightMasked, strideMasked) = PngTestDecoder.DecodeRgba32(pngMasked);

        static int CountDark(byte[] rgba, int width, int height, int stride) {
            var count = 0;
            for (var y = 0; y < height; y++) {
                var row = y * stride;
                for (var x = 0; x < width; x++) {
                    var idx = row + x * 4;
                    var r = rgba[idx + 0];
                    var g = rgba[idx + 1];
                    var b = rgba[idx + 2];
                    if (r < 200 || g < 200 || b < 200) count++;
                }
            }
            return count;
        }

        var solidCount = CountDark(rgbaSolid, widthSolid, heightSolid, strideSolid);
        var maskedCount = CountDark(rgbaMasked, widthMasked, heightMasked, strideMasked);

        Assert.True(maskedCount < solidCount, "Expected mask mode to reduce filled pixels.");
    }

    [Fact]
    public void Render_With_ModuleJitter_Changes_With_Seed() {
        var matrix = new BitMatrix(1, 1);
        matrix[0, 0] = true;

        static QrPngRenderOptions OptionsForSeed(int seed) => new() {
            ModuleSize = 20,
            QuietZone = 0,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            ModuleShape = QrPngModuleShape.Dot,
            ModuleJitter = new QrPngModuleJitterOptions {
                MaxOffsetPx = 3,
                Seed = seed,
            },
        };

        static int Fingerprint(byte[] rgba, int width, int height, int stride) {
            var hash = unchecked((int)2166136261);
            var length = height * stride;
            for (var i = 0; i < length; i++) {
                hash ^= rgba[i];
                hash = unchecked(hash * 16777619);
            }
            return hash;
        }

        var pngA = QrPngRenderer.Render(matrix, OptionsForSeed(100));
        var (rgbaA, widthA, heightA, strideA) = PngTestDecoder.DecodeRgba32(pngA);
        var pngB = QrPngRenderer.Render(matrix, OptionsForSeed(200));
        var (rgbaB, widthB, heightB, strideB) = PngTestDecoder.DecodeRgba32(pngB);

        Assert.Equal(widthA, widthB);
        Assert.Equal(heightA, heightB);

        var fpA = Fingerprint(rgbaA, widthA, heightA, strideA);
        var fpB = Fingerprint(rgbaB, widthB, heightB, strideB);
        Assert.NotEqual(fpA, fpB);
    }

    [Fact]
    public void Render_With_ShapeMap_Produces_Mixed_Shapes() {
        var matrix = new BitMatrix(2, 2);
        matrix[0, 0] = true;
        matrix[1, 0] = true;
        matrix[0, 1] = true;
        matrix[1, 1] = true;

        var baseOpts = new QrPngRenderOptions {
            ModuleSize = 14,
            QuietZone = 0,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            ModuleShape = QrPngModuleShape.Square,
        };

        var mapOpts = new QrPngRenderOptions {
            ModuleSize = 14,
            QuietZone = 0,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
            ModuleShape = QrPngModuleShape.Square,
            ModuleShapeMap = new QrPngModuleShapeMapOptions {
                Mode = QrPngModuleShapeMapMode.Checker,
                PrimaryShape = QrPngModuleShape.Square,
                SecondaryShape = QrPngModuleShape.Dot,
            },
        };

        var pngBase = QrPngRenderer.Render(matrix, baseOpts);
        var (rgbaBase, widthBase, heightBase, strideBase) = PngTestDecoder.DecodeRgba32(pngBase);
        var pngMap = QrPngRenderer.Render(matrix, mapOpts);
        var (rgbaMap, widthMap, heightMap, strideMap) = PngTestDecoder.DecodeRgba32(pngMap);

        static int Fingerprint(byte[] rgba, int width, int height, int stride) {
            var hash = unchecked((int)2166136261);
            var length = height * stride;
            for (var i = 0; i < length; i++) {
                hash ^= rgba[i];
                hash = unchecked(hash * 16777619);
            }
            return hash;
        }

        Assert.Equal(widthBase, widthMap);
        Assert.Equal(heightBase, heightMap);
        Assert.NotEqual(Fingerprint(rgbaBase, widthBase, heightBase, strideBase), Fingerprint(rgbaMap, widthMap, heightMap, strideMap));
    }
}
