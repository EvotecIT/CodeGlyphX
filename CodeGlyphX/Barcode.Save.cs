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

public static partial class Barcode {

    /// <summary>
    /// Saves a barcode to a file based on the file extension.
    /// Defaults to PNG when no extension is provided.
    /// </summary>
    public static string Save(SymbolFormat type, string content, string path, BarcodeOptions? options = null, OutputOptions? extras = null) {
        var format = OutputFormatInfo.Resolve(path, OutputFormat.Png);
        var output = Render(type, content, format, options, extras);
        return OutputWriter.Write(path, output);
    }

    /// <summary>Writes a barcode to a stream in the specified output format.</summary>
    public static void Save(SymbolFormat type, string content, Stream stream, OutputFormat format, BarcodeOptions? options = null, OutputOptions? extras = null) {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        OutputWriter.Write(stream, Render(type, content, format, options, extras));
    }


}
