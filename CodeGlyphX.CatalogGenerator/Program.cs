using System.Text;
using System.Text.Json;
using CodeGlyphX;
using CodeGlyphX.CatalogGenerator;

var arguments = GeneratorArguments.Parse(args);
var repositoryRoot = Path.GetFullPath(arguments.RepositoryRoot);
var formatsPath = Path.Combine(repositoryRoot, "Website", "data", "formats.json");
var markdownPath = Path.Combine(repositoryRoot, "Website", "content", "docs", "symbol-capabilities.md");
var templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "symbol-capabilities.md");

var capabilities = SymbolCapabilities.All;
var catalog = new CatalogOutput {
    Matrix = capabilities
        .Where(item => item.Family is SymbolFamily.Matrix or SymbolFamily.Stacked)
        .Select(item => item.DisplayName)
        .ToArray(),
    Linear = capabilities
        .Where(item => item.Family is SymbolFamily.Linear or SymbolFamily.Postal)
        .Select(item => item.DisplayName)
        .ToArray(),
    Capabilities = capabilities.Select(CapabilityOutput.From).ToArray()
};

var json = JsonSerializer.Serialize(catalog, new JsonSerializerOptions {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true
}).Replace("\r\n", "\n", StringComparison.Ordinal) + "\n";
var template = File.ReadAllText(templatePath);
var markdown = template
    .Replace("{{FORMAT_COUNT}}", capabilities.Count.ToString(), StringComparison.Ordinal)
    .Replace("{{CAPABILITY_TABLE}}", BuildCapabilityTable(capabilities), StringComparison.Ordinal)
    .Replace("\r\n", "\n", StringComparison.Ordinal);
if (!markdown.EndsWith("\n", StringComparison.Ordinal)) markdown += "\n";

var stale = new List<string>();
WriteOrCheck(formatsPath, json, arguments.Check, stale);
WriteOrCheck(markdownPath, markdown, arguments.Check, stale);

if (stale.Count > 0) {
    Console.Error.WriteLine("Generated capability artifacts are stale:");
    foreach (var path in stale) Console.Error.WriteLine($"- {Path.GetRelativePath(repositoryRoot, path)}");
    Console.Error.WriteLine("Run the catalog generator without --check and commit the refreshed artifacts.");
    return 1;
}

Console.WriteLine(arguments.Check
    ? $"Capability artifacts are current ({capabilities.Count} formats)."
    : $"Generated capability artifacts for {capabilities.Count} formats.");
return 0;

static string BuildCapabilityTable(IReadOnlyList<SymbolCapability> capabilities) {
    var builder = new StringBuilder();
    builder.AppendLine("| Format | Family | Encode | Module decode | Image scan | Multiple | GS1 | ECI | Structured append | Geometry |");
    builder.AppendLine("| --- | --- | ---: | ---: | ---: | ---: | --- | --- | --- | ---: |");
    foreach (var capability in capabilities) {
        builder.Append("| ").Append(capability.DisplayName)
            .Append(" | ").Append(capability.Family)
            .Append(" | ").Append(YesNo(capability.Has(SymbolCapabilityFlags.Encode)))
            .Append(" | ").Append(YesNo(capability.Has(SymbolCapabilityFlags.DecodeModules)))
            .Append(" | ").Append(YesNo(capability.Has(SymbolCapabilityFlags.ScanImage)))
            .Append(" | ").Append(YesNo(capability.Has(SymbolCapabilityFlags.ScanMultiple)))
            .Append(" | ").Append(CapabilityText.Direction(capability, SymbolCapabilityFlags.Gs1Encode, SymbolCapabilityFlags.Gs1Decode))
            .Append(" | ").Append(CapabilityText.Direction(capability, SymbolCapabilityFlags.EciEncode, SymbolCapabilityFlags.EciDecode))
            .Append(" | ").Append(CapabilityText.Direction(capability, SymbolCapabilityFlags.StructuredAppendEncode, SymbolCapabilityFlags.StructuredAppendDecode))
            .Append(" | ").Append(YesNo(capability.Has(SymbolCapabilityFlags.ReportsGeometry)))
            .AppendLine(" |");
    }
    return builder.ToString().TrimEnd();
}

static string YesNo(bool value) => value ? "Yes" : "No";

static void WriteOrCheck(string path, string expected, bool check, ICollection<string> stale) {
    var current = File.Exists(path) ? File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal) : null;
    if (string.Equals(current, expected, StringComparison.Ordinal)) return;
    if (check) {
        stale.Add(path);
        return;
    }
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllText(path, expected, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
}

namespace CodeGlyphX.CatalogGenerator {

internal sealed class GeneratorArguments {
    internal string RepositoryRoot { get; private set; } = Environment.CurrentDirectory;
    internal bool Check { get; private set; }

    internal static GeneratorArguments Parse(string[] args) {
        var result = new GeneratorArguments();
        for (var i = 0; i < args.Length; i++) {
            switch (args[i]) {
                case "--check":
                    result.Check = true;
                    break;
                case "--repository-root" when i + 1 < args.Length:
                    result.RepositoryRoot = args[++i];
                    break;
                default:
                    throw new ArgumentException($"Unknown or incomplete argument '{args[i]}'.");
            }
        }
        return result;
    }
}

internal sealed class CatalogOutput {
    public string[] Matrix { get; set; } = Array.Empty<string>();
    public string[] Linear { get; set; } = Array.Empty<string>();
    public CapabilityOutput[] Capabilities { get; set; } = Array.Empty<CapabilityOutput>();
}

internal sealed class CapabilityOutput {
    public string Format { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Family { get; set; } = string.Empty;
    public bool Encode { get; set; }
    public bool ModuleDecode { get; set; }
    public bool ImageScan { get; set; }
    public bool Multiple { get; set; }
    public bool Geometry { get; set; }
    public bool Gs1Encode { get; set; }
    public bool Gs1Decode { get; set; }
    public bool EciEncode { get; set; }
    public bool EciDecode { get; set; }
    public bool StructuredAppendEncode { get; set; }
    public bool StructuredAppendDecode { get; set; }

    internal static CapabilityOutput From(SymbolCapability capability) {
        return new CapabilityOutput {
            Format = capability.Format.ToString(),
            Name = capability.DisplayName,
            Family = capability.Family.ToString(),
            Encode = capability.Has(SymbolCapabilityFlags.Encode),
            ModuleDecode = capability.Has(SymbolCapabilityFlags.DecodeModules),
            ImageScan = capability.Has(SymbolCapabilityFlags.ScanImage),
            Multiple = capability.Has(SymbolCapabilityFlags.ScanMultiple),
            Geometry = capability.Has(SymbolCapabilityFlags.ReportsGeometry),
            Gs1Encode = capability.Has(SymbolCapabilityFlags.Gs1Encode),
            Gs1Decode = capability.Has(SymbolCapabilityFlags.Gs1Decode),
            EciEncode = capability.Has(SymbolCapabilityFlags.EciEncode),
            EciDecode = capability.Has(SymbolCapabilityFlags.EciDecode),
            StructuredAppendEncode = capability.Has(SymbolCapabilityFlags.StructuredAppendEncode),
            StructuredAppendDecode = capability.Has(SymbolCapabilityFlags.StructuredAppendDecode)
        };
    }
}

internal static class CapabilityText {
    internal static string Direction(SymbolCapability capability, SymbolCapabilityFlags encode, SymbolCapabilityFlags decode) {
        var canEncode = capability.Has(encode);
        var canDecode = capability.Has(decode);
        if (canEncode && canDecode) return "Encode + decode";
        if (canEncode) return "Encode";
        if (canDecode) return "Decode";
        return "No";
    }
}

}
