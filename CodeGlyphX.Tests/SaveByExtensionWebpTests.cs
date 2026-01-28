using System;
using System.IO;
using CodeGlyphX;
using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class SaveByExtensionWebpTests {
    [Fact]
    public void Qr_Save_ByExtension_WritesWebp() {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.webp");

        try {
            QR.Save("QR-WEBP", path);
            var bytes = File.ReadAllBytes(path);
            Assert.True(WebpReader.IsWebp(bytes));
        } finally {
            if (File.Exists(path)) {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void Barcode_Save_ByExtension_WritesWebp() {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.webp");

        try {
            Barcode.Save(BarcodeType.Code128, "CODE128-WEBP", path);
            var bytes = File.ReadAllBytes(path);
            Assert.True(WebpReader.IsWebp(bytes));
        } finally {
            if (File.Exists(path)) {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void DataMatrix_Save_ByExtension_WritesWebp() {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.webp");

        try {
            DataMatrixCode.Save("DM-WEBP", path);
            var bytes = File.ReadAllBytes(path);
            Assert.True(WebpReader.IsWebp(bytes));
        } finally {
            if (File.Exists(path)) {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void Pdf417_Save_ByExtension_WritesWebp() {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.webp");

        try {
            Pdf417Code.Save("PDF417-WEBP", path);
            var bytes = File.ReadAllBytes(path);
            Assert.True(WebpReader.IsWebp(bytes));
        } finally {
            if (File.Exists(path)) {
                File.Delete(path);
            }
        }
    }
}
