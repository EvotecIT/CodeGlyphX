using System;
using CodeGlyphX.Internal.ReedSolomon;
using CodeGlyphX.MaxiCode;

namespace CodeGlyphX;

/// <summary>
/// Encodes ISO/IEC 16023 MaxiCode Modes 2 through 6.
/// </summary>
public static class MaxiCodeEncoder {
    /// <summary>Encodes text with automatic Mode 2/3/4 selection.</summary>
    public static MaxiCodeSymbol EncodeText(string text) => EncodeText(text, new MaxiCodeEncodingOptions());

    /// <summary>Encodes text with explicit carrier, ECI, and structured-append options.</summary>
    public static MaxiCodeSymbol EncodeText(string text, MaxiCodeEncodingOptions options) {
        if (text is null) throw new ArgumentNullException(nameof(text));
        if (options is null) throw new ArgumentNullException(nameof(options));
        options = options.Clone();
        var mode = ResolveMode(options);
        var capacity = mode == MaxiCodeMode.FullEcc ? 77 : mode is MaxiCodeMode.StructuredCarrierNumeric or MaxiCodeMode.StructuredCarrierAlphanumeric ? 84 : 93;
        var message = MaxiCodeHighLevelEncoder.EncodeText(text, mode, options, capacity);
        var codewords = BuildCodewords(message, mode, options);
        return new MaxiCodeSymbol(mode, BuildMatrix(codewords));
    }

    private static MaxiCodeMode ResolveMode(MaxiCodeEncodingOptions options) {
        var mode = options.Mode;
        if (mode == MaxiCodeMode.Auto) {
            if (string.IsNullOrWhiteSpace(options.PostalCode)) return MaxiCodeMode.Standard;
            mode = MaxiCodeMode.StructuredCarrierNumeric;
            for (var i = 0; i < options.PostalCode!.Length; i++) {
                var value = options.PostalCode[i];
                if ((value >= '0' && value <= '9') || value == ' ') continue;
                mode = MaxiCodeMode.StructuredCarrierAlphanumeric;
                break;
            }
        }
        if (mode is < MaxiCodeMode.StructuredCarrierNumeric or > MaxiCodeMode.ReaderProgramming) {
            throw new ArgumentOutOfRangeException(nameof(options), options.Mode, "MaxiCode mode must be Auto or 2 through 6.");
        }
        return mode;
    }

    private static byte[] BuildCodewords(byte[] message, MaxiCodeMode mode, MaxiCodeEncodingOptions options) {
        var codewords = new byte[MaxiCodeTables.CodewordCount];
        if (mode is MaxiCodeMode.StructuredCarrierNumeric or MaxiCodeMode.StructuredCarrierAlphanumeric) {
            BuildPrimary(codewords, mode, options);
            Array.Copy(message, 0, codewords, 20, message.Length);
        } else {
            codewords[0] = (byte)mode;
            Array.Copy(message, 0, codewords, 1, 9);
            Array.Copy(message, 9, codewords, 20, message.Length - 9);
        }

        AddPrimaryErrorCorrection(codewords);
        AddSecondaryErrorCorrection(codewords, mode == MaxiCodeMode.FullEcc ? 68 : 84,
            mode == MaxiCodeMode.FullEcc ? 28 : 20);
        return codewords;
    }

    private static void BuildPrimary(byte[] codewords, MaxiCodeMode mode, MaxiCodeEncodingOptions options) {
        if (options.CountryCode is < 0 or > 999) throw new InvalidOperationException("MaxiCode country code must be between 000 and 999.");
        if (options.ServiceClass is < 0 or > 999) throw new InvalidOperationException("MaxiCode service class must be between 000 and 999.");
        if (string.IsNullOrWhiteSpace(options.PostalCode)) throw new InvalidOperationException("MaxiCode Modes 2 and 3 require a postal code.");
        if (mode == MaxiCodeMode.StructuredCarrierNumeric) {
            BuildNumericPrimary(codewords, options.PostalCode!, options.CountryCode, options.ServiceClass);
        } else {
            BuildAlphanumericPrimary(codewords, options.PostalCode!, options.CountryCode, options.ServiceClass);
        }
    }

    private static void BuildNumericPrimary(byte[] codewords, string postalCode, int country, int service) {
        postalCode = postalCode.TrimEnd();
        if (postalCode.Length is < 1 or > 9) throw new InvalidOperationException("Mode 2 postal codes must contain 1 through 9 digits.");
        for (var i = 0; i < postalCode.Length; i++) {
            if (postalCode[i] < '0' || postalCode[i] > '9') throw new InvalidOperationException("Mode 2 postal codes must be numeric.");
        }
        if (country == 840 && postalCode.Length == 5) postalCode += "0000";
        var value = int.Parse(postalCode);
        var length = postalCode.Length;
        codewords[0] = (byte)(((value & 0x03) << 4) | 2);
        codewords[1] = (byte)((value & 0xFC) >> 2);
        codewords[2] = (byte)((value & 0x3F00) >> 8);
        codewords[3] = (byte)((value & 0xFC000) >> 14);
        codewords[4] = (byte)((value & 0x3F00000) >> 20);
        codewords[5] = (byte)(((value & 0x3C000000) >> 26) | ((length & 0x03) << 4));
        codewords[6] = (byte)(((length & 0x3C) >> 2) | ((country & 0x03) << 4));
        codewords[7] = (byte)((country & 0xFC) >> 2);
        codewords[8] = (byte)(((country & 0x300) >> 8) | ((service & 0x0F) << 2));
        codewords[9] = (byte)((service & 0x3F0) >> 4);
    }

    private static void BuildAlphanumericPrimary(byte[] codewords, string postalCode, int country, int service) {
        postalCode = postalCode.ToUpperInvariant();
        if (postalCode.Length is < 1 or > 6) throw new InvalidOperationException("Mode 3 postal codes must contain 1 through 6 Code Set A characters.");
        postalCode = postalCode.PadRight(6, ' ');
        var symbols = new byte[6];
        for (var i = 0; i < symbols.Length; i++) {
            var value = postalCode[i];
            if (value < ' ' || value > 0xFF || !MaxiCodeTables.CanEncode(0, (byte)value)) {
                throw new InvalidOperationException("Mode 3 postal codes must use printable MaxiCode Code Set A characters.");
            }
            symbols[i] = MaxiCodeTables.SymbolForState(0, (byte)value);
        }

        codewords[0] = (byte)(((symbols[5] & 0x03) << 4) | 3);
        codewords[1] = (byte)(((symbols[4] & 0x03) << 4) | ((symbols[5] & 0x3C) >> 2));
        codewords[2] = (byte)(((symbols[3] & 0x03) << 4) | ((symbols[4] & 0x3C) >> 2));
        codewords[3] = (byte)(((symbols[2] & 0x03) << 4) | ((symbols[3] & 0x3C) >> 2));
        codewords[4] = (byte)(((symbols[1] & 0x03) << 4) | ((symbols[2] & 0x3C) >> 2));
        codewords[5] = (byte)(((symbols[0] & 0x03) << 4) | ((symbols[1] & 0x3C) >> 2));
        codewords[6] = (byte)(((symbols[0] & 0x3C) >> 2) | ((country & 0x03) << 4));
        codewords[7] = (byte)((country & 0xFC) >> 2);
        codewords[8] = (byte)(((country & 0x300) >> 8) | ((service & 0x0F) << 2));
        codewords[9] = (byte)((service & 0x3F0) >> 4);
    }

    private static void AddPrimaryErrorCorrection(byte[] codewords) {
        var block = new int[20];
        for (var i = 0; i < 10; i++) block[i] = codewords[i];
        new ReedSolomonEncoder(GenericGf.MaxiCode).Encode(block, 10);
        for (var i = 0; i < block.Length; i++) codewords[i] = (byte)block[i];
    }

    private static void AddSecondaryErrorCorrection(byte[] codewords, int dataLength, int eccPerParity) {
        var halfData = dataLength / 2;
        for (var parity = 0; parity < 2; parity++) {
            var block = new int[halfData + eccPerParity];
            for (var i = 0; i < halfData; i++) block[i] = codewords[20 + i * 2 + parity];
            new ReedSolomonEncoder(GenericGf.MaxiCode).Encode(block, eccPerParity);
            for (var i = 0; i < eccPerParity; i++) {
                codewords[20 + dataLength + i * 2 + parity] = (byte)block[halfData + i];
            }
        }
    }

    private static BitMatrix BuildMatrix(byte[] codewords) {
        var matrix = new BitMatrix(MaxiCodeTables.Width, MaxiCodeTables.Height);
        for (var y = 0; y < MaxiCodeTables.Height; y++) {
            for (var x = 0; x < MaxiCodeTables.Width; x++) {
                var bit = MaxiCodeTables.ModuleBitNumbers[y * MaxiCodeTables.Width + x];
                if (bit >= 0 && (codewords[bit / 6] & (1 << (5 - bit % 6))) != 0) matrix[x, y] = true;
            }
        }
        SetOrientationMarkers(matrix);
        return matrix;
    }

    private static void SetOrientationMarkers(BitMatrix matrix) {
        Set(matrix, 28, 0); Set(matrix, 29, 0);
        Set(matrix, 10, 9); Set(matrix, 11, 9); Set(matrix, 11, 10);
        Set(matrix, 7, 15); Set(matrix, 8, 16);
        Set(matrix, 20, 16); Set(matrix, 20, 17);
        Set(matrix, 10, 22); Set(matrix, 10, 23);
        Set(matrix, 17, 22); Set(matrix, 17, 23);
    }

    private static void Set(BitMatrix matrix, int x, int y) => matrix[x, y] = true;
}
