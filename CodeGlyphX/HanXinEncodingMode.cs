namespace CodeGlyphX;

/// <summary>Han Xin Code payload compaction mode.</summary>
public enum HanXinEncodingMode {
    /// <summary>Selects numeric, text, or binary compaction from the payload.</summary>
    Auto,
    /// <summary>Numeric compaction.</summary>
    Numeric,
    /// <summary>ASCII text compaction.</summary>
    Text,
    /// <summary>Binary byte compaction.</summary>
    Binary
}
