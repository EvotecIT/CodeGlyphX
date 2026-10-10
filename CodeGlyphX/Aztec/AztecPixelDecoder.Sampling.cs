using System;
using System.Collections.Generic;
using System.Threading;
using CodeGlyphX.Internal;

namespace CodeGlyphX.Aztec;

internal static partial class AztecPixelDecoder {
    private static bool TrySampleModules(AztecGrayImage image, bool invert, int size, int moduleSize, int offsetX, int offsetY, CancellationToken cancellationToken, out BitMatrix modules, Dictionary<(int Pitch, int X, int Y), bool>? probes = null) {
        modules = null!;
        // Probe the required alternating central rings before sampling a full
        // candidate. Leave imperfect probes to the tolerant corner detector.
        var key = (moduleSize, offsetX + size / 2 * moduleSize, offsetY + size / 2 * moduleSize);
        if (probes is not null && probes.TryGetValue(key, out var passed)) {
            if (!passed) return false;
        } else {
            var errors = 0;
            var center = size / 2;
            for (var ring = 0; ring < 5; ring++) {
                var expected = (ring & 1) == 0;
                for (var axis = 0; axis < (ring == 0 ? 1 : 8); axis++) {
                    var x = center + (axis == 0 || axis == 4 || axis == 5 ? ring : axis == 1 || axis == 6 || axis == 7 ? -ring : 0);
                    var y = center + (axis == 2 || axis == 4 || axis == 6 ? ring : axis == 3 || axis == 5 || axis == 7 ? -ring : 0);
                    if (SampleMajority3x3(image, offsetX + x * moduleSize + moduleSize / 2,
                        offsetY + y * moduleSize + moduleSize / 2, invert) != expected && ++errors > 4) { if (probes is not null) probes[key] = false; return false; }
                }
            }
            if (probes is not null) probes[key] = true;
        }
        modules = new BitMatrix(size, size);

        var centerOffset = moduleSize / 2;
        for (var y = 0; y < size; y++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
            var sy = offsetY + y * moduleSize + centerOffset;
            for (var x = 0; x < size; x++) {
                if (DecodeBudget.ShouldAbort(cancellationToken)) return false;
                var sx = offsetX + x * moduleSize + centerOffset;
                var black = SampleMajority3x3(image, sx, sy, invert);
                modules.Set(x, y, black);
            }
        }

        return true;
    }

    private static bool TryDecodeWithOffsets(AztecGrayImage image, bool invert, int size, int moduleSize, int offsetX, int offsetY, CancellationToken cancellationToken, out string value, Dictionary<(int Pitch, int X, int Y), bool>? probes = null, AztecDecodeDiagnostics? diagnostics = null) {
        var maxShift = Math.Max(1, moduleSize / 2);
        // A large physical module must not multiply candidate count quadratically
        // by its pixel area. Nine offsets per axis cover the half-module interval.
        var shiftStep = Math.Max(1, (maxShift + 3) / 4);
        for (var dy = -maxShift; dy <= maxShift; dy += shiftStep) {
            for (var dx = -maxShift; dx <= maxShift; dx += shiftStep) {
                if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
                if (!TrySampleModules(image, invert, size, moduleSize, offsetX + dx, offsetY + dy, cancellationToken, out var modules, probes)) {
                    continue;
                }
                if (TryDecodeSampledOrientations(modules, cancellationToken, out value, diagnostics)) return true;
            }
        }

        value = string.Empty;
        return false;
    }

    // Rotate small sampled grids rather than resampling the full image for each
    // orientation. Ring probes and pixel sampling are shared by all orientations.
    private static bool TryDecodeSampledOrientations(BitMatrix modules, CancellationToken cancellationToken, out string value, AztecDecodeDiagnostics? diagnostics) {
        for (var mirror = 0; mirror < 2; mirror++) {
            if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
            if (mirror != 0 && diagnostics is not null) diagnostics.MirroredTried = true;
            var candidate = mirror == 0 ? modules : MirrorSampledGrid(modules);
            for (var rotation = 0; rotation < 4; rotation++) {
                if (DecodeBudget.ShouldAbort(cancellationToken)) { value = string.Empty; return false; }
                if (AztecDecoder.TryDecode(candidate, cancellationToken, out value) && !DecodeBudget.ShouldAbort(cancellationToken)) return true;
                if (rotation != 3) candidate = RotateSampledGrid(candidate);
            }
        }
        value = string.Empty;
        return false;
    }

    private static BitMatrix RotateSampledGrid(BitMatrix source) {
        var result = new BitMatrix(source.Height, source.Width);
        for (var y = 0; y < source.Height; y++)
            for (var x = 0; x < source.Width; x++) result[source.Height - 1 - y, x] = source[x, y];
        return result;
    }

    private static BitMatrix MirrorSampledGrid(BitMatrix source) {
        var result = new BitMatrix(source.Width, source.Height);
        for (var y = 0; y < source.Height; y++)
            for (var x = 0; x < source.Width; x++) result[source.Width - 1 - x, y] = source[x, y];
        return result;
    }

    private static bool SampleMajority3x3(AztecGrayImage image, int px, int py, bool invert) {
        var width = image.Width;
        var height = image.Height;
        var gray = image.Gray;
        var threshold = image.Threshold;

        var black = 0;
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
