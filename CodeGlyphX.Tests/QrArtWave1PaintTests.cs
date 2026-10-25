using System.Xml.Linq;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Svg;
using CodeGlyphX.Tests.TestHelpers;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("GlobalState")]
public sealed class QrArtWave1PaintTests {
    private static readonly Rgba32 Red = new(160, 0, 0);
    private static readonly Rgba32 Blue = new(0, 0, 160);
    private const int Module = 20;

    [Theory]
    [InlineData(QrModuleShape.BarsVertical)]
    [InlineData(QrModuleShape.BarsHorizontal)]
    public void DirectionalBars_JoinOnlyTheirNamedAxisWhenInset(QrModuleShape shape) {
        var modules = new BitMatrix(25, 25);
        modules[12, 12] = modules[13, 12] = modules[12, 13] = true;
        var image = Read(modules, new QrPngRenderOptions {
            ModuleSize = Module, ModuleShape = shape, ModuleScale = 0.7
        });
        Assert.Equal(shape == QrModuleShape.BarsVertical ? Rgba32.White : Rgba32.Black, image.Sample(12, 12, 19, 10));
        Assert.Equal(shape == QrModuleShape.BarsVertical ? Rgba32.Black : Rgba32.White, image.Sample(12, 12, 10, 19));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(255)]
    public void PatternMask_SelectsOriginalInk_WithoutPaintingTheMaskColor(byte alpha) {
        var modules = new BitMatrix(25, 25);
        modules[12, 12] = true;
        var image = Read(modules, new QrPngRenderOptions {
            ModuleSize = Module, Foreground = Blue, ProtectFunctionalPatterns = false,
            ForegroundPattern = new QrForegroundPatternOptions {
                Type = QrForegroundPatternType.DiagonalStripes, SizePx = 6, ThicknessPx = 1,
                Color = new Rgba32(Red.R, Red.G, Red.B, alpha), BlendMode = QrForegroundPatternBlendMode.Mask
            }
        });
        var retained = 0; var removed = 0;
        for (var y = 0; y < Module; y++) for (var x = 0; x < Module; x++) {
            var color = image.Sample(12, 12, x, y);
            Assert.True(color.Equals(Blue) || color.Equals(Rgba32.White), "Mask selects ink; its own color must not be composited.");
            if (color.Equals(Blue)) retained++; else removed++;
        }
        Assert.True(retained > 0 && removed > 0);
    }

    [Theory]
    [InlineData(QrForegroundPatternBlendMode.Overlay)]
    [InlineData(QrForegroundPatternBlendMode.Replace)]
    public void TransparentNonMaskPatterns_LeaveFindersBodyAndLiquidJoinsUnchanged(QrForegroundPatternBlendMode blend) {
        var qr = QR.Encode("TRANSPARENT-FINDER-PAINT");
        var liquid = new BitMatrix(25, 25);
        liquid[12, 12] = liquid[13, 12] = liquid[12, 13] = true;
        foreach (var modules in new[] { qr.Modules, liquid }) {
            var options = new QrPngRenderOptions { ModuleSize = Module, ModuleShape = QrModuleShape.Liquid, Foreground = Red };
            var baseline = Read(modules, options);
            options.ForegroundPattern = new QrForegroundPatternOptions {
                Type = QrForegroundPatternType.DiagonalStripes, SizePx = 1, ThicknessPx = 1,
                BlendMode = blend, Color = Rgba32.Transparent, ApplyToModules = true, ApplyToEyes = true
            };
            var actual = Read(modules, options);
            Assert.Equal(baseline.Pixels, actual.Pixels);
            if (modules == liquid) Assert.Equal(Red, actual.Sample(13, 13, 0, 0));
        }
    }

    [Theory]
    [InlineData(0, true)] [InlineData(1, true)] [InlineData(2, true)] [InlineData(3, true)]
    [InlineData(0, false)] [InlineData(1, false)] [InlineData(2, false)] [InlineData(3, false)]
    public void TargetSizing_UsesTheGuardrailNormalizedQuietZone(int quietZone, bool includesQuietZone) {
        var qr = QR.Encode("WAVE1-SIZING");
        QrRenderOptions Options(int quiet) => new() {
            ModuleShape = QrModuleShape.Circle, QuietZone = quiet, TargetSizePx = 256,
            TargetSizeIncludesQuietZone = includesQuietZone
        };
        var expected = PngTestDecoder.DecodeRgba32(qr.Render(OutputFormat.Png, Options(4)).Data.ToArray());
        var actual = PngTestDecoder.DecodeRgba32(qr.Render(OutputFormat.Png, Options(quietZone)).Data.ToArray());
        Assert.Equal(includesQuietZone ? 256 : 256 + 8 * (256 / qr.Size), actual.width);
        Assert.Equal(actual.width, actual.height);
        Assert.Equal(expected.width, actual.width);
        Assert.Equal(expected.rgba, actual.rgba);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void TargetSizing_RejectsTargetsSmallerThanNormalizedOnePixelModules(bool includesQuietZone) {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => QR.Render("SMALL-TARGET", OutputFormat.Png, new QrRenderOptions {
            ModuleShape = QrModuleShape.Circle, QuietZone = 0, TargetSizePx = 8, TargetSizeIncludesQuietZone = includesQuietZone
        }));
        Assert.Equal(nameof(QrRenderOptions.TargetSizePx), error.ParamName);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public void WholeFinders_RespectGlobalAndCornerPalettes_AndApplyToEyes(bool cornerZone, bool applyToEyes) {
        var qr = QR.Encode("WAVE1-FINDER-PAINT");
        var palette = new QrPaletteOptions { Colors = new[] { Red }, ApplyToEyes = applyToEyes };
        var options = new QrPngRenderOptions { ModuleSize = Module, Eyes = new QrEyeOptions() };
        if (cornerZone) options.ForegroundPaletteZones = new QrPaletteZoneOptions { CornerPalette = palette, CornerSize = 8 };
        else options.ForegroundPalette = palette;
        var image = Read(qr.Modules, options);
        var expected = applyToEyes ? Red : Rgba32.Black;
        Assert.Equal(expected, image.Sample(3, 0, 10, 10));
        Assert.Equal(expected, image.Sample(3, 3, 10, 10));
        Assert.Equal(Rgba32.White, image.Sample(1, 3, 10, 10));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void WholeFinders_RespectTheForegroundPatternApplyToEyesSwitch(bool applyToEyes) {
        var qr = QR.Encode("WAVE1-FINDER-PATTERN");
        var image = Read(qr.Modules, new QrPngRenderOptions {
            ModuleSize = Module, Eyes = new QrEyeOptions(), ForegroundPattern = new QrForegroundPatternOptions {
                Type = QrForegroundPatternType.StippleDots, SizePx = 1, ThicknessPx = 1,
                Color = Red, BlendMode = QrForegroundPatternBlendMode.Replace, ApplyToEyes = applyToEyes
            }
        });
        Assert.Equal(applyToEyes ? Red : Rgba32.Black, image.Sample(3, 0, 10, 10));
        Assert.Equal(applyToEyes ? Red : Rgba32.Black, image.Sample(3, 3, 10, 10));
        Assert.Equal(Rgba32.White, image.Sample(1, 3, 10, 10));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void WholeFinders_PreserveExplicitOuterAndInnerCornerRadii(bool inner) {
        var qr = QR.Encode("WAVE1-CORNER-RADIUS");
        PaintImage Render(int radius) => Read(qr.Modules, new QrPngRenderOptions {
            ModuleSize = Module, Eyes = new QrEyeOptions {
                OuterShape = QrModuleShape.Rounded, InnerShape = QrModuleShape.Rounded,
                OuterCornerRadiusPx = inner ? 1 : radius, InnerCornerRadiusPx = inner ? radius : 1
            }
        });
        var cell = inner ? 2 : 0;
        Assert.Equal(Rgba32.Black, Render(1).Sample(cell, cell, 2, 2));
        Assert.Equal(Rgba32.White, Render(20).Sample(cell, cell, 2, 2));
    }

    [Fact]
    public void ShapeOnlyEyes_PreserveTheGlobalGradient_WithoutMaterializingFlatColors() {
        var qr = QR.Encode("WAVE1-GLOBAL-EYE-GRADIENT");
        var options = new QrRenderOptions {
            ModuleSize = Module, ModuleShape = QrModuleShape.Circle,
            ForegroundGradient = new QrGradientOptions { StartColor = Rgba32.Black, EndColor = new Rgba32(0, 0, 100) }
        };
        var implicitEyes = PngTestDecoder.DecodeRgba32(qr.Render(OutputFormat.Png, options).Data.ToArray());
        options.Eyes = new QrEyeOptions { OuterShape = QrModuleShape.Rounded, InnerShape = QrModuleShape.Rounded };
        var shapeOnly = PngTestDecoder.DecodeRgba32(qr.Render(OutputFormat.Png, options).Data.ToArray());
        var image = new PaintImage(shapeOnly.rgba, shapeOnly.width, shapeOnly.height, Module, 4);
        var left = image.Sample(3, 0, 10, 10);
        var right = image.Sample(qr.Size - 4, 0, 10, 10);
        Assert.True(left.B > 0 && left.B < right.B, "Global horizontal paint must continue across the whole finders.");
        Assert.Equal(implicitEyes.rgba, shapeOnly.rgba);
        Assert.Null(options.Eyes.OuterColor);
        Assert.Null(options.Eyes.InnerColor);
    }

    [Theory]
    [InlineData("Palette")] [InlineData("Pattern")] [InlineData("ShapeMap")]
    public void LiquidConcaveJoins_UseTheSamePaintAsTheSurroundingBody(string paint) {
        var modules = new BitMatrix(25, 25);
        modules[12, 12] = modules[13, 12] = modules[12, 13] = true;
        var options = new QrPngRenderOptions { ModuleSize = Module, ModuleShape = QrModuleShape.Liquid, Foreground = Red };
        if (paint == "Palette") {
            options.Foreground = Rgba32.Black;
            options.ForegroundPalette = new QrPaletteOptions { Colors = new[] { Red } };
        } else if (paint == "Pattern") options.ForegroundPattern = new QrForegroundPatternOptions {
            SizePx = 1, ThicknessPx = 1, Color = Blue, BlendMode = QrForegroundPatternBlendMode.Replace
        };
        else {
            options.ModuleShape = QrModuleShape.Square;
            options.ModuleShapeMap = new QrModuleShapeMapOptions { PrimaryShape = QrModuleShape.Liquid, SecondaryShape = QrModuleShape.Liquid };
        }
        var image = Read(modules, options);
        var expected = paint == "Pattern" ? Blue : Red;
        Assert.Equal(expected, image.Sample(12, 12, 10, 10));
        Assert.Equal(expected, image.Sample(13, 13, 0, 0));
        Assert.Equal(Rgba32.White, image.Sample(13, 13, 10, 10));
    }

    [Fact]
    public void DirectSvg_ScaledSquareGradient_PreservesTheRasterCoverage() {
        var qr = QR.Encode("WAVE1-SVG-SCALE");
        var gradient = new QrGradientOptions { StartColor = Rgba32.Black, EndColor = Blue };
        var svg = XElement.Parse(SvgQrRenderer.Render(qr.Modules, new QrSvgRenderOptions {
            ModuleSize = Module, ModuleScale = 0.5, ForegroundGradient = gradient
        }));
        var expected = Read(qr.Modules, new QrPngRenderOptions { ModuleSize = Module, ModuleScale = 0.5, ForegroundGradient = gradient });
        AssertVectorPixels(svg, qr.Size, expected);
    }

    [Fact]
    public void DirectSvg_SpaceSeparatedRgb_HasTheSamePaintAsCommaSeparatedRgb() {
        var qr = QR.Encode("WAVE1-SVG-CSS");
        var spaced = XElement.Parse(SvgQrRenderer.Render(qr.Modules, new QrSvgRenderOptions {
            ModuleSize = Module, ModuleShape = QrModuleShape.Circle, DarkColor = "rgb(160 0 0)"
        }));
        var comma = XElement.Parse(SvgQrRenderer.Render(qr.Modules, new QrSvgRenderOptions {
            ModuleSize = Module, ModuleShape = QrModuleShape.Circle, DarkColor = "rgb(160, 0, 0)"
        }));
        var expected = Read(qr.Modules, new QrPngRenderOptions { ModuleSize = Module, ModuleShape = QrModuleShape.Circle, Foreground = Red });
        AssertVectorPixels(spaced, qr.Size, expected);
        AssertVectorPixels(comma, qr.Size, expected);
    }

    [Theory]
    [InlineData("grey", 128)]
    [InlineData("Grey", 128)]
    [InlineData("DARKGREY", 169)]
    public void DirectSvg_CssGreyAliases_AreCaseInsensitive(string css, byte shade) {
        var qr = QR.Encode("SVG-CSS-GREY");
        var svg = XElement.Parse(SvgQrRenderer.Render(qr.Modules, new QrSvgRenderOptions {
            ModuleSize = Module, ModuleShape = QrModuleShape.Circle, DarkColor = css
        }));
        var expected = Read(qr.Modules, new QrPngRenderOptions {
            ModuleSize = Module, ModuleShape = QrModuleShape.Circle, Foreground = new Rgba32(shade, shade, shade)
        });
        AssertVectorPixels(svg, qr.Size, expected);
    }

    [Fact]
    public void SvgSolidRegions_CoalesceVertically_WithinACompactArtifactBudget() {
        var modules = new BitMatrix(25, 25);
        for (var y = 0; y < 25; y++) for (var x = 0; x < 25; x++) modules[x, y] = true;
        var svg = SvgQrRenderer.Render(modules, new QrSvgRenderOptions { ModuleSize = 32, Eyes = new QrEyeOptions() });
        Assert.InRange(svg.Length, 1, 4096);
        var expected = Read(modules, new QrPngRenderOptions { ModuleSize = 32, Eyes = new QrEyeOptions() });
        AssertVectorPixels(XElement.Parse(svg), 25, expected);
    }

    [Fact]
    public void SvgContourText_RejectsOutputBeyondItsBound_AfterTheRasterFits() {
        var modules = new BitMatrix(25, 25);
        for (var y = 0; y < 25; y++) for (var x = 0; x < 25; x++) modules[x, y] = (x + y) % 2 == 0;
        var originalLimit = RenderGuards.MaxOutputBytes;
        try {
            RenderGuards.MaxOutputBytes = 8192;
            // A 33x33 RGBA raster fits, while the checkerboard's contour text exceeds its
            // separately bounded representation. The exception must come from SVG expansion.
            var raster = QrPngRenderer.RenderPixels(modules, new QrPngRenderOptions {
                ModuleSize = 1, ModuleShape = QrModuleShape.Rounded
            }, out _, out _, out _);
            Assert.InRange(raster.Length, 1, 8192);
            var error = Assert.Throws<ArgumentException>(() => SvgQrRenderer.Render(modules,
                new QrSvgRenderOptions { ModuleSize = 1, ModuleShape = QrModuleShape.Rounded }));
            Assert.Contains("SVG", error.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("limit", error.Message, StringComparison.OrdinalIgnoreCase);
        } finally { RenderGuards.MaxOutputBytes = originalLimit; }
    }

    [Theory]
    [InlineData("NeonSparkle")]
    [InlineData("Sunburst")]
    [InlineData("MinimalRing")]
    [InlineData("RetroTarget")]
    public void NamedEyePresets_PreserveAccentAndFinderPixelsInSvg(string name) {
        var eyes = name switch {
            "NeonSparkle" => QrEyePresets.NeonSparkle(),
            "Sunburst" => QrEyePresets.Sunburst(),
            "MinimalRing" => QrEyePresets.MinimalRing(),
            "RetroTarget" => QrEyePresets.RetroTarget(),
            _ => throw new System.ArgumentOutOfRangeException(nameof(name))
        };
        eyes.SparkleSeed = eyes.AccentRingSeed = eyes.AccentRaySeed = eyes.AccentStripeSeed = 42;
        var qr = QR.Encode("PRESET-EXPORT-PARITY");
        var options = new QrPngRenderOptions { ModuleSize = 8, ModuleShape = QrModuleShape.Rounded, Eyes = eyes,
            Canvas = new QrCanvasOptions { PaddingPx = 32, Background = new Rgba32(40, 45, 55) } };
        var expected = Read(qr.Modules, options);
        var svg = XElement.Parse(SvgQrRenderer.Render(qr.Modules, new QrSvgRenderOptions { Appearance = options }));
        Assert.DoesNotContain(svg.Descendants(), element => element.Name.LocalName == "image");
        var canvasModules = 2 * options.Canvas!.PaddingPx / options.ModuleSize;
        AssertVectorPixels(svg, qr.Size + canvasModules, expected);
    }
    [Theory]
    [InlineData(148102321, 4)]
    [InlineData(int.MaxValue, int.MaxValue)]
    public void OversizedQrDimensions_RejectBeforeLayoutOrPixelAllocation(int moduleSize, int quietZone) {
        var modules = QR.Encode("X").Modules;
        var options = new QrPngRenderOptions { ModuleSize = moduleSize, QuietZone = quietZone };
        var error = Assert.Throws<System.ArgumentOutOfRangeException>(() =>
            QrPngRenderer.GetScanlineLength(modules, options, out _, out _, out _));
        Assert.Equal(nameof(QrPngRenderOptions.ModuleSize), error.ParamName);
        error = Assert.Throws<System.ArgumentOutOfRangeException>(() =>
            QrPngRenderer.RenderPixels(modules, options, out _, out _, out _));
        Assert.Equal(nameof(QrPngRenderOptions.ModuleSize), error.ParamName);
    }
    private sealed record PaintImage(byte[] Pixels, int Width, int Height, int ModuleSize, int Quiet) {
        internal Rgba32 Sample(int mx, int my, int dx, int dy) {
            var p = (((my + Quiet) * ModuleSize + dy) * Width + (mx + Quiet) * ModuleSize + dx) * 4;
            return new Rgba32(Pixels[p], Pixels[p + 1], Pixels[p + 2], Pixels[p + 3]);
        }
    }

    private static PaintImage Read(BitMatrix modules, QrPngRenderOptions options) {
        var decoded = PngTestDecoder.DecodeRgba32(QrPngRenderer.Render(modules, options));
        return new PaintImage(decoded.rgba, decoded.width, decoded.height, options.ModuleSize, options.QuietZone);
    }

    private static void AssertVectorPixels(XElement root, int size, PaintImage expected) {
        var sample = RenderingContractRegressionTests.ReadVectorCoverage(root, size);
        var actual = new byte[expected.Pixels.Length];
        for (var y = 0; y < expected.Height; y++) for (var x = 0; x < expected.Width; x++) {
            var color = sample((x + 0.5) / expected.ModuleSize, (y + 0.5) / expected.ModuleSize);
            var p = (y * expected.Width + x) * 4;
            actual[p] = color.R; actual[p + 1] = color.G; actual[p + 2] = color.B; actual[p + 3] = color.A;
        }
        Assert.Equal(expected.Pixels, actual);
    }
}
