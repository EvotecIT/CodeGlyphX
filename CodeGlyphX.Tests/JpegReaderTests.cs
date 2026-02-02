using System;
using System.IO;
using CodeGlyphX.Rendering.Jpeg;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Tests.TestHelpers;
using Xunit;

namespace CodeGlyphX.Tests;

public class JpegReaderTests {
    [Fact]
    public void DecodeJpeg_FromMatrixRenderer() {
        var matrix = new BitMatrix(2, 2);
        matrix[0, 0] = true;
        matrix[1, 1] = true;

        var options = new MatrixPngRenderOptions {
            ModuleSize = 2,
            QuietZone = 1
        };

        var jpeg = MatrixJpegRenderer.Render(matrix, options, 90);
        var rgba = JpegReader.DecodeRgba32(jpeg, out var width, out var height);

        Assert.Equal(8, width);
        Assert.Equal(8, height);
        Assert.Equal(width * height * 4, rgba.Length);
    }

    [Fact]
    public void DecodeJpeg_ProgressiveFixture() {
        var path = JpegTestHelpers.GetFixturePath("progressive.jpg");
        var data = File.ReadAllBytes(path);
        var (expectedWidth, expectedHeight) = JpegTestHelpers.ReadJpegSize(data);

        var options = new JpegDecodeOptions(allowTruncated: true);
        var rgba = JpegReader.DecodeRgba32(data, out var width, out var height, options);

        Assert.Equal(expectedWidth, width);
        Assert.Equal(expectedHeight, height);
        Assert.Equal(width * height * 4, rgba.Length);
    }

    [Fact]
    public void DecodeJpeg_ProgressiveFixture_Sunrise() {
        var path = JpegTestHelpers.GetFixturePath("sunrise-progressive.jpg");
        var data = File.ReadAllBytes(path);
        var (expectedWidth, expectedHeight) = JpegTestHelpers.ReadJpegSize(data);

        var options = new JpegDecodeOptions(allowTruncated: true);
        var rgba = JpegReader.DecodeRgba32(data, out var width, out var height, options);

        Assert.Equal(expectedWidth, width);
        Assert.Equal(expectedHeight, height);
        Assert.Equal(width * height * 4, rgba.Length);
    }


    [Fact]
    public void DecodeJpeg_ExifOrientation() {
        var path = JpegTestHelpers.GetFixturePath("Landscape_6.jpg");
        var data = File.ReadAllBytes(path);
        var (rawWidth, rawHeight) = JpegTestHelpers.ReadJpegSize(data);
        var orientation = JpegTestHelpers.ReadExifOrientation(data);
        var rgba = JpegReader.DecodeRgba32(data, out var width, out var height);

        Assert.Equal(6, orientation);
        Assert.Equal(rawHeight, width);
        Assert.Equal(rawWidth, height);
        Assert.Equal(width * height * 4, rgba.Length);
    }

    [Fact]
    public void DecodeJpeg_ExifOrientation_Transpose() {
        var path = JpegTestHelpers.GetFixturePath("Landscape_5.jpg");
        var data = File.ReadAllBytes(path);
        var (rawWidth, rawHeight) = JpegTestHelpers.ReadJpegSize(data);
        var orientation = JpegTestHelpers.ReadExifOrientation(data);

        var rgba = JpegReader.DecodeRgba32(data, out var width, out var height);

        Assert.Equal(5, orientation);
        Assert.Equal(rawHeight, width);
        Assert.Equal(rawWidth, height);
        Assert.Equal(width * height * 4, rgba.Length);
    }

    [Fact]
    public void DecodeJpeg_ExifOrientation_Transverse() {
        var path = JpegTestHelpers.GetFixturePath("Landscape_7.jpg");
        var data = File.ReadAllBytes(path);
        var (rawWidth, rawHeight) = JpegTestHelpers.ReadJpegSize(data);
        var orientation = JpegTestHelpers.ReadExifOrientation(data);

        var rgba = JpegReader.DecodeRgba32(data, out var width, out var height);

        Assert.Equal(7, orientation);
        Assert.Equal(rawHeight, width);
        Assert.Equal(rawWidth, height);
        Assert.Equal(width * height * 4, rgba.Length);
    }

    [Fact]
    public void DecodeJpeg_ExifOrientation_MirrorHorizontal_Synthetic() {
        JpegTestHelpers.AssertExifOrientationSynthetic(2);
    }

    [Fact]
    public void DecodeJpeg_ExifOrientation_RotateRight_Synthetic() {
        JpegTestHelpers.AssertExifOrientationSynthetic(6);
    }

    [Fact]
    public void DecodeJpeg_ExifOrientation_Rotate180_Synthetic() {
        JpegTestHelpers.AssertExifOrientationSynthetic(3);
    }

    [Fact]
    public void DecodeJpeg_ExifOrientation_MirrorVertical_Synthetic() {
        JpegTestHelpers.AssertExifOrientationSynthetic(4);
    }

    [Fact]
    public void DecodeJpeg_ExifOrientation_Transpose_Synthetic() {
        JpegTestHelpers.AssertExifOrientationSynthetic(5);
    }

    [Fact]
    public void DecodeJpeg_ExifOrientation_Transverse_Synthetic() {
        JpegTestHelpers.AssertExifOrientationSynthetic(7);
    }
}
