using CodeGlyphX.Internal;
using System.Threading;

namespace CodeGlyphX.D3Aqr;

/// <summary>Decodes a provisional D3 AQR frame and the QR host through the shared QR decoder.</summary>
public static class D3AqrDecoder {
    // Image sampling owns orientation and geometry. Let it validate one view at a time
    // so the reported orientation describes the frame whose category was decoded.
    internal static bool TryDecodeCanonical(BitMatrix modules, CancellationToken token, out D3AqrDecoded decoded) =>
        TryDecodeView(new BitMatrixView(modules), token, out decoded);
    /// <summary>Decodes an exact module grid, accepting rotations and mirrored orientations.</summary>
    /// <remarks>Frame, timing, category parity and cell-pitch consistency are validated before returning a QR payload.</remarks>
    public static bool TryDecode(BitMatrix modules, out D3AqrDecoded decoded) {
        decoded = null!;
        if (modules is null || modules.Width != modules.Height || modules.Width < 35 || modules.Width > 748) return false;
        for (var mirror = 0; mirror < 2; mirror++)
            for (var rotation = 0; rotation < 4; rotation++)
                if (TryDecodeView(new BitMatrixView(modules, rotation, mirror != 0), CancellationToken.None, out decoded)) return true;
        return false;
    }

    private static bool TryDecodeView(BitMatrixView modules, CancellationToken token, out D3AqrDecoded decoded) {
        decoded = null!;
        for (var pitchIndex = 0; pitchIndex < 3; pitchIndex++) {
            if (DecodeBudget.ShouldAbort(token)) return false;
            var ratio = pitchIndex == 0 ? 2 : pitchIndex == 1 ? 2.5 : 3;
            D3AqrLayout.GetPitch(ratio, out var half, out var hostPitch);
            if (modules.Width < 35 * half) continue;
            var category = 0;
            for (var bit = 0; bit < 14; bit++) {
                var x = bit < 7 ? 2 : 45 - bit * 2;
                var y = bit < 7 ? 31 - bit * 2 : 2;
                if (modules.Get(modules.Width - 1 - x * half, modules.Height - 1 - y * half)) category |= 1 << bit;
            }
            for (var version = 1; version <= 40; version++) {
                if (DecodeBudget.ShouldAbort(token)) return false;
                var hostSize = version * 4 + 17;
                if (D3AqrLayout.GetSize(hostSize, half, hostPitch, out var offset) != modules.Width) continue;
                var frame = D3AqrLayout.BuildFrame(hostSize, category, half, hostPitch, out _);
                var valid = true;
                var end = offset + hostSize * hostPitch;
                for (var y = 0; valid && y < modules.Height; y++) {
                    if (DecodeBudget.ShouldAbort(token)) return false;
                    for (var x = 0; x < modules.Width; x++) {
                        if (x >= offset && x < end && y >= offset && y < end) continue;
                        if (modules.Get(x, y) != frame[x, y]) { valid = false; break; }
                    }
                }
                if (!valid) continue;
                var host = new BitMatrix(hostSize, hostSize);
                for (var y = 0; valid && y < hostSize; y++) {
                    if (DecodeBudget.ShouldAbort(token)) return false;
                    for (var x = 0; valid && x < hostSize; x++) {
                        var value = modules.Get(offset + x * hostPitch, offset + y * hostPitch);
                        host[x, y] = value;
                        for (var dy = 0; valid && dy < hostPitch; dy++)
                            for (var dx = 0; dx < hostPitch; dx++)
                                if (modules.Get(offset + x * hostPitch + dx, offset + y * hostPitch + dy) != value) { valid = false; break; }
                    }
                }
                if (valid && QrDecoder.TryDecode(host, out var qr) && qr.Version == version) {
                    decoded = new D3AqrDecoded(category, ratio, qr);
                    return true;
                }
            }
        }
        return false;
    }
}

/// <summary>The decoded D3 category and complete QR host result.</summary>
public sealed class D3AqrDecoded {
    /// <summary>Gets the fourteen-bit category.</summary>
    public int Category { get; }
    /// <summary>Gets the D3 to QR module-pitch ratio.</summary>
    public double HostRatio { get; }
    /// <summary>Gets the QR payload, encoding and control metadata.</summary>
    public QrDecoded Host { get; }
    /// <summary>Gets the host text.</summary>
    public string Text => Host.Text;
    internal D3AqrDecoded(int category, double ratio, QrDecoded host) { Category = category; HostRatio = ratio; Host = host; }
}
