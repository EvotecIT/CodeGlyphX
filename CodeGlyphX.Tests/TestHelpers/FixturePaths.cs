using System;
using System.IO;

namespace CodeGlyphX.Tests.TestHelpers;

internal static class FixturePaths {
    public static string Get(string category, string fileName) {
        if (string.IsNullOrWhiteSpace(category)) throw new ArgumentException("Category is required.", nameof(category));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("File name is required.", nameof(fileName));

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 10 && dir is not null; i++) {
            var direct = Path.Combine(dir.FullName, "Fixtures", category, fileName);
            if (File.Exists(direct)) return direct;

            var nested = Path.Combine(dir.FullName, "CodeGlyphX.Tests", "Fixtures", category, fileName);
            if (File.Exists(nested)) return nested;

            dir = dir.Parent;
        }

        throw new FileNotFoundException($"{category} fixture not found: {fileName}");
    }
}
