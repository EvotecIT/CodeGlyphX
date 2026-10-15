using System;
using CodeGlyphX.Code128;
using CodeGlyphX.Code39;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.Pdf417;

namespace CodeGlyphX;

/// <summary>Owns HIBC's leading plus, character validation, Mod-43 check character, and carrier adapters.</summary>
/// <remarks>These helpers validate HIBC transport framing. They do not assign labeler codes or validate a complete UDI record.</remarks>
public static class Hibc {
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%";

    /// <summary>Builds a checked HIBC carrier payload from unframed data.</summary>
    public static string Prepare(string data) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        if (data.Length == 0) throw new ArgumentException("HIBC data cannot be empty.", nameof(data));
        var sum = 41; // '+' in the Code 39 character order.
        foreach (var character in data) {
            var value = Alphabet.IndexOf(character);
            if (value < 0) throw new ArgumentException("HIBC accepts Code 39 uppercase characters only.", nameof(data));
            sum = (sum + value) % 43;
        }
        return "+" + data + Alphabet[sum];
    }

    /// <summary>Validates HIBC framing and check character, returning the unframed application data.</summary>
    public static bool TryParse(string payload, out string data) {
        data = string.Empty;
        if (payload is null || payload.Length < 3 || payload[0] != '+') return false;
        var sum = 41;
        for (var i = 1; i < payload.Length - 1; i++) {
            var value = Alphabet.IndexOf(payload[i]);
            if (value < 0) return false;
            sum = (sum + value) % 43;
        }
        if (payload[payload.Length - 1] != Alphabet[sum]) return false;
        data = payload.Substring(1, payload.Length - 2);
        return true;
    }

    /// <summary>Encodes a HIBC payload in Code 39 or Code 128, without adding a second Code 39 check character.</summary>
    public static Barcode1D EncodeLinear(SymbolFormat carrier, string data) {
        var payload = Prepare(data);
        return carrier switch {
            SymbolFormat.Code39 => Code39Encoder.Encode(payload, includeChecksum: false),
            SymbolFormat.Code128 => Code128Encoder.Encode(payload),
            _ => throw new ArgumentException("The HIBC linear carrier must be Code 39 or Code 128.", nameof(carrier))
        };
    }

    /// <summary>Encodes HIBC in QR Code, square Data Matrix, PDF417, or MicroPDF417.</summary>
    public static MatrixSymbol EncodeMatrix(SymbolFormat carrier, string data) {
        var payload = Prepare(data);
        return carrier switch {
            SymbolFormat.QrCode => new MatrixSymbol(carrier, QR.Encode(payload).Modules),
            SymbolFormat.DataMatrix => DataMatrixEncoder.EncodeSymbol(payload),
            SymbolFormat.Pdf417 => Pdf417Encoder.EncodeSymbol(payload),
            SymbolFormat.MicroPdf417 => new MatrixSymbol(carrier, MicroPdf417Encoder.Encode(payload)),
            _ => throw new ArgumentException("This format is not a supported HIBC matrix carrier.", nameof(carrier))
        };
    }

    /// <summary>Encodes HIBC using explicit Data Matrix settings, including rectangular and DMRE models.</summary>
    public static DataMatrixSymbol EncodeDataMatrix(string data, DataMatrixEncodingOptions options) =>
        DataMatrixEncoder.EncodeSymbol(Prepare(data), options ?? throw new ArgumentNullException(nameof(options)));

    /// <summary>Decodes and validates a HIBC payload from an explicitly selected linear carrier.</summary>
    public static bool TryDecodeLinear(SymbolFormat carrier, ReadOnlySpan<bool> modules, out string data) {
        data = string.Empty;
        var type = carrier switch {
            SymbolFormat.Code39 => BarcodeType.Code39,
            SymbolFormat.Code128 => BarcodeType.Code128,
            _ => throw new ArgumentException("The HIBC linear carrier must be Code 39 or Code 128.", nameof(carrier))
        };
        return BarcodeDecoder.TryDecode(modules.ToArray(), type, new BarcodeDecodeOptions { Code39FullAscii = false }, out var decoded)
            && TryParse(decoded.Text, out data);
    }

    /// <summary>Decodes and validates HIBC transport data from a known matrix carrier.</summary>
    public static bool TryDecodeMatrix(SymbolFormat carrier, BitMatrix modules, out string data) {
        data = string.Empty;
        if (carrier != SymbolFormat.QrCode && carrier != SymbolFormat.DataMatrix
            && carrier != SymbolFormat.Pdf417 && carrier != SymbolFormat.MicroPdf417)
            throw new ArgumentException("This format is not a supported HIBC matrix carrier.", nameof(carrier));
        return SymbolDecoder.TryDecode(modules, out var decoded, carrier) && TryParse(decoded.Text, out data);
    }
}
