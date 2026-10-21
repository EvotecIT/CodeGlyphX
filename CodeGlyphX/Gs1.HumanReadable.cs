using System.Text;

namespace CodeGlyphX;

public static partial class Gs1 {
    /// <summary>Formats validated Application Identifier elements for printing, omitting AI 8200's machine-only URL.</summary>
    /// <remarks>Readable parentheses are presentation characters; FNC1 separators remain in the encoded payload.</remarks>
    public static string HumanReadable(string input, bool multiline = false) {
        var text = new StringBuilder();
        foreach (var element in Parse(input)) {
            if (element.Ai == "8200") continue;
            if (multiline && text.Length > 0) text.Append('\n');
            text.Append('(').Append(element.Ai).Append(')').Append(element.Data);
        }
        return text.ToString();
    }
}
