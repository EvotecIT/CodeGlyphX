using System;
using System.Buffers;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class ModuleRunWorkspaceTests {
    [Theory]
    [InlineData(BarcodeType.Code128, "AZ")]
    [InlineData(BarcodeType.Codabar, "12")]
    [InlineData(BarcodeType.Code11, "123456")]
    [InlineData(BarcodeType.Plessey, "1A")]
    [InlineData(BarcodeType.Telepen, "AZ")]
    [InlineData(BarcodeType.ITF, "123456")]
    public void Decode_AlternatingLengthsAndTransforms_PreservesOnlyCurrentRuns(BarcodeType type, string seed) {
        var options = Options();
        var longText = string.Concat(Enumerable.Repeat(seed, 64));
        foreach (var transform in new[] { 0, 1, 2, 3 }) {
            foreach (var text in new[] { longText, seed, longText, seed }) {
                DirtyRunPool();
                var modules = Modules(type, text, transform);
                var snapshot = (bool[])modules.Clone();
                Assert.True(BarcodeDecoder.TryDecode(modules, type, options, out var decoded));
                Assert.Equal(type, decoded.Type);
                Assert.Equal(text, decoded.Text);
                Assert.Equal(snapshot, modules);
                // Default decoding shares runs through earlier rejected formats,
                // including Plessey's separate reversed attempt before Telepen.
                Assert.True(BarcodeDecoder.TryDecode(modules, options, out var inferred));
                Assert.Equal(type, inferred.Type);
                Assert.Equal(text, inferred.Text);
                Assert.False(BarcodeDecoder.TryDecode(Enumerable.Repeat(true, 50).ToArray(), type, options, out _));
            }
        }
    }

    [Fact]
    public void Decode_ConcurrentInputsAndCancelledCalls_KeepIndependentRunStorage() {
        var types = new[] { BarcodeType.Code128, BarcodeType.Codabar, BarcodeType.Code11, BarcodeType.Plessey, BarcodeType.Telepen, BarcodeType.ITF };
        Parallel.For(0, 48, index => {
            var type = types[index % types.Length];
            var text = string.Concat(Enumerable.Repeat("12", 4 + index % 16));
            var modules = Modules(type, text, index % 4);
            var options = Options();
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            Assert.False(BarcodeDecoder.TryDecode(modules, type, options, cancelled.Token, out _));
            Assert.True(BarcodeDecoder.TryDecode(modules, type, options, out var decoded));
            Assert.Equal(type, decoded.Type);
            Assert.Equal(text, decoded.Text);
            Assert.True(BarcodeDecoder.TryDecode(modules, options, out var inferred));
            Assert.Equal(type, inferred.Type);
            Assert.Equal(text, inferred.Text);
        });
    }

    private static BarcodeDecodeOptions Options() => new() {
        Code11Checksum = Code11ChecksumPolicy.RequireValid,
        PlesseyChecksum = PlesseyChecksumPolicy.RequireValid
    };

    private static bool[] Modules(BarcodeType type, string text, int transform) {
        var barcode = BarcodeEncoder.Encode(type, text);
        var modules = new bool[barcode.TotalModules + 24];
        var offset = 12;
        foreach (var segment in barcode.Segments)
            for (var i = 0; i < segment.Modules; i++) modules[offset++] = segment.IsBar;
        if ((transform & 1) != 0) Array.Reverse(modules);
        if ((transform & 2) != 0)
            for (var i = 0; i < modules.Length; i++) modules[i] = !modules[i];
        return modules;
    }

    private static void DirtyRunPool() {
        for (var length = 16; length <= 4096; length *= 2) {
            var storage = ArrayPool<int>.Shared.Rent(length);
            for (var i = 0; i < storage.Length; i++) storage[i] = int.MaxValue;
            ArrayPool<int>.Shared.Return(storage);
        }
    }
}
