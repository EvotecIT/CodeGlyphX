using System.Linq;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("ImageScannerSerial")]
public sealed class ProfilePresentationTests {
    [Fact]
    public void MatrixCaption_PreservesTheSymbolAndQuietZoneWithoutMutatingCallerOptions() {
        var symbol = DataMatrixCode.Encode("HELLO");
        var original = symbol.Modules.Clone();
        var options = new MatrixOptions { ModuleSize = 4, QuietZone = 4, LabelText = "CUSTOMER BATCH 123456789", LabelFontSize = 12, LabelMargin = 8 };
        var output = symbol.Render(OutputFormat.Png, options).ToArray();
        var pixels = ImageReader.DecodeRgba32(output, out var width, out var height);
        var left = (width / 4 - symbol.Modules.Width) / 2;
        for (var y = 0; y < symbol.Modules.Height; y++) for (var x = 0; x < symbol.Modules.Width; x++) {
            Assert.Equal(original[x, y], symbol.Modules[x, y]);
            var pixel = ((y + 4) * 4 * width + (left + x) * 4) * 4;
            Assert.Equal(original[x, y] ? (byte)0 : (byte)255, pixels[pixel]);
        }
        var quietStart = (symbol.Modules.Height + 4) * 4;
        for (var y = quietStart; y < quietStart + 4 * 4; y++)
            Assert.All(Enumerable.Range(0, width), x => Assert.Equal(255, pixels[(y * width + x) * 4]));
        var scan = SymbolScanner.Scan(output, new ScanOptions { Formats = new[] { SymbolFormat.DataMatrix }, EnableTileScan = false });
        Assert.Equal("HELLO", Assert.Single(scan.Symbols).Text);
        Assert.Equal(4, options.QuietZone);
        Assert.Equal("CUSTOMER BATCH 123456789", options.LabelText);
        Assert.True(height > (symbol.Modules.Height + 8) * 4);
    }

    [Fact]
    public void CouponAndPackageCaptions_KeepRoutingAndApplicationFieldsDecodable() {
        const string coupon = "(8110)012345612345611110123";
        var options = new MatrixOptions { ModuleSize = 4, QuietZone = 4 };
        var rendered = NorthAmericanCoupon.Render(coupon, OutputFormat.Png, options: options).ToArray();
        var scan = SymbolScanner.Scan(rendered, new ScanOptions { Formats = new[] { SymbolFormat.Gs1DataBarExpandedStacked }, EnableTileScan = false });
        Assert.Equal("8110012345612345611110123", Assert.Single(scan.Symbols).Text);
        Assert.Null(options.LabelText);
        const string pic = "9101123456789000000013";
        var packageOptions = new BarcodeOptions();
        var package = IntelligentMailPackage.Render(pic, OutputFormat.Png, "12345", packageOptions).ToArray();
        scan = SymbolScanner.Scan(package, new ScanOptions { Formats = new[] { SymbolFormat.Gs1Code128 }, EnableTileScan = false });
        Assert.True(IntelligentMailPackage.TryParse(Assert.Single(scan.Symbols).Text, out var decoded));
        Assert.Equal(pic, decoded.TrackingNumber);
        Assert.Equal("12345", decoded.RoutingZip);
        var svg = IntelligentMailPackage.Render(pic, OutputFormat.Svg, "12345").GetText();
        Assert.Contains(decoded.HumanReadable, svg);
        Assert.Null(packageOptions.LabelText);
    }

    [Fact]
    public void IdentifierCaptions_IncludeTheirProfileAndRespectAnExplicitLabel() {
        Assert.Equal("ISBN 9781565812314", BarcodeProfiles.HumanReadable(LinearBarcodeProfile.Isbn, "9781565812314"));
        Assert.Equal("M 0642118 S", BarcodeProfiles.HumanReadable(LinearBarcodeProfile.MarksAndSpencer, "0642118"));
        var options = new BarcodeOptions { LabelText = "Stock item" };
        var svg = BarcodeProfiles.Render(LinearBarcodeProfile.MarksAndSpencer, "0642118", OutputFormat.Svg, options).GetText();
        Assert.Contains("Stock item", svg);
        Assert.DoesNotContain("M 0642118 S", svg);
        Assert.Equal("Stock item", options.LabelText);
    }
}
