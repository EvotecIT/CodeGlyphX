using System;
using System.IO;

namespace CodeGlyphX.Benchmarks;

internal static class RepoFiles {
    public static byte[] ReadRepoFile(string relativePath) {
        if (string.IsNullOrWhiteSpace(relativePath)) {
            throw new ArgumentException("Path is required.", nameof(relativePath));
        }

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 12 && dir is not null; i++) {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate)) {
                return File.ReadAllBytes(candidate);
            }
            dir = dir.Parent;
        }

        throw new FileNotFoundException($"Could not locate sample file '{relativePath}'.");
    }

    public static string ResolveRepoRoot() {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 12 && dir is not null; i++) {
            var roadmap = Path.Combine(dir.FullName, "ROADMAP.md");
            var git = Path.Combine(dir.FullName, ".git");
            if (File.Exists(roadmap) || Directory.Exists(git)) {
                return dir.FullName;
            }
            dir = dir.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

    public static string EnsureReportDirectory() {
        var root = ResolveRepoRoot();
        var reports = Path.Combine(root, "BenchmarkReports");
        Directory.CreateDirectory(reports);
        return reports;
    }
}

