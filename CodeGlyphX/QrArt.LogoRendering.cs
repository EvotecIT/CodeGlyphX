using System;
using System.Collections.Generic;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Art;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX;

public static partial class QrArt {
    /// <summary>Renders a requested logo without ever omitting it. The encoded symbol is fixed;
    /// failure to place or validate a readable logo throws. Auto preserves every functional pattern.
    /// CenterOverAlignment additionally requires a caller-supplied independent decoder.</summary>
    public static QrLogoComposition RenderLogo(QrCode qr, byte[] logoPng, QrLogoRenderOptions? options = null) {
        if (qr is null) throw new ArgumentNullException(nameof(qr));
        var design = options ?? new QrLogoRenderOptions();
        ValidateLogoDesign(logoPng, design);
        return TryRenderLogo(qr, logoPng, design, out var reason)
            ?? throw new InvalidOperationException(reason);
    }

    /// <summary>Encodes text and renders its requested logo. Correction level or version increases
    /// are tried only when explicitly enabled, and never beyond the encoding version limits.
    /// The result reports the actual symbol and placement; failure throws instead of dropping the logo.</summary>
    public static QrLogoComposition RenderLogo(string payload, byte[] logoPng,
        QrLogoRenderOptions? options = null, QrEncodingOptions? encodingOptions = null) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var design = options ?? new QrLogoRenderOptions();
        ValidateLogoDesign(logoPng, design);
        var encoding = encodingOptions?.Clone() ?? new QrEncodingOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H };
        var initial = QR.Encode(payload, encoding); // Validate the caller's encoding settings before retrying.
        var lastVersion = design.AllowVersionUpgrade ? encoding.MaxVersion : initial.Version;
        var lastLevel = design.AllowErrorCorrectionUpgrade ? QrErrorCorrectionLevel.H : encoding.ErrorCorrectionLevel;
        var reason = "No readable logo fits this symbol.";
        for (var version = initial.Version; version <= lastVersion; version++) {
            for (var level = (int)encoding.ErrorCorrectionLevel; level <= (int)lastLevel; level++) {
                var candidateEncoding = encoding.Clone();
                candidateEncoding.MinVersion = candidateEncoding.MaxVersion = version;
                candidateEncoding.ErrorCorrectionLevel = (QrErrorCorrectionLevel)level;
                QrCode candidate;
                try { candidate = QR.Encode(payload, candidateEncoding); }
                catch (ArgumentException) { continue; } // Stronger correction can exceed this version's data capacity.
                var result = TryRenderLogo(candidate, logoPng, design, out reason);
                if (result is not null) return result;
            }
        }
        throw new InvalidOperationException(reason + " Allow a higher correction level/version or reduce the requested minimum logo size.");
    }

    private static void ValidateLogoDesign(byte[] logoPng, QrLogoRenderOptions design) {
        if (logoPng is null) throw new ArgumentNullException(nameof(logoPng));
        if (logoPng.Length == 0) throw new ArgumentException("A logo PNG is required.", nameof(logoPng));
        if (design.Placement is < LogoPlacement.Auto or > LogoPlacement.CenterOverAlignment)
            throw new ArgumentOutOfRangeException(nameof(design.Placement));
        if (design.Placement == LogoPlacement.CenterOverAlignment && design.IndependentDecoder is null)
            throw new ArgumentException("CenterOverAlignment requires an independent decoder in addition to native verification.", nameof(design));
        if (design.IndependentDecoder is not null && (string.IsNullOrWhiteSpace(design.IndependentDecoderName)
            || string.Equals(design.IndependentDecoderName!.Trim(), "CodeGlyphX", StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Provide the name of a distinct independent decoder implementation.", nameof(design));
        // Validate the supplied image before an encoding retry can turn an invalid logo into a no-fit error.
        Rendering.Png.PngDecoder.DecodeRgba32(logoPng, out _, out _);
    }

    private static QrLogoComposition? TryRenderLogo(QrCode qr, byte[] logoPng, QrLogoRenderOptions design, out string reason) {
        reason = "No readable logo fits the functional-pattern and per-block correction budgets.";
        var budget = FindLogoBudget(qr, design.MaximumSideModules, design.PaddingModules,
            design.ReservedErrorsPerBlock, design.MinimumLogoSideModules, design.Placement);
        if (budget is null) return null;
        var render = design.Appearance is null ? new QrRenderOptions { ModuleSize = 16 } : QrRenderer.CloneOptions(design.Appearance);
        render.LogoPng = null;
        QrRenderer.ResolveLogoSizing(render, qr);
        render.LogoPng = (byte[])logoPng.Clone();
        render.LogoScale = budget.LogoSideModules / (double)qr.Size;
        render.LogoPaddingPx = checked(budget.PaddingModules * render.ModuleSize);
        var maximum = checked(budget.LogoSideModules * render.ModuleSize);
        PngDecoder.DecodeRgba32(logoPng, out var sourceWidth, out var sourceHeight);
        var scale = Math.Min(maximum / (double)sourceWidth, maximum / (double)sourceHeight);
        var targetWidth = Math.Max(1, (int)Math.Round(sourceWidth * scale));
        var targetHeight = Math.Max(1, (int)Math.Round(sourceHeight * scale));
        render.LogoOffsetXPx = checked((budget.XModules + budget.PaddingModules) * render.ModuleSize + (maximum - targetWidth) / 2);
        render.LogoOffsetYPx = checked((budget.YModules + budget.PaddingModules) * render.ModuleSize + (maximum - targetHeight) / 2);
        render.LogoDrawBackground = true;
        if (!LogoHasVisibleSample(logoPng, checked(budget.LogoSideModules * render.ModuleSize),
            render.LogoBackground ?? render.Background)) {
            reason = "The logo has no visible sampled ink against its background. Supply visible artwork or choose a contrasting LogoBackground.";
            return null;
        }
        if (!QrDecoder.TryDecode(qr.Modules, out var expected)) {
            reason = "The source symbol does not contain a decodable payload.";
            return null;
        }
        var png = qr.Render(OutputFormat.Png, render).Data.ToArray();
        if (!ValidateImage(png, expected.Text).AllPassed) {
            reason = "The requested logo did not pass native image validation. Increase the module size or simplify the appearance.";
            return null;
        }
        var decoders = new List<string> { "CodeGlyphX" };
        if (design.IndependentDecoder is not null) {
            var independentText = design.IndependentDecoder((byte[])png.Clone());
            if (!string.Equals(expected.Text, independentText, StringComparison.Ordinal)) {
                reason = "The logo did not recover the exact payload with the independent decoder.";
                return null;
            }
            decoders.Add(design.IndependentDecoderName!.Trim());
        }
        var warnings = budget.CoversAlignment
            ? new[] { "The logo covers the central alignment pattern. Native and independent decoders verified this PNG; verify the intended printed or camera workflow separately." }
            : Array.Empty<string>();
        return new QrLogoComposition(qr, budget, png, warnings, decoders.ToArray(), render);
    }

    // Match the renderer's nearest-neighbor logo sampling so sparse/transparent artwork
    // cannot vanish during scaling while still receiving a successful placement report.
    private static bool LogoHasVisibleSample(byte[] png, int maximum, Rgba32 background) {
        var rgba = Rendering.Png.PngDecoder.DecodeRgba32(png, out var width, out var height);
        var scale = Math.Min(maximum / (double)width, maximum / (double)height);
        var targetWidth = Math.Max(1, (int)Math.Round(width * scale));
        var targetHeight = Math.Max(1, (int)Math.Round(height * scale));
        var visibleBackground = Rgba32Compositor.ComposeOver(background, Rgba32.White);
        for (var y = 0; y < targetHeight; y++) {
            for (var x = 0; x < targetWidth; x++) {
                var source = ((y * height / targetHeight) * width + x * width / targetWidth) * 4;
                var color = Rgba32Compositor.ComposeOver(new Rgba32(rgba[source], rgba[source + 1], rgba[source + 2], rgba[source + 3]), background);
                color = Rgba32Compositor.ComposeOver(color, Rgba32.White);
                if (color.R != visibleBackground.R || color.G != visibleBackground.G || color.B != visibleBackground.B) return true;
            }
        }
        return false;
    }
}
