// Adapted from OfficeIMO.Core, Copyright (c) 2022 Evotec.
// Licensed under the MIT license; see Licenses/OfficeIMO-MIT.txt.

namespace CodeGlyphX.Internal.Fonts;

/// <summary>Base direction of a Unicode text run.</summary>
internal enum ManagedTextDirection {
    /// <summary>No strong directional character was found.</summary>
    Auto = 0,

    /// <summary>The first strong character establishes a left-to-right base direction.</summary>
    LeftToRight = 1,

    /// <summary>The first strong character establishes a right-to-left base direction.</summary>
    RightToLeft = 2,

    /// <summary>Glyphs advance from the top of the line toward the bottom.</summary>
    TopToBottom = 3
}
