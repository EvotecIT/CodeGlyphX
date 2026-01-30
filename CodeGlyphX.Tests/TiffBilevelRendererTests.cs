using CodeGlyphX;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Tiff;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class TiffBilevelRendererTests {
    [Fact]
    public void Render_Bilevel_Matrix_Tiff() {
        var modules = new BitMatrix(1, 1);
        modules[0, 0] = true;

        var opts = new MatrixPngRenderOptions {
            ModuleSize = 1,
            QuietZone = 0
        };

        var tiff = MatrixTiffRenderer.RenderBilevel(modules, opts);
        var rgba = TiffReader.DecodeRgba32(tiff, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(0, rgba[0]);
    }

    [Fact]
    public void Render_Bilevel_Barcode_Tiff() {
        var barcode = new Barcode1D(new[] {
            new BarSegment(true, 1),
            new BarSegment(false, 1)
        });

        var opts = new BarcodePngRenderOptions {
            ModuleSize = 1,
            QuietZone = 0,
            HeightModules = 1
        };

        var tiff = BarcodeTiffRenderer.RenderBilevel(barcode, opts);
        var rgba = TiffReader.DecodeRgba32(tiff, out var width, out var height);

        Assert.Equal(2, width);
        Assert.Equal(1, height);
        Assert.Equal(0, rgba[0]);
        Assert.Equal(255, rgba[4]);
    }

    [Fact]
    public void Render_Bilevel_Tiled_Matrix_Tiff() {
        var modules = new BitMatrix(1, 1);
        modules[0, 0] = true;

        var opts = new MatrixPngRenderOptions {
            ModuleSize = 1,
            QuietZone = 0
        };

        var tiff = MatrixTiffRenderer.RenderBilevelTiled(modules, opts, tileWidth: 2, tileHeight: 2);
        var rgba = TiffReader.DecodeRgba32(tiff, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(0, rgba[0]);
    }

    [Fact]
    public void Render_Bilevel_From_Modules_Respects_Color_Luminance() {
        var modules = new BitMatrix(1, 1);
        modules[0, 0] = true;

        var opts = new MatrixPngRenderOptions {
            ModuleSize = 1,
            QuietZone = 0,
            Foreground = Rgba32.White,
            Background = Rgba32.Black
        };

        var tiff = MatrixTiffRenderer.RenderBilevelFromModules(modules, opts);
        var rgba = TiffReader.DecodeRgba32(tiff, out var width, out var height);

        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(255, rgba[0]);
    }

    [Fact]
    public void Render_Barcode_Bilevel_From_Bars_Respects_Color_Luminance() {
        var barcode = new Barcode1D(new[] {
            new BarSegment(true, 1),
            new BarSegment(false, 1)
        });

        var opts = new BarcodePngRenderOptions {
            ModuleSize = 1,
            QuietZone = 0,
            HeightModules = 1,
            Foreground = Rgba32.White,
            Background = Rgba32.Black
        };

        var tiff = BarcodeTiffRenderer.RenderBilevelFromBars(barcode, opts);
        var rgba = TiffReader.DecodeRgba32(tiff, out var width, out var height);

        Assert.Equal(2, width);
        Assert.Equal(1, height);
        Assert.Equal(255, rgba[0]);
        Assert.Equal(0, rgba[4]);
    }
}
