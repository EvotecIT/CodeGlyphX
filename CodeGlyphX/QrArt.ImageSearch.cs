using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Art;

namespace CodeGlyphX;

public static partial class QrArt {
    /// <summary>
    /// Scores all eight masks for each selected version/ECC combination, decodes the strongest visual
    /// candidates, and returns measured alternatives. No network or AI service is used. Options must
    /// not be modified during this call. Cancellation is cooperative; image decoding follows imageOptions.
    /// </summary>
    public static QrImageSearchResult SearchImage(string payload, byte[] image, QrImageSearchOptions? options = null,
        ImageDecodeOptions? imageOptions = null, CancellationToken cancellationToken = default) =>
        SearchImageCoreAsync(payload, image, options, imageOptions, cancellationToken, false, null).GetAwaiter().GetResult();

    /// <summary>Searches with cooperative scheduling between candidates, including on single-threaded browser hosts.
    /// Progress reports the number of visually screened combinations. Exports are decoded at their requested resolution.</summary>
    public static Task<QrImageSearchResult> SearchImageAsync(string payload, byte[] image, QrImageSearchOptions? options = null,
        ImageDecodeOptions? imageOptions = null, CancellationToken cancellationToken = default, IProgress<int>? progress = null) =>
        SearchImageCoreAsync(payload, image, options, imageOptions, cancellationToken, true, progress);

    private static async Task<QrImageSearchResult> SearchImageCoreAsync(string payload, byte[] image, QrImageSearchOptions? options,
        ImageDecodeOptions? imageOptions, CancellationToken cancellationToken, bool yieldBetweenCandidates, IProgress<int>? progress) {
        if (payload is null) throw new ArgumentNullException(nameof(payload));
        if (image is null) throw new ArgumentNullException(nameof(image));
        options ??= new QrImageSearchOptions();
        options.Validate();
        cancellationToken.ThrowIfCancellationRequested();
        var source = ImageReader.DecodeRgba32(image, imageOptions, out var width, out var height);
        var candidates = new List<(QrCode Code, double Score, QrImageCompositionOptions Composition)>();
        // Screen at six pixels per module; only the shortlisted exports need full-resolution rasterization.
        var layouts = BuildImageLayouts(options);
        var levels = options.IncludeQuartileErrorCorrection
            ? new[] { QrErrorCorrectionLevel.H, QrErrorCorrectionLevel.Q } : new[] { QrErrorCorrectionLevel.H };
        foreach (var ecc in levels) {
            cancellationToken.ThrowIfCancellationRequested();
            var smallest = QR.Encode(payload, new QrEasyOptions { ErrorCorrectionLevel = ecc, RespectPayloadDefaults = false });
            for (var version = smallest.Version; version <= Math.Min(40, smallest.Version + options.AdditionalVersions); version++) {
                for (var mask = 0; mask < 8; mask++) {
                    cancellationToken.ThrowIfCancellationRequested();
                    var code = QR.Encode(payload, new QrEasyOptions { ErrorCorrectionLevel = ecc, RespectPayloadDefaults = false, MinVersion = version, MaxVersion = version, ForceMask = mask });
                    foreach (var layout in layouts) {
                        if (!CanRenderLayout(code, layout)) continue;
                        var screening = layout.WithModuleSize(6);
                        var rendered = QrImageComposer.Render(code, source, width, height, screening, cancellationToken);
                        candidates.Add((code, MeasureFidelity(rendered, source, width, height, screening, cancellationToken), layout));
                        progress?.Report(candidates.Count);
                        if (yieldBetweenCandidates) await Task.Delay(1, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }
        if (candidates.Count == 0) throw new ArgumentException("No candidate fits the configured rendering limits.", nameof(options));
        candidates.Sort((a, b) => {
            var score = b.Score.CompareTo(a.Score);
            if (score != 0) return score;
            var version = a.Code.Version.CompareTo(b.Code.Version);
            if (version != 0) return version;
            var ecc = b.Code.ErrorCorrectionLevel.CompareTo(a.Code.ErrorCorrectionLevel);
            return ecc != 0 ? ecc : a.Code.Mask.CompareTo(b.Code.Mask);
        });
        var measured = new List<(QrCode Code, double Fidelity, QrImageValidationReport Validation, QrImageCompositionOptions Composition)>();
        for (var i = 0; i < Math.Min(options.ValidationCandidates, candidates.Count); i++) {
            cancellationToken.ThrowIfCancellationRequested();
            var candidate = candidates[i];
            var rendered = QrImageComposer.Render(candidate.Code, source, width, height, candidate.Composition, cancellationToken);
            var checks = new List<QrImageValidationCheck>();
            foreach (var check in EnumerateValidationChecks(rendered.GetPixels(), rendered.Size, rendered.Size, payload, options.DecodeBudgetMilliseconds, cancellationToken)) {
                checks.Add(check);
                if (yieldBetweenCandidates) await Task.Delay(1, cancellationToken).ConfigureAwait(false);
            }
            measured.Add((candidate.Code, MeasureFidelity(rendered, source, width, height, candidate.Composition, cancellationToken), new QrImageValidationReport(checks.ToArray()), candidate.Composition));
            if (yieldBetweenCandidates) await Task.Delay(1, cancellationToken).ConfigureAwait(false);
        }
        measured.Sort((a, b) => {
            var passed = Passed(b.Validation).CompareTo(Passed(a.Validation));
            if (passed != 0) return passed;
            var fidelity = b.Fidelity.CompareTo(a.Fidelity);
            if (fidelity != 0) return fidelity;
            var version = a.Code.Version.CompareTo(b.Code.Version);
            if (version != 0) return version;
            var ecc = b.Code.ErrorCorrectionLevel.CompareTo(a.Code.ErrorCorrectionLevel);
            return ecc != 0 ? ecc : a.Code.Mask.CompareTo(b.Code.Mask);
        });
        var validated = measured.Count;
        if (measured.Count > options.Results) measured.RemoveRange(options.Results, measured.Count - options.Results);
        var results = new QrImageCandidate[measured.Count];
        for (var i = 0; i < results.Length; i++) {
            cancellationToken.ThrowIfCancellationRequested();
            var winner = measured[i];
            results[i] = new QrImageCandidate(winner.Code, QrImageComposer.Render(winner.Code, source, width, height, winner.Composition, cancellationToken), winner.Fidelity, winner.Validation, winner.Composition);
            if (yieldBetweenCandidates) await Task.Delay(1, cancellationToken).ConfigureAwait(false);
        }
        return new QrImageSearchResult(candidates.Count, validated, results);
    }

    private static int Passed(QrImageValidationReport report) {
        var count = 0;
        foreach (var check in report.Checks) if (check.Passed) count++;
        return count;
    }

    private static double MeasureFidelity(QrImageComposition image, byte[] source, int width, int height,
        QrImageCompositionOptions options, CancellationToken token) {
        var pixels = image.PixelSpan;
        var border = options.QuietZone * options.ModuleSize;
        var hasCanvas = options.Canvas is not null && options.Canvas.PaddingModules > 0;
        var area = hasCanvas ? image.Size : image.QrSize - 2 * border;
        var originX = hasCanvas ? 0 : image.QrOffsetX + border;
        var originY = hasCanvas ? 0 : image.QrOffsetY + border;
        var sampler = new QrImageSampler(source, width, height, area, options);
        double error = 0, weights = 0;
        // A fixed normalized grid makes differently sized versions comparable without retaining their rasters.
        const int samples = 96;
        for (var y = 0; y < samples; y++) {
            token.ThrowIfCancellationRequested();
            for (var x = 0; x < samples; x++) {
                var px = Math.Min(area - 1, (int)((x + 0.5) * area / samples));
                var py = Math.Min(area - 1, (int)((y + 0.5) * area / samples));
                sampler.Sample(px, py, out var r, out var g, out var b);
                var weight = 1 + 4 * sampler.Protection(px, py, options.Art?.Subject);
                var p = ((py + originY) * image.Size + px + originX) * 4;
                error += weight * (Math.Abs(r - pixels[p]) + Math.Abs(g - pixels[p + 1]) + Math.Abs(b - pixels[p + 2]));
                weights += weight;
            }
        }
        return 100 * (1 - error / (weights * 3 * 255));
    }
}
