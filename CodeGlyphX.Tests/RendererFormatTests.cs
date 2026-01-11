using System;
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
}
