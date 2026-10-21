using System;
using System.Collections.Generic;
using System.Globalization;
using CodeGlyphX.CodeOne;
using CodeGlyphX.Internal;

namespace CodeGlyphX;

/// <summary>Encodes Code One A-H, numeric S, and compact T symbols.</summary>
public static class CodeOneEncoder {
    /// <summary>Encodes text with an explicit size or the smallest A-H size.</summary>
    /// <remarks>Version S encodes an integer; decoding returns its canonical digits without leading zeros.</remarks>
    public static CodeOneSymbol EncodeText(string text, CodeOneEncodingOptions? options = null) {
        if (text is null) throw new ArgumentNullException(nameof(text));
        options ??= new CodeOneEncodingOptions();
        Validate(options);
        if (options.Version is >= CodeOneVersion.S10 and <= CodeOneVersion.S30) return EncodeNumeric(text, options);
        if (options.IsGs1) text = Gs1.ElementString(text);
        var encoding = EncodingUtils.ResolveTextEncoding(text, null, options.EciAssignmentNumber, "Code One", out var eci);
        return Encode(EncodingUtils.GetBytesStrict(encoding, text, nameof(text)), options, eci);
    }

    /// <summary>Encodes exact payload bytes, with optional ECI interpretation.</summary>
    public static CodeOneSymbol EncodeBytes(byte[] bytes, CodeOneEncodingOptions? options = null) {
        if (bytes is null) throw new ArgumentNullException(nameof(bytes));
        options ??= new CodeOneEncodingOptions();
        Validate(options);
        if (options.Version is >= CodeOneVersion.S10 and <= CodeOneVersion.S30) {
            foreach (var value in bytes) if (value < '0' || value > '9') throw new ArgumentException("Version S accepts decimal digits only.", nameof(bytes));
            return EncodeNumeric(EncodingUtils.Latin1.GetString(bytes), options);
        }
        return Encode(bytes, options, options.EciAssignmentNumber);
    }

    private static void Validate(CodeOneEncodingOptions options) {
        if (options.Version is < CodeOneVersion.Auto or > CodeOneVersion.T48) throw new ArgumentOutOfRangeException(nameof(options.Version));
        if (options.EciAssignmentNumber is < 0 or > 999999) throw new ArgumentOutOfRangeException(nameof(options.EciAssignmentNumber));
        if (options.IsGs1 && options.EciAssignmentNumber.HasValue) throw new ArgumentException("Code One GS1 mode cannot be combined with ECI.", nameof(options));
    }

    private static CodeOneSymbol EncodeNumeric(string text, CodeOneEncodingOptions options) {
        if (options.IsGs1 || options.EciAssignmentNumber.HasValue) throw new ArgumentException("Version S has no GS1 or ECI header.", nameof(options));
        var maximumDigits = ((int)options.Version - (int)CodeOneVersion.S10 + 1) * 6;
        if (text.Length == 0 || text.Length > maximumDigits
            || !ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
            throw new ArgumentException("The integer exceeds the selected Code One S size.", nameof(text));
        var layout = CodeOneLayout.All[(int)options.Version - 1];
        var words = new int[layout.DataCount + layout.EccCount];
        number++;
        for (var i = layout.DataCount - 1; i >= 0; i--) { words[i] = (int)(number & 31); number >>= 5; }
        CodeOneErrorCorrection.Process(words, layout, decode: false);
        return new CodeOneSymbol(layout.Place(words), options.Version);
    }

    private static CodeOneSymbol Encode(byte[] bytes, CodeOneEncodingOptions options, int? eci) {
        if (options.IsGs1 && eci.HasValue) throw new ArgumentException("Code One GS1 mode cannot be combined with ECI.", nameof(options));
        if (bytes.Length == 0 || bytes.Length > 3550) throw new ArgumentException("Code One needs a nonempty payload within its capacity.", nameof(bytes));
        var prefix = new List<int>();
        if (options.IsGs1) prefix.Add(232);
        if (eci.HasValue) {
            prefix.Add(129); prefix.Add(93); // Explicit escape-interpretation header.
            var escaped = new List<byte>(bytes.Length + 7) { (byte)'\\' };
            escaped.AddRange(System.Text.Encoding.ASCII.GetBytes(eci.Value.ToString("D6", CultureInfo.InvariantCulture)));
            foreach (var value in bytes) { escaped.Add(value); if (value == '\\') escaped.Add(value); }
            bytes = escaped.ToArray();
        }
        foreach (var layout in CodeOneLayout.All) {
            if (options.Version == CodeOneVersion.Auto ? layout.Version > CodeOneVersion.H : layout.Version != options.Version) continue;
            var data = CodeOnePayloadEncoder.Encode(bytes, options.IsGs1, layout.DataCount - prefix.Count);
            if (data is null) continue;
            var words = new int[layout.DataCount + layout.EccCount];
            prefix.CopyTo(words);
            data.CopyTo(words, prefix.Count);
            for (var i = prefix.Count + data.Length; i < layout.DataCount; i++) words[i] = 129;
            CodeOneErrorCorrection.Process(words, layout, decode: false);
            return new CodeOneSymbol(layout.Place(words), layout.Version);
        }
        throw new ArgumentException("The payload exceeds the selected Code One size.", nameof(bytes));
    }
}
