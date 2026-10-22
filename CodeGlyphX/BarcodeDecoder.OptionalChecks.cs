namespace CodeGlyphX;

public static partial class BarcodeDecoder {
    private static bool ApplyTwoOfFiveCheck(ref string text, BarcodeDecodeOptions? options) {
        var policy = options?.TwoOfFiveChecksum ?? OptionalChecksumPolicy.None;
        if (policy == OptionalChecksumPolicy.None) return true;
        if (text.Length >= 2 && IdentifierChecks.Gs1(text.Substring(0, text.Length - 1)) == text[text.Length - 1]) {
            text = text.Substring(0, text.Length - 1);
            return true;
        }
        return policy == OptionalChecksumPolicy.StripIfValid;
    }
}
