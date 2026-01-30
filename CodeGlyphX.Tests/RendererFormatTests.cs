using System;
using System.Text;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Ascii;
using CodeGlyphX.Rendering.Bmp;
using CodeGlyphX.Rendering.Html;
using CodeGlyphX.Rendering.Ico;
using CodeGlyphX.Rendering.Pam;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Pbm;
using CodeGlyphX.Rendering.Pgm;
using CodeGlyphX.Rendering.Ppm;
using CodeGlyphX.Rendering.Svg;
using CodeGlyphX.Rendering.Svgz;
using CodeGlyphX.Rendering.Tga;
using CodeGlyphX.Rendering.Xbm;
using CodeGlyphX.Rendering.Xpm;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class RendererFormatTests {
    [Fact]
    public void Qr_Renderers_Produce_Expected_Formats() {
        var payload = "https://example.com";
        var png = QrCode.Render(payload, OutputFormat.Png).Data;
        Assert.True(IsPng(png));

        var svg = QrCode.Render(payload, OutputFormat.Svg).GetText();
        Assert.Contains("<svg", svg, StringComparison.OrdinalIgnoreCase);

        var html = QrCode.Render(payload, OutputFormat.Html).GetText();
        Assert.Contains("<table", html, StringComparison.OrdinalIgnoreCase);

        var bmp = QrCode.Render(payload, OutputFormat.Bmp).Data;
        Assert.True(IsBmp(bmp));

        var gif = QrCode.Render(payload, OutputFormat.Gif).Data;
        Assert.True(IsGif(gif));

        var tiff = QrCode.Render(payload, OutputFormat.Tiff).Data;
        Assert.True(IsTiff(tiff));

        var ppm = QrCode.Render(payload, OutputFormat.Ppm).Data;
        Assert.True(IsPpm(ppm));

        var pbm = QrCode.Render(payload, OutputFormat.Pbm).Data;
        Assert.True(IsPbm(pbm));

        var pgm = QrCode.Render(payload, OutputFormat.Pgm).Data;
        Assert.True(IsPgm(pgm));

        var pam = QrCode.Render(payload, OutputFormat.Pam).Data;
        Assert.True(IsPam(pam));

        var xbm = QrCode.Render(payload, OutputFormat.Xbm).GetText();
        Assert.StartsWith("#define", xbm, StringComparison.Ordinal);

        var xpm = QrCode.Render(payload, OutputFormat.Xpm).GetText();
        Assert.Contains("XPM", xpm, StringComparison.Ordinal);

        var tga = QrCode.Render(payload, OutputFormat.Tga).Data;
        Assert.True(IsTga(tga));

        var ico = QrCode.Render(payload, OutputFormat.Ico).Data;
        Assert.True(IsIco(ico));

        var svgz = QrCode.Render(payload, OutputFormat.Svgz).Data;
        Assert.True(IsGzip(svgz));

        var pdf = QrCode.Render(payload, OutputFormat.Pdf).Data;
        Assert.True(IsPdf(pdf));

        var eps = QrCode.Render(payload, OutputFormat.Eps).GetText();
        Assert.True(IsEps(eps));

        var pdfRaster = QrCode.Render(payload, OutputFormat.Pdf, extras: new RenderExtras { VectorMode = RenderMode.Raster }).Data;
        Assert.True(IsPdf(pdfRaster));

        var epsRaster = QrCode.Render(payload, OutputFormat.Eps, extras: new RenderExtras { VectorMode = RenderMode.Raster }).GetText();
        Assert.True(IsEps(epsRaster));

        var ascii = QrCode.Render(payload, OutputFormat.Ascii, extras: new RenderExtras {
            MatrixAscii = new MatrixAsciiRenderOptions { QuietZone = 1 }
        }).GetText();
        Assert.Contains("#", ascii, StringComparison.Ordinal);
    }

    [Fact]
    public void Qr_ArtPresets_Render_Png_Format() {
        var payload = "https://example.com/art";
#pragma warning disable CS0618 // QrArtPresets is deprecated in favor of QrArt.Theme + QrEasyOptions.Art.
        var presets = new[] {
            QrArtPresets.NeonGlowSafe(),
            QrArtPresets.LiquidGlassSafe(),
            QrArtPresets.ConnectedSquircleGlowSafe(),
            QrArtPresets.CutCornerTechSafe(),
            QrArtPresets.InsetRingsSafe(),
            QrArtPresets.StripeEyesSafe(),
            QrArtPresets.PaintSplashSafe(),
            QrArtPresets.PaintSplashPastelSafe(),
        };
#pragma warning restore CS0618

        foreach (var preset in presets) {
            var png = QrCode.Render(payload, OutputFormat.Png, preset).Data;
            Assert.True(ImageReader.TryDetectFormat(png, out var format));
            Assert.Equal(ImageFormat.Png, format);
        }
    }

    [Fact]
    public void Qr_ArtApi_Render_Png_Format() {
        var payload = "https://example.com/art-api";
        var arts = new[] {
            QrArt.Theme(QrArtTheme.NeonGlow, QrArtVariant.Safe, intensity: 60),
            QrArt.Theme(QrArtTheme.StripeEyes, QrArtVariant.Safe, intensity: 58),
            QrArt.Theme(QrArtTheme.PaintSplash, QrArtVariant.Pastel, intensity: 62),
        };

        foreach (var art in arts) {
            var png = QrCode.Render(payload, OutputFormat.Png, new QrEasyOptions { Art = art }).Data;
            Assert.True(ImageReader.TryDetectFormat(png, out var format));
            Assert.Equal(ImageFormat.Png, format);
        }
    }

    [Fact]
    public void Qr_Ascii_UnicodeBlocks_Uses_Block_Glyphs() {
        var payload = "https://example.com";
        var ascii = QrCode.Render(payload, OutputFormat.Ascii, extras: new RenderExtras {
            MatrixAscii = new MatrixAsciiRenderOptions {
                QuietZone = 1,
                UseUnicodeBlocks = true
            }
        }).GetText();

        Assert.Contains("█", ascii, StringComparison.Ordinal);
    }

    [Fact]
    public void Qr_Ascii_AnsiColors_Emits_Escape_Codes() {
        var payload = "https://example.com";
        var ascii = QrCode.Render(payload, OutputFormat.Ascii, extras: new RenderExtras {
            MatrixAscii = new MatrixAsciiRenderOptions {
                QuietZone = 1,
                UseUnicodeBlocks = true,
                UseAnsiColors = true,
                UseAnsiTrueColor = false
            }
        }).GetText();

        Assert.Contains("\u001b[", ascii, StringComparison.Ordinal);
    }

    [Fact]
    public void Qr_Ascii_ConsolePreset_Is_Scan_Friendly() {
        var payload = "https://example.com";
        var ascii = QrCode.Render(payload, OutputFormat.Ascii, extras: new RenderExtras {
            MatrixAscii = AsciiPresets.Console(scale: 3)
        }).GetText();

        Assert.Contains("█", ascii, StringComparison.Ordinal);
        Assert.Contains("\u001b[", ascii, StringComparison.Ordinal);
    }

    [Fact]
    public void Qr_Ascii_ConsoleWrapper_Uses_Preset() {
        var payload = "https://example.com";
        var ascii = QrCode.Render(payload, OutputFormat.Ascii, extras: new RenderExtras {
            MatrixAscii = AsciiPresets.Console(scale: 3)
        }).GetText();

        Assert.Contains("█", ascii, StringComparison.Ordinal);
        Assert.Contains("\u001b[", ascii, StringComparison.Ordinal);
    }

    [Fact]
    public void Matrix_Ascii_HalfBlocks_Compresses_Height() {
        var matrix = new BitMatrix(2, 2);
        matrix[0, 0] = true;
        matrix[1, 1] = true;

        var ascii = MatrixAsciiRenderer.Render(matrix, new MatrixAsciiRenderOptions {
            QuietZone = 0,
            UseHalfBlocks = true,
            UseAnsiColors = false
        });

        var lines = ascii.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        Assert.Single(lines);
        Assert.Contains("▀", ascii, StringComparison.Ordinal);
        Assert.Contains("▄", ascii, StringComparison.Ordinal);
    }

    [Fact]
    public void AsciiConsole_Fit_Clamps_Scale() {
        var matrix = new BitMatrix(21, 21);
        var options = AsciiConsole.Fit(matrix, new AsciiConsoleOptions {
            WindowWidth = 60,
            WindowHeight = 20,
            MinScale = 2,
            MaxScale = 2
        });

        Assert.Equal(2, options.Scale);
    }

    [Fact]
    public void AsciiConsole_Fit_Shrinks_ModuleWidth_When_Needed() {
        var matrix = new BitMatrix(29, 29);
        var options = AsciiConsole.Fit(matrix, new AsciiConsoleOptions {
            WindowWidth = 40,
            WindowHeight = 40,
            UseHalfBlocks = false,
            AllowModuleWidthShrink = true,
            MinScale = 1,
            MaxScale = 1
        });

        Assert.Equal(1, options.ModuleWidth);
    }

    [Fact]
    public void Qr_Ascii_Console_Extras_Use_HalfBlocks() {
        var payload = "https://example.com/console";
        var ascii = QrCode.Render(payload, OutputFormat.Ascii, extras: new RenderExtras {
            AsciiConsole = new AsciiConsoleOptions {
                UseHalfBlocks = true,
                UseAnsiColors = false,
                WindowWidth = 200,
                WindowHeight = 200
            }
        }).GetText();

        Assert.Contains("▀", ascii, StringComparison.Ordinal);
    }

    [Fact]
    public void AsciiConsole_Disables_Unicode_When_Encoding_Does_Not_Support() {
        var matrix = new BitMatrix(21, 21);
        var options = AsciiConsole.Fit(matrix, new AsciiConsoleOptions {
            OutputEncoding = Encoding.ASCII,
            UseHalfBlocks = true,
            UseUnicodeBlocks = true,
            UseAnsiColors = false
        });

        Assert.False(options.UseHalfBlocks);
        Assert.False(options.UseUnicodeBlocks);
    }

    [Fact]
    public void AsciiConsole_Upgrades_ModuleWidth_When_HalfBlocks_Disabled() {
        var matrix = new BitMatrix(21, 21);
        var options = AsciiConsole.Fit(matrix, new AsciiConsoleOptions {
            OutputEncoding = Encoding.ASCII,
            UseHalfBlocks = true,
            UseUnicodeBlocks = true,
            UseAnsiColors = false
        });

        Assert.Equal(2, options.ModuleWidth);
    }

    [Fact]
    public void AsciiConsole_Overrides_Dark_Light_Glyphs() {
        var matrix = new BitMatrix(1, 1);
        matrix[0, 0] = true;

        var options = AsciiConsole.Fit(matrix, new AsciiConsoleOptions {
            UseHalfBlocks = false,
            UseUnicodeBlocks = false,
            UseAnsiColors = false,
            Dark = "X",
            Light = ".",
            QuietZone = 0,
            WindowWidth = 10,
            WindowHeight = 5
        });

        var ascii = MatrixAsciiRenderer.Render(matrix, options);
        Assert.Contains("X", ascii, StringComparison.Ordinal);
    }

    [Fact]
    public void AsciiConsole_HalfBlock_FgOnly_Does_Not_Emit_Background_Color() {
        var payload = "https://example.com/console";
        var ascii = QrCode.Render(payload, OutputFormat.Ascii, extras: new RenderExtras {
            AsciiConsole = new AsciiConsoleOptions {
                UseHalfBlocks = true,
                HalfBlockUseBackground = false,
                UseAnsiColors = true,
                WindowWidth = 120,
                WindowHeight = 60
            }
        }).GetText();

        Assert.DoesNotContain("\u001b[48;", ascii, StringComparison.Ordinal);
    }

    [Fact]
    public void AsciiConsole_AspectRatio_Sets_ModuleWidth() {
        var matrix = new BitMatrix(21, 21);
        var options = AsciiConsole.Fit(matrix, new AsciiConsoleOptions {
            UseHalfBlocks = false,
            CellAspectRatio = 0.5,
            UseAnsiColors = false
        });

        Assert.Equal(2, options.ModuleWidth);
    }

    [Fact]
    public void AsciiConsole_TargetWidth_Adjusts_Scale() {
        var matrix = new BitMatrix(21, 21);
        var options = AsciiConsole.Fit(matrix, new AsciiConsoleOptions {
            WindowWidth = 40,
            WindowHeight = 20,
            TargetWidth = 100,
            TargetHeight = 100,
            PaddingColumns = 0,
            PaddingRows = 0,
            UseHalfBlocks = false,
            ModuleWidth = 1,
            ModuleHeight = 1,
            QuietZone = 0,
            MinScale = 1,
            MaxScale = 4,
            UseAnsiColors = false
        });

        Assert.Equal(4, options.Scale);
    }

    [Fact]
    public void AsciiConsole_PreferScanReliability_Enables_Contrast_Safety() {
        var matrix = new BitMatrix(21, 21);
        var options = AsciiConsole.Fit(matrix, new AsciiConsoleOptions {
            PreferScanReliability = true,
            UseHalfBlocks = true,
            UseAnsiColors = true,
            ColorizeLight = false,
            HalfBlockUseBackground = false,
            MinQuietZone = 0
        });

        Assert.True(options.UseHalfBlockBackground);
        Assert.True(options.AnsiColorizeLight);
        Assert.True(options.EnsureDarkContrast);
        Assert.True(options.QuietZone >= 2);
    }

    [Fact]
    public void Qr_Ascii_Scale_Increases_Output_Size() {
        var payload = "https://example.com";
        var baseAscii = QrCode.Render(payload, OutputFormat.Ascii, extras: new RenderExtras {
            MatrixAscii = new MatrixAsciiRenderOptions {
                QuietZone = 1,
                ModuleWidth = 1,
                ModuleHeight = 1,
                Scale = 1
            }
        }).GetText();
        var scaledAscii = QrCode.Render(payload, OutputFormat.Ascii, extras: new RenderExtras {
            MatrixAscii = new MatrixAsciiRenderOptions {
                QuietZone = 1,
                ModuleWidth = 1,
                ModuleHeight = 1,
                Scale = 2
            }
        }).GetText();

        Assert.True(scaledAscii.Length > baseAscii.Length);
    }

    [Fact]
    public void Qr_Ico_Respects_MultiSize_Options() {
        var payload = "https://example.com";
        var opts = new QrEasyOptions {
            IcoSizes = new[] { 32, 64, 128 }
        };

        var ico = QrCode.Render(payload, OutputFormat.Ico, opts).Data;
        Assert.True(IsIco(ico));
        Assert.Equal(3, GetIcoCount(ico));
    }

    [Fact]
    public void Barcode_Renderers_Produce_Expected_Formats() {
        var barcode = BarcodeEncoder.Encode(BarcodeType.Code128, "CODEGLYPH-123");
        var png = BarcodePngRenderer.Render(barcode, new BarcodePngRenderOptions());
        Assert.True(IsPng(png));

        var svg = SvgBarcodeRenderer.Render(barcode, new BarcodeSvgRenderOptions());
        Assert.Contains("<svg", svg, StringComparison.OrdinalIgnoreCase);

        var html = HtmlBarcodeRenderer.Render(barcode, new BarcodeHtmlRenderOptions());
        Assert.Contains("<table", html, StringComparison.OrdinalIgnoreCase);

        var bmp = BarcodeBmpRenderer.Render(barcode, new BarcodePngRenderOptions());
        Assert.True(IsBmp(bmp));

        var gif = Barcode.Render(BarcodeType.Code128, "CODEGLYPH-123", OutputFormat.Gif).Data;
        Assert.True(IsGif(gif));

        var tiff = Barcode.Render(BarcodeType.Code128, "CODEGLYPH-123", OutputFormat.Tiff).Data;
        Assert.True(IsTiff(tiff));

        var ppm = BarcodePpmRenderer.Render(barcode, new BarcodePngRenderOptions());
        Assert.True(IsPpm(ppm));

        var pbm = BarcodePbmRenderer.Render(barcode, new BarcodePngRenderOptions());
        Assert.True(IsPbm(pbm));

        var pgm = BarcodePgmRenderer.Render(barcode, new BarcodePngRenderOptions());
        Assert.True(IsPgm(pgm));

        var pam = BarcodePamRenderer.Render(barcode, new BarcodePngRenderOptions());
        Assert.True(IsPam(pam));

        var xbm = BarcodeXbmRenderer.Render(barcode, new BarcodePngRenderOptions());
        Assert.StartsWith("#define", xbm, StringComparison.Ordinal);

        var xpm = BarcodeXpmRenderer.Render(barcode, new BarcodePngRenderOptions());
        Assert.Contains("XPM", xpm, StringComparison.Ordinal);

        var tga = BarcodeTgaRenderer.Render(barcode, new BarcodePngRenderOptions());
        Assert.True(IsTga(tga));

        var ico = BarcodeIcoRenderer.Render(barcode, new BarcodePngRenderOptions());
        Assert.True(IsIco(ico));

        var svgz = BarcodeSvgzRenderer.Render(barcode, new BarcodeSvgRenderOptions());
        Assert.True(IsGzip(svgz));

        var pdf = Barcode.Render(BarcodeType.Code128, "CODEGLYPH-123", OutputFormat.Pdf).Data;
        Assert.True(IsPdf(pdf));

        var eps = Barcode.Render(BarcodeType.Code128, "CODEGLYPH-123", OutputFormat.Eps).GetText();
        Assert.True(IsEps(eps));

        var pdfRaster = Barcode.Render(BarcodeType.Code128, "CODEGLYPH-123", OutputFormat.Pdf, extras: new RenderExtras { VectorMode = RenderMode.Raster }).Data;
        Assert.True(IsPdf(pdfRaster));

        var epsRaster = Barcode.Render(BarcodeType.Code128, "CODEGLYPH-123", OutputFormat.Eps, extras: new RenderExtras { VectorMode = RenderMode.Raster }).GetText();
        Assert.True(IsEps(epsRaster));

        var ascii = BarcodeAsciiRenderer.Render(barcode, new BarcodeAsciiRenderOptions { QuietZone = 1, Height = 2 });
        Assert.Contains("#", ascii, StringComparison.Ordinal);
    }

    [Fact]
    public void Qr_Vector_Pdf_Eps_Use_Curves_For_Rounded_Modules() {
        var opts = new QrEasyOptions {
            ModuleShape = QrPngModuleShape.Rounded,
            ModuleCornerRadiusPx = 2,
        };

        var pdf = QrCode.Render("https://example.com", OutputFormat.Pdf, opts, new RenderExtras { VectorMode = RenderMode.Vector }).Data;
        var pdfText = Encoding.ASCII.GetString(pdf);
        Assert.Contains(" c\n", pdfText, StringComparison.Ordinal);
        Assert.DoesNotContain("/Subtype /Image", pdfText, StringComparison.Ordinal);

        var eps = QrCode.Render("https://example.com", OutputFormat.Eps, opts, new RenderExtras { VectorMode = RenderMode.Vector }).GetText();
        Assert.Contains("curveto", eps, StringComparison.Ordinal);
        Assert.DoesNotContain("colorimage", eps, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Qr_Vector_Mode_Falls_Back_To_Raster_When_Gradient() {
        var opts = new QrEasyOptions {
            ForegroundGradient = new QrPngGradientOptions {
                Type = QrPngGradientType.Horizontal,
                StartColor = new Rgba32(0, 0, 0),
                EndColor = new Rgba32(255, 0, 0),
            },
        };

        var pdf = QrCode.Render("https://example.com", OutputFormat.Pdf, opts, new RenderExtras { VectorMode = RenderMode.Vector }).Data;
        var pdfText = Encoding.ASCII.GetString(pdf);
        Assert.Contains("/Subtype /Image", pdfText, StringComparison.Ordinal);

        var eps = QrCode.Render("https://example.com", OutputFormat.Eps, opts, new RenderExtras { VectorMode = RenderMode.Vector }).GetText();
        Assert.Contains("colorimage", eps, StringComparison.Ordinal);
    }

    [Fact]
    public void Qr_Vector_Mode_Falls_Back_To_Raster_When_Logo() {
        var logoMatrix = new BitMatrix(1, 1);
        logoMatrix[0, 0] = true;

        var logoPng = QrPngRenderer.Render(logoMatrix, new QrPngRenderOptions {
            ModuleSize = 2,
            QuietZone = 0,
            Foreground = Rgba32.Black,
            Background = Rgba32.White,
        });

        var opts = new QrEasyOptions {
            LogoPng = logoPng,
        };

        var pdf = QrCode.Render("https://example.com", OutputFormat.Pdf, opts, new RenderExtras { VectorMode = RenderMode.Vector }).Data;
        var pdfText = Encoding.ASCII.GetString(pdf);
        Assert.Contains("/Subtype /Image", pdfText, StringComparison.Ordinal);

        var eps = QrCode.Render("https://example.com", OutputFormat.Eps, opts, new RenderExtras { VectorMode = RenderMode.Vector }).GetText();
        Assert.Contains("colorimage", eps, StringComparison.Ordinal);
    }

    private static bool IsPng(byte[] data) {
        if (data is null || data.Length < 8) return false;
        return data[0] == 0x89 &&
               data[1] == 0x50 &&
               data[2] == 0x4E &&
               data[3] == 0x47 &&
               data[4] == 0x0D &&
               data[5] == 0x0A &&
               data[6] == 0x1A &&
               data[7] == 0x0A;
    }

    private static bool IsBmp(byte[] data) {
        if (data is null || data.Length < 2) return false;
        return data[0] == (byte)'B' && data[1] == (byte)'M';
    }

    private static bool IsGif(byte[] data) {
        if (data is null || data.Length < 6) return false;
        return data[0] == (byte)'G' &&
               data[1] == (byte)'I' &&
               data[2] == (byte)'F' &&
               data[3] == (byte)'8' &&
               (data[4] == (byte)'7' || data[4] == (byte)'9') &&
               data[5] == (byte)'a';
    }

    private static bool IsTiff(byte[] data) {
        if (data is null || data.Length < 4) return false;
        return (data[0] == (byte)'I' && data[1] == (byte)'I' && data[2] == 42 && data[3] == 0) ||
               (data[0] == (byte)'M' && data[1] == (byte)'M' && data[2] == 0 && data[3] == 42);
    }

    private static bool IsPpm(byte[] data) {
        if (data is null || data.Length < 2) return false;
        return data[0] == (byte)'P' && data[1] == (byte)'6';
    }

    private static bool IsPbm(byte[] data) {
        if (data is null || data.Length < 2) return false;
        return data[0] == (byte)'P' && data[1] == (byte)'4';
    }

    private static bool IsPgm(byte[] data) {
        if (data is null || data.Length < 2) return false;
        return data[0] == (byte)'P' && data[1] == (byte)'5';
    }

    private static bool IsPam(byte[] data) {
        if (data is null || data.Length < 2) return false;
        return data[0] == (byte)'P' && data[1] == (byte)'7';
    }

    private static bool IsTga(byte[] data) {
        if (data is null || data.Length < 3) return false;
        return data[1] == 0 && data[2] == 2;
    }

    private static bool IsIco(byte[] data) {
        if (data is null || data.Length < 4) return false;
        return data[0] == 0 && data[1] == 0 && data[2] == 1 && data[3] == 0;
    }

    private static int GetIcoCount(byte[] data) {
        if (data is null || data.Length < 6) return 0;
        return data[4] | (data[5] << 8);
    }

    private static bool IsGzip(byte[] data) {
        if (data is null || data.Length < 2) return false;
        return data[0] == 0x1F && data[1] == 0x8B;
    }

    private static bool IsPdf(byte[] data) {
        if (data is null || data.Length < 5) return false;
        return data[0] == (byte)'%' &&
               data[1] == (byte)'P' &&
               data[2] == (byte)'D' &&
               data[3] == (byte)'F' &&
               data[4] == (byte)'-';
    }

    private static bool IsEps(string text) {
        if (string.IsNullOrEmpty(text)) return false;
        return text.StartsWith("%!PS-Adobe", StringComparison.Ordinal);
    }
}
