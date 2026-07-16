// Portions adapted from the Zint backend, Copyright (c) Robin Stuart and contributors.
// Licensed under BSD-3-Clause; see THIRD-PARTY-NOTICES.md.

using System;
using CodeGlyphX.Internal.ReedSolomon;

namespace CodeGlyphX.HanXin;

internal static class HanXinMatrixCodec {
    internal static int Evaluate(BitMatrix matrix) {
        var size = matrix.Width;
        var result = 0;
        for (var x = 0; x < size; x++) {
            for (var y = 0; y <= size - 7; y++) {
                if (matrix[x, y] && matrix[x, y + 1] != matrix[x, y + 5] && matrix[x, y + 2] &&
                    !matrix[x, y + 3] && matrix[x, y + 4] && matrix[x, y + 6]) {
                    if (HasThreeLightBefore(matrix, x, y, vertical: true) || HasThreeLightAfter(matrix, x, y + 7, vertical: true)) result += 50;
                    y++;
                }
            }
        }
        for (var y = 0; y < size; y++) {
            for (var x = 0; x <= size - 7; x++) {
                if (MatchesFinderLike(matrix, x, y)) {
                    if (HasThreeLightBefore(matrix, x, y, vertical: false) || HasThreeLightAfter(matrix, x + 7, y, vertical: false)) result += 50;
                    x++;
                }
            }
        }
        for (var x = 0; x < size; x++) result += RunPenalty(matrix, x, vertical: true);
        for (var y = 0; y < size; y++) result += RunPenalty(matrix, y, vertical: false);
        return result;
    }

    internal static BitMatrix Encode(byte[] data, int version, int eccLevel, int mask) {
        var size = version * 2 + 21;
        var grid = SetupGrid(version);
        var dataCapacity = HanXinTables.DataCodewords(version, eccLevel);
        var datastream = new byte[dataCapacity];
        Array.Copy(data, datastream, data.Length);
        var fullstream = AddEcc(datastream, version, eccLevel);
        var picket = MakePicketFence(fullstream);
        var bit = 0;
        for (var i = 0; i < grid.Length && bit < picket.Length * 8; i++) {
            if (grid[i] != 0) continue;
            if ((picket[bit >> 3] & 0x80 >> (bit & 7)) != 0) grid[i] = 0x01;
            bit++;
        }
        ApplyMask(grid, size, mask);
        SetFunctionInfo(grid, size, version, eccLevel, mask);
        return ToMatrix(grid, size);
    }

    internal static bool TryDecode(BitMatrix modules, out byte[] data, out int version, out int eccLevel, out int mask) {
        data = Array.Empty<byte>(); version = eccLevel = mask = 0;
        if (modules is null || modules.Width != modules.Height || modules.Width is < 23 or > 189 || ((modules.Width - 21) & 1) != 0) return false;
        version = (modules.Width - 21) / 2;
        if (version is < 1 or > 84 || !TryReadFunctionInfo(modules, version, out eccLevel, out mask)) return false;
        var size = modules.Width;
        var grid = SetupGrid(version);
        var unmasked = modules.Clone();
        ApplyMask(unmasked, grid, mask);
        var total = HanXinTables.TotalCodewords(version);
        var picket = new byte[total];
        var bit = 0;
        for (var i = 0; i < grid.Length && bit < total * 8; i++) {
            if (grid[i] != 0) continue;
            if (unmasked[i % size, i / size]) picket[bit >> 3] |= (byte)(0x80 >> (bit & 7));
            bit++;
        }
        if (bit != total * 8) return false;
        var fullstream = UndoPicketFence(picket);
        return TryCorrectAndExtract(fullstream, version, eccLevel, out data);
    }

    private static byte[] AddEcc(byte[] datastream, int version, int eccLevel) {
        var full = new byte[HanXinTables.TotalCodewords(version)];
        var field = GenericGf.HanXinData8;
        var input = 0;
        var output = 0;
        for (var batch = 0; batch < 3; batch++) {
            HanXinTables.GetBlockBatch(version, eccLevel, batch, out var count, out var dataLength, out var eccLength);
            for (var blockIndex = 0; blockIndex < count; blockIndex++) {
                var block = new int[dataLength + eccLength];
                for (var i = 0; i < dataLength; i++) {
                    block[i] = input < datastream.Length ? datastream[input] : 0;
                    full[output++] = (byte)block[i]; input++;
                }
                new ReedSolomonEncoder(field).Encode(block, eccLength);
                for (var i = 0; i < eccLength; i++) full[output++] = (byte)block[dataLength + i];
            }
        }
        if (output != full.Length) throw new InvalidOperationException("Han Xin block table does not match symbol capacity.");
        return full;
    }

    private static bool TryCorrectAndExtract(byte[] full, int version, int eccLevel, out byte[] data) {
        data = new byte[HanXinTables.DataCodewords(version, eccLevel)];
        var input = 0;
        var output = 0;
        try {
            for (var batch = 0; batch < 3; batch++) {
                HanXinTables.GetBlockBatch(version, eccLevel, batch, out var count, out var dataLength, out var eccLength);
                for (var blockIndex = 0; blockIndex < count; blockIndex++) {
                    var block = new int[dataLength + eccLength];
                    for (var i = 0; i < block.Length; i++) block[i] = full[input++];
                    new ReedSolomonDecoder(GenericGf.HanXinData8).Decode(block, eccLength);
                    for (var i = 0; i < dataLength && output < data.Length; i++) data[output++] = (byte)block[i];
                }
            }
            return input == full.Length && output == data.Length;
        } catch (ReedSolomonException) { data = Array.Empty<byte>(); return false; }
    }

    private static byte[] MakePicketFence(byte[] full) {
        var result = new byte[full.Length];
        var output = 0;
        for (var start = 0; start < 13; start++) for (var i = start; i < full.Length; i += 13) result[output++] = full[i];
        return result;
    }

    private static byte[] UndoPicketFence(byte[] picket) {
        var result = new byte[picket.Length];
        var input = 0;
        for (var start = 0; start < 13; start++) for (var i = start; i < result.Length; i += 13) result[i] = picket[input++];
        return result;
    }

    private static byte[] SetupGrid(int version) {
        var size = version * 2 + 21;
        var grid = new byte[size * size];
        PlaceTopLeft(grid, size);
        PlaceFinder(grid, size, 0, size - 7);
        PlaceFinder(grid, size, size - 7, 0);
        PlaceBottomRight(grid, size);
        for (var i = 0; i < 8; i++) {
            Function(grid, size, i, 7); Function(grid, size, 7, i);
            Function(grid, size, size - i - 1, 7); Function(grid, size, 7, size - i - 1);
            Function(grid, size, size - 8, i); Function(grid, size, i, size - 8);
            Function(grid, size, size - i - 1, size - 8); Function(grid, size, size - 8, size - i - 1);
        }
        for (var i = 0; i < 9; i++) {
            Function(grid, size, i, 8); Function(grid, size, 8, i);
            Function(grid, size, size - i - 1, 8); Function(grid, size, 8, size - i - 1);
            Function(grid, size, size - 9, i); Function(grid, size, i, size - 9);
            Function(grid, size, size - i - 1, size - 9); Function(grid, size, size - 9, size - i - 1);
        }
        if (version > 3) AddAlignment(grid, size, version);
        return grid;
    }

    private static void AddAlignment(byte[] grid, int size, int version) {
        var k = HanXinTables.ModuleK(version);
        var r = HanXinTables.ModuleR(version);
        var m = HanXinTables.ModuleM(version);
        var y = 0;
        var moduleY = 0;
        do {
            var height = moduleY < m ? k : r - 1;
            if ((moduleY & 1) == 0) { if ((m & 1) == 1) PlotAssistant(grid, size, 0, y); }
            else { if ((m & 1) == 0) PlotAssistant(grid, size, 0, y); PlotAssistant(grid, size, size - 1, y); }
            moduleY++; y += height;
        } while (y < size);

        var x = size - 1;
        var moduleX = 0;
        do {
            var width = moduleX < m ? k : r - 1;
            if ((moduleX & 1) == 0) { if ((m & 1) == 1) PlotAssistant(grid, size, x, size - 1); }
            else { if ((m & 1) == 0) PlotAssistant(grid, size, x, size - 1); PlotAssistant(grid, size, x, 0); }
            moduleX++; x -= width;
        } while (x >= 0);

        var columnSwitch = true;
        y = 0; moduleY = 0;
        do {
            var height = moduleY < m ? k : r - 1;
            var rowSwitch = columnSwitch;
            columnSwitch = !columnSwitch;
            x = size - 1; moduleX = 0;
            do {
                var width = moduleX < m ? k : r - 1;
                if (rowSwitch && !(y == 0 && x == size - 1)) PlotAlignment(grid, size, x, y, width, height);
                rowSwitch = !rowSwitch; moduleX++; x -= width;
            } while (x >= 0);
            moduleY++; y += height;
        } while (y < size);
    }

    private static void PlaceTopLeft(byte[] grid, int size) => PlacePattern(grid, size, 0, 0, new byte[] { 0x7F, 0x40, 0x5F, 0x50, 0x57, 0x57, 0x57 });
    private static void PlaceFinder(byte[] grid, int size, int x, int y) => PlacePattern(grid, size, x, y, new byte[] { 0x7F, 0x01, 0x7D, 0x05, 0x75, 0x75, 0x75 });
    private static void PlaceBottomRight(byte[] grid, int size) => PlacePattern(grid, size, size - 7, size - 7, new byte[] { 0x75, 0x75, 0x75, 0x05, 0x7D, 0x01, 0x7F });

    private static void PlacePattern(byte[] grid, int size, int x, int y, byte[] rows) {
        for (var py = 0; py < 7; py++) for (var px = 0; px < 7; px++) grid[(y + py) * size + x + px] = (byte)(0x10 | ((rows[py] & 0x40 >> px) != 0 ? 1 : 0));
    }

    private static void PlotAlignment(byte[] grid, int size, int x, int y, int width, int height) {
        SafePlot(grid, size, x, y, true); SafePlot(grid, size, x - 1, y + 1, false);
        for (var i = 1; i <= width; i++) { SafePlot(grid, size, x - i, y, true); SafePlot(grid, size, x - i - 1, y + 1, false); }
        for (var i = 1; i < height; i++) { SafePlot(grid, size, x, y + i, true); SafePlot(grid, size, x - 1, y + i + 1, false); }
    }

    private static void PlotAssistant(byte[] grid, int size, int x, int y) {
        for (var dy = -1; dy <= 1; dy++) for (var dx = -1; dx <= 1; dx++) SafePlot(grid, size, x + dx, y + dy, dx == 0 && dy == 0);
    }

    private static void SafePlot(byte[] grid, int size, int x, int y, bool dark) {
        if (x < 0 || y < 0 || x >= size || y >= size || grid[y * size + x] != 0) return;
        grid[y * size + x] = (byte)(0x10 | (dark ? 1 : 0));
    }

    private static void Function(byte[] grid, int size, int x, int y) { if ((uint)x < (uint)size && (uint)y < (uint)size) grid[y * size + x] = 0x10; }

    private static void SetFunctionInfo(byte[] grid, int size, int version, int eccLevel, int mask) {
        var value = (version + 20) << 4 | (eccLevel - 1) << 2 | mask;
        var words = new int[7];
        words[0] = value >> 8 & 0x0F; words[1] = value >> 4 & 0x0F; words[2] = value & 0x0F;
        new ReedSolomonEncoder(GenericGf.AztecParam).Encode(words, 4);
        var bits = new bool[34];
        var position = 0;
        for (var i = 0; i < words.Length; i++) for (var bit = 3; bit >= 0; bit--) bits[position++] = (words[i] & 1 << bit) != 0;
        for (var i = 0; i < 9; i++) {
            SetFunction(grid, size, i, 8, bits[i]); SetFunction(grid, size, size - i - 1, size - 9, bits[i]);
            SetFunction(grid, size, 8, 8 - i, bits[i + 8]); SetFunction(grid, size, size - 9, size - 9 + i, bits[i + 8]);
            SetFunction(grid, size, size - 9, i, bits[i + 17]); SetFunction(grid, size, 8, size - 1 - i, bits[i + 17]);
            SetFunction(grid, size, size - 9 + i, 8, bits[i + 25]); SetFunction(grid, size, 8 - i, size - 9, bits[i + 25]);
        }
    }

    private static bool TryReadFunctionInfo(BitMatrix matrix, int expectedVersion, out int eccLevel, out int mask) {
        eccLevel = mask = 0;
        var primary = new bool[28];
        var secondary = new bool[28];
        var size = matrix.Width;
        for (var i = 0; i < 9; i++) {
            primary[i] = matrix[i, 8];
            primary[i + 8] = matrix[8, 8 - i];
            primary[i + 17] = matrix[size - 9, i];
            secondary[i] = matrix[size - i - 1, size - 9];
            secondary[i + 8] = matrix[size - 9, size - 9 + i];
            secondary[i + 17] = matrix[8, size - 1 - i];
        }
        primary[26] = matrix[size - 8, 8];
        primary[27] = matrix[size - 7, 8];
        secondary[26] = matrix[7, size - 9];
        secondary[27] = matrix[6, size - 9];

        var primaryValid = TryDecodeFunctionInfo(primary, expectedVersion, out var primaryEcc, out var primaryMask);
        var secondaryValid = TryDecodeFunctionInfo(secondary, expectedVersion, out var secondaryEcc, out var secondaryMask);
        if (!primaryValid && !secondaryValid) return false;
        if (primaryValid && secondaryValid && (primaryEcc != secondaryEcc || primaryMask != secondaryMask)) return false;
        eccLevel = primaryValid ? primaryEcc : secondaryEcc;
        mask = primaryValid ? primaryMask : secondaryMask;
        return true;
    }

    private static bool TryDecodeFunctionInfo(bool[] bits, int expectedVersion, out int eccLevel, out int mask) {
        eccLevel = mask = 0;
        var words = new int[7];
        for (var i = 0; i < 28; i++) if (bits[i]) words[i >> 2] |= 1 << (3 - (i & 3));
        try { new ReedSolomonDecoder(GenericGf.AztecParam).Decode(words, 4); }
        catch (ReedSolomonException) { return false; }
        var value = words[0] << 8 | words[1] << 4 | words[2];
        var version = (value >> 4) - 20;
        eccLevel = (value >> 2 & 0x03) + 1;
        mask = value & 0x03;
        return version == expectedVersion;
    }

    private static void SetFunction(byte[] grid, int size, int x, int y, bool dark) { grid[y * size + x] = (byte)(0x10 | (dark ? 1 : 0)); }

    private static void ApplyMask(byte[] grid, int size, int mask) {
        if (mask == 0) return;
        for (var y = 0; y < size; y++) for (var x = 0; x < size; x++) {
            var index = y * size + x;
            if ((grid[index] & 0xF0) != 0 && grid[index] != 0x01) continue;
            if (grid[index] >= 0x10 || !MaskApplies(x, y, mask)) continue;
            grid[index] ^= 1;
        }
    }

    private static void ApplyMask(BitMatrix matrix, byte[] functionGrid, int mask) {
        if (mask == 0) return;
        var size = matrix.Width;
        for (var y = 0; y < size; y++) for (var x = 0; x < size; x++) if (functionGrid[y * size + x] == 0 && MaskApplies(x, y, mask)) matrix[x, y] = !matrix[x, y];
    }

    private static bool MaskApplies(int x, int y, int mask) {
        var i = y + 1; var j = x + 1;
        return mask == 1 ? ((i + j) & 1) == 0 : mask == 2 ? (((i + j) % 3 + j % 3) & 1) == 0 : ((i % j + j % i + i % 3 + j % 3) & 1) == 0;
    }

    private static BitMatrix ToMatrix(byte[] grid, int size) {
        var result = new BitMatrix(size, size);
        for (var i = 0; i < grid.Length; i++) if ((grid[i] & 1) != 0) result[i % size, i / size] = true;
        return result;
    }

    private static bool MatchesFinderLike(BitMatrix matrix, int x, int y) {
        return matrix[x, y] && matrix[x + 2, y] && !matrix[x + 3, y] && matrix[x + 4, y] &&
               matrix[x + 6, y] && matrix[x + 1, y] != matrix[x + 5, y];
    }

    private static bool HasThreeLightBefore(BitMatrix matrix, int x, int y, bool vertical) {
        for (var offset = 1; offset <= 3; offset++) {
            var position = (vertical ? y : x) - offset;
            if (position < 0) return true;
            if (vertical ? matrix[x, position] : matrix[position, y]) return false;
        }
        return true;
    }

    private static bool HasThreeLightAfter(BitMatrix matrix, int x, int y, bool vertical) {
        for (var offset = 0; offset < 3; offset++) {
            var position = (vertical ? y : x) + offset;
            if (position >= matrix.Width) return true;
            if (vertical ? matrix[x, position] : matrix[position, y]) return false;
        }
        return true;
    }

    private static int RunPenalty(BitMatrix matrix, int fixedPosition, bool vertical) {
        var state = false;
        var block = 0;
        var penalty = 0;
        for (var i = 0; i < matrix.Width; i++) {
            var value = vertical ? matrix[fixedPosition, i] : matrix[i, fixedPosition];
            if (value == state) block++;
            else { if (block >= 3) penalty += block * 4; block = 1; state = value; }
        }
        if (block >= 3) penalty += block * 4;
        return penalty;
    }
}
