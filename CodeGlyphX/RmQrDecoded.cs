using System;

namespace CodeGlyphX;

/// <summary>
/// Result of decoding a rectangular Micro QR (rMQR) module grid.
/// </summary>
public sealed class RmQrDecoded {
    /// <summary>Gets the decoded ISO/IEC 23941 symbol version (1..32).</summary>
    public int Version { get; }

    /// <summary>Gets the human-readable version name, such as R11x27.</summary>
    public string VersionName { get; }

    /// <summary>Gets the decoded error correction level.</summary>
    public QrErrorCorrectionLevel ErrorCorrectionLevel { get; }

    /// <summary>Gets whether FNC1 in first position identifies a GS1 payload.</summary>
    public bool IsGs1 { get; }

    /// <summary>Gets the last decoded ECI assignment number, when present.</summary>
    public int? EciAssignmentNumber { get; }

    /// <summary>Gets the decoded payload bytes.</summary>
    public byte[] Bytes { get; }

    /// <summary>Gets the decoded payload interpreted as text.</summary>
    public string Text { get; }

    internal RmQrDecoded(
        int version,
        string versionName,
        QrErrorCorrectionLevel ecc,
        bool isGs1,
        int? eciAssignmentNumber,
        byte[] bytes,
        string text) {
        Version = version;
        VersionName = versionName ?? throw new ArgumentNullException(nameof(versionName));
        ErrorCorrectionLevel = ecc;
        IsGs1 = isGs1;
        EciAssignmentNumber = eciAssignmentNumber;
        Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
        Text = text ?? throw new ArgumentNullException(nameof(text));
    }
}
