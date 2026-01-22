using System;
using System.Numerics;

namespace CodeGlyphX.Postal;

internal static class UspsImbHelpers {
    internal static void SplitContent(string content, out string tracker, out string zip) {
        if (content is null) throw new ArgumentNullException(nameof(content));
        content = content.Trim();
        if (content.Length == 0) throw new InvalidOperationException("USPS IMB content cannot be empty.");
        if (content.Length > 32) throw new InvalidOperationException("USPS IMB content cannot exceed 32 characters.");

        var dashIndex = content.IndexOf('-');
        if (dashIndex >= 0) {
            if (content.LastIndexOf('-') != dashIndex) throw new InvalidOperationException("USPS IMB supports a single '-' separator only.");
            tracker = content.Substring(0, dashIndex);
            zip = content.Substring(dashIndex + 1);
        } else {
            tracker = content;
            zip = string.Empty;
        }

        if (tracker.Length != 20) throw new InvalidOperationException("USPS IMB tracking code must be 20 digits.");
        if (!IsDigitsOnly(tracker)) throw new InvalidOperationException("USPS IMB tracking code must contain digits only.");

        if (zip.Length != 0 && zip.Length != 5 && zip.Length != 9 && zip.Length != 11) {
            throw new InvalidOperationException("USPS IMB routing code must be 5, 9, or 11 digits (or omitted).");
        }
        if (zip.Length > 0 && !IsDigitsOnly(zip)) {
            throw new InvalidOperationException("USPS IMB routing code must contain digits only.");
        }
    }

    internal static BigInteger BuildAccum(string tracker, string zip) {
        var accum = ParseDigits(zip);
        accum += GetZipAdder(zip.Length);
        accum *= 10;
        accum += tracker[0] - '0';
        accum *= 5;
        accum += tracker[1] - '0';
        for (var i = 2; i < tracker.Length; i++) {
            accum *= 10;
            accum += tracker[i] - '0';
        }
        return accum;
    }

    internal static bool TryDecodeAccum(BigInteger accum, out string tracker, out string zip) {
        var trackChars = new char[20];
        for (var i = 19; i >= 2; i--) {
            var digit = (int)(accum % 10);
            if ((uint)digit > 9) { tracker = string.Empty; zip = string.Empty; return false; }
            trackChars[i] = (char)('0' + digit);
            accum /= 10;
        }

        var digit1 = (int)(accum % 5);
        if ((uint)digit1 > 4) { tracker = string.Empty; zip = string.Empty; return false; }
        trackChars[1] = (char)('0' + digit1);
        accum /= 5;

        var digit0 = (int)(accum % 10);
        if ((uint)digit0 > 9) { tracker = string.Empty; zip = string.Empty; return false; }
        trackChars[0] = (char)('0' + digit0);
        accum /= 10;

        if (!TryDecodeZip(accum, out zip)) {
            tracker = string.Empty;
            return false;
        }

        tracker = new string(trackChars);
        return true;
    }

    internal static int ComputeCrc(BigInteger accum) {
        Span<int> bytes = stackalloc int[13];
        for (var i = 0; i < bytes.Length; i++) {
            var shift = 96 - (8 * i);
            var value = (accum >> shift) & 255;
            bytes[i] = (int)value;
        }
        return ComputeCrc(bytes);
    }

    private static int ComputeCrc(ReadOnlySpan<int> bytes) {
        const int generatorPolynomial = 0x0F35;
        var frameCheckSequence = 0x07FF;
        var byteArrayPtr = 0;

        var data = bytes[byteArrayPtr] << 5;
        byteArrayPtr++;
        for (var bit = 2; bit < 8; bit++) {
            if (((frameCheckSequence ^ data) & 0x400) != 0) {
                frameCheckSequence = (frameCheckSequence << 1) ^ generatorPolynomial;
            } else {
                frameCheckSequence <<= 1;
            }
            frameCheckSequence &= 0x7FF;
            data <<= 1;
        }

        for (var byteIndex = 1; byteIndex < 13; byteIndex++) {
            data = bytes[byteArrayPtr] << 3;
            byteArrayPtr++;
            for (var bit = 0; bit < 8; bit++) {
                if (((frameCheckSequence ^ data) & 0x400) != 0) {
                    frameCheckSequence = (frameCheckSequence << 1) ^ generatorPolynomial;
                } else {
                    frameCheckSequence <<= 1;
                }
                frameCheckSequence &= 0x7FF;
                data <<= 1;
            }
        }

        return frameCheckSequence;
    }

    private static bool TryDecodeZip(BigInteger accum, out string zip) {
        if (accum < 0) { zip = string.Empty; return false; }
        if (accum.IsZero) { zip = string.Empty; return true; }

        var adder11 = new BigInteger(1000100001);
        var adder9 = new BigInteger(100001);
        var adder5 = new BigInteger(1);

        if (accum >= adder11) {
            var value = accum - adder11;
            if (value <= 99999999999L) {
                zip = value.ToString().PadLeft(11, '0');
                return true;
            }
        }

        if (accum >= adder9) {
            var value = accum - adder9;
            if (value <= 999999999L) {
                zip = value.ToString().PadLeft(9, '0');
                return true;
            }
        }

        if (accum >= adder5) {
            var value = accum - adder5;
            if (value <= 99999) {
                zip = value.ToString().PadLeft(5, '0');
                return true;
            }
        }

        zip = string.Empty;
        return false;
    }

    private static BigInteger ParseDigits(string digits) {
        if (digits.Length == 0) return BigInteger.Zero;
        BigInteger value = 0;
        for (var i = 0; i < digits.Length; i++) {
            value *= 10;
            value += digits[i] - '0';
        }
        return value;
    }

    private static BigInteger GetZipAdder(int length) {
        return length switch {
            0 => BigInteger.Zero,
            5 => new BigInteger(1),
            9 => new BigInteger(100001),
            11 => new BigInteger(1000100001),
            _ => throw new InvalidOperationException("USPS IMB routing code length is invalid.")
        };
    }

    private static bool IsDigitsOnly(string value) {
        for (var i = 0; i < value.Length; i++) {
            var ch = value[i];
            if (ch < '0' || ch > '9') return false;
        }
        return true;
    }
}
