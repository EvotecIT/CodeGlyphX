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

/// <summary>
/// One-line QR generation helpers with sane defaults.
/// </summary>
/// <example>
/// <code>
/// using CodeGlyphX;
/// var png = QrEasy.RenderPng("https://example.com");
/// var svg = QrEasy.RenderSvg("https://example.com");
/// </code>
/// </example>
public static partial class QrEasy {
    /// <summary>
    /// Encodes a payload into a <see cref="QrCode"/> with defaults.
    /// </summary>
    public static QrCode Encode(string payload, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
        return EncodePayload(payload, opts);
    }

    /// <summary>
    /// Detects a payload type and encodes it into a <see cref="QrCode"/>.
    /// </summary>
    public static QrCode EncodeAuto(string payload, QrPayloadDetectOptions? detectOptions = null, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var detected = QrPayloads.Detect(payload, detectOptions);
        return Encode(detected, options);
    }

    /// <summary>
    /// Encodes a payload with embedded defaults.
    /// </summary>
    public static QrCode Encode(QrPayloadData payload, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        return EncodePayload(payload.Text, opts);
    }

    /// <summary>
    /// Evaluates QR art safety for a payload and options.
    /// </summary>
    public static QrArtSafetyReport EvaluateSafety(string payload, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = options is null ? new QrEasyOptions() : CloneOptions(options);
        var qr = Encode(payload, opts);
        var render = BuildPngOptions(opts, payload, qr);
        return QrArtSafety.Evaluate(qr, render);
    }

    /// <summary>
    /// Evaluates QR art safety for a payload with embedded defaults.
    /// </summary>
    public static QrArtSafetyReport EvaluateSafety(QrPayloadData payload, QrEasyOptions? options = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var opts = MergeOptions(payload, options);
        var qr = Encode(payload.Text, opts);
        var render = BuildPngOptions(opts, payload.Text, qr);
        return QrArtSafety.Evaluate(qr, render);
    }
}
