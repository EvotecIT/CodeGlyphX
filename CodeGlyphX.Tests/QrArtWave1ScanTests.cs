using System.Globalization;
using System.Text;
using System.Xml.Linq;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;
using Xunit;
using Xunit.Abstractions;
using ZXing;
using ZXing.Common;

namespace CodeGlyphX.Tests;

/// <summary>Interoperability checks on exported artwork, independently of the art heuristic score.</summary>
[Collection("ImageScannerSerial")]
public sealed class QrArtWave1ScanTests {
    private const string Payload = "https://example.com/qr-art/wave1?scan=independent";
    private readonly ITestOutputHelper _output;

    public QrArtWave1ScanTests(ITestOutputHelper output) => _output = output;

    // Coherent eye profiles cross every body. The separate eye matrix covers all 36 ring/ball
    // pairs without multiplying unrelated preset textures and canvas decorations into that matrix.
    private static readonly (QrModuleShape Outer, QrModuleShape Inner)[] EyeProfiles = {
        (QrModuleShape.Square, QrModuleShape.Square),
        (QrModuleShape.Rounded, QrModuleShape.Circle),
        (QrModuleShape.Circle, QrModuleShape.Circle),
        (QrModuleShape.Squircle, QrModuleShape.Squircle),
        (QrModuleShape.Leaf, QrModuleShape.Leaf),
        (QrModuleShape.Rounded, QrModuleShape.Diamond),
        (QrModuleShape.CutCorner, QrModuleShape.Circle),
    };

    public static IEnumerable<object[]> BodyAndEyeCases() {
        foreach (var body in Enum.GetValues<QrModuleShape>().Where(body => !IsDecorativeBody(body))) {
            yield return new object[] { body, -1 }; // Default eyes must also become whole finders.
            for (var eye = 0; eye < EyeProfiles.Length; eye++) yield return new object[] { body, eye };
        }
    }

    public static IEnumerable<object[]> EyePairCases() {
        var rings = new[] { QrModuleShape.Square, QrModuleShape.Rounded, QrModuleShape.Circle,
            QrModuleShape.Squircle, QrModuleShape.Leaf, QrModuleShape.CutCorner };
        var balls = new[] { QrModuleShape.Square, QrModuleShape.Rounded, QrModuleShape.Circle,
            QrModuleShape.Squircle, QrModuleShape.Diamond, QrModuleShape.Leaf };
        foreach (var ring in rings) foreach (var ball in balls) {
            if (ring == QrModuleShape.Square && ball == QrModuleShape.Diamond) continue;
            yield return new object[] { ring, ball };
        }
    }

    public static IEnumerable<object[]> PresetCases() {
        foreach (var theme in Enum.GetValues<QrArtTheme>())
            foreach (var variant in Enum.GetValues<QrArtVariant>()) yield return new object[] { theme, variant };
    }

    public static IEnumerable<object[]> StyleCases() => Enum.GetValues<QrRenderStyle>().Select(s => new object[] { s });
    public static IEnumerable<object[]> FrameCases() => Enum.GetValues<QrEyeFrameStyle>().Select(s => new object[] { s });
    public static IEnumerable<object[]> ShapeCases() => Enum.GetValues<QrModuleShape>().Select(s => new object[] { s });
    public static IEnumerable<object[]> EyePresetCases() => new[] { "NeonSparkle", "Sunburst", "MinimalRing", "RetroTarget" }
        .Select(name => new object[] { name });
    public static IEnumerable<object[]> ArtPaletteCases() => new[] { "Warm", "Cool", "Pastel", "HighContrast", "Neon" }
        .Select(name => new object[] { name });
    public static IEnumerable<object[]> DecorativeBodyCases() {
        foreach (var body in Enum.GetValues<QrModuleShape>().Where(IsDecorativeBody))
            for (var eye = -1; eye < EyeProfiles.Length; eye++) yield return new object[] { body, eye };
    }

    private static bool IsDecorativeBody(QrModuleShape body) => body is QrModuleShape.DotGrid
        or QrModuleShape.Plus or QrModuleShape.Star or QrModuleShape.Heart;

    [Theory]
    [MemberData(nameof(BodyAndEyeCases))]
    [Trait("Category", "CorpusTiming")]
    public void BodyAndEyes_DecodeWithStrictZxingAndLibrary_AfterResizingRotationAndBlur(QrModuleShape body, int eyeProfile) {
        AssertScanMatrix(BodyOptions(body, eyeProfile));
    }

    [Theory]
    [MemberData(nameof(EyePairCases))]
    [Trait("Category", "CorpusTiming")]
    public void EyePairs_DecodeWithStrictZxingAndLibrary_AfterResizingRotationAndBlur(QrModuleShape ring, QrModuleShape ball) {
        var options = BodyOptions(QrModuleShape.Square, -1);
        options.Eyes = new QrEyeOptions { OuterShape = ring, InnerShape = ball };
        AssertScanMatrix(options);
    }

    [Theory]
    [Trait("ScanClassification", "DecorativeVerifyBeforePrint")]
    [MemberData(nameof(DecorativeBodyCases))]
    [Trait("Category", "CorpusTiming")]
    public void DecorativeBodies_ReportObservedScans_AndNeverReturnADifferentPayload(QrModuleShape body, int eyeProfile) {
        ReportDecorativeScans(BodyOptions(body, eyeProfile));
    }

    [Fact]
    [Trait("ScanClassification", "DecorativeVerifyBeforePrint")]
    [Trait("Category", "CorpusTiming")]
    public void DecorativeSquareRingDiamondBall_ReportsObservedScans_AndNeverReturnsADifferentPayload() {
        var options = BodyOptions(QrModuleShape.Square, -1);
        options.Eyes = new QrEyeOptions { OuterShape = QrModuleShape.Square, InnerShape = QrModuleShape.Diamond };
        ReportDecorativeScans(options);
    }

    [Theory]
    [MemberData(nameof(PresetCases))]
    [Trait("Category", "CorpusTiming")]
    public void ArtPresets_DecodeWithStrictZxingAndLibrary_AfterResizingRotationAndBlur(QrArtTheme theme, QrArtVariant variant) {
        AssertScanMatrix(new QrRenderOptions { TargetSizePx = 1024, Art = QrArt.Theme(theme, variant) });
    }

    [Theory]
    [MemberData(nameof(StyleCases))]
    [Trait("Category", "CorpusTiming")]
    public void RenderStyles_DecodeWithStrictZxingAndLibrary_AfterResizingRotationAndBlur(QrRenderStyle style) {
        AssertScanMatrix(new QrRenderOptions { TargetSizePx = 1024, Style = style });
    }

    [Theory]
    [MemberData(nameof(FrameCases))]
    [Trait("Category", "CorpusTiming")]
    public void EyeFrames_DecodeWithStrictZxingAndLibrary_AfterResizingRotationAndBlur(QrEyeFrameStyle frame) {
        var options = BodyOptions(QrModuleShape.Rounded, -1);
        options.Eyes = new QrEyeOptions { UseFrame = true, FrameStyle = frame,
            OuterShape = QrModuleShape.Rounded, InnerShape = QrModuleShape.Circle };
        AssertScanMatrix(options);
    }

    [Theory]
    [MemberData(nameof(EyePresetCases))]
    [Trait("Category", "CorpusTiming")]
    public void EyePresets_DecodeWithStrictZxingAndLibrary_AfterResizingRotationAndBlur(string preset) {
        var options = BodyOptions(QrModuleShape.Rounded, -1);
        options.Eyes = EyePreset(preset);
        AssertScanMatrix(options);
    }

    [Theory]
    [MemberData(nameof(ArtPaletteCases))]
    [Trait("Category", "CorpusTiming")]
    public void ArtPalettes_DecodeWithStrictZxingAndLibrary_AfterResizingRotationAndBlur(string paletteName) {
        AssertScanMatrix(PaletteOptions(paletteName));
    }

    [Theory]
    [MemberData(nameof(ShapeCases))]
    public void BodyChoices_PreserveTheRequestedShapeThroughTheFacade(QrModuleShape body) {
        var qr = QR.Encode(Payload, EncodingOptions());
        var options = BodyOptions(body, 1);
        options.TargetSizePx = 0;
        options.ModuleSize = 12;
        var actual = qr.RenderPixels(out var actualWidth, out var actualHeight, out var actualStride, options);
        var expected = QrPngRenderer.RenderPixels(qr.Modules, new QrPngRenderOptions {
            ModuleSize = 12, ModuleShape = body, Eyes = new QrEyeOptions {
                OuterShape = QrModuleShape.Rounded, InnerShape = QrModuleShape.Circle
            }
        }, out var width, out var height, out var stride);
        Assert.Equal(width, actualWidth);
        Assert.Equal(height, actualHeight);
        Assert.Equal(stride, actualStride);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(1024)]
    [InlineData(512)]
    [InlineData(256)]
    public void UnframedTargetSize_IsExact_AndRetainsAnUnbrokenQuietZone(int size) {
        var options = BodyOptions(QrModuleShape.Circle, 1);
        options.TargetSizePx = size;
        Assert.True(ImageReader.TryDecodeRgba32(RenderPng(options), out var rgba, out var width, out var height));
        Assert.Equal(size, width);
        Assert.Equal(size, height);
        var qr = QR.Encode(Payload, EncodingOptions());
        var quietPixels = 4 * (size / (qr.Size + 8));
        for (var y = 0; y < size; y++) for (var x = 0; x < size; x++) {
            if (x >= quietPixels && y >= quietPixels && x < size - quietPixels && y < size - quietPixels) continue;
            var p = (y * size + x) * 4;
            Assert.Equal(255, rgba[p]);
            Assert.Equal(255, rgba[p + 1]);
            Assert.Equal(255, rgba[p + 2]);
            Assert.Equal(255, rgba[p + 3]);
        }
    }

    [Theory]
    [InlineData(QrModuleShape.Circle)]
    [InlineData(QrModuleShape.Rounded)]
    [InlineData(QrModuleShape.ConnectedRounded)]
    public void TypicalMonochromeArt_ExportsPngWithin60KB_WithAntialiasedCoverage(QrModuleShape body) {
        var png = RenderPng(BodyOptions(body, 1));
        Assert.InRange(png.Length, 1, 60_000);
        Assert.True(ImageReader.TryDecodeRgba32(png, out var rgba, out _, out _));
        var partialCoverage = 0;
        for (var p = 0; p < rgba.Length; p += 4) {
            Assert.Equal(255, rgba[p + 3]);
            Assert.Equal(rgba[p], rgba[p + 1]);
            Assert.Equal(rgba[p], rgba[p + 2]);
            if (rgba[p] > 0 && rgba[p] < 255) partialCoverage++;
        }
        Assert.True(partialCoverage > 100, $"Curved black-on-white artwork needs edge coverage; found {partialCoverage} intermediate pixels.");
        _output.WriteLine($"PNG bytes={png.Length}; antialiased pixels={partialCoverage}");
    }

    [Fact]
    public void ImplicitCircleEyes_RenderAContinuousRingAndBall_WithALightSeparator() {
        var qr = QR.Encode(Payload, EncodingOptions());
        const int module = 20;
        var rgba = QrPngRenderer.RenderPixels(qr.Modules, new QrPngRenderOptions {
            ModuleSize = module, QuietZone = 4, ModuleShape = QrModuleShape.Dot,
            Eyes = new QrEyeOptions { OuterShape = QrModuleShape.Circle, InnerShape = QrModuleShape.Circle }
        }, out var width, out _, out _);
        // Points between individual module circles distinguish a whole finder from a dot grid.
        foreach (var (x, y) in new[] { (2.05, 0.8), (3.5, 0.8), (2.15, 3.5), (3.5, 3.5) })
            Assert.True(Sample(x, y) < 64, $"Expected continuous finder ink at ({x},{y}).");
        Assert.True(Sample(1.5, 3.5) > 223, "The separator between the 7x7 ring and 3x3 ball must stay light.");
        Assert.True(Sample(0.2, 0.2) > 223, "Circular finder corners must remain outside the ring.");

        byte Sample(double x, double y) => rgba[(((int)((4 + y) * module) * width) + (int)((4 + x) * module)) * 4];
    }

    [Fact]
    public void StyledTimingAndAlignment_KeepDarkCenters_AndUseCurvedGeometry() {
        var qr = QR.Encode("WAVE1-TIMING", new QrEncodingOptions {
            ErrorCorrectionLevel = QrErrorCorrectionLevel.H, MinVersion = 5, MaxVersion = 5
        });
        const int module = 20;
        var rgba = QrPngRenderer.RenderPixels(qr.Modules, new QrPngRenderOptions {
            ModuleSize = module, ModuleShape = QrModuleShape.Circle,
            Eyes = new QrEyeOptions { OuterShape = QrModuleShape.Circle, InnerShape = QrModuleShape.Circle }
        }, out var width, out _, out _);
        // Version 5 has one alignment pattern centered at (30,30). Timing dark cells are
        // (10,6)/(6,10); checking both orientations catches one-sided protection mistakes.
        foreach (var (x, y) in new[] { (10.0, 6.0), (6.0, 10.0) }) {
            Assert.True(Sample(x + 0.5, y + 0.5) < 32, "Timing module center must remain dark.");
            Assert.True(Sample(x + 0.05, y + 0.05) > 223, "Timing corners must follow the circular body.");
        }
        Assert.True(Sample(30.5, 30.5) < 32, "The whole 5x5 alignment keeps its central ball.");
        Assert.True(Sample(29.5, 30.5) > 223, "The alignment separator must stay light.");
        Assert.True(Sample(30.5, 28.5) < 32, "The alignment outer ring must remain dark.");
        Assert.True(Sample(28.05, 28.05) > 223, "The alignment ring must use curved whole-eye geometry.");

        byte Sample(double x, double y) => rgba[(((int)((4 + y) * module) * width) + (int)((4 + x) * module)) * 4];
    }

    [Theory]
    [MemberData(nameof(ShapeCases))]
    public void SvgBodies_ExportRealVectorArtwork_WithResolvedPaintReferences(QrModuleShape body) {
        var options = BodyOptions(body, 1);
        options.TargetSizePx = 256;
        options.ForegroundGradient = new QrGradientOptions {
            StartColor = new Rgba32(0, 0, 0), EndColor = new Rgba32(40, 50, 60)
        };
        var root = ParseSvg(options);
        Assert.Contains(root.Descendants(), e => e.Name.LocalName is "path" or "circle" or "rect" or "polygon" or "ellipse");
        // The canonical appearance exporter may preserve a gradient through colored contours
        // rather than an SVG gradient element. Either must retain more than a flat ink color.
        var paintedColors = root.DescendantsAndSelf().Attributes("fill").Select(a => a.Value).Distinct().ToArray();
        Assert.True(paintedColors.Length > 2 || root.Descendants().Any(e => e.Name.LocalName is "linearGradient" or "radialGradient"),
            "The exported vector artwork must preserve its non-flat foreground gradient.");
        Assert.DoesNotContain(root.Descendants(), e => e.Name.LocalName is "image" or "foreignObject");
        AssertResolvedPaintReferences(root);
        if (body != QrModuleShape.Square) {
            options.ModuleShape = QrModuleShape.Square;
            var square = ParseSvg(options);
            Assert.NotEqual(Geometry(square), Geometry(root));
        }
    }

    [Fact]
    public void SvgCanvas_ChangesTheExportedBounds_AndPreservesTheVectorCode() {
        var options = BodyOptions(QrModuleShape.ConnectedRounded, 1);
        options.TargetSizePx = 256;
        var plain = ParseSvg(options);
        options.Canvas = new QrCanvasOptions { PaddingPx = 40, Background = new Rgba32(230, 210, 190), CornerRadiusPx = 24 };
        var canvas = ParseSvg(options);
        Assert.True(Extent(canvas, "width") > Extent(plain, "width"));
        Assert.True(Extent(canvas, "height") > Extent(plain, "height"));
        Assert.Contains(canvas.Descendants(), e => e.Name.LocalName == "path");
        AssertResolvedPaintReferences(canvas);
    }

    private void AssertScanMatrix(QrRenderOptions options) {
        var results = ScanMatrix(options).ToArray();
        foreach (var result in results) _output.WriteLine(result.ToString());
        Assert.Equal(9, results.Length);
        var failures = results.Where(r => !r.StrictPassed || !r.LibraryPassed).ToArray();
        Assert.True(failures.Length == 0, string.Join(Environment.NewLine, failures.Select(r => r.ToString())));
    }

    private void ReportDecorativeScans(QrRenderOptions options) {
        options.ArtGuardrailsEnabled = false; // Observe unqualified artwork without the runtime gate rejecting it.
        var results = ScanMatrix(options).ToArray();
        Assert.Equal(9, results.Length);
        _output.WriteLine("DECORATIVE: verify before print. These observations are not scan-safety acceptance passes.");
        foreach (var result in results) {
            _output.WriteLine(result.ToString());
            if (result.StrictText is not null) Assert.Equal(Payload, result.StrictText);
            if (result.LibraryText is not null) Assert.Equal(Payload, result.LibraryText);
        }
    }

    internal sealed record ScanObservation(int Size, string Transform, string? StrictText, string? LibraryText, string Expected = Payload) {
        internal bool StrictPassed => StrictText == Expected;
        internal bool LibraryPassed => LibraryText == Expected;
        public override string ToString() => $"{Size}/{Transform}: ZXing={StrictPassed}; library={LibraryPassed}";
    }

    // Shared only with the task-owned diagnostic runner. No decoder result is inferred from a
    // heuristic score, and no failing style is silently counted as a successful scan.
    internal static IEnumerable<ScanObservation> ScanMatrix(QrRenderOptions options) {
        return ScanImage(RenderPng(options), Payload);
    }

    internal static IEnumerable<ScanObservation> ScanImage(byte[] png, string payload) {
        Assert.True(ImageReader.TryDecodeRgba32(png, out var rgba, out var width, out var height));
        foreach (var size in new[] { 1024, 512, 256 }) {
            var targetHeight = (int)Math.Round(size * height / (double)width);
            var resized = ResizeArea(rgba, width, height, size, targetHeight);
            foreach (var transform in new[] { "plain", "rotate2deg", "blur121" }) {
                var pixels = transform == "rotate2deg" ? Rotate2Degrees(resized, size, targetHeight)
                    : transform == "blur121" ? Blur(resized, size, targetHeight) : resized;
                var reader = new BarcodeReaderGeneric {
                    AutoRotate = false,
                    Options = new DecodingOptions { TryHarder = false, PureBarcode = false, TryInverted = false,
                        PossibleFormats = new[] { BarcodeFormat.QR_CODE } }
                };
                var strict = reader.Decode(new RGBLuminanceSource(pixels, size, targetHeight, RGBLuminanceSource.BitmapFormat.RGBA32));
                var decoded = QrDecoder.TryDecode(pixels, size, targetHeight, size * 4, PixelFormat.Rgba32,
                    out var library);
                yield return new ScanObservation(size, transform, strict?.Text, decoded ? library.Text : null, payload);
            }
        }
    }

    internal static QrRenderOptions BodyOptions(QrModuleShape body, int eyeProfile) => new() {
        TargetSizePx = 1024, QuietZone = 4, ModuleShape = body, ModuleScale = 1,
        ArtGuardrailsEnabled = !IsDecorativeBody(body),
        Eyes = eyeProfile < 0 ? null : new QrEyeOptions {
            OuterShape = EyeProfiles[eyeProfile].Outer, InnerShape = EyeProfiles[eyeProfile].Inner
        }
    };

    internal static QrEyeOptions EyePreset(string name) => name switch {
        "NeonSparkle" => QrEyePresets.NeonSparkle(),
        "Sunburst" => QrEyePresets.Sunburst(),
        "MinimalRing" => QrEyePresets.MinimalRing(),
        "RetroTarget" => QrEyePresets.RetroTarget(),
        _ => throw new ArgumentOutOfRangeException(nameof(name))
    };

    internal static QrRenderOptions PaletteOptions(string name) {
        var palette = name switch {
            "Warm" => QrArtPalettes.Warm(), "Cool" => QrArtPalettes.Cool(),
            "Pastel" => QrArtPalettes.Pastel(), "HighContrast" => QrArtPalettes.HighContrast(),
            "Neon" => QrArtPalettes.Neon(), _ => throw new ArgumentOutOfRangeException(nameof(name))
        };
        var options = BodyOptions(QrModuleShape.ConnectedRounded, -1);
        options.Foreground = palette.Foreground;
        options.Background = palette.Background;
        options.ForegroundPalette = palette.Palette;
        return options;
    }

    private static QrEncodingOptions EncodingOptions() => new() { ErrorCorrectionLevel = QrErrorCorrectionLevel.H };
    private static byte[] RenderPng(QrRenderOptions options) => QR.Render(Payload, OutputFormat.Png, options, EncodingOptions()).Data.ToArray();
    private static XElement ParseSvg(QrRenderOptions options) => XElement.Parse(Encoding.UTF8.GetString(
        QR.Render(Payload, OutputFormat.Svg, options, EncodingOptions()).Data.Span));

    private static double Extent(XElement root, string name) => double.Parse(
        ((string?)root.Attribute(name) ?? throw new Xunit.Sdk.XunitException($"Missing SVG {name}.")).Replace("px", ""), CultureInfo.InvariantCulture);

    private static string Geometry(XElement root) => string.Join(";", root.Descendants()
        .Where(e => e.Name.LocalName is "path" or "circle" or "rect" or "polygon" or "ellipse")
        .Select(e => string.Join(" ", e.Attributes().Where(a => a.Name.LocalName is not ("fill" or "id"))
            .Select(a => a.Name.LocalName + "=" + a.Value))));

    private static void AssertResolvedPaintReferences(XElement root) {
        var ids = root.DescendantsAndSelf().Attributes("id").Select(a => a.Value).ToArray();
        Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
        foreach (var value in root.DescendantsAndSelf().Attributes().Select(a => a.Value).Where(v => v.StartsWith("url(#", StringComparison.Ordinal))) {
            Assert.EndsWith(")", value);
            Assert.Contains(value[5..^1], ids);
        }
    }

    // Independent pixel transforms intentionally do not call the production scaler or filters.
    private static byte[] ResizeArea(byte[] source, int width, int height, int size, int targetHeight) {
        var target = new byte[size * targetHeight * 4];
        var scaleX = width / (double)size;
        var scaleY = height / (double)targetHeight;
        for (var y = 0; y < targetHeight; y++) for (var x = 0; x < size; x++) {
            var x0 = x * scaleX; var x1 = (x + 1) * scaleX;
            var y0 = y * scaleY; var y1 = (y + 1) * scaleY;
            for (var channel = 0; channel < 3; channel++) {
                var sum = 0.0;
                for (var sy = (int)y0; sy < Math.Ceiling(y1) && sy < height; sy++)
                    for (var sx = (int)x0; sx < Math.Ceiling(x1) && sx < width; sx++) {
                        var weight = (Math.Min(x1, sx + 1) - Math.Max(x0, sx)) * (Math.Min(y1, sy + 1) - Math.Max(y0, sy));
                        var p = (sy * width + sx) * 4;
                        var alpha = source[p + 3] / 255.0;
                        sum += (source[p + channel] * alpha + 255 * (1 - alpha)) * weight;
                    }
                target[(y * size + x) * 4 + channel] = (byte)Math.Clamp((int)Math.Round(sum / (scaleX * scaleY)), 0, 255);
            }
            target[(y * size + x) * 4 + 3] = 255;
        }
        return target;
    }

    private static byte[] Rotate2Degrees(byte[] source, int size, int targetHeight) {
        var target = new byte[source.Length];
        var radians = Math.PI / 90;
        var cos = Math.Cos(radians); var sin = Math.Sin(radians); var center = (size - 1) / 2.0; var centerY = (targetHeight - 1) / 2.0;
        for (var y = 0; y < targetHeight; y++) for (var x = 0; x < size; x++) {
            var sx = cos * (x - center) + sin * (y - centerY) + center;
            var sy = -sin * (x - center) + cos * (y - centerY) + centerY;
            var ix = (int)Math.Floor(sx); var iy = (int)Math.Floor(sy);
            var fx = sx - ix; var fy = sy - iy;
            for (var c = 0; c < 3; c++) {
                var value = Sample(ix, iy, c) * (1 - fx) * (1 - fy) + Sample(ix + 1, iy, c) * fx * (1 - fy)
                    + Sample(ix, iy + 1, c) * (1 - fx) * fy + Sample(ix + 1, iy + 1, c) * fx * fy;
                target[(y * size + x) * 4 + c] = (byte)Math.Clamp((int)Math.Round(value), 0, 255);
            }
            target[(y * size + x) * 4 + 3] = 255;
        }
        return target;
        byte Sample(int x, int y, int channel) => x < 0 || y < 0 || x >= size || y >= targetHeight ? (byte)255 : source[(y * size + x) * 4 + channel];
    }

    private static byte[] Blur(byte[] source, int size, int targetHeight) {
        var target = new byte[source.Length];
        var weights = new[] { 1, 2, 1 };
        for (var y = 0; y < targetHeight; y++) for (var x = 0; x < size; x++) {
            for (var c = 0; c < 3; c++) {
                var sum = 0;
                for (var yy = -1; yy <= 1; yy++) for (var xx = -1; xx <= 1; xx++)
                    sum += source[(Math.Clamp(y + yy, 0, targetHeight - 1) * size + Math.Clamp(x + xx, 0, size - 1)) * 4 + c] * weights[yy + 1] * weights[xx + 1];
                target[(y * size + x) * 4 + c] = (byte)((sum + 8) / 16);
            }
            target[(y * size + x) * 4 + 3] = 255;
        }
        return target;
    }
}
