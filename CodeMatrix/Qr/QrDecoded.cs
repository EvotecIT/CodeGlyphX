using System;

namespace CodeMatrix;

public sealed class QrDecoded {
    public int Version { get; }
    public QrErrorCorrectionLevel ErrorCorrectionLevel { get; }
    public int Mask { get; }

    public byte[] Bytes { get; }
    public string Text { get; }

    internal QrDecoded(int version, QrErrorCorrectionLevel ecc, int mask, byte[] bytes, string text) {
        Version = version;
        ErrorCorrectionLevel = ecc;
        Mask = mask;
        Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
        Text = text ?? throw new ArgumentNullException(nameof(text));
    }
}

