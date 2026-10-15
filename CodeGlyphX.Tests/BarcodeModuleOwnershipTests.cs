using System;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class BarcodeModuleOwnershipTests {
    [Theory]
    [InlineData(BarcodeType.Code128, "ARRAY-128")]
    [InlineData(BarcodeType.ITF, "123456")]
    [InlineData(BarcodeType.Telepen, "ABC123")]
    public void DecodePreservesCallerModulesAcrossTrimAndTransforms(BarcodeType type, string text) {
        var barcode = BarcodeEncoder.Encode(type, text);
        // Unpadded input reaches the shared-array path; padded input exercises
        // trimming. Reverse and invert force the decoder's transform attempts.
        foreach (var padding in new[] { 0, 12 }) {
            for (var transform = 0; transform < 4; transform++) {
                var modules = new bool[barcode.TotalModules + padding * 2];
                var offset = padding;
                foreach (var segment in barcode.Segments)
                    for (var i = 0; i < segment.Modules; i++) modules[offset++] = segment.IsBar;
                if ((transform & 1) != 0) Array.Reverse(modules);
                if ((transform & 2) != 0)
                    for (var i = 0; i < modules.Length; i++) modules[i] = !modules[i];
                var snapshot = (bool[])modules.Clone();

                Assert.True(BarcodeDecoder.TryDecode(modules, type, out var decoded));
                Assert.Equal(type, decoded.Type);
                Assert.Equal(text, decoded.Text);
                Assert.Equal(snapshot, modules);
                Assert.True(BarcodeDecoder.TryDecode(modules, type, out var repeated));
                Assert.Equal(decoded.Text, repeated.Text);
                Assert.Equal(snapshot, modules);
            }
        }
    }

    [Fact]
    public void RejectedModuleInputsRemainUnchanged() {
        foreach (var modules in new[] { Array.Empty<bool>(), new bool[80], new[] { true }, new[] { true, false, true, false, true } }) {
            var snapshot = (bool[])modules.Clone();
            Assert.False(BarcodeDecoder.TryDecode(modules, BarcodeType.Code128, out _));
            Assert.Equal(snapshot, modules);
        }
    }
}
