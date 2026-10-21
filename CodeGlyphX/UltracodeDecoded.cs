using System;
using CodeGlyphX.Internal;

namespace CodeGlyphX;

/// <summary>A checked eight-bit Ultracode payload, including its error-correction level.</summary>
public sealed class UltracodeDecoded {
    private readonly byte[] _bytes;
    /// <summary>Gets an independent copy of the exact payload bytes.</summary>
    public byte[] Bytes => (byte[])_bytes.Clone();
    /// <summary>Gets the Latin-1 interpretation of the payload.</summary>
    public string Text => EncodingUtils.Latin1.GetString(_bytes);
    /// <summary>Gets the error-correction level recovered from the checked message.</summary>
    public UltracodeErrorCorrectionLevel ErrorCorrectionLevel { get; }
    /// <summary>Gets the number of codeword positions corrected or recovered as erasures.</summary>
    public int CorrectedCodewords { get; }
    /// <summary>Gets the clockwise print rotation relative to the supplied grid.</summary>
    public int RotationDegrees { get; }
    internal UltracodeDecoded(byte[] bytes, UltracodeErrorCorrectionLevel level, int correctedCodewords, int rotationDegrees = 0) {
        _bytes = bytes; ErrorCorrectionLevel = level; CorrectedCodewords = correctedCodewords; RotationDegrees = rotationDegrees;
    }
    internal UltracodeDecoded WithRotation(int degrees) => new(_bytes, ErrorCorrectionLevel, CorrectedCodewords, degrees);
}
