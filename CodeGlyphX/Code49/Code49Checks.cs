namespace CodeGlyphX.Code49;

/// <summary>Computes Code 49's row checks and weighted X/Y/Z symbol check pairs.</summary>
internal static class Code49Checks {
    internal static void Apply(int[,] words, int rows) {
        for (var row = 0; row < rows - 1; row++) {
            var sum = 0;
            for (var at = 0; at < 7; at++) sum += words[row, at];
            words[row, 7] = sum % 49;
        }
        var last = rows - 1;
        var x = words[last, 6] * 20;
        var y = words[last, 6] * 16;
        var z = words[last, 6] * 38;
        var position = 0;
        for (var row = 0; row < last; row++) {
            for (var pair = 0; pair < 4; pair++) {
                var value = words[row, pair * 2] * 49 + words[row, pair * 2 + 1];
                x += Code49Tables.XWeights[position] * value;
                y += Code49Tables.YWeights[position] * value;
                z += Code49Tables.ZWeights[position++] * value;
            }
        }
        if (rows > 6) SetPair(words, last, 0, z % 2401);
        var first = words[last, 0] * 49 + words[last, 1];
        x += Code49Tables.XWeights[position] * first;
        y += Code49Tables.YWeights[position++] * first;
        SetPair(words, last, 2, y % 2401);
        x += Code49Tables.XWeights[position] * (words[last, 2] * 49 + words[last, 3]);
        SetPair(words, last, 4, x % 2401);
        var lastSum = 0;
        for (var at = 0; at < 7; at++) lastSum += words[last, at];
        words[last, 7] = lastSum % 49;
    }

    private static void SetPair(int[,] words, int row, int at, int value) {
        words[row, at] = value / 49;
        words[row, at + 1] = value % 49;
    }
}
