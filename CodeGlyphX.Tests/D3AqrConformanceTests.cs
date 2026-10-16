using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using CodeGlyphX.D3Aqr;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class D3AqrConformanceTests {
    [Fact]
    public void TokenAwareModulePath_RejectsCanceledRequestsAndLeavesGridReusable() {
        var symbol = D3AqrEncoder.EncodeText("D3-CANCELLATION", 42, 2.5);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.False(D3AqrDecoder.TryDecode(symbol.Modules, cancellation.Token, out _));
        Assert.True(D3AqrDecoder.TryDecode(symbol.Modules, CancellationToken.None, out var decoded));
        Assert.Equal("D3-CANCELLATION", decoded.Text);
        Assert.Equal(42, decoded.Category);
    }

    [Theory]
    [InlineData(2.0000000000000004)]
    [InlineData(2.4999999999999996)]
    [InlineData(3.0000000000000004)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void PitchIsADiscreteChoice_RejectsNearbyAndNonFiniteRatios(double ratio) {
        Assert.Throws<ArgumentOutOfRangeException>(() => D3AqrEncoder.EncodeText("HELLO", 1, ratio));
    }

    [Fact]
    public void DraftCategoryFrameAndQrHost_ConformToIndependentBwipp() {
        using var stream = typeof(D3AqrConformanceTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Conformance.bwipp-current.json");
        using var document = JsonDocument.Parse(stream!);
        var count = 0;
        foreach (var fixture in document.RootElement.GetProperty("fixtures").EnumerateArray()) {
            if (fixture.GetProperty("bcid").GetString() != "d3aqr") continue;
            var value = fixture.GetProperty("text").GetString()!;
            var category = fixture.GetProperty("category").GetInt32();
            var ratio = fixture.GetProperty("ratio").GetDouble();
            var version = fixture.GetProperty("version").GetInt32();
            var bits = fixture.GetProperty("modules").GetString()!;
            var size = fixture.GetProperty("width").GetInt32();
            var external = new BitMatrix(size, size);
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++) external[x, y] = bits[y * size + x] == '1';
            Assert.True(D3AqrDecoder.TryDecode(external, out var decoded), $"{version}/{ratio}/{category}");
            Assert.Equal(value, decoded.Text);
            Assert.Equal(category, decoded.Category);
            Assert.Equal(ratio, decoded.HostRatio);
            Assert.Equal(version, decoded.Host.Version);
            Assert.Equal(QrErrorCorrectionLevel.M, decoded.Host.ErrorCorrectionLevel);
            Assert.Equal(3, decoded.Host.Mask);
            var encoded = D3AqrEncoder.EncodeText(value, category, ratio, new QrEncodingOptions {
                MinVersion = version, MaxVersion = version, ForceMask = 3,
                TextEncoding = QrTextEncoding.Latin1, EciMode = QrEciMode.Never
            });
            Assert.Equal(size, encoded.Width);
            var actual = string.Concat(Enumerable.Range(0, size).SelectMany(y => Enumerable.Range(0, size).Select(x => encoded.Modules[x, y] ? '1' : '0')));
            Assert.True(bits == actual, $"Encoded {version}/{ratio}/{category}; first difference {Enumerable.Range(0, bits.Length).FirstOrDefault(i => bits[i] != actual[i])}");
            var detected = SymbolDecoder.Decode(external, SymbolFormat.D3Aqr);
            Assert.Equal(category, Assert.IsType<D3AqrSymbolMetadata>(detected.Metadata).Category);
            count++;
        }
        Assert.Equal(36, count);
    }

    [Fact]
    public void FrameParityAndPitchDamage_AreRejectedWhileQrMetadataSurvivesOrientation() {
        var symbol = D3AqrEncoder.EncodeText("0109521234543213", 2842, 2.5, new QrEncodingOptions {
            Fnc1Mode = QrFnc1Mode.FirstPosition, MinVersion = 3, MaxVersion = 3
        });
        var rotated = CodeGlyphX.Internal.BitMatrixTransforms.Rotate90(symbol.Modules);
        Assert.True(D3AqrDecoder.TryDecode(rotated, out var decoded));
        Assert.Equal(QrFnc1Mode.FirstPosition, decoded.Host.Fnc1Mode);
        var damaged = symbol.Modules.Clone();
        damaged[damaged.Width - 1 - 2 * 5, damaged.Height - 1 - 33 * 5] = !damaged[damaged.Width - 1 - 2 * 5, damaged.Height - 1 - 33 * 5];
        Assert.False(D3AqrDecoder.TryDecode(damaged, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => D3AqrEncoder.EncodeText("HELLO", 16384));
        Assert.Throws<ArgumentOutOfRangeException>(() => D3AqrEncoder.EncodeText("HELLO", 1, 2.1));
    }
}
