using System;

namespace CodeGlyphX.Rendering;

internal static class BarcodeLabelText {
    public static string Normalize(string? text) {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        return text!.Replace(CodeGlyphX.Gs1.GroupSeparator, ' ');
    }
}
