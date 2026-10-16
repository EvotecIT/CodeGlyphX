using System;
using System.IO;
using CodeGlyphX.D3Aqr;
using CodeGlyphX.Mailmark;
using CodeGlyphX.Payloads;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Examples;

/// <summary>Runnable specialist formats, checked identifiers and application captions.</summary>
internal static class SpecialistBarcodeExample {
    internal static void Run(string outputDir) {
        var linear = new (SymbolFormat Format, string Value)[] {
            (SymbolFormat.Ean2, "12"), (SymbolFormat.Ean5, "52495"), (SymbolFormat.TelepenNumeric, "123456"),
            (SymbolFormat.Coop2Of5, "0123456789"), (SymbolFormat.Datalogic2Of5, "0123456789"),
            (SymbolFormat.Bc412, "BRC412AB"), (SymbolFormat.FacingIdentificationMark, "A"),
            (SymbolFormat.Flattermarken, "11099"), (SymbolFormat.ChannelCode, "1234"), (SymbolFormat.PosiCode, "ABC123")
        };
        foreach (var sample in linear) {
            var barcode = Barcode.Encode(sample.Format, sample.Value);
            var modules = new bool[barcode.TotalModules];
            var at = 0;
            foreach (var run in barcode.Segments)
                for (var x = 0; x < run.Modules; x++) modules[at++] = run.IsBar;
            if (!BarcodeDecoder.TryDecode(modules, SymbolCapabilities.Get(sample.Format).LegacyBarcodeType, out var decoded)
                || decoded.Text != sample.Value) throw new InvalidOperationException($"{sample.Format} example did not decode.");
            barcode.Save(Path.Combine(outputDir, sample.Format + ".png"), new BarcodeOptions { LabelText = sample.Value });
        }
        var matrices = new (SymbolFormat Format, string Value)[] {
            (SymbolFormat.Code16K, "HELLO"), (SymbolFormat.Code49, "HELLO"), (SymbolFormat.CodablockF, "HELLO"),
            (SymbolFormat.Mailmark4State, "1100000000000XY11     "), (SymbolFormat.Daft, "DAFT")
        };
        foreach (var sample in matrices) {
            var symbol = MatrixBarcodeEncoder.EncodeSymbol(sample.Format, sample.Value);
            if (!SymbolDecoder.TryDecode(symbol.Modules, out var decoded, sample.Format) || decoded.Text != sample.Value)
                throw new InvalidOperationException($"{sample.Format} example did not decode.");
            symbol.Save(Path.Combine(outputDir, sample.Format + ".png"), new MatrixOptions { LabelText = sample.Value });
        }
        var framed = D3AqrEncoder.EncodeText("https://example.com", category: 2842);
        if (!D3AqrDecoder.TryDecode(framed.Modules, out var d3) || d3.Category != 2842)
            throw new InvalidOperationException("The D3 AQR example did not preserve its category.");
        framed.Save(Path.Combine(outputDir, "D3Aqr.png"));

        OutputWriter.Write(Path.Combine(outputDir, "isbn.png"), BarcodeProfiles.Render(LinearBarcodeProfile.Isbn, "9781565812314", OutputFormat.Png));
        OutputWriter.Write(Path.Combine(outputDir, "mands.png"), BarcodeProfiles.Render(LinearBarcodeProfile.MarksAndSpencer, "0642118", OutputFormat.Png));
        OutputWriter.Write(Path.Combine(outputDir, "package.png"), IntelligentMailPackage.Render("9101123456789000000013", OutputFormat.Png, "12345"));
        OutputWriter.Write(Path.Combine(outputDir, "coupon.png"), NorthAmericanCoupon.Render("(8110)012345612345611110123", OutputFormat.Png));
        var hibc = Hibc.EncodeMatrix(SymbolFormat.MicroPdf417, "A999BJC5D6E71");
        if (!Hibc.TryDecodeMatrix(SymbolFormat.MicroPdf417, hibc.Modules, out var hibcData) || hibcData != "A999BJC5D6E71")
            throw new InvalidOperationException("The HIBC MicroPDF417 example did not decode.");
        hibc.Save(Path.Combine(outputDir, "hibc-micro.png"), new MatrixOptions { LabelText = Hibc.Prepare(hibcData) });
        var mail = Mailmark2D.Prepare("1001234", "12345678", "AB19XY1A", customerContent: "www.xyz.com");
        Mailmark2D.Encode(mail).Save(Path.Combine(outputDir, "mailmark-2d.png"), new MatrixOptions { LabelText = "Mailmark type 29" });
        var payment = new SwissQrCodePayload(
            new SwissQrCodePayload.Iban("CH9300762011623852957", SwissQrIbanType.Iban), SwissQrCurrency.CHF,
            SwissQrCodePayload.Contact.CreateStructured("Max Muster & Söhne", "Musterstrasse", "123", "8000", "Seldwyla", "CH"),
            new SwissQrCodePayload.Reference(SwissQrReferenceType.SCOR, "RF18539007547034"), amount: 50);
        var swiss = SwissQrCode.Encode(payment);
        if (!SwissQrCode.TryDecode(swiss.Modules, out var parsed) || parsed.ToString() != payment.ToString())
            throw new InvalidOperationException("The Swiss QR example did not preserve the payment.");
        swiss.Save(Path.Combine(outputDir, "swiss-payment.png"));
    }
}
