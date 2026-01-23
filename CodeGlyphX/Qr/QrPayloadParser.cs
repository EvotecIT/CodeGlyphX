using System;
using System.Buffers;
using System.Collections.Generic;
using CodeGlyphX;

namespace CodeGlyphX.Qr;

internal readonly struct QrPayloadSegment {
    public QrTextEncoding Encoding { get; }
    public byte[] Buffer { get; }
    public int Offset { get; }
    public int Length { get; }

    public QrPayloadSegment(QrTextEncoding encoding, byte[] buffer, int offset, int length) {
        Encoding = encoding;
        Buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
        if (offset + length > buffer.Length) throw new ArgumentOutOfRangeException(nameof(length));
        Offset = offset;
        Length = length;
    }

    public ReadOnlySpan<byte> Span => new ReadOnlySpan<byte>(Buffer, Offset, Length);
}

internal static class QrPayloadParser {
    private const string AlphanumericTable = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";
    private readonly struct SegmentInfo {
        public QrTextEncoding Encoding { get; }
        public int Start { get; }
        public int Length { get; }

        public SegmentInfo(QrTextEncoding encoding, int start, int length) {
            Encoding = encoding;
            Start = start;
            Length = length;
        }
    }

    private ref struct SegmentBuilder {
        private Span<SegmentInfo> _buffer;
        private int _count;
        private List<SegmentInfo>? _overflow;

        public SegmentBuilder(Span<SegmentInfo> buffer) {
            _buffer = buffer;
            _count = 0;
            _overflow = null;
        }

        public void Add(QrTextEncoding encoding, int start, int length) {
            if (_overflow is not null) {
                _overflow.Add(new SegmentInfo(encoding, start, length));
                return;
            }
            if (_count < _buffer.Length) {
                _buffer[_count++] = new SegmentInfo(encoding, start, length);
                return;
            }
            _overflow = new List<SegmentInfo>(_buffer.Length * 2);
            for (var i = 0; i < _count; i++) _overflow.Add(_buffer[i]);
            _overflow.Add(new SegmentInfo(encoding, start, length));
        }

        public void BuildSegments(byte[] payload, out QrPayloadSegment[] segments) {
            var total = _overflow?.Count ?? _count;
            if (total == 0) {
                segments = Array.Empty<QrPayloadSegment>();
                return;
            }
            segments = new QrPayloadSegment[total];
            if (_overflow is not null) {
                for (var i = 0; i < _overflow.Count; i++) {
                    var seg = _overflow[i];
                    segments[i] = new QrPayloadSegment(seg.Encoding, payload, seg.Start, seg.Length);
                }
                return;
            }
            for (var i = 0; i < _count; i++) {
                var seg = _buffer[i];
                segments[i] = new QrPayloadSegment(seg.Encoding, payload, seg.Start, seg.Length);
            }
        }
    }

    public static bool TryParse(byte[] dataCodewords, int version, Func<bool>? shouldStop, out byte[] payload, out QrPayloadSegment[] segments, out QrStructuredAppend? structuredAppend, out QrFnc1Mode fnc1Mode) {
        payload = null!;
        segments = null!;
        structuredAppend = null;
        fnc1Mode = QrFnc1Mode.None;

        if (dataCodewords is null) return false;
        if (version is < 1 or > 40) return false;
        if (shouldStop?.Invoke() == true) return false;

        var bitLen = dataCodewords.Length * 8;
        var bitPos = 0;
        var stop = shouldStop;
        var stopCounter = 0;
        const int stopMask = 15;

        bool ShouldStopSparse() {
            if (stop is null) return false;
            if ((stopCounter++ & stopMask) != 0) return false;
            return stop();
        }

        int ReadBits(int n) {
            if (ShouldStopSparse()) return -1;
            if (n == 0) return 0;
            if (n < 0 || n > 31) throw new ArgumentOutOfRangeException(nameof(n));
            if (bitPos + n > bitLen) return -1;
            var byteIndex = bitPos >> 3;
            var bitOffset = bitPos & 7;
            var totalBits = bitOffset + n;
            var bytesNeeded = (totalBits + 7) >> 3;
            ulong acc = bytesNeeded switch {
                1 => dataCodewords[byteIndex],
                2 => ((ulong)dataCodewords[byteIndex] << 8) | dataCodewords[byteIndex + 1],
                3 => ((ulong)dataCodewords[byteIndex] << 16) | ((ulong)dataCodewords[byteIndex + 1] << 8) | dataCodewords[byteIndex + 2],
                4 => ((ulong)dataCodewords[byteIndex] << 24) | ((ulong)dataCodewords[byteIndex + 1] << 16) | ((ulong)dataCodewords[byteIndex + 2] << 8) | dataCodewords[byteIndex + 3],
                5 => ((ulong)dataCodewords[byteIndex] << 32) | ((ulong)dataCodewords[byteIndex + 1] << 24) | ((ulong)dataCodewords[byteIndex + 2] << 16) | ((ulong)dataCodewords[byteIndex + 3] << 8) | dataCodewords[byteIndex + 4],
                _ => 0UL
            };
            var extraBits = (bytesNeeded * 8) - totalBits;
            acc >>= extraBits;
            var mask = (1UL << n) - 1UL;
            var val = (int)(acc & mask);
            bitPos += n;
            return val;
        }

        var segmentBuilder = new SegmentBuilder(stackalloc SegmentInfo[4]);
        var encoding = QrTextEncoding.Latin1;
        var segmentStart = 0;
        var segmentLength = 0;
        var buffer = ArrayPool<byte>.Shared.Rent(64);
        var count = 0;

        void EnsureCapacity(int required) {
            if (required <= buffer.Length) return;
            var nextSize = buffer.Length;
            if (nextSize == 0) nextSize = 64;
            while (nextSize < required) {
                nextSize *= 2;
            }
            var next = ArrayPool<byte>.Shared.Rent(nextSize);
            Buffer.BlockCopy(buffer, 0, next, 0, count);
            ArrayPool<byte>.Shared.Return(buffer);
            buffer = next;
        }

        void AddByte(byte b) {
            if (count == buffer.Length) EnsureCapacity(count + 1);
            buffer[count++] = b;
            segmentLength++;
        }

        try {
            while (true) {
                if (stop?.Invoke() == true) return false;
                var mode = ReadBits(4);
                if (mode < 0) {
                    // Some encoders omit an explicit terminator if the payload exactly fills the available space,
                    // or leave <4 padding bits that can't form a mode indicator. Accept only zero padding.
                    var remaining = bitLen - bitPos;
                    if (remaining <= 0) break;
                    if (remaining < 4 && AreRemainingBitsZero(dataCodewords, bitPos, bitLen)) break;
                    return false;
                }

                if (mode == 0) {
                    FlushSegment(ref segmentBuilder, encoding, ref segmentStart, ref segmentLength, count);
                    payload = count == 0 ? Array.Empty<byte>() : SliceBuffer(buffer, count);
                    segmentBuilder.BuildSegments(payload, out segments);
                    return true;
                }

                if (mode == 0b0101) {
                    // FNC1 (first position)
                    fnc1Mode = QrFnc1Mode.FirstPosition;
                    continue;
                }

                if (mode == 0b1001) {
                    // FNC1 (second position)
                    fnc1Mode = QrFnc1Mode.SecondPosition;
                    continue;
                }

                if (mode == 0b0100) {
                    var countBits = QrTables.GetByteModeCharCountBits(version);
                    var charCount = ReadBits(countBits);
                    if (charCount < 0) return false;
                    EnsureCapacity(count + charCount);

                    for (var i = 0; i < charCount; i++) {
                        if (ShouldStopSparse()) return false;
                        var b = ReadBits(8);
                        if (b < 0) return false;
                        AddByte((byte)b);
                    }

                    continue;
                }

                if (mode == 0b1000) {
                    // Kanji mode (Shift-JIS JIS X 0208)
                    var countBits = QrTables.GetKanjiModeCharCountBits(version);
                    var charCount = ReadBits(countBits);
                    if (charCount < 0) return false;
                    EnsureCapacity(count + (charCount * 2));

                    FlushSegment(ref segmentBuilder, encoding, ref segmentStart, ref segmentLength, count);
                    var previousEncoding = encoding;
                    encoding = QrTextEncoding.ShiftJis;

                    for (var i = 0; i < charCount; i++) {
                        if (ShouldStopSparse()) return false;
                        var v = ReadBits(13);
                        if (v < 0) return false;
                        var assembled = ((v / 0xC0) << 8) | (v % 0xC0);
                        var sjis = assembled < 0x1F00 ? assembled + 0x8140 : assembled + 0xC140;
                        AddByte((byte)(sjis >> 8));
                        AddByte((byte)sjis);
                    }

                    FlushSegment(ref segmentBuilder, encoding, ref segmentStart, ref segmentLength, count);
                    encoding = previousEncoding;
                    continue;
                }

                if (mode == 0b0011) {
                    // Structured append: 8-bit sequence indicator + 8-bit parity.
                    var sequence = ReadBits(8);
                    var parity = ReadBits(8);
                    if (sequence < 0 || parity < 0) return false;

                    var index = (sequence >> 4) & 0x0F;
                    var total = sequence & 0x0F;
                    structuredAppend = new QrStructuredAppend(index, total, parity);
                    continue;
                }

                if (mode == 0b0001) {
                    // Numeric mode
                    var countBits = QrTables.GetNumericModeCharCountBits(version);
                    var charCount = ReadBits(countBits);
                    if (charCount < 0) return false;
                    EnsureCapacity(count + charCount);

                    var remaining = charCount;
                    while (remaining >= 3) {
                        if (ShouldStopSparse()) return false;
                        var v = ReadBits(10);
                        if (v < 0 || v > 999) return false;
                        AddByte((byte)('0' + (v / 100)));
                        AddByte((byte)('0' + ((v / 10) % 10)));
                        AddByte((byte)('0' + (v % 10)));
                        remaining -= 3;
                    }

                    if (remaining == 2) {
                        if (ShouldStopSparse()) return false;
                        var v = ReadBits(7);
                        if (v < 0 || v > 99) return false;
                        AddByte((byte)('0' + (v / 10)));
                        AddByte((byte)('0' + (v % 10)));
                    } else if (remaining == 1) {
                        if (ShouldStopSparse()) return false;
                        var v = ReadBits(4);
                        if (v < 0 || v > 9) return false;
                        AddByte((byte)('0' + v));
                    }

                    continue;
                }

                if (mode == 0b0010) {
                    // Alphanumeric mode
                    var countBits = QrTables.GetAlphanumericModeCharCountBits(version);
                    var charCount = ReadBits(countBits);
                    if (charCount < 0) return false;
                    EnsureCapacity(count + charCount);

                    var remaining = charCount;
                    if (fnc1Mode == QrFnc1Mode.None) {
                        while (remaining >= 2) {
                            if (ShouldStopSparse()) return false;
                            var v = ReadBits(11);
                            if (v < 0 || v >= 45 * 45) return false;
                            var a = v / 45;
                            var b = v % 45;
                            AddByte((byte)AlphanumericTable[a]);
                            AddByte((byte)AlphanumericTable[b]);
                            remaining -= 2;
                        }

                        if (remaining == 1) {
                            if (ShouldStopSparse()) return false;
                            var v = ReadBits(6);
                            if (v < 0 || v >= 45) return false;
                            AddByte((byte)AlphanumericTable[v]);
                        }
                    } else {
                        var pendingPercent = false;
                        void AppendAlpha(byte b) {
                            if (b == (byte)'%') {
                                if (pendingPercent) {
                                    AddByte((byte)'%');
                                    pendingPercent = false;
                                } else {
                                    pendingPercent = true;
                                }
                                return;
                            }
                            if (pendingPercent) {
                                AddByte(0x1D);
                                pendingPercent = false;
                            }
                            AddByte(b);
                        }

                        while (remaining >= 2) {
                            if (ShouldStopSparse()) return false;
                            var v = ReadBits(11);
                            if (v < 0 || v >= 45 * 45) return false;
                            var a = v / 45;
                            var b = v % 45;
                            AppendAlpha((byte)AlphanumericTable[a]);
                            AppendAlpha((byte)AlphanumericTable[b]);
                            remaining -= 2;
                        }

                        if (remaining == 1) {
                            if (ShouldStopSparse()) return false;
                            var v = ReadBits(6);
                            if (v < 0 || v >= 45) return false;
                            AppendAlpha((byte)AlphanumericTable[v]);
                        }

                        if (pendingPercent) {
                            AddByte(0x1D);
                        }
                    }

                    continue;
                }

                if (mode == 0b0111) {
                    if (!TryReadEciAssignmentNumber(ReadBits, out var assignmentNumber)) return false;

                    // Minimal set for OTP / URL QR use cases.
                    var newEncoding = assignmentNumber switch {
                        3 => QrTextEncoding.Latin1,      // ISO-8859-1
                        4 => QrTextEncoding.Iso8859_2,   // ISO-8859-2
                        6 => QrTextEncoding.Iso8859_4,   // ISO-8859-4
                        7 => QrTextEncoding.Iso8859_5,   // ISO-8859-5
                        9 => QrTextEncoding.Iso8859_7,   // ISO-8859-7
                        12 => QrTextEncoding.Iso8859_10, // ISO-8859-10
                        15 => QrTextEncoding.Iso8859_15, // ISO-8859-15
                        20 => QrTextEncoding.ShiftJis,   // Shift-JIS
                        26 => QrTextEncoding.Utf8,       // UTF-8
                        27 => QrTextEncoding.Ascii,      // US-ASCII
                        _ => encoding,
                    };

                    if (newEncoding != encoding) {
                        FlushSegment(ref segmentBuilder, encoding, ref segmentStart, ref segmentLength, count);
                        encoding = newEncoding;
                    }

                    // Unknown ECI: accept but keep current encoding (best-effort).
                    continue;
                }

                return false;
            }

            FlushSegment(ref segmentBuilder, encoding, ref segmentStart, ref segmentLength, count);
            payload = count == 0 ? Array.Empty<byte>() : SliceBuffer(buffer, count);
            segmentBuilder.BuildSegments(payload, out segments);
            return true;
        } finally {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static void FlushSegment(ref SegmentBuilder builder, QrTextEncoding encoding, ref int segmentStart, ref int segmentLength, int count) {
        if (segmentLength == 0) return;
        builder.Add(encoding, segmentStart, segmentLength);
        segmentStart = count;
        segmentLength = 0;
    }

    private static byte[] SliceBuffer(byte[] buffer, int count) {
        if (count == 0) return Array.Empty<byte>();
        var result = new byte[count];
        Buffer.BlockCopy(buffer, 0, result, 0, count);
        return result;
    }

    private static bool TryReadEciAssignmentNumber(Func<int, int> readBits, out int assignmentNumber) {
        assignmentNumber = 0;

        var first = readBits(8);
        if (first < 0) return false;

        // QR ECI encoding:
        // 0xxxxxxx: 7-bit value
        // 10xxxxxx xxxxxxxx: 14-bit value
        // 110xxxxx xxxxxxxx xxxxxxxx: 21-bit value
        if ((first & 0b1000_0000) == 0) {
            assignmentNumber = first & 0b0111_1111;
            return true;
        }

        if ((first & 0b1100_0000) == 0b1000_0000) {
            var second = readBits(8);
            if (second < 0) return false;
            assignmentNumber = ((first & 0b0011_1111) << 8) | second;
            return true;
        }

        if ((first & 0b1110_0000) == 0b1100_0000) {
            var rest = readBits(16);
            if (rest < 0) return false;
            assignmentNumber = ((first & 0b0001_1111) << 16) | rest;
            return true;
        }

        return false;
    }

    private static bool AreRemainingBitsZero(byte[] bytes, int bitPos, int bitLen) {
        for (var i = bitPos; i < bitLen; i++) {
            var b = (bytes[i >> 3] >> (7 - (i & 7))) & 1;
            if (b != 0) return false;
        }
        return true;
    }
}
