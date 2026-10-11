#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Qr;

internal static partial class QrPixelDecoder {
    private static void CollectAllFromImage(int scale, QrGrayImage baseImage, QrProfileSettings settings, QrCollectionSink sink, DecodeBudget budget, QrGrayImagePool? pool) {
        if (budget.IsExpired) return;
        var candidates = RentCandidateList();
        try {
            // Multi-result decoding must recognize every symbol that the single-result path can
            // recognize. Run its conservative primary pass against the already-created gray image,
            // then continue collecting additional symbols within the same caller budget.
            if (Math.Max(baseImage.Width, baseImage.Height) <= 900) {
                if (TryDecodeFromGray(
                        scale,
                        baseImage.Threshold,
                        baseImage,
                        invert: false,
                        candidates,
                        sink.Accept,
                        aggressive: settings.AggressiveSampling,
                        stylized: settings.StylizedSampling,
                        budget,
                        out var primary,
                        out var primaryDiagnostics)) {
                    AddResult(sink.Results, sink.Seen, primary, sink.Accept);
                } else if (TryDecodeNormalizedPhotograph(scale, baseImage, settings, candidates, primaryDiagnostics,
                    budget, sink.Accept, pool, out var photograph, out _)) {
                    AddResult(sink.Results, sink.Seen, photograph, sink.Accept);
                }
                candidates.Clear();
                if (budget.IsExpired) return;
            }

            CollectAllFromImageCore(baseImage, settings, sink, budget, candidates, pool);
            if (settings.AllowNormalize) {
                if (budget.IsNearDeadline(150)) return;
                var normalized = baseImage.WithLocalNormalize(GetNormalizeWindow(baseImage), pool);
                if (!budget.IsExpired) {
                    CollectAllFromImageCore(normalized, settings, sink, budget, candidates, pool);
                }
            }
        } finally {
            ReturnCandidateList(candidates);
        }
    }

    private static void CollectAllFromImageCore(QrGrayImage baseImage, QrProfileSettings settings, QrCollectionSink sink, DecodeBudget budget, List<QrFinderPatternDetector.FinderPattern> candidates, QrGrayImagePool? pool) {
        Span<byte> thresholds = stackalloc byte[12];
        var thresholdCount = BuildThresholdCandidates(baseImage, settings, budget, ref thresholds);
        CollectGlobalThresholdPasses(baseImage, settings, sink, budget, candidates, thresholds, thresholdCount);
        if (!settings.AllowAdaptiveThreshold || budget.IsNearDeadline(150)) return;

        CollectHybridThresholdPasses(baseImage, settings, sink, budget, candidates, pool);
        if (budget.IsNearDeadline(150)) return;

        CollectAdaptiveThresholdPasses(baseImage, settings, sink, budget, candidates, pool);
    }

    private static int BuildThresholdCandidates(QrGrayImage baseImage, QrProfileSettings settings, DecodeBudget budget, ref Span<byte> thresholds) {
        var count = 0;
        if (settings.AllowExtraThresholds) {
            var mid = (baseImage.Min + baseImage.Max) / 2;
            var range = baseImage.Max - baseImage.Min;

            AddThresholdCandidate(ref thresholds, ref count, mid);
            AddThresholdCandidate(ref thresholds, ref count, baseImage.Threshold);
            AddThresholdCandidate(ref thresholds, ref count, mid - 16);
            AddThresholdCandidate(ref thresholds, ref count, mid + 16);
            if (settings.AggressiveSampling) {
                AddThresholdCandidate(ref thresholds, ref count, mid - 32);
                AddThresholdCandidate(ref thresholds, ref count, mid + 32);
            }
            if (count < thresholds.Length) {
                AddPercentileThresholds(baseImage, ref thresholds, ref count);
            }
            if (range > 0) {
                AddThresholdCandidate(ref thresholds, ref count, baseImage.Min + range / 3);
                AddThresholdCandidate(ref thresholds, ref count, baseImage.Min + (range * 2) / 3);
            }
        } else {
            AddThresholdCandidate(ref thresholds, ref count, baseImage.Threshold);
        }

        var thresholdLimit = GetBudgetThresholdLimit(budget);
        return Math.Min(count, thresholdLimit);
    }

    private static void CollectGlobalThresholdPasses(QrGrayImage baseImage, QrProfileSettings settings, QrCollectionSink sink, DecodeBudget budget, List<QrFinderPatternDetector.FinderPattern> candidates, Span<byte> thresholds, int thresholdCount) {
        for (var i = 0; i < thresholdCount; i++) {
            if (budget.IsExpired) return;
            var image = baseImage.WithThreshold(thresholds[i]);
            CollectFromImage(image, invert: false, sink.Results, sink.Seen, sink.Accept, candidates, budget, settings.AggressiveSampling, settings.StylizedSampling);
            CollectFromImage(image, invert: true, sink.Results, sink.Seen, sink.Accept, candidates, budget, settings.AggressiveSampling, settings.StylizedSampling);
        }
    }

    private static void CollectHybridThresholdPasses(QrGrayImage baseImage, QrProfileSettings settings, QrCollectionSink sink, DecodeBudget budget, List<QrFinderPatternDetector.FinderPattern> candidates, QrGrayImagePool? pool) {
        var hybrid = CreateHybridThresholdImage(baseImage, settings.AggressiveSampling, pool);
        CollectFromImage(hybrid, invert: false, sink.Results, sink.Seen, sink.Accept, candidates, budget, settings.AggressiveSampling, settings.StylizedSampling);
        CollectFromImage(hybrid, invert: true, sink.Results, sink.Seen, sink.Accept, candidates, budget, settings.AggressiveSampling, settings.StylizedSampling);
        if (budget.IsNearDeadline(150)) return;

        var hybridClosed = hybrid.WithBinaryClose(settings.AggressiveSampling ? 2 : 1, pool);
        CollectFromImage(hybridClosed, invert: false, sink.Results, sink.Seen, sink.Accept, candidates, budget, settings.AggressiveSampling, settings.StylizedSampling);
        var hybridOpen = hybrid.WithBinaryOpen(1, pool);
        CollectFromImage(hybridOpen, invert: false, sink.Results, sink.Seen, sink.Accept, candidates, budget, settings.AggressiveSampling, settings.StylizedSampling);
    }

    private static void CollectAdaptiveThresholdPasses(QrGrayImage baseImage, QrProfileSettings settings, QrCollectionSink sink, DecodeBudget budget, List<QrFinderPatternDetector.FinderPattern> candidates, QrGrayImagePool? pool) {
        var adaptive = baseImage.WithAdaptiveThreshold(windowSize: 15, offset: 8, pool);
        CollectFromImage(adaptive, invert: false, sink.Results, sink.Seen, sink.Accept, candidates, budget, settings.AggressiveSampling, settings.StylizedSampling);
        CollectFromImage(adaptive, invert: true, sink.Results, sink.Seen, sink.Accept, candidates, budget, settings.AggressiveSampling, settings.StylizedSampling);

        if (budget.IsExpired || !settings.AllowBlur || !settings.AggressiveSampling || budget.IsNearDeadline(150)) return;
        var adaptiveSoft = baseImage.WithAdaptiveThreshold(windowSize: 31, offset: 0, pool);
        CollectFromImage(adaptiveSoft, invert: false, sink.Results, sink.Seen, sink.Accept, candidates, budget, settings.AggressiveSampling, settings.StylizedSampling);
        CollectFromImage(adaptiveSoft, invert: true, sink.Results, sink.Seen, sink.Accept, candidates, budget, settings.AggressiveSampling, settings.StylizedSampling);
    }

    private readonly struct QrCollectionSink {
        public PooledList<QrDecoded> Results { get; }
        public HashSet<byte[]>? Seen { get; }
        public Func<QrDecoded, bool>? Accept { get; }

        public QrCollectionSink(PooledList<QrDecoded> results, HashSet<byte[]>? seen, Func<QrDecoded, bool>? accept) {
            Results = results;
            Seen = seen;
            Accept = accept;
        }
    }

}
#endif
