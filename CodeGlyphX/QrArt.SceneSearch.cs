using System;
using System.Threading;
using System.Threading.Tasks;
using CodeGlyphX.Rendering.Art;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX;

public static partial class QrArt {
    /// <summary>Chooses a scene using actual PNG delivery reports, with a bounded sequence of larger QR
    /// placements and optional square modules. Illustrations, payload, seed and other layers are retained.
    /// A best candidate with failed checks is returned honestly; this does not certify phones or printers.</summary>
    public static QrSceneSearchResult SearchScene(string payload, QrSceneOptions? design = null, QrSceneSearchOptions? options = null, CancellationToken cancellationToken = default) =>
        SearchSceneCoreAsync(payload, design, options, cancellationToken, false).GetAwaiter().GetResult();

    /// <summary>Runs delivery-aware selection with cooperative scheduling between reports for browser hosts.</summary>
    public static Task<QrSceneSearchResult> SearchSceneAsync(string payload, QrSceneOptions? design = null, QrSceneSearchOptions? options = null, CancellationToken cancellationToken = default) =>
        SearchSceneCoreAsync(payload, design, options, cancellationToken, true);

    private static async Task<QrSceneSearchResult> SearchSceneCoreAsync(string payload, QrSceneOptions? design,
        QrSceneSearchOptions? options, CancellationToken token, bool yieldBetweenChecks) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        var settings = (options ?? new QrSceneSearchOptions()).Copy();
        var original = (design ?? QrScenePresets.Create(QrSceneStyle.TropicalGarden)).Clone();
        if (settings.MaxQrScale < original.Qr.Scale) throw new ArgumentException("Maximum QR scale cannot shrink the requested QR.", nameof(options));
        token.ThrowIfCancellationRequested();
        var code = QrCode.Encode(payload, new QrEasyOptions { ErrorCorrectionLevel = QrErrorCorrectionLevel.H });
        QrSceneCandidate? best = null; var attempted = 0; var validated = 0;
        // Original, halfway enlargement, maximum enlargement; then the same sizes with square data modules.
        for (var shapeIndex = 0; shapeIndex < 2; shapeIndex++) {
            if (shapeIndex == 1 && (!settings.AllowSquareFallback || original.ModuleShape == QrPngModuleShape.Square)) break;
            for (var scaleIndex = 0; scaleIndex < 3; scaleIndex++) {
                if (attempted >= settings.MaxCandidates) break;
                if (scaleIndex > 0 && settings.MaxQrScale == original.Qr.Scale) continue;
                var candidateDesign = original.Clone();
                candidateDesign.Qr.Scale = original.Qr.Scale + (settings.MaxQrScale - original.Qr.Scale) * scaleIndex / 2;
                if (shapeIndex == 1) candidateDesign.ModuleShape = QrPngModuleShape.Square;
                attempted++; token.ThrowIfCancellationRequested();
                var qrSize = (int)Math.Floor(candidateDesign.Size * candidateDesign.Qr.Scale / (code.Size + 8)) * (code.Size + 8);
                if (scaleIndex > 0 && !TryGetScenePlacement(candidateDesign, qrSize, out _, out _)) continue;
                var scene = ComposeSceneCore(payload, code, candidateDesign, token);
                var png = scene.ToPng();
                var report = yieldBetweenChecks
                    ? await ValidateDeliveryAsync(png, payload, settings.Delivery, cancellationToken: token).ConfigureAwait(false)
                    : ValidateDelivery(png, payload, settings.Delivery, cancellationToken: token);
                token.ThrowIfCancellationRequested(); validated++;
                var candidate = new QrSceneCandidate(scene, report);
                if (best is null || candidate.PassedChecks > best.PassedChecks) best = candidate;
                if (best.Validation.AllPassed) return new QrSceneSearchResult(best, attempted, validated);
            }
        }
        return new QrSceneSearchResult(best!, attempted, validated);
    }
}
