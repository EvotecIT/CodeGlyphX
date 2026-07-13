using System;
using System.Globalization;
using System.IO;
using CodeGlyphX.Payloads;
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
using CodeGlyphX.Rendering.Xbm;
using CodeGlyphX.Rendering.Xpm;

namespace CodeGlyphX;

public static partial class QrEasy {

    /// <summary>
    /// Renders a QR code to a raw RGBA pixel buffer (no PNG encoding).
    /// </summary>
    public static byte[] RenderPixels(string payload, out int widthPx, out int heightPx, out int stride, QrEasyOptions? options = null) {
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, qr);
        return QrPngRenderer.RenderPixels(qr.Modules, render, out widthPx, out heightPx, out stride);
    }

    /// <summary>
    /// Renders a QR code to a raw RGBA pixel buffer (no PNG encoding) for a payload with embedded defaults.
    /// </summary>
    public static byte[] RenderPixels(QrPayloadData payload, out int widthPx, out int heightPx, out int stride, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, qr);
        return QrPngRenderer.RenderPixels(qr.Modules, render, out widthPx, out heightPx, out stride);
    }


}
