using System.Reflection;
using CodeGlyphX.Rendering;

namespace CodeGlyphX.Examples;

internal sealed class ExampleRunner {
    private readonly string _outputDir;
    private readonly List<ExampleResult> _results = new();

    public ExampleRunner(string outputDir) {
        _outputDir = outputDir ?? throw new ArgumentNullException(nameof(outputDir));
    }

    public static string PrepareOutputDirectory(string subfolderName = "Examples") {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        if (!string.IsNullOrEmpty(assemblyLocation)) {
            var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
            if (!string.IsNullOrEmpty(assemblyDirectory)) {
                Directory.SetCurrentDirectory(assemblyDirectory);
            }
        }

        var outputDir = Path.Combine(Directory.GetCurrentDirectory(), subfolderName);
        Directory.CreateDirectory(outputDir);
        return outputDir;
    }

    public void Run(string name, Action<string> example) {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Example name is required.", nameof(name));
        if (example is null) throw new ArgumentNullException(nameof(example));

        Console.WriteLine($"Running: {name}");
        try {
            example(_outputDir);
            _results.Add(new ExampleResult(name, null));
        } catch (Exception ex) {
            _results.Add(new ExampleResult(name, ex));
            Console.WriteLine($"Failed:  {name} ({ex.GetType().Name})");
            var slug = Slugify(name);
            ex.ToString().WriteText(_outputDir, $"_failed-{slug}.txt");
        }
    }

    public void PrintSummary() {
        var failures = 0;
        for (var i = 0; i < _results.Count; i++) {
            if (_results[i].Error is not null) failures++;
        }

        Console.WriteLine();
        Console.WriteLine($"Examples written to: {_outputDir}");
        if (failures == 0) {
            Console.WriteLine($"All {_results.Count} examples completed.");
            return;
        }

        Console.WriteLine($"Completed with {failures} failure(s).");
        for (var i = 0; i < _results.Count; i++) {
            var result = _results[i];
            if (result.Error is null) continue;
            Console.WriteLine($" - {result.Name}: {result.Error.Message}");
        }
    }

    private static string Slugify(string value) {
        var buffer = new char[value.Length];
        var length = 0;
        for (var i = 0; i < value.Length; i++) {
            var ch = value[i];
            if (ch >= 'A' && ch <= 'Z') {
                buffer[length++] = (char)(ch + 32);
            } else if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9')) {
                buffer[length++] = ch;
            } else if (ch is ' ' or '-' or '_' or '.') {
                buffer[length++] = '-';
            }
        }
        return length == 0 ? "example" : new string(buffer, 0, length);
    }

    private sealed class ExampleResult {
        public ExampleResult(string name, Exception? error) {
            Name = name;
            Error = error;
        }

        public string Name { get; }
        public Exception? Error { get; }
    }
}
