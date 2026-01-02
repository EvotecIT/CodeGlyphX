using System;

namespace CodeMatrix;

public sealed class QrCode {
    public int Version { get; }
    public QrErrorCorrectionLevel ErrorCorrectionLevel { get; }
    public int Mask { get; }
    public BitMatrix Modules { get; }

    public int Size => Modules.Width;

    public QrCode(int version, QrErrorCorrectionLevel errorCorrectionLevel, int mask, BitMatrix modules) {
        if (version is < 1 or > 40) throw new ArgumentOutOfRangeException(nameof(version));
        if (mask is < 0 or > 7) throw new ArgumentOutOfRangeException(nameof(mask));
        Modules = modules ?? throw new ArgumentNullException(nameof(modules));
        if (modules.Width != modules.Height) throw new ArgumentException("Matrix must be square.", nameof(modules));

        Version = version;
        ErrorCorrectionLevel = errorCorrectionLevel;
        Mask = mask;
    }
}

