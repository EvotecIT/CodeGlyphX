using System;
using System.IO;
using System.Threading;
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
/// Simple barcode helpers with fluent and static APIs.
/// </summary>
/// <remarks>
/// Use <see cref="Save(CodeGlyphX.SymbolFormat,string,string,CodeGlyphX.BarcodeOptions,CodeGlyphX.Rendering.OutputOptions)"/> to pick the output format by file extension.
/// </remarks>
/// <example>
/// <code>
/// using CodeGlyphX;
/// Barcode.Save(SymbolFormat.Code128, "PRODUCT-12345", "barcode.png");
/// </code>
/// </example>
public static partial class Barcode {
    /// <summary>
    /// Starts a fluent barcode builder.
    /// </summary>
    public static BarcodeBuilder Create(SymbolFormat type, string content, BarcodeOptions? options = null) {
        return new BarcodeBuilder(type, content, options);
    }

    /// <summary>
    /// Encodes a 1D barcode.
    /// </summary>
    public static Barcode1D Encode(SymbolFormat type, string content) {
        var barcode = BarcodeEncoder.Encode(SymbolCapabilities.GetBarcodeType(type), content);
        return new Barcode1D(barcode.Segments, type);
    }
}
