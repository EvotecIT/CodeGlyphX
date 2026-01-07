using CodeMatrix;
using CodeMatrix.Rendering.Png;
using CodeMatrix.Tests.TestHelpers;
using Xunit;

namespace CodeMatrix.Tests;

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
}
