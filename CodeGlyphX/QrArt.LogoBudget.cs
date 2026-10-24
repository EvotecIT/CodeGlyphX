using System;
using CodeGlyphX.Qr;
using CodeGlyphX.Rendering.Art;

namespace CodeGlyphX;

public static partial class QrArt {
    /// <summary>Finds the largest pattern-free square logo knockout with its center in the
    /// central third of the QR core, choosing the location
    /// nearest the center among equally sized candidates. Padding counts against each
    /// interleaved block's unknown-error budget. Functional patterns remain intact.
    /// Throws when a readable logo cannot fit; a requested logo is never silently omitted.</summary>
    /// <param name="qr">Encoded symbol whose blocks determine the correction budget.</param>
    /// <param name="maximumSideModules">Maximum knockout side including padding; zero uses the QR size.</param>
    /// <param name="paddingModules">Padding on each side of the logo.</param>
    /// <param name="reservedErrorsPerBlock">Unknown errors reserved in each block for other damage.</param>
    /// <param name="minimumLogoSideModules">Minimum readable logo side, excluding padding.</param>
    public static QrLogoBudget FitLogo(QrCode qr, int maximumSideModules = 0,
        int paddingModules = 1, int reservedErrorsPerBlock = 2, int minimumLogoSideModules = 3) {
        return FindLogoBudget(qr, maximumSideModules, paddingModules, reservedErrorsPerBlock,
            minimumLogoSideModules, LogoPlacement.Auto)
            ?? throw new InvalidOperationException("No readable logo fits the functional-pattern and per-block correction budgets. "
                + "Allow a higher correction level or QR version, reduce the minimum logo size, or choose a different placement explicitly.");
    }

    private static QrLogoBudget? FindLogoBudget(QrCode qr, int maximumSideModules,
        int paddingModules, int reservedErrorsPerBlock, int minimumLogoSideModules, LogoPlacement placement) {
        if (qr is null) throw new ArgumentNullException(nameof(qr));
        if (maximumSideModules < 0) throw new ArgumentOutOfRangeException(nameof(maximumSideModules));
        if (paddingModules < 0 || paddingModules > qr.Size) throw new ArgumentOutOfRangeException(nameof(paddingModules));
        if (reservedErrorsPerBlock < 0) throw new ArgumentOutOfRangeException(nameof(reservedErrorsPerBlock));
        if (minimumLogoSideModules < 1 || minimumLogoSideModules > 177)
            throw new ArgumentOutOfRangeException(nameof(minimumLogoSideModules));
        if (placement is < LogoPlacement.Auto or > LogoPlacement.CenterOverAlignment)
            throw new ArgumentOutOfRangeException(nameof(placement));
        var layout = new QrCodewordLayout(qr);
        var correctable = layout.ParityPerBlock / 2;
        var allowance = correctable - reservedErrorsPerBlock;
        if (allowance < 0) return null;
        var counts = new int[layout.BlockCount];
        var stamps = new int[layout.CodewordBlocks.Length];
        var stamp = 0;
        var size = qr.Size;
        var center = size / 2;
        var centerAlignment = Array.IndexOf(QrTables.GetAlignmentPatternPositions(qr.Version), center) >= 0;
        var prefixSide = size + 1;
        var prefix = new int[prefixSide * prefixSide];
        for (var y = 0; y < size; y++) {
            for (var x = 0; x < size; x++) {
                var allowedAlignment = placement == LogoPlacement.CenterOverAlignment && centerAlignment
                    && Math.Abs(x - center) <= 2 && Math.Abs(y - center) <= 2;
                var blocked = layout.Functions[x, y] && !allowedAlignment ? 1 : 0;
                prefix[(y + 1) * prefixSide + x + 1] = blocked + prefix[y * prefixSide + x + 1]
                    + prefix[(y + 1) * prefixSide + x] - prefix[y * prefixSide + x];
            }
        }
        var maximum = maximumSideModules == 0 ? size : Math.Min(maximumSideModules, size);
        if ((maximum & 1) == 0) maximum--;
        var minimum = minimumLogoSideModules + paddingModules * 2;
        for (var side = maximum; side >= minimum; side -= 2) {
            var bestDistance = int.MaxValue;
            QrLogoBudget? best = null;
            var first = placement == LogoPlacement.CenterOverAlignment ? (size - side) / 2 : 0;
            var last = placement == LogoPlacement.CenterOverAlignment ? first : size - side;
            for (var y = first; y <= last; y++) {
                for (var x = first; x <= last; x++) {
                    var dx = x * 2 + side - size;
                    var dy = y * 2 + side - size;
                    // A larger gap at an edge is not a central brand mark.
                    if (Math.Abs(dx) > size / 3 || Math.Abs(dy) > size / 3) continue;
                    var distance = dx * dx + dy * dy;
                    if (distance >= bestDistance || Blocked(x, y, side) != 0 || !Fits(x, y, side)) continue;
                    var coversAlignment = centerAlignment && x <= center + 2 && y <= center + 2
                        && x + side > center - 2 && y + side > center - 2;
                    best = new QrLogoBudget(x, y, side, paddingModules, correctable,
                        reservedErrorsPerBlock, counts, placement, coversAlignment);
                    bestDistance = distance;
                }
            }
            if (best is not null) return best;
        }
        return null;

        int Blocked(int x, int y, int side) => prefix[(y + side) * prefixSide + x + side]
            - prefix[y * prefixSide + x + side] - prefix[(y + side) * prefixSide + x] + prefix[y * prefixSide + x];

        bool Fits(int left, int top, int side) {
            Array.Clear(counts, 0, counts.Length);
            stamp++;
            for (var y = top; y < top + side; y++) {
                for (var x = left; x < left + side; x++) {
                    var word = layout.ModuleCodewords[y * size + x];
                    if (word < 0 || stamps[word] == stamp) continue;
                    stamps[word] = stamp;
                    if (++counts[layout.CodewordBlocks[word]] > allowance) return false;
                }
            }
            return true;
        }
    }
}
