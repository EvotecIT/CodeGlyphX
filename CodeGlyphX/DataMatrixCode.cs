using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Ascii;
using CodeGlyphX.Rendering.Bmp;
using CodeGlyphX.Rendering.Eps;
using CodeGlyphX.Rendering.Html;
using CodeGlyphX.Rendering.Ico;
using CodeGlyphX.Rendering.Jpeg;
using CodeGlyphX.Rendering.Pam;
using CodeGlyphX.Rendering.Pbm;
using CodeGlyphX.Rendering.Pgm;
using CodeGlyphX.Rendering.Pdf;
using CodeGlyphX.Rendering.Ppm;
using CodeGlyphX.Rendering.Png;
using CodeGlyphX.Rendering.Svg;
using CodeGlyphX.Rendering.Svgz;
using CodeGlyphX.Rendering.Tga;
using CodeGlyphX.Rendering.Webp;
using CodeGlyphX.Rendering.Xbm;
using CodeGlyphX.Rendering.Xpm;

namespace CodeGlyphX;

/// <summary>
/// Simple Data Matrix helpers with fluent and static APIs.
/// </summary>
/// <example>
/// <code>
/// using CodeGlyphX;
/// DataMatrixCode.Save("Serial: ABC123", "datamatrix.png");
/// </code>
/// </example>
public static partial class DataMatrixCode {
    /// <summary>
    /// Starts a fluent Data Matrix builder for text payloads.
    /// </summary>
    public static DataMatrixBuilder Create(string text, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        return new DataMatrixBuilder(text, mode, options);
    }

    /// <summary>
    /// Starts a fluent Data Matrix builder for text payloads with explicit encoding options.
    /// </summary>
    public static DataMatrixBuilder Create(string text, DataMatrixEncodingOptions encodingOptions, MatrixOptions? options = null) {
        return new DataMatrixBuilder(text, encodingOptions, options);
    }

    /// <summary>
    /// Starts a fluent Data Matrix builder for byte payloads.
    /// </summary>
    public static DataMatrixBuilder Create(byte[] data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto, MatrixOptions? options = null) {
        return new DataMatrixBuilder(data, mode, options);
    }

    /// <summary>
    /// Starts a fluent Data Matrix builder for byte payloads with explicit encoding options.
    /// </summary>
    public static DataMatrixBuilder Create(byte[] data, DataMatrixEncodingOptions encodingOptions, MatrixOptions? options = null) {
        return new DataMatrixBuilder(data, encodingOptions, options);
    }

    /// <summary>
    /// Encodes a text payload as Data Matrix.
    /// </summary>
    public static BitMatrix Encode(string text, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto) {
        return DataMatrixEncoder.Encode(text, mode);
    }

    /// <summary>
    /// Encodes a text payload as Data Matrix with explicit encoding options.
    /// </summary>
    public static BitMatrix Encode(string text, DataMatrixEncodingOptions options) {
        return DataMatrixEncoder.Encode(text, options);
    }

    /// <summary>
    /// Encodes a byte payload as Data Matrix.
    /// </summary>
    public static BitMatrix EncodeBytes(byte[] data, DataMatrixEncodingMode mode = DataMatrixEncodingMode.Auto) {
        return DataMatrixEncoder.EncodeBytes(data, mode);
    }

    /// <summary>
    /// Encodes a byte payload as Data Matrix with explicit encoding options.
    /// </summary>
    public static BitMatrix EncodeBytes(byte[] data, DataMatrixEncodingOptions options) {
        return DataMatrixEncoder.EncodeBytes(data, options);
    }

    /// <summary>
    /// Encodes a machine-readable GS1 element string with FNC1 in first position.
    /// </summary>
    public static BitMatrix EncodeGs1(string elementString, DataMatrixEncodingOptions? options = null) {
        return DataMatrixEncoder.EncodeGs1(elementString, options);
    }

    /// <summary>
    /// Encodes a Macro 05 payload body.
    /// </summary>
    public static BitMatrix EncodeMacro05(string body, DataMatrixEncodingOptions? options = null) {
        return DataMatrixEncoder.EncodeMacro05(body, options);
    }

    /// <summary>
    /// Encodes a Macro 06 payload body.
    /// </summary>
    public static BitMatrix EncodeMacro06(string body, DataMatrixEncodingOptions? options = null) {
        return DataMatrixEncoder.EncodeMacro06(body, options);
    }

    /// <summary>
    /// Encodes pre-split text parts as a Data Matrix structured-append sequence.
    /// </summary>
    public static BitMatrix[] EncodeStructuredAppend(IReadOnlyList<string> parts, int fileId1 = 1, int fileId2 = 1, DataMatrixEncodingOptions? options = null) {
        return DataMatrixEncoder.EncodeStructuredAppend(parts, fileId1, fileId2, options);
    }

    /// <summary>
    /// Encodes pre-split byte parts as a Data Matrix structured-append sequence.
    /// </summary>
    public static BitMatrix[] EncodeStructuredAppend(IReadOnlyList<byte[]> parts, int fileId1 = 1, int fileId2 = 1, DataMatrixEncodingOptions? options = null) {
        return DataMatrixEncoder.EncodeStructuredAppend(parts, fileId1, fileId2, options);
    }
}
