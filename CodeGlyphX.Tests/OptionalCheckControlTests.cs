using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeGlyphX.Printing;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class OptionalCheckControlTests {
    [Theory]
    [InlineData(LinearBarcodeProfile.Isbn, "978-0-306-40615-7", "ISBN 978-0-306-40615")]
    [InlineData(LinearBarcodeProfile.Isbn, "0-306-40615-2", "ISBN 0-306-40615")]
    [InlineData(LinearBarcodeProfile.Isbn, "0-306-40615", "ISBN 0-306-40615")]
    [InlineData(LinearBarcodeProfile.Ismn, "M-2306-7118-7", "ISMN 979-0-2306-7118-7")]
    [InlineData(LinearBarcodeProfile.Ismn, "9790230671187", "ISMN 979-0-2306-7118-7")]
    public void PublishingPrintCaptions_PreserveIsbnGroupingAndCanonicalIsmnElements(LinearBarcodeProfile profile, string value, string expected) {
        var layout = BarcodePrint.Layout(profile, value, new BarcodePrintOptions { Font = BarcodePrintLayoutTests.Font() });
        Assert.Contains(layout.Text, text => text.Text == expected);
        var canonical = profile == LinearBarcodeProfile.Isbn ? "9780306406157" : "9790230671187";
        Assert.Equal((profile == LinearBarcodeProfile.Isbn ? "ISBN " : "ISMN ") + canonical, BarcodeProfiles.HumanReadable(profile, value));
        Assert.Equal(canonical, string.Concat(layout.Text.Where(text => text.Text != expected).Select(text => text.Text)));
    }

    [Theory]
    [InlineData("9790023456786", "ISMN 979-0-023-45678-6")]
    [InlineData("9790423456782", "ISMN 979-0-42345-678-2")]
    [InlineData("9790723456789", "ISMN 979-0-723456-78-9")]
    [InlineData("9790923456787", "ISMN 979-0-9234567-8-7")]
    public void IsmnPrintedElements_FollowEachRegistrantRange(string identifier, string caption) {
        var layout = BarcodePrint.Layout(LinearBarcodeProfile.Ismn, identifier, new BarcodePrintOptions { Font = BarcodePrintLayoutTests.Font() });
        Assert.Contains(layout.Text, text => text.Text == caption);
    }
    [Fact]
    public void CheckedSymbols_MatchIndependentRunPatternsAndChecksWithoutDoubleAppending() {
        using var stream = typeof(OptionalCheckControlTests).Assembly.GetManifestResourceStream("CodeGlyphX.Tests.Fixtures.Conformance.bwipp-optional-checks.txt");
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream!);
        var count = 0;
        while (reader.ReadLine() is { } line) {
            if (line.StartsWith("#") || line.Length == 0) continue;
            var row = line.Split('\t');
            var type = Type(row[0]);
            var options = new BarcodeEncodingOptions { CheckMode = BarcodeCheckMode.Compute, Code39FullAscii = row[0] == "code39ext" };
            var symbol = BarcodeEncoder.Encode(type, row[1], options);
            var runs = row[2].Split(',').Select(int.Parse).ToArray();
            // BWIPP may retain a trailing inter-character space outside the final guard.
            if ((runs.Length & 1) == 0) runs = runs.Take(runs.Length - 1).ToArray();
            Assert.Equal(runs.Length, symbol.Segments.Count);
            if (type == BarcodeType.Bc412) Assert.Equal(runs, symbol.Segments.Select(segment => segment.Modules));
            // Legal wide:narrow ratios vary by implementation; every element's narrow/wide class must agree.
            Assert.Equal(runs.Select((width, i) => width > (type == BarcodeType.Plessey && (i & 1) != 0 ? 2 : 1)), symbol.Segments.Select(s => s.Modules > 1));
            var expectedText = row[0] == "code39ext" ? "*" + row[1] + row[3][row[3].Length - 2] + "*" : row[3];
            Assert.Equal(expectedText, symbol.HumanReadableText);
            var checkedInput = type is BarcodeType.Code39 ? symbol.HumanReadableText!.Trim('*') : symbol.HumanReadableText!;
            options.CheckMode = BarcodeCheckMode.Validate;
            Assert.Equal(Bits(symbol), Bits(BarcodeEncoder.Encode(type, checkedInput, options)));
            var decode = new BarcodeDecodeOptions {
                Code39Checksum = Code39ChecksumPolicy.RequireValid, Code39FullAscii = options.Code39FullAscii,
                Code11Checksum = Code11ChecksumPolicy.RequireValid, CodabarChecksum = OptionalChecksumPolicy.RequireValid,
                TwoOfFiveChecksum = OptionalChecksumPolicy.RequireValid
            };
            Assert.True(BarcodeDecoder.TryDecode(Expand(runs), type, decode, out var decoded), row[0] + ":" + row[1]);
            var data = type == BarcodeType.Codabar ? row[1].Substring(1, row[1].Length - 2) : row[1];
            if (type == BarcodeType.ITF && data.Length % 2 == 0) data = "0" + data;
            Assert.Equal(data, decoded.Text);
            count++;
        }
        Assert.Equal(41, count);
    }

    [Theory]
    [InlineData(BarcodeType.Code39, "ABC")]
    [InlineData(BarcodeType.Code11, "123456789")]
    [InlineData(BarcodeType.Code11, "1234567890")]
    [InlineData(BarcodeType.Codabar, "12345")]
    [InlineData(BarcodeType.ITF, "12345")]
    [InlineData(BarcodeType.Industrial2of5, "12345")]
    [InlineData(BarcodeType.MSI, "12345")]
    [InlineData(BarcodeType.Plessey, "A1B2")]
    public void CheckVisibility_ChangesPrintedTextOnlyAndHonorsCallerOverrides(BarcodeType type, string value) {
        var shown = BarcodeEncoder.Encode(type, value, new BarcodeEncodingOptions { CheckMode = BarcodeCheckMode.Compute });
        var hidden = BarcodeEncoder.Encode(type, value, new BarcodeEncodingOptions { CheckMode = BarcodeCheckMode.Compute, PrintCheckCharacters = false });
        Assert.Equal(Bits(shown), Bits(hidden));
        Assert.NotEqual(shown.HumanReadableText, hidden.HumanReadableText);
        var options = new BarcodePrintOptions { Font = BarcodePrintLayoutTests.Font() };
        Assert.Equal(hidden.HumanReadableText, Assert.Single(BarcodePrint.Layout(hidden, options).Text).Text);
        Assert.Null(options.HumanReadableText);
        options.HumanReadableText = "CUSTOM";
        Assert.Equal("CUSTOM", Assert.Single(BarcodePrint.Layout(shown, options).Text).Text);
        options.IncludeHumanReadable = false;
        Assert.Empty(BarcodePrint.Layout(shown, options).Text);
    }

    [Fact]
    public void Builder_CopiesEncodingOptionsAndUsesThemForPrintAndPixelOutputs() {
        var options = new BarcodeEncodingOptions { CheckMode = BarcodeCheckMode.None, Code39FullAscii = true };
        var builder = Barcode.Create(SymbolFormat.Code39, "abc").WithEncodingOptions(options);
        options.Code39FullAscii = false;
        options.CheckMode = BarcodeCheckMode.Compute;
        Assert.Equal("*abc*", builder.Encode().HumanReadableText);
        Assert.Equal("*abc*", Assert.Single(builder.LayoutPrint(new BarcodePrintOptions { Font = BarcodePrintLayoutTests.Font() }).Text).Text);
        Assert.Equal(builder.Encode().Render(OutputFormat.Png).ToArray(), builder.Render(OutputFormat.Png).ToArray());
    }

    [Fact]
    public void SuppliedChecks_RejectIncorrectOrImpossibleInputsAndMsiAmbiguity() {
        foreach (var type in new[] { BarcodeType.Code39, BarcodeType.Code11, BarcodeType.Codabar, BarcodeType.ITF, BarcodeType.Plessey, BarcodeType.MSI })
            Assert.Throws<ArgumentException>(() => BarcodeEncoder.Encode(type, "12345678901", new BarcodeEncodingOptions { CheckMode = BarcodeCheckMode.Validate }));
        Assert.Throws<ArgumentException>(() => BarcodeEncoder.Encode(BarcodeType.Plessey, "12345", new BarcodeEncodingOptions { CheckMode = BarcodeCheckMode.None }));
        var options = new BarcodeEncodingOptions { CheckMode = BarcodeCheckMode.Validate, MsiChecksum = MsiChecksumType.Mod11 };
        Assert.Throws<ArgumentException>(() => BarcodeEncoder.Encode(BarcodeType.MSI, "7110", options));
        options.MsiExpectedDataLength = 2;
        Assert.Equal("7110", BarcodeEncoder.Encode(BarcodeType.MSI, "7110", options).HumanReadableText);
        options.PrintCheckCharacters = false;
        Assert.Equal("71", BarcodeEncoder.Encode(BarcodeType.MSI, "7110", options).HumanReadableText);
        options.MsiExpectedDataLength = 3;
        Assert.Equal("711", BarcodeEncoder.Encode(BarcodeType.MSI, "7110", options).HumanReadableText);
    }

    [Fact]
    public void OptionalPolicies_RetainDataUnlessExplicitlySelectedAndRejectBadChecks() {
        foreach (var type in new[] { BarcodeType.Codabar, BarcodeType.ITF, BarcodeType.Industrial2of5, BarcodeType.Matrix2of5, BarcodeType.IATA2of5, BarcodeType.Coop2of5, BarcodeType.Datalogic2of5 }) {
            var symbol = BarcodeEncoder.Encode(type, "12345", new BarcodeEncodingOptions { CheckMode = BarcodeCheckMode.Compute });
            Assert.True(BarcodeDecoder.TryDecode(Modules(symbol), type, out var raw));
            var required = new BarcodeDecodeOptions { TwoOfFiveChecksum = OptionalChecksumPolicy.RequireValid, CodabarChecksum = OptionalChecksumPolicy.RequireValid };
            Assert.True(BarcodeDecoder.TryDecode(Modules(symbol), type, required, out var data));
            Assert.Equal("12345", data.Text);
            Assert.Equal(6, raw.Text.Length);
            var invalid = BarcodeEncoder.Encode(type, "123456", new BarcodeEncodingOptions { CheckMode = BarcodeCheckMode.None });
            Assert.False(BarcodeDecoder.TryDecode(Modules(invalid), type, required, out _));
            required.TwoOfFiveChecksum = required.CodabarChecksum = OptionalChecksumPolicy.StripIfValid;
            Assert.True(BarcodeDecoder.TryDecode(Modules(invalid), type, required, out var retained));
            Assert.Equal("123456", retained.Text);
        }
    }

    [Fact]
    public void CodabarAliases_NormalizeFramingAndPrintChecksInCanonicalAlphabet() {
        var symbol = BarcodeEncoder.Encode(BarcodeType.Codabar, "T12345N", new BarcodeEncodingOptions { CheckMode = BarcodeCheckMode.Compute });
        Assert.Equal("A123450B", symbol.HumanReadableText);
        Assert.Equal(Bits(symbol), Bits(BarcodeEncoder.Encode(BarcodeType.Codabar, "12345", new BarcodeEncodingOptions { CheckMode = BarcodeCheckMode.Compute })));
        Assert.Equal("C12345D", BarcodeEncoder.Encode(BarcodeType.Codabar, "*12345E", new BarcodeEncodingOptions()).HumanReadableText);
    }

    [Fact]
    public void MandatoryCarrierChecksAndLegacyDefaults_ArePreserved() {
        foreach (var type in new[] { BarcodeType.Code39, BarcodeType.Code11, BarcodeType.Codabar, BarcodeType.MSI, BarcodeType.Plessey, BarcodeType.ITF }) {
            var value = type == BarcodeType.ITF ? "123456" : "12345";
            Assert.Equal(Bits(BarcodeEncoder.Encode(type, value)), Bits(BarcodeEncoder.Encode(type, value, new BarcodeEncodingOptions())));
        }
        Assert.Equal(Bits(BarcodeEncoder.Encode(BarcodeType.Code128, "12345")), Bits(BarcodeEncoder.Encode(BarcodeType.Code128, "12345", new BarcodeEncodingOptions { CheckMode = BarcodeCheckMode.None })));
        Assert.Throws<ArgumentException>(() => BarcodeEncoder.Encode(BarcodeType.Code128, "12345", new BarcodeEncodingOptions { CheckMode = BarcodeCheckMode.Compute }));
        Assert.Throws<ArgumentException>(() => BarcodeEncoder.Encode(BarcodeType.Code11, "12345", new BarcodeEncodingOptions { Code39FullAscii = true }));
        Assert.Throws<ArgumentOutOfRangeException>(() => BarcodeEncoder.Encode(BarcodeType.Code39, "ABC", new BarcodeEncodingOptions { CheckMode = (BarcodeCheckMode)99 }));
    }

    [Theory]
    [InlineData(SymbolFormat.Codabar, 0)]
    [InlineData(SymbolFormat.Codabar, 90)]
    [InlineData(SymbolFormat.Itf, 180)]
    [InlineData(SymbolFormat.Itf, 270)]
    public void Scanner_PreservesExplicitOptionalCheckPolicies(SymbolFormat format, int rotation) {
        var layout = Barcode.Create(format, "12345").WithEncodingOptions(new BarcodeEncodingOptions { CheckMode = BarcodeCheckMode.Compute })
            .LayoutPrint(new BarcodePrintOptions { IncludeHumanReadable = false, XDimensionMm = .508 });
        var scan = SymbolScanner.Scan(layout.Render(OutputFormat.Png, new BarcodePrintPlacement { RotationDegrees = rotation }).ToArray(), new ScanOptions {
            Formats = new[] { format }, TimeoutMilliseconds = 5000,
            Barcode = new BarcodeDecodeOptions { CodabarChecksum = OptionalChecksumPolicy.RequireValid, TwoOfFiveChecksum = OptionalChecksumPolicy.RequireValid }
        });
        Assert.Equal("12345", Assert.Single(scan.Symbols).Text);
    }

    [Theory]
    [InlineData("123456789", "1234567890")]
    [InlineData("1234567890", "123456789019")]
    public void Code11_CheckCountIsSelectedByOriginalDataLength(string data, string expected) {
        var symbol = BarcodeEncoder.Encode(BarcodeType.Code11, data, new BarcodeEncodingOptions());
        Assert.Equal(expected, symbol.HumanReadableText);
        Assert.True(BarcodeDecoder.TryDecode(Modules(symbol), BarcodeType.Code11, new BarcodeDecodeOptions { Code11Checksum = Code11ChecksumPolicy.RequireValid }, out var result));
        Assert.Equal(data, result.Text);
    }

    private static BarcodeType Type(string bcid) => bcid switch {
        "code39" or "code39ext" => BarcodeType.Code39, "code11" => BarcodeType.Code11,
        "rationalizedCodabar" => BarcodeType.Codabar, "plessey" => BarcodeType.Plessey, "bc412" => BarcodeType.Bc412,
        "interleaved2of5" => BarcodeType.ITF, "industrial2of5" => BarcodeType.Industrial2of5,
        "matrix2of5" => BarcodeType.Matrix2of5, "iata2of5" => BarcodeType.IATA2of5,
        "coop2of5" => BarcodeType.Coop2of5, _ => BarcodeType.Datalogic2of5
    };
    private static bool[] Expand(int[] runs) => runs.SelectMany((width, index) => Enumerable.Repeat((index & 1) == 0, width)).ToArray();
    private static bool[] Modules(Barcode1D symbol) => symbol.Segments.SelectMany(s => Enumerable.Repeat(s.IsBar, s.Modules)).ToArray();
    private static string Bits(Barcode1D symbol) => string.Concat(Modules(symbol).Select(b => b ? '1' : '0'));
}
