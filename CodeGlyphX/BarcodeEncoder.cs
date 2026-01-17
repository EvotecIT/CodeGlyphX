using System;
using CodeGlyphX.Code11;
using CodeGlyphX.Code128;
using CodeGlyphX.Code39;
using CodeGlyphX.Code93;
using CodeGlyphX.Codabar;
using CodeGlyphX.Ean;
using CodeGlyphX.Itf;
using CodeGlyphX.Msi;
using CodeGlyphX.Plessey;
using CodeGlyphX.UpcA;
using CodeGlyphX.UpcE;

namespace CodeGlyphX;

/// <summary>
/// Encodes supported barcode symbologies into a <see cref="Barcode1D"/> model.
/// </summary>
public static class BarcodeEncoder {
    /// <summary>
    /// Encodes a barcode value using the specified <see cref="BarcodeType"/>.
    /// </summary>
    /// <exception cref="NotSupportedException">Thrown when the requested type is not implemented yet.</exception>
    public static Barcode1D Encode(BarcodeType type, string value) {
        return type switch {
            BarcodeType.Code128 => Code128Encoder.Encode(value),
            BarcodeType.GS1_128 => Code128Encoder.EncodeGs1(Gs1.ElementString(value)),
            BarcodeType.Code39 => Code39Encoder.Encode(value, includeChecksum: true, fullAsciiMode: false),
            BarcodeType.Code93 => Code93Encoder.Encode(value, includeChecksum: true, fullAsciiMode: false),
            BarcodeType.EAN => EanEncoder.Encode(value),
            BarcodeType.UPCA => UpcAEncoder.Encode(value),
            BarcodeType.UPCE => UpcEEncoder.Encode(value, UpcENumberSystem.Zero),
            BarcodeType.ITF14 => Itf14Encoder.Encode(value),
            BarcodeType.Codabar => CodabarEncoder.Encode(value),
            BarcodeType.MSI => MsiEncoder.Encode(value, MsiChecksumType.Mod10),
            BarcodeType.Code11 => Code11Encoder.Encode(value, includeChecksum: true),
            BarcodeType.Plessey => PlesseyEncoder.Encode(value),
            BarcodeType.KixCode => throw new NotSupportedException("BarcodeType.KixCode is a 2D barcode. Use MatrixBarcodeEncoder."),
            BarcodeType.DataMatrix => throw new NotSupportedException("BarcodeType.DataMatrix is a 2D barcode. Use MatrixBarcodeEncoder."),
            BarcodeType.PDF417 => throw new NotSupportedException("BarcodeType.PDF417 is a 2D barcode. Use MatrixBarcodeEncoder."),
            _ => throw new NotSupportedException(
                $"BarcodeType.{type} is not supported yet. See ROADMAP.md: Phase 5 — Improvements."),
        };
    }

    /// <summary>
    /// Encodes a Code 39 barcode.
    /// </summary>
    public static Barcode1D EncodeCode39(string value, bool includeChecksum = true, bool fullAsciiMode = false) =>
        Code39Encoder.Encode(value, includeChecksum, fullAsciiMode);

    /// <summary>
    /// Encodes a Code 93 barcode.
    /// </summary>
    public static Barcode1D EncodeCode93(string value, bool includeChecksum = true, bool fullAsciiMode = false) =>
        Code93Encoder.Encode(value, includeChecksum, fullAsciiMode);

    /// <summary>
    /// Encodes an EAN-8 or EAN-13 barcode.
    /// </summary>
    public static Barcode1D EncodeEan(string value) => EanEncoder.Encode(value);

    /// <summary>
    /// Encodes a UPC-A barcode.
    /// </summary>
    public static Barcode1D EncodeUpcA(string value) => UpcAEncoder.Encode(value);

    /// <summary>
    /// Encodes a UPC-E barcode.
    /// </summary>
    public static Barcode1D EncodeUpcE(string value, UpcENumberSystem numberSystem = UpcENumberSystem.Zero) =>
        UpcEEncoder.Encode(value, numberSystem);

    /// <summary>
    /// Encodes an ITF-14 barcode.
    /// </summary>
    public static Barcode1D EncodeItf14(string value) => Itf14Encoder.Encode(value);

    /// <summary>
    /// Encodes a Codabar barcode.
    /// </summary>
    public static Barcode1D EncodeCodabar(string value, char start = 'A', char stop = 'B') =>
        CodabarEncoder.Encode(value, start, stop);

    /// <summary>
    /// Encodes an MSI barcode.
    /// </summary>
    public static Barcode1D EncodeMsi(string value, MsiChecksumType checksum = MsiChecksumType.Mod10) =>
        MsiEncoder.Encode(value, checksum);

    /// <summary>
    /// Encodes a Code 11 barcode.
    /// </summary>
    public static Barcode1D EncodeCode11(string value, bool includeChecksum = true) =>
        Code11Encoder.Encode(value, includeChecksum);

    /// <summary>
    /// Encodes a Plessey barcode.
    /// </summary>
    public static Barcode1D EncodePlessey(string value) => PlesseyEncoder.Encode(value);
}
