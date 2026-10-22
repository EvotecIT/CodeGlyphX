using System;
using CodeGlyphX.Aztec;
using CodeGlyphX.Code128;
using CodeGlyphX.Code39;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.Pdf417;

namespace CodeGlyphX;

public static partial class Hibc {
    /// <summary>Encodes data or a supplied checked HIBC payload in Code 39 or Code 128, with one application check.</summary>
    public static Barcode1D EncodeLinear(SymbolFormat carrier, string input, HibcEncodingOptions options) {
        ValidateCarrierOptions(carrier, options);
        var payload = PrepareInput(input, options);
        var encoded = carrier switch {
            SymbolFormat.Code39 => Code39Encoder.Encode(payload, includeChecksum: false),
            SymbolFormat.Code128 => Code128Encoder.Encode(payload),
            _ => throw new ArgumentException("The HIBC linear carrier must be Code 39 or Code 128.", nameof(carrier))
        };
        return new Barcode1D(encoded.Segments, encoded.Format, HumanReadable(payload));
    }

    /// <summary>Encodes HIBC using the selected carrier's normal size, shape and error-correction controls.</summary>
    public static MatrixSymbol EncodeMatrix(SymbolFormat carrier, string input, HibcEncodingOptions options) {
        ValidateCarrierOptions(carrier, options);
        var payload = PrepareInput(input, options);
        return carrier switch {
            SymbolFormat.QrCode => new MatrixSymbol(carrier, QR.Encode(payload, options.Qr).Modules),
            SymbolFormat.Aztec => AztecEncoder.EncodeSymbol(payload, new AztecEncodeOptions {
                TextEncoding = options.Aztec?.TextEncoding, EciAssignmentNumber = options.Aztec?.EciAssignmentNumber,
                Layers = options.Aztec?.Layers, Compact = options.Aztec?.Compact,
                ErrorCorrectionPercent = options.Aztec?.ErrorCorrectionPercent ?? 23
            }),
            SymbolFormat.DataMatrix => DataMatrixEncoder.EncodeSymbol(payload, options.DataMatrix ?? new DataMatrixEncodingOptions()),
            SymbolFormat.Pdf417 => Pdf417Encoder.EncodeSymbol(payload, options.Pdf417),
            SymbolFormat.MicroPdf417 => new MatrixSymbol(carrier, MicroPdf417Encoder.Encode(payload, options.MicroPdf417)),
            SymbolFormat.CodablockF => CodablockF.CodablockFEncoder.Encode(payload, options.CodablockFColumns ?? 8),
            _ => throw new ArgumentException("This format is not a supported HIBC matrix carrier.", nameof(carrier))
        };
    }

    internal static string PrepareInput(string input, HibcEncodingOptions options) {
        if (options is null) throw new ArgumentNullException(nameof(options));
        if (input is null) throw new ArgumentNullException(nameof(input));
        if (input.Length > 32768) throw new ArgumentException("The HIBC input exceeds the text limit.", nameof(input));
        if (options.InputMode == HibcInputMode.Data) return options.ValidateStructure ? PrepareStructured(input) : Prepare(input);
        if (options.InputMode != HibcInputMode.CheckedPayload) throw new ArgumentOutOfRangeException(nameof(options.InputMode));
        if (!TryParse(input, out _)) throw new ArgumentException("The supplied HIBC flag or check character is incorrect.", nameof(input));
        if (options.ValidateStructure && (!TryParseStructured(input, out var parsed) || parsed.Production?.HasLegacyLot == true))
            throw new ArgumentException("The supplied HIBC supplier fields are invalid or use a legacy lot structure.", nameof(input));
        return input;
    }

    private static void ValidateCarrierOptions(SymbolFormat carrier, HibcEncodingOptions options) {
        if (options is null) throw new ArgumentNullException(nameof(options));
        if ((options.Qr is not null && carrier != SymbolFormat.QrCode) || (options.Aztec is not null && carrier != SymbolFormat.Aztec)
            || (options.DataMatrix is not null && carrier != SymbolFormat.DataMatrix) || (options.Pdf417 is not null && carrier != SymbolFormat.Pdf417)
            || (options.MicroPdf417 is not null && carrier != SymbolFormat.MicroPdf417) || (options.CodablockFColumns.HasValue && carrier != SymbolFormat.CodablockF))
            throw new ArgumentException("A HIBC encoding control does not apply to the selected carrier.", nameof(options));
        if ((options.Qr is not null && options.Qr.Fnc1Mode != QrFnc1Mode.None) || options.DataMatrix?.IsGs1 == true)
            throw new ArgumentException("HIBC and GS1 application modes cannot be combined.", nameof(options));
        if (options.DataMatrix is not null && (options.DataMatrix.ReaderProgramming || options.DataMatrix.Macro != DataMatrixMacro.None))
            throw new ArgumentException("HIBC Data Matrix cannot use reader-programming or Macro message envelopes.", nameof(options));
    }
}
