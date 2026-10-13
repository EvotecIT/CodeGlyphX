using System;
using System.Threading;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Aztec;

internal static partial class AztecPixelDecoder {
    private static bool TrySampleModules(AztecGrayImage image, bool invert, int size, int moduleSize, int offsetX, int offsetY, CancellationToken cancellationToken, out BitMatrixView modules, BullseyeProbeCache? probes = null) {
        modules = default;
        // Probe the required alternating central rings before sampling a full
        // candidate. Leave imperfect probes to the tolerant corner detector.
        var centerX = offsetX + size / 2 * moduleSize;
        var centerY = offsetY + size / 2 * moduleSize;
        if (probes is not null && probes.TryRead(moduleSize, centerX, centerY, out var passed)) {
            if (!passed) return false;
        } else {
            var errors = 0;
            var center = size / 2;
            for (var ring = 0; ring < 5; ring++) {
                var expected = (ring & 1) == 0;
                for (var axis = 0; axis < (ring == 0 ? 1 : 8); axis++) {
                    var x = center + (axis == 0 || axis == 4 || axis == 5 ? ring : axis == 1 || axis == 6 || axis == 7 ? -ring : 0);
                    var y = center + (axis == 2 || axis == 4 || axis == 6 ? ring : axis == 3 || axis == 5 || axis == 7 ? -ring : 0);
                    if (SampleModule(image, offsetX + x * moduleSize + moduleSize / 2,
                        offsetY + y * moduleSize + moduleSize / 2, moduleSize, invert) != expected && ++errors > 4) { probes?.Store(moduleSize, centerX, centerY, false); return false; }
                }
            }
            probes?.Store(moduleSize, centerX, centerY, true);
        }
        modules = CreateSampledView(image, invert, size, moduleSize, offsetX, offsetY, cancellationToken);
        return !DecodeBudget.ShouldAbort(cancellationToken);
    }

    // Keep the capture outside the probe method so rejected offsets allocate nothing.
    private static BitMatrixView CreateSampledView(AztecGrayImage image, bool invert,
        int size, int moduleSize, int offsetX, int offsetY, CancellationToken cancellationToken) {
        var centerOffset = moduleSize / 2;
        var samples = 0;
        return new BitMatrixView(size, size, (x, y) => {
            if ((samples++ & 255) == 0 && DecodeBudget.ShouldAbort(cancellationToken))
                throw new OperationCanceledException(cancellationToken);
            return SampleModule(image, offsetX + x * moduleSize + centerOffset,
                offsetY + y * moduleSize + centerOffset, moduleSize, invert);
        });
    }

    private static bool TryDecodeWithOffsets(AztecGrayImage image, bool invert, int size, int moduleSize, int offsetX, int offsetY, CancellationToken cancellationToken, out string value, BullseyeProbeCache? probes = null, AztecDecodeDiagnostics? diagnostics = null) {
        var maxShift = Math.Max(1, moduleSize / 2);
        // A large physical module must not multiply candidate count quadratically
        // by its pixel area. Nine offsets per axis cover the half-module interval.
        var shiftStep = Math.Max(1, (maxShift + 3) / 4);
        var budget = DecodeBudget.Capture();
        var candidates = 0;
        for (var dy = -maxShift; dy <= maxShift; dy += shiftStep) {
            for (var dx = -maxShift; dx <= maxShift; dx += shiftStep) {
                // A rejected central-ring probe reads at most 33 groups of nine pixels.
                // Cancellation stays live per offset; accepted views keep their own checks.
                if (cancellationToken.IsCancellationRequested ||
                    (candidates++ & 31) == 0 && budget.IsExpired) { value = string.Empty; return false; }
                if (!TrySampleModules(image, invert, size, moduleSize, offsetX + dx, offsetY + dy, cancellationToken, out var modules, probes)) {
                    continue;
                }
                if (TryDecodeSampledOrientations(modules, cancellationToken, out value, diagnostics)) return true;
            }
        }

        value = string.Empty;
        return false;
    }

    // Share sampling and inspect orientations through a view. Rejected framing
    // and metadata do not need eight copies of a candidate's payload grid.
    private static bool TryDecodeSampledOrientations(BitMatrixView modules, CancellationToken cancellationToken, out string value, AztecDecodeDiagnostics? diagnostics) {
        for (var mirror = 0; mirror < 2; mirror++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
            if (mirror != 0 && diagnostics is not null) diagnostics.MirroredTried = true;
            for (var rotation = 0; rotation < 4; rotation++) {
                if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
                if (AztecDecoder.TryDecode(modules.Orient(rotation, mirror != 0), cancellationToken, out value) && !DecodeBudget.ShouldAbort(cancellationToken)) return true;
            }
        }
        value = string.Empty;
        return false;
    }

    private static bool SampleModule(AztecGrayImage image, int px, int py, int moduleSize, bool invert) {
        var width = image.Width;
        var height = image.Height;
        var gray = image.Gray;
        var threshold = image.Threshold;

        // A 3x3 vote crosses the boundary of one- and two-pixel modules.
        // Preserve their centre sample; larger modules retain noise averaging.
        if (moduleSize < 3) {
            px = Math.Max(0, Math.Min(width - 1, px));
            py = Math.Max(0, Math.Min(height - 1, py));
            var dark = gray[py * width + px] <= threshold;
            return invert ? !dark : dark;
        }

        var black = 0;
        // Interior modules need the same nine votes without repeated edge clamps.
        // Keep clamping below for candidates whose samples reach an image edge.
        if (px > 0 && px < width - 1 && py > 0 && py < height - 1) {
            var at = py * width + px;
            black = (gray[at - width - 1] <= threshold ? 1 : 0)
                + (gray[at - width] <= threshold ? 1 : 0)
                + (gray[at - width + 1] <= threshold ? 1 : 0)
                + (gray[at - 1] <= threshold ? 1 : 0)
                + (gray[at] <= threshold ? 1 : 0)
                + (gray[at + 1] <= threshold ? 1 : 0)
                + (gray[at + width - 1] <= threshold ? 1 : 0)
                + (gray[at + width] <= threshold ? 1 : 0)
                + (gray[at + width + 1] <= threshold ? 1 : 0);
            return invert ? black < 5 : black >= 5;
        }
        if (!invert) {
            for (var dy = -1; dy <= 1; dy++) {
                var y = py + dy;
                if (y < 0) y = 0;
                else if (y >= height) y = height - 1;
                var row = y * width;

                for (var dx = -1; dx <= 1; dx++) {
                    var x = px + dx;
                    if (x < 0) x = 0;
                    else if (x >= width) x = width - 1;

                    if (gray[row + x] <= threshold) black++;
                }
            }
        } else {
            for (var dy = -1; dy <= 1; dy++) {
                var y = py + dy;
                if (y < 0) y = 0;
                else if (y >= height) y = height - 1;
                var row = y * width;

                for (var dx = -1; dx <= 1; dx++) {
                    var x = px + dx;
                    if (x < 0) x = 0;
                    else if (x >= width) x = width - 1;

                    if (gray[row + x] > threshold) black++;
                }
            }
        }

        return black >= 5;
    }

}
