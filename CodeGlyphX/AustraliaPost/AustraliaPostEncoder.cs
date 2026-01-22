using System;
using System.Collections.Generic;
using System.Globalization;

namespace CodeGlyphX.AustraliaPost;

/// <summary>
/// Encodes Australia Post customer barcodes.
/// </summary>
public static class AustraliaPostEncoder {
    /// <summary>
    /// Encodes an Australia Post customer barcode into a <see cref="BitMatrix"/>.
    /// </summary>
    /// <remarks>
    /// Supported payload lengths:
    /// 8 (Standard), 13 (Customer 2, C table), 16 (Customer 2, N table),
    /// 18 (Customer 3, C table), 23 (Customer 3, N table).
    /// </remarks>
    public static BitMatrix Encode(string content) {
        if (content is null) throw new ArgumentNullException(nameof(content));
        content = content.Trim();
        if (content.Length == 0) throw new InvalidOperationException("Australia Post content cannot be empty.");

        if (!TryParseFormat(content, out var format, out var fcc, out var table, out var dpid, out var customerInfo)) {
            throw new InvalidOperationException("Australia Post expects 8, 13, 16, 18, or 23 characters (see documentation).");
        }

        var infoBars = new List<int>(GetInfoBarsCapacity(format));

        AppendFcc(infoBars, fcc);
        AppendDigits(infoBars, dpid);

        if (format == AustraliaPostFormat.Standard) {
            infoBars.Add(3); // filler bar
        } else {
            AppendCustomer(infoBars, customerInfo, table!.Value, format);
        }

        var parityBars = BuildParityBars(infoBars);

        var bars = new List<int>(2 + infoBars.Count + parityBars.Count + 2) {
            AustraliaPostTables.StartBarAscender,
            AustraliaPostTables.StartBarTracker
        };
        bars.AddRange(infoBars);
        bars.AddRange(parityBars);
        bars.Add(AustraliaPostTables.StopBarAscender);
        bars.Add(AustraliaPostTables.StopBarTracker);

        return BuildMatrix(bars);
    }

    private static bool TryParseFormat(
        string content,
        out AustraliaPostFormat format,
        out int fcc,
        out AustraliaPostCustomerEncodingTable? table,
        out string dpid,
        out string customerInfo) {
        format = AustraliaPostFormat.Standard;
        fcc = AustraliaPostTables.FccStandard;
        table = null;
        dpid = string.Empty;
        customerInfo = string.Empty;

        var length = content.Length;
        if (length != 8 && length != 13 && length != 16 && length != 18 && length != 23) return false;

        dpid = content.Substring(0, 8);
        if (!IsDigitsOnly(dpid)) return false;

        if (length == 8) {
            format = AustraliaPostFormat.Standard;
            fcc = AustraliaPostTables.FccStandard;
            return true;
        }

        customerInfo = content.Substring(8);
        if (length == 13) {
            format = AustraliaPostFormat.Customer2;
            fcc = AustraliaPostTables.FccCustomer2;
            table = AustraliaPostCustomerEncodingTable.C;
            return IsCContent(customerInfo);
        }

        if (length == 16) {
            format = AustraliaPostFormat.Customer2;
            fcc = AustraliaPostTables.FccCustomer2;
            table = AustraliaPostCustomerEncodingTable.N;
            return IsDigitsOnly(customerInfo);
        }

        if (length == 18) {
            format = AustraliaPostFormat.Customer3;
            fcc = AustraliaPostTables.FccCustomer3;
            table = AustraliaPostCustomerEncodingTable.C;
            return IsCContent(customerInfo);
        }

        format = AustraliaPostFormat.Customer3;
        fcc = AustraliaPostTables.FccCustomer3;
        table = AustraliaPostCustomerEncodingTable.N;
        return IsDigitsOnly(customerInfo);
    }

    private static bool IsDigitsOnly(string value) {
        for (var i = 0; i < value.Length; i++) {
            var ch = value[i];
            if (ch < '0' || ch > '9') return false;
        }
        return true;
    }

    private static bool IsCContent(string value) {
        for (var i = 0; i < value.Length; i++) {
            var ch = value[i];
            if (!AustraliaPostTables.CEncodeValues.ContainsKey(ch)) return false;
        }
        return true;
    }

    private static void AppendFcc(List<int> bars, int fcc) {
        var fccText = fcc.ToString("00", CultureInfo.InvariantCulture);
        AppendDigits(bars, fccText);
    }

    private static void AppendDigits(List<int> bars, string digits) {
        for (var i = 0; i < digits.Length; i++) {
            var digit = digits[i] - '0';
            var pair = AustraliaPostTables.NEncodePairs[digit];
            bars.Add(pair[0]);
            bars.Add(pair[1]);
        }
    }

    private static void AppendCustomer(List<int> bars, string customerInfo, AustraliaPostCustomerEncodingTable table, AustraliaPostFormat format) {
        var fieldLength = format == AustraliaPostFormat.Customer2 ? 16 : 31;
        var start = bars.Count;
        if (table == AustraliaPostCustomerEncodingTable.N) {
            AppendDigits(bars, customerInfo);
        } else {
            for (var i = 0; i < customerInfo.Length; i++) {
                var ch = customerInfo[i];
                if (!AustraliaPostTables.CEncodeValues.TryGetValue(ch, out var value)) {
                    throw new InvalidOperationException($"Character '{ch}' is not supported by Australia Post C encoding.");
                }
                var b0 = value / 16;
                var b1 = (value / 4) % 4;
                var b2 = value % 4;
                bars.Add(b0);
                bars.Add(b1);
                bars.Add(b2);
            }
        }

        while (bars.Count - start < fieldLength) {
            bars.Add(3);
        }
        if (bars.Count - start != fieldLength) {
            throw new InvalidOperationException("Australia Post customer field length does not match expected format.");
        }
    }

    private static int GetInfoBarsCapacity(AustraliaPostFormat format) {
        if (format == AustraliaPostFormat.Standard) return 21;
        return format == AustraliaPostFormat.Customer2 ? 36 : 51;
    }

    private static List<int> BuildParityBars(List<int> infoBars) {
        var symbols = new int[infoBars.Count / 3];
        for (var i = 0; i < symbols.Length; i++) {
            var idx = i * 3;
            symbols[i] = AustraliaPostTables.BarDigitsToValue(infoBars[idx], infoBars[idx + 1], infoBars[idx + 2]);
        }

        var parity = AustraliaPostTables.ComputeParity(symbols);
        var parityBars = new List<int>(12);
        Span<int> digits = stackalloc int[3];
        for (var i = 0; i < parity.Length; i++) {
            AustraliaPostTables.ValueToBarDigits(parity[i], digits);
            parityBars.Add(digits[0]);
            parityBars.Add(digits[1]);
            parityBars.Add(digits[2]);
        }

        return parityBars;
    }

    private static BitMatrix BuildMatrix(List<int> bars) {
        var width = bars.Count * 2 - 1;
        var matrix = new BitMatrix(width, AustraliaPostTables.BarcodeHeight);

        var x = 0;
        for (var i = 0; i < bars.Count; i++) {
            SetBar(matrix, x, bars[i]);
            x += 2;
        }

        return matrix;
    }

    private static void SetBar(BitMatrix matrix, int x, int value) {
        matrix[x, AustraliaPostTables.TrackerRowTop] = true;
        matrix[x, AustraliaPostTables.TrackerRowBottom] = true;

        var hasAsc = value == 0 || value == 1;
        var hasDesc = value == 0 || value == 2;

        if (hasAsc) {
            matrix[x, 0] = true;
            matrix[x, 1] = true;
            matrix[x, 2] = true;
        }

        if (hasDesc) {
            matrix[x, 5] = true;
            matrix[x, 6] = true;
            matrix[x, 7] = true;
        }
    }
}
