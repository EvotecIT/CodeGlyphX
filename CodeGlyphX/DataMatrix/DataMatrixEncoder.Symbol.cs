using System;

namespace CodeGlyphX.DataMatrix;

public static partial class DataMatrixEncoder {
    /// <summary>Encodes text while retaining the selected symbol size and capacity.</summary>
    public static DataMatrixSymbol EncodeSymbol(string text, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto) =>
        CreateSymbol(Encode(text, mode));

    /// <summary>Encodes text with explicit options while retaining selected symbol metadata.</summary>
    public static DataMatrixSymbol EncodeSymbol(string text, DataMatrixEncodingOptions options) =>
        CreateSymbol(Encode(text, options));

    /// <summary>Encodes bytes while retaining the selected symbol size and capacity.</summary>
    public static DataMatrixSymbol EncodeBytesSymbol(byte[] data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto) =>
        CreateSymbol(EncodeBytes(data, mode));

    /// <summary>Encodes bytes with explicit options while retaining selected symbol metadata.</summary>
    public static DataMatrixSymbol EncodeBytesSymbol(byte[] data, DataMatrixEncodingOptions options) =>
        CreateSymbol(EncodeBytes(data, options));

    internal static DataMatrixSymbol CreateSymbol(BitMatrix modules) {
        if (!DataMatrixSymbolInfo.TryGetForSize(modules.Height, modules.Width, out var info)) {
            throw new InvalidOperationException("The encoded Data Matrix dimensions do not identify a supported symbol size.");
        }
        return new DataMatrixSymbol(modules, info);
    }
}
