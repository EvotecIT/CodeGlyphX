namespace CodeGlyphX;

/// <summary>Han Xin Code payload compaction mode.</summary>
public enum HanXinEncodingMode {
    /// <summary>Selects numeric, text, or binary compaction from the payload's supported repertoire.</summary>
    Auto,
    /// <summary>Numeric compaction.</summary>
    Numeric,
    /// <summary>Han Xin text compaction. Characters outside its defined repertoire are rejected.</summary>
    Text,
    /// <summary>Binary byte compaction.</summary>
    Binary
}
