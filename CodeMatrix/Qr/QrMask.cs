using System;

namespace CodeGlyphX.Qr;

internal static class QrMask {
    public static bool ShouldInvert(int mask, int x, int y) {
        return mask switch {
            0 => ((x + y) & 1) == 0,
            1 => (y & 1) == 0,
            2 => x % 3 == 0,
            3 => (x + y) % 3 == 0,
            4 => (((y / 2) + (x / 3)) & 1) == 0,
            5 => ((x * y) % 2 + (x * y) % 3) == 0,
            6 => ((((x * y) % 2) + ((x * y) % 3)) & 1) == 0,
            7 => ((((x + y) % 2) + ((x * y) % 3)) & 1) == 0,
            _ => throw new ArgumentOutOfRangeException(nameof(mask)),
        };
    }

    public static int ComputePenalty(BitMatrix modules) {
        if (modules is null) throw new ArgumentNullException(nameof(modules));
        if (modules.Width != modules.Height) throw new ArgumentException("Matrix must be square.", nameof(modules));
        var size = modules.Width;

        var penalty = 0;

        // N1: Adjacent modules in row/column in same color
        for (var y = 0; y < size; y++) {
            var runColor = modules[0, y];
            var runLen = 1;
            for (var x = 1; x < size; x++) {
                var color = modules[x, y];
                if (color == runColor) {
                    runLen++;
                } else {
                    if (runLen >= 5) penalty += 3 + (runLen - 5);
                    runColor = color;
                    runLen = 1;
                }
            }
            if (runLen >= 5) penalty += 3 + (runLen - 5);
        }

        for (var x = 0; x < size; x++) {
            var runColor = modules[x, 0];
            var runLen = 1;
            for (var y = 1; y < size; y++) {
                var color = modules[x, y];
                if (color == runColor) {
                    runLen++;
                } else {
                    if (runLen >= 5) penalty += 3 + (runLen - 5);
                    runColor = color;
                    runLen = 1;
                }
            }
            if (runLen >= 5) penalty += 3 + (runLen - 5);
        }

        // N2: 2x2 blocks of same color
        for (var y = 0; y < size - 1; y++) {
            for (var x = 0; x < size - 1; x++) {
                var c = modules[x, y];
                if (c == modules[x + 1, y] &&
                    c == modules[x, y + 1] &&
                    c == modules[x + 1, y + 1]) {
                    penalty += 3;
                }
            }
        }

        // N3: Finder-like patterns in rows
        for (var y = 0; y < size; y++) {
            for (var x = 0; x < size - 10; x++) {
                if (IsFinderLike(modules, x, y, dx: 1, dy: 0)) penalty += 40;
            }
        }

        // N3: Finder-like patterns in columns
        for (var x = 0; x < size; x++) {
            for (var y = 0; y < size - 10; y++) {
                if (IsFinderLike(modules, x, y, dx: 0, dy: 1)) penalty += 40;
            }
        }

        // N4: Proportion of dark modules
        var dark = 0;
        var total = size * size;
        for (var y = 0; y < size; y++) {
            for (var x = 0; x < size; x++) {
                if (modules[x, y]) dark++;
            }
        }
        var percent = (dark * 100) / total;
        var k = Math.Abs(percent - 50) / 5;
        penalty += k * 10;

        return penalty;
    }

    private static bool IsFinderLike(BitMatrix modules, int x, int y, int dx, int dy) {
        // Patterns (1=dark,0=light):
        // 10111010000  OR  00001011101
        return
            Sample(modules, x, y, dx, dy, 1) &&
            !Sample(modules, x, y, dx, dy, 2) &&
            Sample(modules, x, y, dx, dy, 3) &&
            Sample(modules, x, y, dx, dy, 4) &&
            Sample(modules, x, y, dx, dy, 5) &&
            !Sample(modules, x, y, dx, dy, 6) &&
            Sample(modules, x, y, dx, dy, 7) &&
            !Sample(modules, x, y, dx, dy, 8) &&
            !Sample(modules, x, y, dx, dy, 9) &&
            !Sample(modules, x, y, dx, dy, 10) &&
            !Sample(modules, x, y, dx, dy, 11)
            ||
            !Sample(modules, x, y, dx, dy, 1) &&
            !Sample(modules, x, y, dx, dy, 2) &&
            !Sample(modules, x, y, dx, dy, 3) &&
            !Sample(modules, x, y, dx, dy, 4) &&
            Sample(modules, x, y, dx, dy, 5) &&
            !Sample(modules, x, y, dx, dy, 6) &&
            Sample(modules, x, y, dx, dy, 7) &&
            Sample(modules, x, y, dx, dy, 8) &&
            Sample(modules, x, y, dx, dy, 9) &&
            !Sample(modules, x, y, dx, dy, 10) &&
            Sample(modules, x, y, dx, dy, 11);
    }

    private static bool Sample(BitMatrix modules, int x, int y, int dx, int dy, int offset1Based) {
        var xx = x + dx * (offset1Based - 1);
        var yy = y + dy * (offset1Based - 1);
        return modules[xx, yy];
    }
}

