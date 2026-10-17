using System;

namespace CodeGlyphX.D3Aqr;

/// <summary>Encodes the provisional D3 AQR category frame around a standard QR host.</summary>
/// <remarks>Implements the Rev C draft layout used by the pinned conformance reference. This format is provisional.</remarks>
public static class D3AqrEncoder {
    /// <summary>Encodes text with the existing QR engine and adds a fourteen-bit category.</summary>
    public static D3AqrSymbol EncodeText(string value, int category, double hostRatio = 2, QrEncodingOptions? options = null) =>
        Wrap(QrCodeEncoder.EncodeText(value, options ?? new QrEncodingOptions()), category, hostRatio);

    /// <summary>Adds a D3 category to an existing QR symbol, preserving its encoded payload and metadata.</summary>
    /// <param name="host">Encoded QR host without a quiet zone.</param>
    /// <param name="category">Category number from 0 through 16383.</param>
    /// <param name="hostRatio">D3 module pitch divided by QR module pitch: 2, 2.5 or 3.</param>
    public static D3AqrSymbol Wrap(QrCode host, int category, double hostRatio = 2) {
        if (host is null) throw new ArgumentNullException(nameof(host));
        if (category < 0 || category > 16383) throw new ArgumentOutOfRangeException(nameof(category));
        D3AqrLayout.GetPitch(hostRatio, out var half, out var hostPitch);
        var matrix = D3AqrLayout.BuildFrame(host.Size, category, half, hostPitch, out var offset);
        for (var y = 0; y < host.Size * hostPitch; y++)
            for (var x = 0; x < host.Size * hostPitch; x++) matrix[offset + x, offset + y] = host.Modules[x / hostPitch, y / hostPitch];
        return new D3AqrSymbol(matrix, host, category, hostRatio);
    }
}

/// <summary>A provisional D3 AQR symbol and its independently encoded QR host.</summary>
public sealed class D3AqrSymbol : MatrixSymbol {
    /// <summary>Gets the standard QR host and its version, mask and error correction.</summary>
    public QrCode Host { get; }
    /// <summary>Gets the fourteen-bit category.</summary>
    public int Category { get; }
    /// <summary>Gets the D3 to host module-pitch ratio.</summary>
    public double HostRatio { get; }
    internal D3AqrSymbol(BitMatrix matrix, QrCode host, int category, double hostRatio) : base(SymbolFormat.D3Aqr, matrix) {
        Host = host;
        Category = category;
        HostRatio = hostRatio;
    }
}
