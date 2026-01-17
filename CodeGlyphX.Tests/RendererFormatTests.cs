using System;
using System.Text;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Ascii;
using CodeGlyphX.Rendering.Bmp;
using CodeGlyphX.Rendering.Html;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Svg;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class RendererFormatTests {
    [Fact]
    public void Qr_Renderers_Produce_Expected_Formats() {
        var payload = "https://example.com";
        var png = QrEasy.RenderPng(payload);
        Assert.True(IsPng(png));

        var svg = QrEasy.RenderSvg(payload);
        Assert.Contains("<svg", svg, StringComparison.OrdinalIgnoreCase);

        var html = QrEasy.RenderHtml(payload);
        Assert.Contains("<table", html, StringComparison.OrdinalIgnoreCase);

        var bmp = QrEasy.RenderBmp(payload);
        Assert.True(IsBmp(bmp));

        var pdf = QrEasy.RenderPdf(payload);
        Assert.True(IsPdf(pdf));

        var eps = QrEasy.RenderEps(payload);
        Assert.True(IsEps(eps));

        var pdfRaster = QrEasy.RenderPdf(payload, mode: RenderMode.Raster);
        Assert.True(IsPdf(pdfRaster));

        var epsRaster = QrEasy.RenderEps(payload, mode: RenderMode.Raster);
        Assert.True(IsEps(epsRaster));

        var ascii = QrEasy.RenderAscii(payload, new MatrixAsciiRenderOptions { QuietZone = 1 });
        Assert.Contains("#", ascii, StringComparison.Ordinal);
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

        var pdf = Barcode.Pdf(BarcodeType.Code128, "CODEGLYPH-123");
        Assert.True(IsPdf(pdf));

        var eps = Barcode.Eps(BarcodeType.Code128, "CODEGLYPH-123");
        Assert.True(IsEps(eps));

        var pdfRaster = Barcode.Pdf(BarcodeType.Code128, "CODEGLYPH-123", mode: RenderMode.Raster);
        Assert.True(IsPdf(pdfRaster));

        var epsRaster = Barcode.Eps(BarcodeType.Code128, "CODEGLYPH-123", mode: RenderMode.Raster);
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

        var pdf = QrEasy.RenderPdf("https://example.com", opts, RenderMode.Vector);
        var pdfText = Encoding.ASCII.GetString(pdf);
        Assert.Contains(" c\n", pdfText, StringComparison.Ordinal);
        Assert.DoesNotContain("/Subtype /Image", pdfText, StringComparison.Ordinal);

        var eps = QrEasy.RenderEps("https://example.com", opts, RenderMode.Vector);
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

        var pdf = QrEasy.RenderPdf("https://example.com", opts, RenderMode.Vector);
        var pdfText = Encoding.ASCII.GetString(pdf);
        Assert.Contains("/Subtype /Image", pdfText, StringComparison.Ordinal);

        var eps = QrEasy.RenderEps("https://example.com", opts, RenderMode.Vector);
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

        var pdf = QrEasy.RenderPdf("https://example.com", opts, RenderMode.Vector);
        var pdfText = Encoding.ASCII.GetString(pdf);
        Assert.Contains("/Subtype /Image", pdfText, StringComparison.Ordinal);

        var eps = QrEasy.RenderEps("https://example.com", opts, RenderMode.Vector);
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
