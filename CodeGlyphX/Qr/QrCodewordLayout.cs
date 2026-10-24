using System;

namespace CodeGlyphX.Qr;

/// <summary>Maps placed data/parity bits to their interleaved Reed-Solomon blocks.
/// Remainder modules have no codeword; functional modules remain separate.</summary>
internal sealed class QrCodewordLayout {
    internal BitMatrix Functions { get; }
    internal int[] ModuleCodewords { get; }
    internal int[] CodewordBlocks { get; }
    internal int BlockCount { get; }
    internal int ParityPerBlock { get; }

    internal QrCodewordLayout(QrCode qr) {
        var size = qr.Size;
        Functions = QrStructureAnalysis.BuildFunctionMask(qr.Version, size);
        BlockCount = QrTables.GetNumBlocks(qr.Version, qr.ErrorCorrectionLevel);
        ParityPerBlock = QrTables.GetEccCodewordsPerBlock(qr.Version, qr.ErrorCorrectionLevel);
        var words = QrTables.GetNumRawDataModules(qr.Version) / 8;
        CodewordBlocks = new int[words];
        var shortBlocks = BlockCount - words % BlockCount;
        var shortData = words / BlockCount - ParityPerBlock;
        var cursor = 0;
        // Data columns are interleaved first. Only long blocks have the extra data column.
        for (var column = 0; column <= shortData; column++)
            for (var block = 0; block < BlockCount; block++)
                if (column < shortData || block >= shortBlocks) CodewordBlocks[cursor++] = block;
        for (var column = 0; column < ParityPerBlock; column++)
            for (var block = 0; block < BlockCount; block++) CodewordBlocks[cursor++] = block;
        if (cursor != words) throw new InvalidOperationException("QR interleaved block layout is inconsistent.");

        ModuleCodewords = new int[size * size];
        for (var i = 0; i < ModuleCodewords.Length; i++) ModuleCodewords[i] = -1;
        var bit = 0;
        var upward = true;
        for (var right = size - 1; right >= 1; right -= 2) {
            if (right == 6) right = 5;
            for (var row = 0; row < size; row++) {
                var y = upward ? size - 1 - row : row;
                for (var column = 0; column < 2; column++) {
                    var x = right - column;
                    if (Functions[x, y]) continue;
                    if (bit < words * 8) ModuleCodewords[y * size + x] = bit / 8;
                    bit++;
                }
            }
            upward = !upward;
        }
        if (bit != QrTables.GetNumRawDataModules(qr.Version))
            throw new InvalidOperationException("QR placed-module layout is inconsistent.");
    }
}
