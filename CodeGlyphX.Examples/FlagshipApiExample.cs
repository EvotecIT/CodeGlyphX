using System.IO;
using CodeGlyphX.Aztec;
using CodeGlyphX.Payloads;
using CodeGlyphX.Pdf417;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Examples;

/// <summary>
/// Compiled and executed by CI so the primary public examples cannot silently drift.
/// </summary>
internal static class FlagshipApiExample {
    public static void Run(string outputDir) {
        var contact = QrPayload.VCard(
            firstName: "Ava",
            lastName: "Stone",
            phone: "+14155550198",
            email: "ava@example.com",
            organization: "CodeGlyphX");

        QR.Save(contact, Path.Combine(outputDir, "contact.png"));
        Barcode.Save(BarcodeType.EAN, "5901234123457", Path.Combine(outputDir, "product.svg"));
        DataMatrixCode.Save("LOT-2026-0042", Path.Combine(outputDir, "lot.png"), options: new MatrixOptions {
            ModuleSize = 6,
            QuietZone = 2
        });
        Pdf417Code.Save("DOCUMENT-2026-0042", Path.Combine(outputDir, "document.pdf"), new Pdf417EncodeOptions {
            ErrorCorrectionLevel = 5
        });
        AztecCode.Save("TICKET-2026-0042", Path.Combine(outputDir, "ticket.svg"), new AztecEncodeOptions {
            ErrorCorrectionPercent = 33
        });

        var styled = QR.Create("https://codeglyphx.com")
            .WithErrorCorrection(QrErrorCorrectionLevel.H)
            .Render(OutputFormat.Png);
        OutputWriter.Write(Path.Combine(outputDir, "styled.png"), styled);

        var scannerImage = QrCode.Render("SCANNER-AOT", OutputFormat.Png).Data;
        var scan = SymbolScanner.Scan(scannerImage, new ScanOptions {
            Formats = new[] { SymbolFormat.QrCode },
            TimeoutMilliseconds = 15000,
            Qr = new QrPixelDecodeOptions {
                Profile = QrDecodeProfile.Fast,
                MaxScale = 1,
                DisableTransforms = true
            }
        });
        if (!scan.IsSuccess || scan.Symbols.Count != 1 || scan.Symbols[0].Text != "SCANNER-AOT") {
            throw new System.InvalidOperationException("The unified scanner NativeAOT smoke test failed.");
        }

        var micro = MicroQrCodeEncoder.EncodeAlphanumeric("ABC123", QrErrorCorrectionLevel.L);
        OutputWriter.Write(
            Path.Combine(outputDir, "microqr.png"),
            MatrixBarcode.Render(micro.Modules, OutputFormat.Png));
    }
}
