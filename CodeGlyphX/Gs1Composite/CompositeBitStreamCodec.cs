// Portions adapted from the Zint backend, Copyright (c) Robin Stuart and contributors.
// Licensed under BSD-3-Clause; see THIRD-PARTY-NOTICES.md.

using System;
using System.Collections.Generic;
using System.Text;

namespace CodeGlyphX.Gs1Composite;

internal static class CompositeBitStreamCodec {
    private const int Numeric = 0;
    private const int Alphanumeric = 1;
    private const int IsoIec646 = 2;
    private const string AlphanumericPunctuation = "*,-./";
    private const string IsoIecPunctuation = "!\"%&'()*+,-./:;<=>?_ ";

    internal static bool[] Encode(string elementString, Gs1CompositeComponent component, int columns,
        int linearWidth, out int pdfColumns, out int errorCorrectionLevel) {
        if (elementString is null) throw new ArgumentNullException(nameof(elementString));
        if (elementString.Length == 0) throw new ArgumentException("The composite component cannot be empty.", nameof(elementString));
        if (elementString[0] == Gs1.GroupSeparator) throw new ArgumentException("A GS1 element string cannot start with FNC1.", nameof(elementString));

        // ISO/IEC 24723 encodation method 0. It accepts the complete GS1 general
        // field and remains interoperable even when the date/lot and AI 90
        // compaction methods would produce a shorter symbol.
        var bits = new List<bool>(elementString.Length * 7 + 32) { false };
        var mode = Numeric;
        EncodeGeneralField(elementString, bits, ref mode, out var lastDigit);

        var target = GetTargetBitLength(bits.Count, component, columns, linearWidth,
            out pdfColumns, out errorCorrectionLevel);
        if (target == 0) throw new ArgumentException($"Input is too long for {component}.", nameof(elementString));

        if (lastDigit >= 0) {
            var remainder = target - bits.Count;
            if (remainder is >= 4 and <= 6) {
                Append(bits, lastDigit + 1, 4);
                if (remainder > 4) Append(bits, 0, remainder - 4);
            } else {
                Append(bits, 11 * lastDigit + 18, 7);
                target = GetTargetBitLength(bits.Count, component, columns, linearWidth,
                    out pdfColumns, out errorCorrectionLevel);
                if (target == 0) throw new ArgumentException($"Input is too long for {component}.", nameof(elementString));
            }
        }

        if (mode == Numeric) Append(bits, 0, 4);
        while (bits.Count < target) Append(bits, 4, 5);
        if (bits.Count > target) bits.RemoveRange(target, bits.Count - target);
        return bits.ToArray();
    }

    internal static bool TryDecode(bool[] bits, out string elementString) {
        elementString = string.Empty;
        if (bits is null || bits.Length == 0 || bits[0]) return false;

        var sb = new StringBuilder(bits.Length / 5);
        var position = 1;
        var mode = Numeric;
        while (position < bits.Length) {
            if (MatchesPadding(bits, position, mode)) {
                elementString = sb.ToString();
                return elementString.Length != 0;
            }

            if (mode == Numeric) {
                var remaining = bits.Length - position;
                if (remaining is >= 4 and <= 6) {
                    if (!TryRead(bits, ref position, 4, out var terminal) || terminal is < 1 or > 10) return false;
                    for (; position < bits.Length; position++) if (bits[position]) return false;
                    sb.Append((char)('0' + terminal - 1));
                    elementString = sb.ToString();
                    return true;
                }
                if (Peek(bits, position, 4) == 0) {
                    position += 4;
                    mode = Alphanumeric;
                    continue;
                }
                if (!TryRead(bits, ref position, 7, out var value) || value < 8) return false;
                value -= 8;
                var first = value / 11;
                var second = value % 11;
                if (first > 10 || second > 10) return false;
                if (first == 10) sb.Append(Gs1.GroupSeparator);
                else sb.Append((char)('0' + first));
                if (second == 10) {
                    if (!MatchesPadding(bits, position, mode)) sb.Append(Gs1.GroupSeparator);
                } else {
                    sb.Append((char)('0' + second));
                }
                continue;
            }

            if (mode == Alphanumeric) {
                if (Peek(bits, position, 5) == 15) {
                    position += 5;
                    sb.Append(Gs1.GroupSeparator);
                    mode = Numeric;
                } else if (Peek(bits, position, 5) == 4) {
                    position += 5;
                    mode = IsoIec646;
                } else if (Peek(bits, position, 3) == 0) {
                    position += 3;
                    mode = Numeric;
                } else {
                    var five = Peek(bits, position, 5);
                    if (five is >= 5 and <= 14) {
                        position += 5;
                        sb.Append((char)('0' + five - 5));
                    } else if (!TryRead(bits, ref position, 6, out var six)) {
                        return false;
                    } else if (six is >= 32 and <= 57) {
                        sb.Append((char)('A' + six - 32));
                    } else if (six is >= 58 and <= 62) {
                        sb.Append(AlphanumericPunctuation[six - 58]);
                    } else {
                        return false;
                    }
                }
                continue;
            }

            if (Peek(bits, position, 5) == 15) {
                position += 5;
                sb.Append(Gs1.GroupSeparator);
                mode = Numeric;
            } else if (Peek(bits, position, 3) == 0) {
                position += 3;
                mode = Numeric;
            } else if (Peek(bits, position, 5) == 4) {
                position += 5;
                mode = Alphanumeric;
            } else {
                var five = Peek(bits, position, 5);
                if (five is >= 5 and <= 14) {
                    position += 5;
                    sb.Append((char)('0' + five - 5));
                    continue;
                }
                var seven = Peek(bits, position, 7);
                if (seven is >= 64 and <= 89) {
                    position += 7;
                    sb.Append((char)(seven + 1));
                } else if (seven is >= 90 and <= 115) {
                    position += 7;
                    sb.Append((char)(seven + 7));
                } else if (!TryRead(bits, ref position, 8, out var eight) || eight < 232 || eight >= 232 + IsoIecPunctuation.Length) {
                    return false;
                } else {
                    sb.Append(IsoIecPunctuation[eight - 232]);
                }
            }
        }

        elementString = sb.ToString();
        return elementString.Length != 0;
    }

    private static void EncodeGeneralField(string value, List<bool> bits, ref int mode, out int lastDigit) {
        lastDigit = -1;
        var index = 0;
        while (index < value.Length) {
            var type = GetType(value[index]);
            if (type < 0) throw new ArgumentException($"Character U+{(int)value[index]:X4} is not valid in a GS1 Composite general field.", nameof(value));

            if (mode == Numeric) {
                if (index + 1 < value.Length && type == Numeric && GetType(value[index + 1]) == Numeric) {
                    var first = value[index] == Gs1.GroupSeparator ? 10 : value[index] - '0';
                    var second = value[index + 1] == Gs1.GroupSeparator ? 10 : value[index + 1] - '0';
                    Append(bits, 11 * first + second + 8, 7);
                    index += 2;
                } else if (index + 1 == value.Length && type == Numeric) {
                    if (value[index] == Gs1.GroupSeparator) throw new ArgumentException("A GS1 element string cannot end with FNC1.", nameof(value));
                    lastDigit = value[index] - '0';
                    index++;
                } else {
                    Append(bits, 0, 4);
                    mode = Alphanumeric;
                }
                continue;
            }

            if (mode == Alphanumeric) {
                if (value[index] == Gs1.GroupSeparator) {
                    Append(bits, 15, 5);
                    mode = Numeric;
                    index++;
                } else if (type == IsoIec646) {
                    Append(bits, 4, 5);
                    mode = IsoIec646;
                } else if (Next(value, index, 6, Numeric, -1) || NextTerminate(value, index, 4, 5, Numeric)) {
                    Append(bits, 0, 3);
                    mode = Numeric;
                } else if (IsAsciiDigit(value[index])) {
                    Append(bits, value[index] - 43, 5);
                    index++;
                } else if (value[index] is >= 'A' and <= 'Z') {
                    Append(bits, value[index] - 33, 6);
                    index++;
                } else {
                    Append(bits, AlphanumericPunctuation.IndexOf(value[index]) + 58, 6);
                    index++;
                }
                continue;
            }

            if (value[index] == Gs1.GroupSeparator) {
                Append(bits, 15, 5);
                mode = Numeric;
                index++;
                continue;
            }

            var nextTenNotIso = NextNone(value, index, 10, IsoIec646);
            if (nextTenNotIso && Next(value, index, 4, Numeric, -1)) {
                Append(bits, 0, 3);
                mode = Numeric;
            } else if (nextTenNotIso && Next(value, index, 5, Alphanumeric, Numeric)) {
                Append(bits, 4, 5);
                mode = Alphanumeric;
            } else if (IsAsciiDigit(value[index])) {
                Append(bits, value[index] - 43, 5);
                index++;
            } else if (value[index] is >= 'A' and <= 'Z') {
                Append(bits, value[index] - 1, 7);
                index++;
            } else if (value[index] is >= 'a' and <= 'z') {
                Append(bits, value[index] - 7, 7);
                index++;
            } else {
                Append(bits, IsoIecPunctuation.IndexOf(value[index]) + 232, 8);
                index++;
            }
        }
    }

    private static int GetTargetBitLength(int count, Gs1CompositeComponent component, int columns, int linearWidth,
        out int pdfColumns, out int errorCorrectionLevel) {
        pdfColumns = columns;
        errorCorrectionLevel = 0;
        if (component == Gs1CompositeComponent.CcA) return FindTarget(count, columns, new[] {
            new[] { 59, 78, 88, 108, 118, 138, 167 },
            new[] { 78, 98, 118, 138, 167 },
            new[] { 78, 108, 138, 167, 197 }
        });
        if (component == Gs1CompositeComponent.CcB) return FindTarget(count, columns, new[] {
            new[] { 56, 104, 160, 208, 256, 296, 336 },
            new[] { 32, 72, 112, 152, 208, 304, 416, 536, 648, 768 },
            new[] { 56, 96, 152, 208, 264, 352, 496, 672, 840, 1016, 1184 }
        });

        var byteLength = (count + 7) >> 3;
        var used = byteLength / 6 * 5 + byteLength % 6;
        var ecc = used <= 40 ? 2 : used <= 160 ? 3 : used <= 320 ? 4 : used <= 833 ? 5 : used <= 865 ? 4 : 0;
        if (ecc == 0) return 0;
        var eccCount = 1 << (ecc + 1);
        used += eccCount + 3;
        var ccColumns = linearWidth == 68 ? 1 : (linearWidth - 52) / 17;
        if (ccColumns > 30) ccColumns = 30;
        if (ccColumns < 1) ccColumns = 1;
        var rows = (used + ccColumns - 1) / ccColumns;
        while (rows > 30 && ccColumns < 30) rows = (used + ++ccColumns - 1) / ccColumns;
        if (rows > 30) return 0;
        if (rows < 3) rows = 3;
        var targetCodewords = ccColumns * rows - eccCount - 3;
        var targetBytes = 6 * (targetCodewords / 5) + targetCodewords % 5;
        pdfColumns = ccColumns;
        errorCorrectionLevel = ecc;
        return targetBytes << 3;
    }

    private static int FindTarget(int count, int columns, int[][] targets) {
        if (columns is < 2 or > 4) return 0;
        var row = targets[columns - 2];
        for (var i = 0; i < row.Length; i++) if (count <= row[i]) return row[i];
        return 0;
    }

    private static int GetType(char value) {
        if (value == Gs1.GroupSeparator || IsAsciiDigit(value)) return Numeric;
        if (value is >= 'A' and <= 'Z' || AlphanumericPunctuation.IndexOf(value) >= 0) return Alphanumeric;
        if (value is >= 'a' and <= 'z' || IsoIecPunctuation.IndexOf(value) >= 0) return IsoIec646;
        return -1;
    }

    private static bool IsAsciiDigit(char value) => value is >= '0' and <= '9';

    private static bool Next(string value, int index, int count, int type, int alternative) {
        if (index + count > value.Length) return false;
        for (var i = 0; i < count; i++) {
            var current = GetType(value[index + i]);
            if (current != type && current != alternative) return false;
        }
        return true;
    }

    private static bool NextTerminate(string value, int index, int minimum, int maximum, int type) {
        var remaining = value.Length - index;
        if (remaining < minimum || remaining > maximum) return false;
        for (var i = index; i < value.Length; i++) if (GetType(value[i]) != type) return false;
        return true;
    }

    private static bool NextNone(string value, int index, int count, int type) {
        var end = Math.Min(value.Length, index + count);
        for (var i = index; i < end; i++) if (GetType(value[i]) == type) return false;
        return true;
    }

    private static bool MatchesPadding(bool[] bits, int position, int mode) {
        if (position >= bits.Length) return true;
        var index = position;
        if (mode == Numeric) {
            for (var i = 0; i < 4 && index < bits.Length; i++, index++) if (bits[index]) return false;
        }
        var pattern = new[] { false, false, true, false, false };
        for (var i = 0; index < bits.Length; index++, i++) if (bits[index] != pattern[i % 5]) return false;
        return true;
    }

    private static int Peek(bool[] bits, int position, int count) {
        if (position + count > bits.Length) return -1;
        var value = 0;
        for (var i = 0; i < count; i++) value = (value << 1) | (bits[position + i] ? 1 : 0);
        return value;
    }

    private static bool TryRead(bool[] bits, ref int position, int count, out int value) {
        value = Peek(bits, position, count);
        if (value < 0) return false;
        position += count;
        return true;
    }

    internal static void Append(List<bool> bits, int value, int count) {
        for (var bit = count - 1; bit >= 0; bit--) bits.Add(((value >> bit) & 1) != 0);
    }
}
