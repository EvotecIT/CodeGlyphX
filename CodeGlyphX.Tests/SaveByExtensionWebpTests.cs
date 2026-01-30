using System;
using System.IO;
using CodeGlyphX;
using CodeGlyphX.Rendering.Webp;
using Xunit;

namespace CodeGlyphX.Tests;

[Collection("WebpTests")]

public sealed class SaveByExtensionWebpTests {
    [Fact]
    public void Qr_Save_ByExtension_WritesWebp() {
        AssertWebpSaved(path => QR.Save("QR-WEBP", path));
    }

    [Fact]
    public void Barcode_Save_ByExtension_WritesWebp() {
        AssertWebpSaved(path => Barcode.Save(BarcodeType.Code128, "CODE128-WEBP", path));
    }

    [Fact]
    public void DataMatrix_Save_ByExtension_WritesWebp() {
        AssertWebpSaved(path => DataMatrixCode.Save("DM-WEBP", path));
    }

    [Fact]
    public void Pdf417_Save_ByExtension_WritesWebp() {
        AssertWebpSaved(path => Pdf417Code.Save("PDF417-WEBP", path));
    }

    private static void AssertWebpSaved(Action<string> saveAction) {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.webp");

        try {
            saveAction(path);
            var bytes = File.ReadAllBytes(path);
            Assert.True(WebpReader.IsWebp(bytes));
        } finally {
            if (File.Exists(path)) {
                File.Delete(path);
            }
        }
    }
}
