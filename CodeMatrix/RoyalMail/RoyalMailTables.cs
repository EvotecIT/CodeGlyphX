namespace CodeMatrix.RoyalMail;

internal static class RoyalMailTables {
    public const int BarcodeHeight = 8;

    public static readonly RoyalMailBarTypes[][] Symbols = {
        new[] { RoyalMailBarTypes.Tracker, RoyalMailBarTypes.Tracker, RoyalMailBarTypes.FullHeight, RoyalMailBarTypes.FullHeight },
        new[] { RoyalMailBarTypes.Tracker, RoyalMailBarTypes.Descender, RoyalMailBarTypes.Ascender, RoyalMailBarTypes.FullHeight },
        new[] { RoyalMailBarTypes.Tracker, RoyalMailBarTypes.Descender, RoyalMailBarTypes.FullHeight, RoyalMailBarTypes.Ascender },
        new[] { RoyalMailBarTypes.Descender, RoyalMailBarTypes.Tracker, RoyalMailBarTypes.Ascender, RoyalMailBarTypes.FullHeight },
        new[] { RoyalMailBarTypes.Descender, RoyalMailBarTypes.Tracker, RoyalMailBarTypes.FullHeight, RoyalMailBarTypes.Ascender },
        new[] { RoyalMailBarTypes.Descender, RoyalMailBarTypes.Descender, RoyalMailBarTypes.Ascender, RoyalMailBarTypes.Ascender },
        new[] { RoyalMailBarTypes.Tracker, RoyalMailBarTypes.Ascender, RoyalMailBarTypes.Descender, RoyalMailBarTypes.FullHeight },
        new[] { RoyalMailBarTypes.Tracker, RoyalMailBarTypes.FullHeight, RoyalMailBarTypes.Tracker, RoyalMailBarTypes.FullHeight },
        new[] { RoyalMailBarTypes.Tracker, RoyalMailBarTypes.FullHeight, RoyalMailBarTypes.Descender, RoyalMailBarTypes.Ascender },
        new[] { RoyalMailBarTypes.Descender, RoyalMailBarTypes.Ascender, RoyalMailBarTypes.Tracker, RoyalMailBarTypes.FullHeight },
        new[] { RoyalMailBarTypes.Descender, RoyalMailBarTypes.Ascender, RoyalMailBarTypes.Descender, RoyalMailBarTypes.Ascender },
        new[] { RoyalMailBarTypes.Descender, RoyalMailBarTypes.FullHeight, RoyalMailBarTypes.Tracker, RoyalMailBarTypes.Ascender },
        new[] { RoyalMailBarTypes.Tracker, RoyalMailBarTypes.Ascender, RoyalMailBarTypes.FullHeight, RoyalMailBarTypes.Descender },
        new[] { RoyalMailBarTypes.Tracker, RoyalMailBarTypes.FullHeight, RoyalMailBarTypes.Ascender, RoyalMailBarTypes.Descender },
        new[] { RoyalMailBarTypes.Tracker, RoyalMailBarTypes.FullHeight, RoyalMailBarTypes.FullHeight, RoyalMailBarTypes.Tracker },
        new[] { RoyalMailBarTypes.Descender, RoyalMailBarTypes.Ascender, RoyalMailBarTypes.Ascender, RoyalMailBarTypes.Descender },
        new[] { RoyalMailBarTypes.Descender, RoyalMailBarTypes.Ascender, RoyalMailBarTypes.FullHeight, RoyalMailBarTypes.Tracker },
        new[] { RoyalMailBarTypes.Descender, RoyalMailBarTypes.FullHeight, RoyalMailBarTypes.Ascender, RoyalMailBarTypes.Tracker },
        new[] { RoyalMailBarTypes.Ascender, RoyalMailBarTypes.Tracker, RoyalMailBarTypes.Descender, RoyalMailBarTypes.FullHeight },
        new[] { RoyalMailBarTypes.Ascender, RoyalMailBarTypes.Descender, RoyalMailBarTypes.Tracker, RoyalMailBarTypes.FullHeight },
        new[] { RoyalMailBarTypes.Ascender, RoyalMailBarTypes.Descender, RoyalMailBarTypes.Descender, RoyalMailBarTypes.Ascender },
        new[] { RoyalMailBarTypes.FullHeight, RoyalMailBarTypes.Tracker, RoyalMailBarTypes.Tracker, RoyalMailBarTypes.FullHeight },
        new[] { RoyalMailBarTypes.FullHeight, RoyalMailBarTypes.Tracker, RoyalMailBarTypes.Descender, RoyalMailBarTypes.Ascender },
        new[] { RoyalMailBarTypes.FullHeight, RoyalMailBarTypes.Descender, RoyalMailBarTypes.Tracker, RoyalMailBarTypes.Ascender },
        new[] { RoyalMailBarTypes.Ascender, RoyalMailBarTypes.Tracker, RoyalMailBarTypes.FullHeight, RoyalMailBarTypes.Descender },
        new[] { RoyalMailBarTypes.Ascender, RoyalMailBarTypes.Descender, RoyalMailBarTypes.Ascender, RoyalMailBarTypes.Descender },
        new[] { RoyalMailBarTypes.Ascender, RoyalMailBarTypes.Descender, RoyalMailBarTypes.FullHeight, RoyalMailBarTypes.Tracker },
        new[] { RoyalMailBarTypes.FullHeight, RoyalMailBarTypes.Tracker, RoyalMailBarTypes.Ascender, RoyalMailBarTypes.Descender },
        new[] { RoyalMailBarTypes.FullHeight, RoyalMailBarTypes.Tracker, RoyalMailBarTypes.FullHeight, RoyalMailBarTypes.Tracker },
        new[] { RoyalMailBarTypes.FullHeight, RoyalMailBarTypes.Descender, RoyalMailBarTypes.Ascender, RoyalMailBarTypes.Tracker },
        new[] { RoyalMailBarTypes.Ascender, RoyalMailBarTypes.Ascender, RoyalMailBarTypes.Descender, RoyalMailBarTypes.Descender },
        new[] { RoyalMailBarTypes.Ascender, RoyalMailBarTypes.FullHeight, RoyalMailBarTypes.Tracker, RoyalMailBarTypes.Descender },
        new[] { RoyalMailBarTypes.Ascender, RoyalMailBarTypes.FullHeight, RoyalMailBarTypes.Descender, RoyalMailBarTypes.Tracker },
        new[] { RoyalMailBarTypes.FullHeight, RoyalMailBarTypes.Ascender, RoyalMailBarTypes.Tracker, RoyalMailBarTypes.Descender },
        new[] { RoyalMailBarTypes.FullHeight, RoyalMailBarTypes.Ascender, RoyalMailBarTypes.Descender, RoyalMailBarTypes.Tracker },
        new[] { RoyalMailBarTypes.FullHeight, RoyalMailBarTypes.FullHeight, RoyalMailBarTypes.Tracker, RoyalMailBarTypes.Tracker }
    };
}
