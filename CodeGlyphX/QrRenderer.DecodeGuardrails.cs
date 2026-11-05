using System;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX;

internal static partial class QrRenderer {
    // Recognition is deliberately separate from BuildPngOptions: static heuristic reports
    // must stay inexpensive and must not claim to validate a rendered artifact.
    private static QrPngRenderOptions BuildGuardedPngOptions(QrRenderOptions opts, QrCode qr) {
        var render = BuildPngOptions(opts, qr);
        if (!opts.ArtGuardrailsEnabled || !HasArtHints(render, opts)) return render;

        var pixels = QrPngRenderer.RenderPixels(qr.Modules, render, out var width, out var height, out var stride);
        VerifyRenderedArt(qr, pixels, width, height, stride);
        return render;
    }

    // PNG output validates the pixels it encodes instead of rendering the appearance a second time.
    private static byte[] RenderGuardedPng(QrRenderOptions opts, QrCode qr, OutputOptions? outputOptions) {
        var render = BuildPngOptions(opts, qr);
        if (outputOptions is not null) render.PngCompressionLevel = outputOptions.ResolvePngCompression(render.PngCompressionLevel);
        if (!opts.ArtGuardrailsEnabled || !HasArtHints(render, opts)) return QrPngRenderer.Render(qr.Modules, render);
        return QrPngRenderer.Render(qr.Modules, render, (pixels, width, height, stride) => VerifyRenderedArt(qr, pixels, width, height, stride));
    }

    /// <summary>Checks canonical appearance pixels with the native decoder's standard profile.
    /// No transforms, stylized sampling or independent-reader dependencies are used.</summary>
    private static void VerifyRenderedArt(QrCode qr, byte[] pixels, int width, int height, int stride) {
        for (var p = 3; p < pixels.Length; p += 4) {
            if (pixels[p] == 255) continue;
            pixels = QrArt.CompositeValidationPixelsOnWhite((byte[])pixels.Clone());
            break;
        }
        var options = new QrPixelDecodeOptions {
            Profile = QrDecodeProfile.Balanced,
            DisableTransforms = true,
            BudgetMilliseconds = 1000,
        };
        if (!QrDecoder.TryDecode(qr.Modules, out var expected)
            || !QrImageDecoder.TryDecode(pixels, width, height, stride, PixelFormat.Rgba32, options, out var actual)
            || !SamePayload(expected, actual)) {
            throw new InvalidOperationException("The styled QR appearance did not pass native decode validation. "
                + "Increase the output size or simplify the artwork. For decorative output, disable ArtGuardrailsEnabled and verify the exported image separately.");
        }
    }

    private static bool SamePayload(QrDecoded expected, QrDecoded actual) {
        if (expected.Bytes.Length != actual.Bytes.Length
            || !string.Equals(expected.Text, actual.Text, StringComparison.Ordinal)
            || expected.Fnc1Mode != actual.Fnc1Mode
            || expected.Fnc1ApplicationIndicator != actual.Fnc1ApplicationIndicator) return false;
        for (var i = 0; i < expected.Bytes.Length; i++)
            if (expected.Bytes[i] != actual.Bytes[i]) return false;
        return true;
    }
}
