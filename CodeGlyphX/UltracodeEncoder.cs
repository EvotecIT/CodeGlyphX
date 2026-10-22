using System;
using CodeGlyphX.Ultracode;

namespace CodeGlyphX;

/// <summary>Encodes Latin-1 text or exact bytes in Ultracode's eight-bit mode.</summary>
public static class UltracodeEncoder {
    /// <summary>Encodes characters U+0000 through U+00FF with automatic size selection.</summary>
    public static UltracodeSymbol EncodeText(string text, UltracodeErrorCorrectionLevel errorCorrectionLevel = UltracodeErrorCorrectionLevel.EC2) {
        if (text is null) throw new ArgumentNullException(nameof(text));
        if (text.Length > 263) throw new ArgumentException("The input exceeds Ultracode's eight-bit capacity.", nameof(text));
        var bytes = new byte[text.Length];
        for (var i = 0; i < bytes.Length; i++) {
            if (text[i] > 255) throw new ArgumentException("Ultracode text must contain Latin-1 characters only.", nameof(text));
            bytes[i] = (byte)text[i];
        }
        return EncodeBytes(bytes, errorCorrectionLevel);
    }

    /// <summary>Encodes exact bytes with automatic size selection and a one-cell quiet-zone requirement.</summary>
    public static UltracodeSymbol EncodeBytes(byte[] bytes, UltracodeErrorCorrectionLevel errorCorrectionLevel = UltracodeErrorCorrectionLevel.EC2) {
        if (bytes is null) throw new ArgumentNullException(nameof(bytes));
        if (errorCorrectionLevel < UltracodeErrorCorrectionLevel.EC1 || errorCorrectionLevel > UltracodeErrorCorrectionLevel.EC5)
            throw new ArgumentOutOfRangeException(nameof(errorCorrectionLevel));
        if (bytes.Length > 263) throw new ArgumentException("The input exceeds Ultracode's eight-bit capacity.", nameof(bytes));
        var count = bytes.Length + 3;
        var parityCount = UltracodeLayout.ParityCount(count, errorCorrectionLevel);
        if (count + parityCount > 282) throw new ArgumentException("The input exceeds capacity at the selected error-correction level.", nameof(bytes));
        var message = new int[count];
        message[0] = 257; message[1] = count; message[2] = parityCount - 3;
        for (var i = 0; i < bytes.Length; i++) message[3 + i] = bytes[i];
        var parity = UltracodeErrorCorrection.Encode(message, parityCount);
        var layout = new UltracodeLayout(count, parityCount);
        var words = new int[layout.TileCount];
        words[0] = message[0]; words[1] = count;
        Array.Copy(parity, 0, words, 2, parity.Length);
        words[parityCount + 2] = count + parityCount;
        words[parityCount + 3] = 283;
        words[parityCount + 4] = message[2];
        Array.Copy(message, 3, words, parityCount + 5, bytes.Length);
        for (var i = count + parityCount + 2; i < words.Length - 1; i++) words[i] = 284;
        words[words.Length - 1] = parityCount;
        return new UltracodeSymbol(layout.Place(words), errorCorrectionLevel);
    }
}
