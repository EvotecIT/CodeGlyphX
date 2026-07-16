// Portions adapted from the Zint backend, Copyright (c) Robin Stuart and contributors.
// Licensed under BSD-3-Clause; see THIRD-PARTY-NOTICES.md.

using System;
using System.Collections.Generic;
using System.Text;
using CodeGlyphX.Internal;

namespace CodeGlyphX.HanXin;

internal static class HanXinPayloadCodec {
    internal sealed class Decoded {
        internal string Text { get; }
        internal byte[] Bytes { get; }
        internal int[] EciAssignments { get; }
        internal Decoded(string text, byte[] bytes, int[] eciAssignments) { Text = text; Bytes = bytes; EciAssignments = eciAssignments; }
    }

    private sealed class Bits {
        private readonly List<bool> _values = new();
        internal int Count => _values.Count;
        internal void Append(int value, int count) { for (var bit = count - 1; bit >= 0; bit--) _values.Add((value & 1 << bit) != 0); }
        internal byte[] ToBytes() {
            var result = new byte[(_values.Count + 7) / 8];
            for (var i = 0; i < _values.Count; i++) if (_values[i]) result[i >> 3] |= (byte)(0x80 >> (i & 7));
            return result;
        }
    }

    private ref struct Reader {
        private readonly ReadOnlySpan<byte> _bytes;
        private int _position;
        internal int Remaining => _bytes.Length * 8 - _position;
        internal Reader(ReadOnlySpan<byte> bytes) { _bytes = bytes; _position = 0; }
        internal bool TryRead(int count, out int value) {
            value = 0;
            if (count < 0 || count > 24 || Remaining < count) return false;
            for (var i = 0; i < count; i++) { value = value << 1 | ((_bytes[_position >> 3] >> (7 - (_position & 7))) & 1); _position++; }
            return true;
        }
    }

    private sealed class PayloadBuilder {
        private readonly List<byte> _all = new();
        private readonly List<byte> _pending = new();
        private readonly StringBuilder _text = new();
        private Encoding _encoding = EncodingUtils.Latin1;
        internal byte[] Bytes => _all.ToArray();
        internal string Text { get { Flush(); return _text.ToString(); } }
        internal void Add(byte value) { _all.Add(value); _pending.Add(value); }
        internal void Add(byte[] values) { foreach (var value in values) Add(value); }
        internal void Eci(int assignment) { Flush(); _encoding = MapEncoding(assignment) ?? _encoding; }
        private void Flush() { if (_pending.Count == 0) return; _text.Append(_encoding.GetString(_pending.ToArray())); _pending.Clear(); }
        private static Encoding? MapEncoding(int eci) {
            return EncodingUtils.TryGetEncoding(eci, out var encoding) ? encoding : null;
        }
    }

    internal static byte[] EncodeText(string text, HanXinEncodingOptions options, out int bitLength) {
        if (text is null) throw new ArgumentNullException(nameof(text));
        var mode = ResolveMode(text, options);
        var eci = options.EciAssignmentNumber;
        byte[] bytes;
        if (mode == HanXinEncodingMode.Binary) {
            var encoding = EncodingUtils.ResolveTextEncoding(text, options.TextEncoding, eci, "Han Xin", out eci);
            bytes = EncodingUtils.GetBytesStrict(encoding, text, nameof(text));
        } else {
            if (mode == HanXinEncodingMode.Text && !CanEncodeText(text)) {
                throw new ArgumentException("Text Han Xin mode accepts only characters from the Han Xin text-compaction repertoire.", nameof(text));
            }
            bytes = EncodingUtils.Latin1.GetBytes(text);
        }
        return Encode(bytes, mode, eci ?? 0, out bitLength);
    }

    internal static byte[] EncodeBytes(byte[] bytes, HanXinEncodingOptions options, out int bitLength) =>
        Encode(bytes, HanXinEncodingMode.Binary, options.EciAssignmentNumber ?? 0, out bitLength);

    private static byte[] Encode(byte[] data, HanXinEncodingMode mode, int eci, out int bitLength) {
        if (data.Length == 0) throw new ArgumentException("Han Xin Code requires a non-empty payload.", nameof(data));
        if (eci is < 0 or > 999999) throw new ArgumentOutOfRangeException(nameof(eci));
        var bits = new Bits();
        AppendEci(bits, eci);
        if (mode == HanXinEncodingMode.Numeric) EncodeNumeric(bits, data);
        else if (mode == HanXinEncodingMode.Text) EncodeText(bits, data);
        else EncodeBinary(bits, data);
        bitLength = bits.Count;
        return bits.ToBytes();
    }

    private static void AppendEci(Bits bits, int eci) {
        if (eci == 0) return;
        bits.Append(8, 4);
        if (eci <= 127) bits.Append(eci, 8);
        else if (eci <= 16383) { bits.Append(2, 2); bits.Append(eci, 14); }
        else { bits.Append(6, 3); bits.Append(eci, 21); }
    }

    private static void EncodeNumeric(Bits bits, byte[] data) {
        bits.Append(1, 4);
        var lastCount = 0;
        for (var i = 0; i < data.Length;) {
            lastCount = Math.Min(3, data.Length - i);
            var value = 0;
            for (var j = 0; j < lastCount; j++) {
                if (data[i + j] is < (byte)'0' or > (byte)'9') throw new ArgumentException("Numeric Han Xin mode accepts digits only.", nameof(data));
                value = value * 10 + data[i + j] - '0';
            }
            bits.Append(value, 10); i += lastCount;
        }
        bits.Append(1020 + lastCount, 10);
    }

    private static void EncodeText(Bits bits, byte[] data) {
        bits.Append(2, 4);
        var submode = 1;
        for (var i = 0; i < data.Length; i++) {
            var next = IsText1(data[i]) ? 1 : 2;
            if (next == 2 && Text2(data[i]) < 0) throw new ArgumentException("Text Han Xin mode accepts ASCII characters only.", nameof(data));
            if (next != submode) { bits.Append(62, 6); submode = next; }
            bits.Append(submode == 1 ? Text1(data[i]) : Text2(data[i]), 6);
        }
        bits.Append(63, 6);
    }

    private static void EncodeBinary(Bits bits, byte[] data) {
        if (data.Length > 8191) throw new ArgumentException("A Han Xin binary segment cannot exceed 8191 bytes.", nameof(data));
        bits.Append(3, 4); bits.Append(data.Length, 13);
        for (var i = 0; i < data.Length; i++) bits.Append(data[i], 8);
    }

    internal static bool TryDecode(byte[] dataCodewords, out Decoded decoded) {
        decoded = null!;
        var reader = new Reader(dataCodewords);
        var payload = new PayloadBuilder();
        var assignments = new List<int>();
        while (reader.Remaining >= 4) {
            if (!reader.TryRead(4, out var mode)) return false;
            if (mode == 0) break;
            if (mode == 8) {
                if (!ReadEci(ref reader, out var eci)) return false;
                assignments.Add(eci); payload.Eci(eci); continue;
            }
            if (mode == 1) { if (!DecodeNumeric(ref reader, payload)) return false; continue; }
            if (mode == 2) { if (!DecodeText(ref reader, payload)) return false; continue; }
            if (mode == 3) { if (!DecodeBinary(ref reader, payload)) return false; continue; }
            return false;
        }
        decoded = new Decoded(payload.Text, payload.Bytes, assignments.ToArray());
        return decoded.Bytes.Length > 0;
    }

    private static bool ReadEci(ref Reader reader, out int eci) {
        eci = 0;
        if (!reader.TryRead(1, out var first)) return false;
        if (first == 0) { if (!reader.TryRead(7, out var rest)) return false; eci = rest; return true; }
        if (!reader.TryRead(1, out var second)) return false;
        if (second == 0) { if (!reader.TryRead(14, out eci)) return false; return true; }
        if (!reader.TryRead(1, out var third) || third != 0 || !reader.TryRead(21, out eci)) return false;
        return eci <= 999999;
    }

    private static bool DecodeNumeric(ref Reader reader, PayloadBuilder payload) {
        var values = new List<int>();
        while (reader.TryRead(10, out var value)) {
            if (value is >= 1021 and <= 1023) {
                if (values.Count == 0) return false;
                for (var i = 0; i < values.Count; i++) {
                    var digits = i == values.Count - 1 ? value - 1020 : 3;
                    var text = values[i].ToString().PadLeft(digits, '0');
                    if (text.Length != digits) return false;
                    foreach (var ch in text) payload.Add((byte)ch);
                }
                return true;
            }
            if (value > 999) return false;
            values.Add(value);
        }
        return false;
    }

    private static bool DecodeText(ref Reader reader, PayloadBuilder payload) {
        var submode = 1;
        while (reader.TryRead(6, out var value)) {
            if (value == 63) return true;
            if (value == 62) { submode = submode == 1 ? 2 : 1; continue; }
            var character = submode == 1 ? DecodeText1(value) : DecodeText2(value);
            if (character < 0) return false;
            payload.Add((byte)character);
        }
        return false;
    }

    private static bool DecodeBinary(ref Reader reader, PayloadBuilder payload) {
        if (!reader.TryRead(13, out var count) || count < 1 || reader.Remaining < count * 8) return false;
        for (var i = 0; i < count; i++) { if (!reader.TryRead(8, out var value)) return false; payload.Add((byte)value); }
        return true;
    }

    private static HanXinEncodingMode ResolveMode(string text, HanXinEncodingOptions options) {
        if (options.Mode != HanXinEncodingMode.Auto) return options.Mode;
        var numeric = text.Length > 0;
        var textCompaction = text.Length > 0;
        for (var i = 0; i < text.Length; i++) {
            numeric &= text[i] is >= '0' and <= '9';
            textCompaction &= IsTextCharacter(text[i]);
        }
        return numeric ? HanXinEncodingMode.Numeric : textCompaction ? HanXinEncodingMode.Text : HanXinEncodingMode.Binary;
    }

    private static bool CanEncodeText(string text) {
        for (var i = 0; i < text.Length; i++) if (!IsTextCharacter(text[i])) return false;
        return text.Length > 0;
    }

    private static bool IsTextCharacter(char value) {
        if (value > 127) return false;
        var ascii = (byte)value;
        return IsText1(ascii) || Text2(ascii) >= 0;
    }

    private static bool IsText1(byte value) => value is >= (byte)'0' and <= (byte)'9' || value is >= (byte)'A' and <= (byte)'Z' || value is >= (byte)'a' and <= (byte)'z';
    private static int Text1(byte value) => value <= '9' ? value - '0' : value <= 'Z' ? value - 'A' + 10 : value - 'a' + 36;
    private static int Text2(byte value) => value <= 27 ? value : value is >= 32 and <= 47 ? value - 32 + 28 : value is >= 58 and <= 64 ? value - 58 + 44 : value is >= 91 and <= 96 ? value - 91 + 51 : value is >= 123 and <= 127 ? value - 123 + 57 : -1;
    private static int DecodeText1(int value) => value <= 9 ? value + '0' : value <= 35 ? value - 10 + 'A' : value <= 61 ? value - 36 + 'a' : -1;
    private static int DecodeText2(int value) => value <= 27 ? value : value <= 43 ? value - 28 + 32 : value <= 50 ? value - 44 + 58 : value <= 56 ? value - 51 + 91 : value <= 61 ? value - 57 + 123 : -1;
}
