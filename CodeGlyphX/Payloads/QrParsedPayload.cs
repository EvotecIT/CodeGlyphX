using System;

namespace CodeGlyphX.Payloads;

/// <summary>
/// Parsed QR payload with detected type and optional raw value.
/// </summary>
public sealed class QrParsedPayload {
    /// <summary>
    /// Detected payload type.
    /// </summary>
    public QrPayloadType Type { get; }

    /// <summary>
    /// Raw payload text.
    /// </summary>
    public string Raw { get; }

    /// <summary>
    /// Gets the parsed payload object (type-specific), when available.
    /// </summary>
    public object? Value { get; }

    internal QrParsedPayload(QrPayloadType type, string raw, object? value) {
        Type = type;
        Raw = raw ?? throw new ArgumentNullException(nameof(raw));
        Value = value;
    }

    /// <summary>
    /// Attempts to get the parsed payload as a specific type.
    /// </summary>
    public bool TryGet<T>(out T value) {
        if (Value is T typed) {
            value = typed;
            return true;
        }
        value = default!;
        return false;
    }
}
