using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CodeGlyphX.Gs1Composite;

internal static partial class CompositeBitStreamCodec {
    private const string Ai90CompactLetters = "BDHIJKLNPQRSTVWZ";

    private static bool TryEncodeAi90(string text, List<bool> bits, ref int mode, out string general, out bool alphaPad) {
        general = text; alphaPad = false;
        var end = text.IndexOf(Gs1.GroupSeparator, 2);
        if (end < 0) end = text.Length;
        var letterPosition = 2;
        if (end <= 2 || text[2] == '0') return false;
        while (letterPosition < end && letterPosition < 5 && text[letterPosition] >= '0' && text[letterPosition] <= '9')
            letterPosition++;
        if (letterPosition >= end || letterPosition > 5 || text[letterPosition] < 'A' || text[letterPosition] > 'Z') return false;
        var numericPrefix = letterPosition == 2 ? 0 : int.Parse(text.Substring(2, letterPosition - 2), CultureInfo.InvariantCulture);
        var alpha = 0; var numeric = 0; var other = 0;
        for (var i = letterPosition + 1; i < end; i++) {
            if (text[i] >= 'A' && text[i] <= 'Z') alpha++;
            else if (text[i] >= '0' && text[i] <= '9') numeric++;
            else other++;
        }
        var alphaMode = other == 0 && alpha > numeric;
        Append(bits, 3, 2);
        if (alphaMode) Append(bits, 3, 2);
        else if (other == 0 && alpha == 0) Append(bits, 2, 2);
        else { bits.Add(false); mode = Alphanumeric; }
        var croppedAi = 0;
        if (end < text.Length) {
            if (text.Length > end + 3 && text.Substring(end + 1, 2) == "21") croppedAi = 2;
            else if (text.Length > end + 5 && text.Substring(end + 1, 4) == "8004") croppedAi = 4;
        }
        if (croppedAi == 0) bits.Add(false);
        else Append(bits, croppedAi == 2 ? 2 : 3, 2);
        var compactLetter = numericPrefix < 31 ? Ai90CompactLetters.IndexOf(text[letterPosition]) : -1;
        if (compactLetter >= 0) {
            Append(bits, numericPrefix, 5);
            Append(bits, compactLetter, 4);
        } else {
            Append(bits, 31, 5);
            Append(bits, numericPrefix, 10);
            Append(bits, text[letterPosition] - 'A', 5);
        }
        var start = letterPosition + 1;
        if (alphaMode) {
            for (var i = start; i < end; i++) {
                var ch = text[i];
                if (ch >= 'A' && ch <= 'Z') Append(bits, ch - 'A', 5);
                else Append(bits, ch - '0' + 52, 6);
            }
            start = end;
            if (end < text.Length) { Append(bits, 31, 5); start++; }
        }
        general = text.Substring(start);
        if (croppedAi > 0) general = general.Remove(end + 1 - start, croppedAi);
        alphaPad = alphaMode && general.Length == 0;
        return true;
    }

    private static bool TryDecodeAi90(bool[] bits, out string text) {
        text = string.Empty;
        var position = 2;
        if (!TryRead(bits, ref position, 1, out var initialMode)) return false;
        var mode = Alphanumeric;
        var alphaMode = false;
        if (initialMode != 0) {
            if (!TryRead(bits, ref position, 1, out var special)) return false;
            mode = Numeric; alphaMode = special != 0;
        }
        if (!TryRead(bits, ref position, 1, out var cropFlag)) return false;
        var croppedAi = string.Empty;
        if (cropFlag != 0) {
            if (!TryRead(bits, ref position, 1, out var kind)) return false;
            croppedAi = kind == 0 ? "21" : "8004";
        }
        if (!TryRead(bits, ref position, 5, out var number)) return false;
        int letter;
        if (number == 31) {
            if (!TryRead(bits, ref position, 10, out number) || number > 999
                || !TryRead(bits, ref position, 5, out letter) || letter > 25) return false;
            letter += 'A';
        } else {
            if (!TryRead(bits, ref position, 4, out var compact) || compact >= Ai90CompactLetters.Length) return false;
            letter = Ai90CompactLetters[compact];
        }
        var prefix = "90" + (number == 0 ? string.Empty : number.ToString(CultureInfo.InvariantCulture)) + (char)letter;
        var alphaText = new StringBuilder();
        if (alphaMode) {
            while (position < bits.Length) {
                if (bits.Length - position < 5) {
                    // A truncated terminal FNC1 is permitted at the symbol boundary.
                    while (position < bits.Length) if (!bits[position++]) return false;
                    if (croppedAi.Length > 0) return false;
                    text = prefix + alphaText;
                    return true;
                }
                var character = Peek(bits, position, 5);
                if (character == 31) { position += 5; break; }
                if (character < 26) {
                    position += 5; alphaText.Append((char)('A' + character));
                } else {
                    if (!TryRead(bits, ref position, 6, out var digit) || digit < 52 || digit > 61) return false;
                    alphaText.Append((char)('0' + digit - 52));
                }
            }
        }
        var general = string.Empty;
        if (position < bits.Length && !MatchesPadding(bits, position, mode)
            && !TryDecodeGeneralField(bits, position, mode, out general)) return false;
        if (alphaMode) {
            if (croppedAi.Length > 0 && general.Length == 0) return false;
            text = prefix + alphaText + (general.Length == 0 ? string.Empty : Gs1.GroupSeparator + croppedAi + general);
        } else {
            var tail = general;
            if (croppedAi.Length > 0) {
                var boundary = general.IndexOf(Gs1.GroupSeparator);
                if (boundary < 0) return false;
                tail = general.Insert(boundary + 1, croppedAi);
            }
            text = prefix + tail;
        }
        return true;
    }
}
