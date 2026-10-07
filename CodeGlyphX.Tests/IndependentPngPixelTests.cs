using System;
using System.IO;
using System.Security.Cryptography;
using CodeGlyphX.Rendering;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class IndependentPngPixelTests {
    // Reference pixels were decoded by Pillow 12.3.0, independently of CodeGlyphX.
    // Fixtures and permission notice are documented in Fixtures/ImageSamples/README.md.
    [Theory]
    [InlineData("basn0g01", "661985e83f94a569510ded43e65edb11f4ced1121c611209f7abe9a9c40c71a8")]
    [InlineData("basn0g02", "166bd68377b119b5e93e73ef554e35de7471bdd2fc3bc2070f0f7bd5be82ae97")]
    [InlineData("basn0g04", "b05a4bc8e7079c8aa0e491086ccb156dd4bdbc67e57bb8c9d803d7e75778da9e")]
    [InlineData("basn2c16", "7c4b73e829f02793549b4480e25f0c0b332abcb24ac059dbad855fd1d726c17a")]
    [InlineData("basi2c16", "7c4b73e829f02793549b4480e25f0c0b332abcb24ac059dbad855fd1d726c17a")]
    [InlineData("basn3p08", "b1c3302eceae6738c36edafa98c8054824d9440f3ba53a3f17cc81d29acc32cc")]
    [InlineData("basi3p08", "b1c3302eceae6738c36edafa98c8054824d9440f3ba53a3f17cc81d29acc32cc")]
    [InlineData("basn6a16", "f6912d034804dc6b009afea0108cd07b524f79ac84d670f92ce077eec63bead7")]
    [InlineData("basi6a16", "f6912d034804dc6b009afea0108cd07b524f79ac84d670f92ce077eec63bead7")]
    [InlineData("tp1n3p08", "51c649f49f88b17f100c94eca99ecee1168d89201c2456b967a1846a58401654")]
    public void DecodeReconstructsEveryReferencePixel(string name, string expectedHash) {
        var resource = "CodeGlyphX.Tests.Fixtures.ImageSamples.pngsuite-" + name + ".png";
        using var stream = typeof(IndependentPngPixelTests).Assembly.GetManifestResourceStream(resource);
        Assert.NotNull(stream);
        using var input = new MemoryStream();
        stream!.CopyTo(input);

        Assert.True(ImageReader.TryDecodeRgba32(input.ToArray(), out var rgba, out var width, out var height));
        Assert.Equal(32, width);
        Assert.Equal(32, height);
        using var sha = SHA256.Create();
        var actualHash = BitConverter.ToString(sha.ComputeHash(rgba)).Replace("-", "").ToLowerInvariant();
        Assert.Equal(expectedHash, actualHash);
    }
}
