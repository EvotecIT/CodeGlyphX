using System.Globalization;

namespace CodeGlyphX.Rendering;

internal static class GuardMessages {
    private const double OneKb = 1024d;
    private const double OneMb = 1024d * 1024d;

    public static string ForBytes(string message, long actual, long max) {
        if (max <= 0 || actual < 0) return message;
        var baseMessage = TrimPeriod(message);
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0} (got {1}, max {2}).",
            baseMessage,
            FormatBytes(actual),
            FormatBytes(max));
    }

    public static string ForPixels(string message, int width, int height, long actual, long max) {
        if (max <= 0 || actual < 0) return message;
        var baseMessage = TrimPeriod(message);
        var dims = (width > 0 && height > 0)
            ? string.Format(CultureInfo.InvariantCulture, "{0}x{1}", width, height)
            : actual.ToString("N0", CultureInfo.InvariantCulture);
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0} (got {1} = {2} px, max {3} px).",
            baseMessage,
            dims,
            actual.ToString("N0", CultureInfo.InvariantCulture),
            max.ToString("N0", CultureInfo.InvariantCulture));
    }

    private static string FormatBytes(long bytes) {
        if (bytes < OneKb) return string.Format(CultureInfo.InvariantCulture, "{0} B", bytes);
        if (bytes < OneMb) return string.Format(CultureInfo.InvariantCulture, "{0:0.#} KB", bytes / OneKb);
        return string.Format(CultureInfo.InvariantCulture, "{0:0.#} MB", bytes / OneMb);
    }

    private static string TrimPeriod(string message) {
        return string.IsNullOrWhiteSpace(message)
            ? string.Empty
            : message.Trim().TrimEnd('.');
    }
}
