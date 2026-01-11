using System;
using System.Collections.Generic;
using System.Text;

namespace CodeGlyphX.Internal;

internal static class QrEncoding {
    public static bool TryGetEciAssignment(QrTextEncoding encoding, out int assignmentNumber) {
        assignmentNumber = encoding switch {
            QrTextEncoding.Latin1 => 3,
            QrTextEncoding.Iso8859_2 => 4,
            QrTextEncoding.Iso8859_4 => 6,
            QrTextEncoding.Iso8859_5 => 7,
            QrTextEncoding.Iso8859_7 => 9,
            QrTextEncoding.Iso8859_10 => 12,
            QrTextEncoding.Iso8859_15 => 15,
            QrTextEncoding.ShiftJis => 20,
            QrTextEncoding.Utf8 => 26,
            QrTextEncoding.Ascii => 27,
            _ => -1
        };

        return assignmentNumber > 0;
    }

    public static byte[] Encode(string text, QrTextEncoding encoding) {
        if (text is null) throw new ArgumentNullException(nameof(text));

        return encoding switch {
            QrTextEncoding.Utf8 => Encoding.UTF8.GetBytes(text),
            QrTextEncoding.Ascii => Encoding.ASCII.GetBytes(text),
            QrTextEncoding.Latin1 => EncodeLatin1(text),
            QrTextEncoding.Iso8859_2 => EncodeSingleByte(text, ISO8859_2_MAP),
            QrTextEncoding.Iso8859_4 => EncodeSingleByte(text, ISO8859_4_MAP),
            QrTextEncoding.Iso8859_5 => EncodeSingleByte(text, ISO8859_5_MAP),
            QrTextEncoding.Iso8859_7 => EncodeSingleByte(text, ISO8859_7_MAP),
            QrTextEncoding.Iso8859_10 => EncodeSingleByte(text, ISO8859_10_MAP),
            QrTextEncoding.Iso8859_15 => EncodeSingleByte(text, ISO8859_15_MAP),
            QrTextEncoding.ShiftJis => QrShiftJis.Encode(text),
            _ => Encoding.UTF8.GetBytes(text)
        };
    }

    public static bool CanEncode(string text, QrTextEncoding encoding) {
        if (text is null) return false;

        switch (encoding) {
            case QrTextEncoding.Utf8:
                return true;
            case QrTextEncoding.Ascii:
                for (var i = 0; i < text.Length; i++) {
                    if (text[i] > 0x7F) return false;
                }
                return true;
            case QrTextEncoding.Latin1:
                for (var i = 0; i < text.Length; i++) {
                    if (text[i] > 0xFF) return false;
                }
                return true;
            case QrTextEncoding.Iso8859_2:
                return CanEncodeWithMap(text, ISO8859_2_MAP);
            case QrTextEncoding.Iso8859_4:
                return CanEncodeWithMap(text, ISO8859_4_MAP);
            case QrTextEncoding.Iso8859_5:
                return CanEncodeWithMap(text, ISO8859_5_MAP);
            case QrTextEncoding.Iso8859_7:
                return CanEncodeWithMap(text, ISO8859_7_MAP);
            case QrTextEncoding.Iso8859_10:
                return CanEncodeWithMap(text, ISO8859_10_MAP);
            case QrTextEncoding.Iso8859_15:
                return CanEncodeWithMap(text, ISO8859_15_MAP);
            case QrTextEncoding.ShiftJis:
                return QrShiftJis.CanEncode(text);
            default:
                return false;
        }
    }

    public static string Decode(QrTextEncoding encoding, byte[] bytes) {
        if (bytes is null) throw new ArgumentNullException(nameof(bytes));
        if (bytes.Length == 0) return string.Empty;

        return encoding switch {
            QrTextEncoding.Utf8 => Encoding.UTF8.GetString(bytes),
            QrTextEncoding.Ascii => Encoding.ASCII.GetString(bytes),
            QrTextEncoding.Latin1 => DecodeSingleByte(bytes, LATIN1),
            QrTextEncoding.Iso8859_2 => DecodeSingleByte(bytes, ISO8859_2),
            QrTextEncoding.Iso8859_4 => DecodeSingleByte(bytes, ISO8859_4),
            QrTextEncoding.Iso8859_5 => DecodeSingleByte(bytes, ISO8859_5),
            QrTextEncoding.Iso8859_7 => DecodeSingleByte(bytes, ISO8859_7),
            QrTextEncoding.Iso8859_10 => DecodeSingleByte(bytes, ISO8859_10),
            QrTextEncoding.Iso8859_15 => DecodeSingleByte(bytes, ISO8859_15),
            QrTextEncoding.ShiftJis => QrShiftJis.Decode(bytes),
            _ => Encoding.UTF8.GetString(bytes)
        };
    }

    private static byte[] EncodeLatin1(string text) {
        var bytes = new byte[text.Length];
        for (var i = 0; i < text.Length; i++) {
            var c = text[i];
            bytes[i] = c <= 0xFF ? (byte)c : (byte)'?';
        }
        return bytes;
    }

    private static byte[] EncodeSingleByte(string text, Dictionary<char, byte> map) {
        var bytes = new byte[text.Length];
        for (var i = 0; i < text.Length; i++) {
            var c = text[i];
            bytes[i] = map.TryGetValue(c, out var b) ? b : (byte)'?';
        }
        return bytes;
    }

    private static bool CanEncodeWithMap(string text, Dictionary<char, byte> map) {
        for (var i = 0; i < text.Length; i++) {
            if (!map.ContainsKey(text[i])) return false;
        }
        return true;
    }

    private static string DecodeSingleByte(byte[] bytes, char[] table) {
        var chars = new char[bytes.Length];
        for (var i = 0; i < bytes.Length; i++) {
            chars[i] = table[bytes[i]];
        }
        return new string(chars);
    }

    private static Dictionary<char, byte> BuildMap(char[] table) {
        var map = new Dictionary<char, byte>(table.Length);
        for (var i = 0; i < table.Length; i++) {
            var c = table[i];
            if (!map.ContainsKey(c)) map[c] = (byte)i;
        }
        return map;
    }

    private static char[] BuildLatin1Table() {
        var table = new char[256];
        for (var i = 0; i < table.Length; i++) {
            table[i] = (char)i;
        }
        return table;
    }

    private static readonly char[] LATIN1 = BuildLatin1Table();

private static readonly char[] ISO8859_2 = new char[] {
    '\u0000', '\u0001', '\u0002', '\u0003', '\u0004', '\u0005', '\u0006', '\u0007', '\u0008', '\u0009', '\u000A', '\u000B', '\u000C', '\u000D', '\u000E', '\u000F',
    '\u0010', '\u0011', '\u0012', '\u0013', '\u0014', '\u0015', '\u0016', '\u0017', '\u0018', '\u0019', '\u001A', '\u001B', '\u001C', '\u001D', '\u001E', '\u001F',
    ' ', '!', '"', '#', '$', '%', '&', '\'', '(', ')', '*', '+', ',', '-', '.', '/',
    '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ':', ';', '<', '=', '>', '?',
    '@', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O',
    'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '[', '\\', ']', '^', '_',
    '`', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o',
    'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '{', '|', '}', '~', '\u007F',
    '\u0080', '\u0081', '\u0082', '\u0083', '\u0084', '\u0085', '\u0086', '\u0087', '\u0088', '\u0089', '\u008A', '\u008B', '\u008C', '\u008D', '\u008E', '\u008F',
    '\u0090', '\u0091', '\u0092', '\u0093', '\u0094', '\u0095', '\u0096', '\u0097', '\u0098', '\u0099', '\u009A', '\u009B', '\u009C', '\u009D', '\u009E', '\u009F',
    '\u00A0', '\u0104', '\u02D8', '\u0141', '\u00A4', '\u013D', '\u015A', '\u00A7', '\u00A8', '\u0160', '\u015E', '\u0164', '\u0179', '\u00AD', '\u017D', '\u017B',
    '\u00B0', '\u0105', '\u02DB', '\u0142', '\u00B4', '\u013E', '\u015B', '\u02C7', '\u00B8', '\u0161', '\u015F', '\u0165', '\u017A', '\u02DD', '\u017E', '\u017C',
    '\u0154', '\u00C1', '\u00C2', '\u0102', '\u00C4', '\u0139', '\u0106', '\u00C7', '\u010C', '\u00C9', '\u0118', '\u00CB', '\u011A', '\u00CD', '\u00CE', '\u010E',
    '\u0110', '\u0143', '\u0147', '\u00D3', '\u00D4', '\u0150', '\u00D6', '\u00D7', '\u0158', '\u016E', '\u00DA', '\u0170', '\u00DC', '\u00DD', '\u0162', '\u00DF',
    '\u0155', '\u00E1', '\u00E2', '\u0103', '\u00E4', '\u013A', '\u0107', '\u00E7', '\u010D', '\u00E9', '\u0119', '\u00EB', '\u011B', '\u00ED', '\u00EE', '\u010F',
    '\u0111', '\u0144', '\u0148', '\u00F3', '\u00F4', '\u0151', '\u00F6', '\u00F7', '\u0159', '\u016F', '\u00FA', '\u0171', '\u00FC', '\u00FD', '\u0163', '\u02D9',
};

private static readonly char[] ISO8859_4 = new char[] {
    '\u0000', '\u0001', '\u0002', '\u0003', '\u0004', '\u0005', '\u0006', '\u0007', '\u0008', '\u0009', '\u000A', '\u000B', '\u000C', '\u000D', '\u000E', '\u000F',
    '\u0010', '\u0011', '\u0012', '\u0013', '\u0014', '\u0015', '\u0016', '\u0017', '\u0018', '\u0019', '\u001A', '\u001B', '\u001C', '\u001D', '\u001E', '\u001F',
    ' ', '!', '"', '#', '$', '%', '&', '\'', '(', ')', '*', '+', ',', '-', '.', '/',
    '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ':', ';', '<', '=', '>', '?',
    '@', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O',
    'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '[', '\\', ']', '^', '_',
    '`', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o',
    'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '{', '|', '}', '~', '\u007F',
    '\u0080', '\u0081', '\u0082', '\u0083', '\u0084', '\u0085', '\u0086', '\u0087', '\u0088', '\u0089', '\u008A', '\u008B', '\u008C', '\u008D', '\u008E', '\u008F',
    '\u0090', '\u0091', '\u0092', '\u0093', '\u0094', '\u0095', '\u0096', '\u0097', '\u0098', '\u0099', '\u009A', '\u009B', '\u009C', '\u009D', '\u009E', '\u009F',
    '\u00A0', '\u0104', '\u0138', '\u0156', '\u00A4', '\u0128', '\u013B', '\u00A7', '\u00A8', '\u0160', '\u0112', '\u0122', '\u0166', '\u00AD', '\u017D', '\u00AF',
    '\u00B0', '\u0105', '\u02DB', '\u0157', '\u00B4', '\u0129', '\u013C', '\u02C7', '\u00B8', '\u0161', '\u0113', '\u0123', '\u0167', '\u014A', '\u017E', '\u014B',
    '\u0100', '\u00C1', '\u00C2', '\u00C3', '\u00C4', '\u00C5', '\u00C6', '\u012E', '\u010C', '\u00C9', '\u0118', '\u00CB', '\u0116', '\u00CD', '\u00CE', '\u012A',
    '\u0110', '\u0145', '\u014C', '\u0136', '\u00D4', '\u00D5', '\u00D6', '\u00D7', '\u00D8', '\u0172', '\u00DA', '\u00DB', '\u00DC', '\u0168', '\u016A', '\u00DF',
    '\u0101', '\u00E1', '\u00E2', '\u00E3', '\u00E4', '\u00E5', '\u00E6', '\u012F', '\u010D', '\u00E9', '\u0119', '\u00EB', '\u0117', '\u00ED', '\u00EE', '\u012B',
    '\u0111', '\u0146', '\u014D', '\u0137', '\u00F4', '\u00F5', '\u00F6', '\u00F7', '\u00F8', '\u0173', '\u00FA', '\u00FB', '\u00FC', '\u0169', '\u016B', '\u02D9',
};

private static readonly char[] ISO8859_5 = new char[] {
    '\u0000', '\u0001', '\u0002', '\u0003', '\u0004', '\u0005', '\u0006', '\u0007', '\u0008', '\u0009', '\u000A', '\u000B', '\u000C', '\u000D', '\u000E', '\u000F',
    '\u0010', '\u0011', '\u0012', '\u0013', '\u0014', '\u0015', '\u0016', '\u0017', '\u0018', '\u0019', '\u001A', '\u001B', '\u001C', '\u001D', '\u001E', '\u001F',
    ' ', '!', '"', '#', '$', '%', '&', '\'', '(', ')', '*', '+', ',', '-', '.', '/',
    '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ':', ';', '<', '=', '>', '?',
    '@', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O',
    'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '[', '\\', ']', '^', '_',
    '`', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o',
    'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '{', '|', '}', '~', '\u007F',
    '\u0080', '\u0081', '\u0082', '\u0083', '\u0084', '\u0085', '\u0086', '\u0087', '\u0088', '\u0089', '\u008A', '\u008B', '\u008C', '\u008D', '\u008E', '\u008F',
    '\u0090', '\u0091', '\u0092', '\u0093', '\u0094', '\u0095', '\u0096', '\u0097', '\u0098', '\u0099', '\u009A', '\u009B', '\u009C', '\u009D', '\u009E', '\u009F',
    '\u00A0', '\u0401', '\u0402', '\u0403', '\u0404', '\u0405', '\u0406', '\u0407', '\u0408', '\u0409', '\u040A', '\u040B', '\u040C', '\u00AD', '\u040E', '\u040F',
    '\u0410', '\u0411', '\u0412', '\u0413', '\u0414', '\u0415', '\u0416', '\u0417', '\u0418', '\u0419', '\u041A', '\u041B', '\u041C', '\u041D', '\u041E', '\u041F',
    '\u0420', '\u0421', '\u0422', '\u0423', '\u0424', '\u0425', '\u0426', '\u0427', '\u0428', '\u0429', '\u042A', '\u042B', '\u042C', '\u042D', '\u042E', '\u042F',
    '\u0430', '\u0431', '\u0432', '\u0433', '\u0434', '\u0435', '\u0436', '\u0437', '\u0438', '\u0439', '\u043A', '\u043B', '\u043C', '\u043D', '\u043E', '\u043F',
    '\u0440', '\u0441', '\u0442', '\u0443', '\u0444', '\u0445', '\u0446', '\u0447', '\u0448', '\u0449', '\u044A', '\u044B', '\u044C', '\u044D', '\u044E', '\u044F',
    '\u2116', '\u0451', '\u0452', '\u0453', '\u0454', '\u0455', '\u0456', '\u0457', '\u0458', '\u0459', '\u045A', '\u045B', '\u045C', '\u00A7', '\u045E', '\u045F',
};

private static readonly char[] ISO8859_7 = new char[] {
    '\u0000', '\u0001', '\u0002', '\u0003', '\u0004', '\u0005', '\u0006', '\u0007', '\u0008', '\u0009', '\u000A', '\u000B', '\u000C', '\u000D', '\u000E', '\u000F',
    '\u0010', '\u0011', '\u0012', '\u0013', '\u0014', '\u0015', '\u0016', '\u0017', '\u0018', '\u0019', '\u001A', '\u001B', '\u001C', '\u001D', '\u001E', '\u001F',
    ' ', '!', '"', '#', '$', '%', '&', '\'', '(', ')', '*', '+', ',', '-', '.', '/',
    '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ':', ';', '<', '=', '>', '?',
    '@', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O',
    'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '[', '\\', ']', '^', '_',
    '`', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o',
    'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '{', '|', '}', '~', '\u007F',
    '\u0080', '\u0081', '\u0082', '\u0083', '\u0084', '\u0085', '\u0086', '\u0087', '\u0088', '\u0089', '\u008A', '\u008B', '\u008C', '\u008D', '\u008E', '\u008F',
    '\u0090', '\u0091', '\u0092', '\u0093', '\u0094', '\u0095', '\u0096', '\u0097', '\u0098', '\u0099', '\u009A', '\u009B', '\u009C', '\u009D', '\u009E', '\u009F',
    '\u00A0', '\u2018', '\u2019', '\u00A3', '\u20AC', '\u20AF', '\u00A6', '\u00A7', '\u00A8', '\u00A9', '\u037A', '\u00AB', '\u00AC', '\u00AD', '\uFFFD', '\u2015',
    '\u00B0', '\u00B1', '\u00B2', '\u00B3', '\u0384', '\u0385', '\u0386', '\u00B7', '\u0388', '\u0389', '\u038A', '\u00BB', '\u038C', '\u00BD', '\u038E', '\u038F',
    '\u0390', '\u0391', '\u0392', '\u0393', '\u0394', '\u0395', '\u0396', '\u0397', '\u0398', '\u0399', '\u039A', '\u039B', '\u039C', '\u039D', '\u039E', '\u039F',
    '\u03A0', '\u03A1', '\uFFFD', '\u03A3', '\u03A4', '\u03A5', '\u03A6', '\u03A7', '\u03A8', '\u03A9', '\u03AA', '\u03AB', '\u03AC', '\u03AD', '\u03AE', '\u03AF',
    '\u03B0', '\u03B1', '\u03B2', '\u03B3', '\u03B4', '\u03B5', '\u03B6', '\u03B7', '\u03B8', '\u03B9', '\u03BA', '\u03BB', '\u03BC', '\u03BD', '\u03BE', '\u03BF',
    '\u03C0', '\u03C1', '\u03C2', '\u03C3', '\u03C4', '\u03C5', '\u03C6', '\u03C7', '\u03C8', '\u03C9', '\u03CA', '\u03CB', '\u03CC', '\u03CD', '\u03CE', '\uFFFD',
};

private static readonly char[] ISO8859_10 = new char[] {
    '\u0000', '\u0001', '\u0002', '\u0003', '\u0004', '\u0005', '\u0006', '\u0007', '\u0008', '\u0009', '\u000A', '\u000B', '\u000C', '\u000D', '\u000E', '\u000F',
    '\u0010', '\u0011', '\u0012', '\u0013', '\u0014', '\u0015', '\u0016', '\u0017', '\u0018', '\u0019', '\u001A', '\u001B', '\u001C', '\u001D', '\u001E', '\u001F',
    ' ', '!', '"', '#', '$', '%', '&', '\'', '(', ')', '*', '+', ',', '-', '.', '/',
    '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ':', ';', '<', '=', '>', '?',
    '@', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O',
    'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '[', '\\', ']', '^', '_',
    '`', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o',
    'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '{', '|', '}', '~', '\u007F',
    '\u0080', '\u0081', '\u0082', '\u0083', '\u0084', '\u0085', '\u0086', '\u0087', '\u0088', '\u0089', '\u008A', '\u008B', '\u008C', '\u008D', '\u008E', '\u008F',
    '\u0090', '\u0091', '\u0092', '\u0093', '\u0094', '\u0095', '\u0096', '\u0097', '\u0098', '\u0099', '\u009A', '\u009B', '\u009C', '\u009D', '\u009E', '\u009F',
    '\u00A0', '\u0104', '\u0112', '\u0122', '\u012A', '\u0128', '\u0136', '\u00A7', '\u013B', '\u0110', '\u0160', '\u0166', '\u017D', '\u00AD', '\u016A', '\u014A',
    '\u00B0', '\u0105', '\u0113', '\u0123', '\u012B', '\u0129', '\u0137', '\u00B7', '\u013C', '\u0111', '\u0161', '\u0167', '\u017E', '\u2015', '\u016B', '\u014B',
    '\u0100', '\u00C1', '\u00C2', '\u00C3', '\u00C4', '\u00C5', '\u00C6', '\u012E', '\u010C', '\u00C9', '\u0118', '\u00CB', '\u0116', '\u00CD', '\u00CE', '\u00CF',
    '\u00D0', '\u0145', '\u014C', '\u00D3', '\u00D4', '\u00D5', '\u00D6', '\u0168', '\u00D8', '\u0172', '\u00DA', '\u00DB', '\u00DC', '\u00DD', '\u00DE', '\u00DF',
    '\u0101', '\u00E1', '\u00E2', '\u00E3', '\u00E4', '\u00E5', '\u00E6', '\u012F', '\u010D', '\u00E9', '\u0119', '\u00EB', '\u0117', '\u00ED', '\u00EE', '\u00EF',
    '\u00F0', '\u0146', '\u014D', '\u00F3', '\u00F4', '\u00F5', '\u00F6', '\u0169', '\u00F8', '\u0173', '\u00FA', '\u00FB', '\u00FC', '\u00FD', '\u00FE', '\u0138',
};

private static readonly char[] ISO8859_15 = new char[] {
    '\u0000', '\u0001', '\u0002', '\u0003', '\u0004', '\u0005', '\u0006', '\u0007', '\u0008', '\u0009', '\u000A', '\u000B', '\u000C', '\u000D', '\u000E', '\u000F',
    '\u0010', '\u0011', '\u0012', '\u0013', '\u0014', '\u0015', '\u0016', '\u0017', '\u0018', '\u0019', '\u001A', '\u001B', '\u001C', '\u001D', '\u001E', '\u001F',
    ' ', '!', '"', '#', '$', '%', '&', '\'', '(', ')', '*', '+', ',', '-', '.', '/',
    '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ':', ';', '<', '=', '>', '?',
    '@', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O',
    'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '[', '\\', ']', '^', '_',
    '`', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o',
    'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '{', '|', '}', '~', '\u007F',
    '\u0080', '\u0081', '\u0082', '\u0083', '\u0084', '\u0085', '\u0086', '\u0087', '\u0088', '\u0089', '\u008A', '\u008B', '\u008C', '\u008D', '\u008E', '\u008F',
    '\u0090', '\u0091', '\u0092', '\u0093', '\u0094', '\u0095', '\u0096', '\u0097', '\u0098', '\u0099', '\u009A', '\u009B', '\u009C', '\u009D', '\u009E', '\u009F',
    '\u00A0', '\u00A1', '\u00A2', '\u00A3', '\u20AC', '\u00A5', '\u0160', '\u00A7', '\u0161', '\u00A9', '\u00AA', '\u00AB', '\u00AC', '\u00AD', '\u00AE', '\u00AF',
    '\u00B0', '\u00B1', '\u00B2', '\u00B3', '\u017D', '\u00B5', '\u00B6', '\u00B7', '\u017E', '\u00B9', '\u00BA', '\u00BB', '\u0152', '\u0153', '\u0178', '\u00BF',
    '\u00C0', '\u00C1', '\u00C2', '\u00C3', '\u00C4', '\u00C5', '\u00C6', '\u00C7', '\u00C8', '\u00C9', '\u00CA', '\u00CB', '\u00CC', '\u00CD', '\u00CE', '\u00CF',
    '\u00D0', '\u00D1', '\u00D2', '\u00D3', '\u00D4', '\u00D5', '\u00D6', '\u00D7', '\u00D8', '\u00D9', '\u00DA', '\u00DB', '\u00DC', '\u00DD', '\u00DE', '\u00DF',
    '\u00E0', '\u00E1', '\u00E2', '\u00E3', '\u00E4', '\u00E5', '\u00E6', '\u00E7', '\u00E8', '\u00E9', '\u00EA', '\u00EB', '\u00EC', '\u00ED', '\u00EE', '\u00EF',
    '\u00F0', '\u00F1', '\u00F2', '\u00F3', '\u00F4', '\u00F5', '\u00F6', '\u00F7', '\u00F8', '\u00F9', '\u00FA', '\u00FB', '\u00FC', '\u00FD', '\u00FE', '\u00FF',
};

    private static readonly Dictionary<char, byte> ISO8859_2_MAP = BuildMap(ISO8859_2);
    private static readonly Dictionary<char, byte> ISO8859_4_MAP = BuildMap(ISO8859_4);
    private static readonly Dictionary<char, byte> ISO8859_5_MAP = BuildMap(ISO8859_5);
    private static readonly Dictionary<char, byte> ISO8859_7_MAP = BuildMap(ISO8859_7);
    private static readonly Dictionary<char, byte> ISO8859_10_MAP = BuildMap(ISO8859_10);
    private static readonly Dictionary<char, byte> ISO8859_15_MAP = BuildMap(ISO8859_15);
}
