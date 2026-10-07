using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CodeGlyphX;
using CodeGlyphX.Rendering;

internal static class Program {
    private static async Task Main() {
        const string payload = "PACKAGE-CONTRACT";
        var qr = QR.Encode(payload: payload, encodingOptions: new QrEncodingOptions {
            ErrorCorrectionLevel = QrErrorCorrectionLevel.Q,
            EciMode = QrEciMode.Never,
            ForceMask = 0
        });
        var appearance = new QrRenderOptions { ModuleSize = 14, QuietZone = 6 };
        var outputOptions = new OutputOptions { PngCompressionLevel = 1 };
        var output = qr.Render(format: OutputFormat.Png, renderOptions: appearance, outputOptions: outputOptions);
        ReadOnlyMemory<byte> bytes = output.Data;
        Require(bytes.Length > 0, "QR output is empty.");
        Require(QR.Render(payload, format: OutputFormat.Svg).GetText().Contains("<svg"), "Default rendering failed.");
        Require(QR.Create(payload)
            .WithEncoding(encoding => encoding.EciMode = QrEciMode.Never)
            .WithRendering(rendering => rendering.ModuleSize = 3)
            .Render(OutputFormat.Svg).Data.Length > 0, "The packaged builder failed.");

        var decoded = SymbolDecoder.Decode(qr.Modules, format: SymbolFormat.QrCode);
        Require(decoded.Text == payload && decoded.HasRawBytes, "QR module payload was not preserved.");
        Require(decoded.Metadata is QrSymbolMetadata metadata && metadata.Version == qr.Version &&
            metadata.ErrorCorrectionLevel == QrErrorCorrectionLevel.Q, "QR metadata was not preserved.");

        using var stream = new MemoryStream();
        stream.WriteByte(255);
        qr.Save(stream, format: OutputFormat.Png, renderOptions: appearance, outputOptions: outputOptions);
        Require(stream.CanWrite, "Saving closed the caller's stream.");
        stream.Position = 1;
        var scan = await SymbolScanner.ScanAsync(stream, options: new ScanOptions {
            Formats = new[] { SymbolFormat.QrCode },
            TimeoutMilliseconds = 5000
        }, cancellationToken: CancellationToken.None);
        Require(scan.IsSuccess && scan.Symbols.Count == 1 && scan.Symbols[0].Text == payload,
            "The packaged scanner could not read the remaining stream.");
        Require(stream.Position == stream.Length, "Scanning did not consume the remaining stream.");

        var matrix = DataMatrixCode.Encode("LOT-42");
        Require(matrix.Format == SymbolFormat.DataMatrix && matrix.Rows > 0 && matrix.Columns > 0,
            "Data Matrix encoding lost its structural metadata.");
        var pdf417 = Pdf417Code.Encode("DOCUMENT-42");
        Require(pdf417.Format == SymbolFormat.Pdf417 && pdf417.Rows > 0 && pdf417.Columns > 0,
            "PDF417 encoding lost its structural metadata.");
        var barcode = Barcode.Encode(type: SymbolFormat.Code128, content: "PRODUCT-42");
        Require(barcode.TotalModules > 0, "Linear encoding failed.");
        Console.WriteLine("Package consumer contracts passed.");
    }

    private static void Require(bool condition, string message) {
        if (!condition) throw new InvalidOperationException(message);
    }
}
