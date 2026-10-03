using System;
using System.Threading;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Art;

namespace CodeGlyphX;

public static partial class QrArt {
    /// <summary>
    /// Composes seeded procedural artwork with a high-error-correction QR code. No source image,
    /// model, network access or external graphics dependency is required. Validate the final export
    /// at its delivery size. Caller options are not modified or retained.
    /// </summary>
    public static QrImageComposition ComposePattern(string payload, QrArtPatternOptions? pattern = null,
        QrImageCompositionOptions? composition = null, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        pattern ??= new QrArtPatternOptions();
        composition ??= new QrImageCompositionOptions {
            ModuleSize = 16, Strength = 0.95,
            Canvas = new QrImageCanvasOptions { PaddingModules = 10 },
            Art = new QrImageArtOptions { Scale = 0.95, DetailProtection = 0.35, Finders = QrImageFinderStyle.Rounded }
        };
        composition.Validate();
        var rasterizer = new QrPatternRasterizer(pattern);
        var qr = QrCode.Encode(payload, new QrEasyOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
        var side = (qr.Size + 2 * composition.QuietZone + 2 * (composition.Canvas?.PaddingModules ?? 0)) * composition.ModuleSize;
        RenderGuards.EnsureOutputPixels(side, side, "Procedural QR composition exceeds output limits.");
        RenderGuards.EnsureOutputBytes((long)side * side * 4, "Procedural QR composition exceeds output limits.");
        var sourceSize = Math.Min(512, side);
        var source = rasterizer.Draw(sourceSize, cancellationToken);
        return QrImageComposer.Render(qr, source, sourceSize, sourceSize, composition, cancellationToken);
    }
}
