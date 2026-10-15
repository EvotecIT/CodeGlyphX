// Shared helpers for the inner-page visuals: every code on those pages is encoded and drawn by CodeGlyphX here,
// written under static/visuals/ and described in data/visuals.json so templates never guess sizes.
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CodeGlyphX;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Website.Specimens;

internal static partial class SiteVisuals {
    static readonly UTF8Encoding Utf8 = new(false);
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);

    // Night panel palette (navigation thumbnails are always shown on the night surface).
    static readonly Rgba32 Clear = new(0, 0, 0, 0);
    static readonly Rgba32 NightInk = new(236, 233, 248);
    static readonly Rgba32 Violet = new(167, 139, 250);
    static readonly Rgba32 Pink = new(244, 114, 182);
    static readonly Rgba32 Cyan = new(103, 232, 249);

    static readonly OutputOptions PngOut = new() { PngCompressionLevel = 9 };
    static string root = "";
    static string staticRoot = "";

    sealed record Asset(string Src, int W, int H);

    /// <summary>Writes a file under static/visuals and returns its site path.</summary>
    static string WriteVisual(string relative, string text) {
        var path = Path.Combine(staticRoot, "visuals", relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text, Utf8);
        return "/visuals/" + relative;
    }

    static string WriteVisual(string relative, byte[] data) {
        var path = Path.Combine(staticRoot, "visuals", relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, data);
        return "/visuals/" + relative;
    }

    static readonly Regex RootSize = new("<svg[^>]*?\\swidth=\"(?<w>[\\d.]+)\"[^>]*?\\sheight=\"(?<h>[\\d.]+)\"", RegexOptions.Compiled, RegexTimeout);
    static readonly Regex RootViewBox = new("<svg[^>]*?\\sviewBox=\"(?<vb>[^\"]+)\"", RegexOptions.Compiled, RegexTimeout);

    static (int W, int H) SvgSize(string svg) {
        var m = RootSize.Match(svg);
        if (!m.Success) throw new InvalidOperationException("SVG without width/height.");
        return ((int)Math.Round(double.Parse(m.Groups["w"].Value, Inv)), (int)Math.Round(double.Parse(m.Groups["h"].Value, Inv)));
    }

    /// <summary>Saves CodeGlyphX SVG output as a visual and records its intrinsic size.</summary>
    static Asset SaveSvg(string relative, string svg) {
        var (w, h) = SvgSize(svg);
        return new Asset(WriteVisual(relative, svg), w, h);
    }

    /// <summary>Saves PNG bytes as a visual; the size is read back with CodeGlyphX's own image reader.</summary>
    static Asset SavePng(string relative, byte[] png) {
        ImageReader.DecodeRgba32(png, out var w, out var h);
        return new Asset(WriteVisual(relative, png), w, h);
    }

    /// <summary>
    /// Places a CodeGlyphX SVG inside a composed SVG: the root element keeps its viewBox and gets a position and size,
    /// so the modules stay exactly as the library drew them.
    /// </summary>
    static string Nest(string svg, double x, double y, double w, double h, string? extra = null) {
        var body = svg.Trim();
        if (body.StartsWith("<?xml", StringComparison.Ordinal)) body = body[(body.IndexOf("?>", StringComparison.Ordinal) + 2)..].TrimStart();
        var vb = RootViewBox.Match(body);
        var viewBox = vb.Success ? vb.Groups["vb"].Value : null;
        var close = body.IndexOf('>');
        var open = $"<svg x=\"{F(x)}\" y=\"{F(y)}\" width=\"{F(w)}\" height=\"{F(h)}\"{(viewBox is null ? "" : $" viewBox=\"{viewBox}\"")} preserveAspectRatio=\"xMidYMid meet\"{(extra is null ? "" : " " + extra)}";
        var head = body[..close];
        if (head.Contains("shape-rendering", StringComparison.Ordinal)) open += " shape-rendering=\"crispEdges\"";
        return open + body[close..];
    }

    /// <summary>
    /// Styled QR output. CodeGlyphX's SVG writer refuses appearance it cannot express as vectors (module shapes, for
    /// one), so those render as PNG. Returns SVG markup either way: the library's SVG, or an SVG wrapper around its PNG.
    /// </summary>
    static string QrMarkup(string payload, QrRenderOptions options, QrEncodingOptions? encoding = null) {
        string? svg = null;
        try {
            svg = QR.Render(payload, OutputFormat.Svg, options, encoding).GetText();
        } catch (NotSupportedException) {
        }
        var png = QR.Render(payload, OutputFormat.Png, options, encoding, PngOut).ToArray();
        if (svg is not null && Utf8.GetByteCount(svg) <= png.Length * 1.5) return svg;
        ImageReader.DecodeRgba32(png, out var w, out var h);
        return $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{w}\" height=\"{h}\" viewBox=\"0 0 {w} {h}\"><image width=\"{w}\" height=\"{h}\" href=\"data:image/png;base64,{Convert.ToBase64String(png)}\"/></svg>";
    }
    /// <summary>
    /// Saves a styled QR as SVG when the vector writer supports the options and the result is not much larger than
    /// the PNG; otherwise as PNG (shaped modules make very long SVG paths).
    /// </summary>
    static Asset SaveQr(string relativeWithoutExtension, string payload, QrRenderOptions options, QrEncodingOptions? encoding = null) {
        string? svg = null;
        try {
            svg = QR.Render(payload, OutputFormat.Svg, options, encoding).GetText();
        } catch (NotSupportedException) {
        }
        var png = QR.Render(payload, OutputFormat.Png, options, encoding, PngOut).ToArray();
        if (svg is not null && Utf8.GetByteCount(svg) <= png.Length * 1.5) return SaveSvg(relativeWithoutExtension + ".svg", svg);
        return SavePng(relativeWithoutExtension + ".png", png);
    }
    static string F(double v) => Math.Round(v, 2).ToString("0.##", Inv);

    static string Hex(Rgba32 c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    static string Esc(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    static MatrixOptions Matrix(Rgba32 fg, Rgba32? bg = null, int module = 4, int quiet = 0) =>
        new() { Foreground = fg, Background = bg ?? Clear, ModuleSize = module, QuietZone = quiet };

    static BarcodeOptions Bars(Rgba32 fg, Rgba32? bg = null, int module = 2, int quiet = 0, int height = 40) =>
        new() { Foreground = fg, Background = bg ?? Clear, ModuleSize = module, QuietZone = quiet, HeightModules = height };

    static QrRenderOptions QrNight(Rgba32 fg, QrModuleShape? shape = null, int module = 4) =>
        new() { Foreground = fg, Background = Clear, ModuleSize = module, QuietZone = 0, ModuleShape = shape };

    /// <summary>Decodes a rendered PNG with CodeGlyphX so captions only state what the library reads back.</summary>
    static string? Scan(byte[] png, params SymbolFormat[] formats) {
        var scan = SymbolScanner.Scan(png, new ScanOptions { Formats = formats.Length == 0 ? null : formats, TimeoutMilliseconds = 8000 });
        return scan.IsSuccess && scan.Symbols.Count > 0 ? scan.Symbols[0].Text : null;
    }

    static string Kb(int bytes) => bytes < 1024 ? bytes.ToString(Inv) + " B" : (bytes / 1024d).ToString(bytes < 10240 ? "0.0" : "0", Inv) + " KB";
}
