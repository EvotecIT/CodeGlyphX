using System;
using CodeGlyphX.Qr;
using CodeGlyphX.Rendering.Art;

namespace CodeGlyphX;

public static partial class QrArt {
    /// <summary>Finds the largest centered odd-module square logo knockout permitted by
    /// each Reed-Solomon block's unknown-error budget. Padding is included in the damage.
    /// No finder, timing, alignment, format or version module may be covered.
    /// A centered alignment pattern can prevent any fit; check <see cref="QrLogoBudget.CanPlace"/>.
    /// Render with integer module sizing, logo scale LogoSideModules / qr.Size and padding
    /// PaddingModules * moduleSize. Subsequent artwork or resampling still needs scan validation.</summary>
    /// <param name="qr">Encoded symbol whose version and correction level determine the blocks.</param>
    /// <param name="maximumSideModules">Maximum knockout side, including padding; zero uses the symbol size.</param>
    /// <param name="paddingModules">Padding on each side of the logo.</param>
    /// <param name="reservedErrorsPerBlock">Unknown errors reserved per block outside the knockout.</param>
    public static QrLogoBudget FitLogo(QrCode qr, int maximumSideModules = 0,
        int paddingModules = 1, int reservedErrorsPerBlock = 2) {
        if (qr is null) throw new ArgumentNullException(nameof(qr));
        if (maximumSideModules < 0) throw new ArgumentOutOfRangeException(nameof(maximumSideModules));
        if (paddingModules < 0 || paddingModules > qr.Size)
            throw new ArgumentOutOfRangeException(nameof(paddingModules));
        if (reservedErrorsPerBlock < 0) throw new ArgumentOutOfRangeException(nameof(reservedErrorsPerBlock));

        var layout = new QrCodewordLayout(qr);
        var correctable = layout.ParityPerBlock / 2;
        var allowance = correctable - reservedErrorsPerBlock;
        var counts = new int[layout.BlockCount];
        var seen = new bool[layout.CodewordBlocks.Length];
        var maximum = maximumSideModules == 0 ? qr.Size : Math.Min(maximumSideModules, qr.Size);
        if ((maximum & 1) == 0) maximum--;
        if (allowance >= 0) {
            for (var side = maximum; side > paddingModules * 2; side -= 2) {
                Array.Clear(counts, 0, counts.Length);
                Array.Clear(seen, 0, seen.Length);
                var offset = (qr.Size - side) / 2;
                if (Fits(offset, side)) return new QrLogoBudget(offset, side, paddingModules,
                    correctable, reservedErrorsPerBlock, counts);
            }
        }
        return new QrLogoBudget(0, 0, paddingModules, correctable, reservedErrorsPerBlock, new int[layout.BlockCount]);

        bool Fits(int offset, int side) {
            for (var y = offset; y < offset + side; y++) {
                for (var x = offset; x < offset + side; x++) {
                    if (layout.Functions[x, y]) return false;
                    var word = layout.ModuleCodewords[y * qr.Size + x];
                    if (word < 0 || seen[word]) continue;
                    seen[word] = true;
                    var block = layout.CodewordBlocks[word];
                    if (++counts[block] > allowance) return false;
                }
            }
            return true;
        }
    }
}
