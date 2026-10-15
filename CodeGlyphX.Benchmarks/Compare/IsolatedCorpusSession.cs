#nullable enable
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Threading;

namespace CodeGlyphX.Benchmarks.Tools;

/// <summary>Owns one version of the public corpus adapter in a collectible assembly context.</summary>
public sealed class IsolatedCorpusSession : IDisposable {
    private sealed class Context : AssemblyLoadContext {
        private readonly string _root;

        internal Context(string root) : base(isCollectible: true) => _root = root;

        protected override Assembly? Load(AssemblyName name) {
            var path = Path.Combine(_root, name.Name + ".dll");
            return File.Exists(path) ? LoadFromAssemblyPath(path) : null;
        }
    }

    private static long _nextIdentity;
    private readonly Context _context;
    private object? _session;
    private object? _output;
    private MethodInfo? _decode;
    private MethodInfo? _evaluate;

    /// <summary>Identifies this retained adapter instance within the benchmark process.</summary>
    public long Identity { get; }
    /// <summary>Gets the number of preloaded requests.</summary>
    public int Count { get; }
    /// <summary>Gets the exact loaded core's SHA-256.</summary>
    public string CoreSha256 { get; }
    /// <summary>Gets the loaded core's build version, including its source revision when available.</summary>
    public string CoreInformationalVersion { get; }
    /// <summary>Gets the corpus adapter assembly's SHA-256.</summary>
    public string HostSha256 { get; }
    /// <summary>Reports whether a completed decode is available for evaluation outside timing.</summary>
    public bool HasDecodedResult => _output is not null;

    /// <summary>Loads one compiled host and preloads its unchanged corpus inputs.</summary>
    public IsolatedCorpusSession(string assemblyRoot, string manifest, string imageRoot) {
        var root = Path.GetFullPath(assemblyRoot);
        _context = new Context(root);
        try {
            // Bind this root's core explicitly before resolving the adapter.
            // An incomplete root must not borrow a loaded default-context core.
            var core = _context.LoadFromAssemblyPath(Path.Combine(root, "CodeGlyphX.dll"));
            var hostPath = Path.Combine(root, "CodeGlyphX.Benchmarks.dll");
            var host = _context.LoadFromAssemblyPath(hostPath);
            var type = host.GetType("CodeGlyphX.Benchmarks.CorpusSession", throwOnError: true)!;
            _session = Activator.CreateInstance(type, new object[] { manifest, imageRoot })!;
            _decode = type.GetMethod("Decode") ?? throw new MissingMethodException(type.FullName, "Decode");
            _evaluate = type.GetMethod("Evaluate") ?? throw new MissingMethodException(type.FullName, "Evaluate");
            Count = (int)(type.GetProperty("Count")?.GetValue(_session)
                ?? throw new MissingMemberException(type.FullName, "Count"));
            CoreSha256 = HashFile(core.Location);
            HostSha256 = HashFile(hostPath);
            CoreInformationalVersion = core.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty;
            Identity = Interlocked.Increment(ref _nextIdentity);
        } catch {
            _session = null;
            _context.Unload();
            throw;
        }
    }

    /// <summary>Runs the robust CodeGlyphX lane; input loading and evaluation remain separate.</summary>
    public void Decode() {
        var session = _session ?? throw new ObjectDisposedException(nameof(IsolatedCorpusSession));
        _output = null;
        _output = _decode!.Invoke(session, new object[] { "CodeGlyphX", true });
    }

    /// <summary>Evaluates the last completed decode through that same version's public adapter.</summary>
    public Array Evaluate() {
        var session = _session ?? throw new ObjectDisposedException(nameof(IsolatedCorpusSession));
        if (_output is null) throw new InvalidOperationException("Decode must complete before evaluation.");
        return (Array)_evaluate!.Invoke(session, new object[] { "CodeGlyphX", _output })!;
    }

    /// <summary>Releases adapter references and requests unloading; completion belongs to the CLR.</summary>
    public void Dispose() {
        if (_session is null) return;
        _output = null;
        _session = null;
        _decode = null;
        _evaluate = null;
        _context.Unload();
    }

    private static string HashFile(string path) {
        using var file = File.OpenRead(path);
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(file));
    }
}
