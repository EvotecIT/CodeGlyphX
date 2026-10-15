namespace CodeGlyphX.Benchmarks;

// Text and physical payload are separate contracts. Preserve raw output and score these documented representations explicitly.
internal static class CorpusScoring {
    internal static string Canonical(string text, string format) {
        if (format is "Gs1DataBarExpanded" or "Gs1DataBarExpandedStacked") {
            try { return Gs1.ElementString(text); }
            catch (FormatException) { return text; } // Invalid decoded/oracle text remains visible, never aborts scoring.
        }
        if (format == "Codabar" && text.Length >= 2 && "ABCD".Contains(text[0]) && "ABCD".Contains(text[^1])) return text[1..^1];
        if (format == "UpcA" && text.Length == 13 && text[0] == '0') return text[1..];
        if (format == "UpcE") {
            if (text.Length == 13 && text[0] == '0') return text[1..];
            if (text.Length == 8 && text.All(char.IsAsciiDigit)) {
                var d = text.AsSpan();
                return d[6] switch {
                    '0' or '1' or '2' => $"{d[0]}{d[1]}{d[2]}{d[6]}0000{d[3]}{d[4]}{d[5]}{d[7]}",
                    '3' => $"{d[0]}{d[1]}{d[2]}{d[3]}00000{d[4]}{d[5]}{d[7]}",
                    '4' => $"{d[0]}{d[1]}{d[2]}{d[3]}{d[4]}00000{d[5]}{d[7]}",
                    _ => $"{d[0]}{d[1]}{d[2]}{d[3]}{d[4]}{d[5]}0000{d[6]}{d[7]}"
                };
            }
        }
        if (format == "Gs1DataBarOmnidirectional") {
            if (text.StartsWith("(01)", StringComparison.Ordinal)) text = text[4..];
            else if (text.Length == 16 && text.StartsWith("01", StringComparison.Ordinal)) text = text[2..];
            if (text.Length == 13 && text.All(char.IsAsciiDigit)) {
                var sum = text.Select((c, i) => (c - '0') * (i % 2 == 0 ? 3 : 1)).Sum();
                text += (char)('0' + (10 - sum % 10) % 10);
            }
        }
        return text;
    }
}
