using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CodeGlyphX.AustraliaPost;

/// <summary>
/// Decodes Australia Post customer barcodes.
/// </summary>
public static class AustraliaPostDecoder {
    /// <summary>
    /// Attempts to decode an Australia Post customer barcode from a <see cref="BitMatrix"/>.
    /// </summary>
    public static bool TryDecode(BitMatrix modules, out string text) {
        if (TryDecode(modules, preferredCustomerTable: null, out var decoded)) {
            text = decoded.Value;
            return true;
        }

        text = string.Empty;
        return false;
    }

    /// <summary>
    /// Attempts to decode an Australia Post customer barcode from a <see cref="BitMatrix"/>.
    /// </summary>
    /// <remarks>
    /// For Customer Barcode 3 (67 bars), numeric-only customer data can be ambiguous between N and C tables.
    /// Provide <paramref name="preferredCustomerTable"/> to disambiguate if needed.
    /// </remarks>
    public static bool TryDecode(BitMatrix modules, AustraliaPostCustomerEncodingTable? preferredCustomerTable, out AustraliaPostDecoded decoded) {
        decoded = null!;
        if (modules is null) return false;
        if (modules.Width <= 0 || modules.Height != AustraliaPostTables.BarcodeHeight) return false;

        var bars = ExtractBars(modules);
        if (bars.Count != 37 && bars.Count != 52 && bars.Count != 67) return false;
        if (bars[0] != AustraliaPostTables.StartBarAscender || bars[1] != AustraliaPostTables.StartBarTracker) return false;
        if (bars[bars.Count - 2] != AustraliaPostTables.StopBarAscender || bars[bars.Count - 1] != AustraliaPostTables.StopBarTracker) return false;

        var format = bars.Count == 37 ? AustraliaPostFormat.Standard : (bars.Count == 52 ? AustraliaPostFormat.Customer2 : AustraliaPostFormat.Customer3);
        var dataBars = bars.GetRange(2, bars.Count - 4);

        if (dataBars.Count % 3 != 0) return false;
        var symbolCount = dataBars.Count / 3;
        if (symbolCount < 5) return false;

        var symbols = new int[symbolCount];
        for (var i = 0; i < symbolCount; i++) {
            var idx = i * 3;
            symbols[i] = AustraliaPostTables.BarDigitsToValue(dataBars[idx], dataBars[idx + 1], dataBars[idx + 2]);
        }

        var infoSymbols = symbolCount - 4;
        var paritySymbols = new int[4];
        Array.Copy(symbols, infoSymbols, paritySymbols, 0, 4);
        var expectedParity = AustraliaPostTables.ComputeParity(symbols.AsSpan(0, infoSymbols));
        for (var i = 0; i < 4; i++) {
            if (paritySymbols[i] != expectedParity[i]) return false;
        }

        var fccBars = dataBars.GetRange(0, 4);
        if (!TryDecodeDigits(fccBars, out var fccText)) return false;
        var fcc = int.Parse(fccText, CultureInfo.InvariantCulture);

        if (format == AustraliaPostFormat.Standard && fcc != AustraliaPostTables.FccStandard) return false;
        if (format == AustraliaPostFormat.Customer2 && fcc != AustraliaPostTables.FccCustomer2) return false;
        if (format == AustraliaPostFormat.Customer3 && fcc != AustraliaPostTables.FccCustomer3) return false;

        var dpidBars = dataBars.GetRange(4, 16);
        if (!TryDecodeDigits(dpidBars, out var dpid)) return false;

        var customerField = dataBars.GetRange(20, dataBars.Count - 20 - 12);
        var customerInfo = string.Empty;
        AustraliaPostCustomerEncodingTable? table = null;
        var ambiguous = false;

        if (format == AustraliaPostFormat.Standard) {
            if (customerField.Count != 1 || customerField[0] != 3) return false;
        } else {
            if (!TryDecodeCustomer(customerField, format, preferredCustomerTable, out customerInfo, out table, out ambiguous)) {
                return false;
            }
        }

        decoded = new AustraliaPostDecoded(format, fcc, dpid, customerInfo, table, ambiguous);
        return true;
    }

    private static bool TryDecodeCustomer(
        List<int> customerBars,
        AustraliaPostFormat format,
        AustraliaPostCustomerEncodingTable? preferredTable,
        out string customerInfo,
        out AustraliaPostCustomerEncodingTable? table,
        out bool ambiguous) {
        customerInfo = string.Empty;
        table = null;
        ambiguous = false;

        if (format == AustraliaPostFormat.Customer2) {
            if (customerBars.Count != 16) return false;
            var last = customerBars[customerBars.Count - 1];
            if (last == 3) {
                if (!TryDecodeC(customerBars, dropTrailingFiller: true, out customerInfo)) return false;
                table = AustraliaPostCustomerEncodingTable.C;
                return true;
            }

            if (!TryDecodeN(customerBars, dropTrailingFiller: false, out customerInfo)) return false;
            table = AustraliaPostCustomerEncodingTable.N;
            return true;
        }

        if (format != AustraliaPostFormat.Customer3) return false;
        if (customerBars.Count != 31) return false;

        if (preferredTable == AustraliaPostCustomerEncodingTable.C) {
            if (!TryDecodeC(customerBars, dropTrailingFiller: true, out customerInfo)) return false;
            table = AustraliaPostCustomerEncodingTable.C;
            return true;
        }

        if (preferredTable == AustraliaPostCustomerEncodingTable.N) {
            if (!TryDecodeN(customerBars, dropTrailingFiller: true, out customerInfo)) return false;
            table = AustraliaPostCustomerEncodingTable.N;
            return true;
        }

        var nOk = TryDecodeN(customerBars, dropTrailingFiller: true, out var nValue);
        var cOk = TryDecodeC(customerBars, dropTrailingFiller: true, out var cValue);

        if (nOk && cOk) {
            ambiguous = true;
            customerInfo = nValue;
            table = AustraliaPostCustomerEncodingTable.N;
            return true;
        }

        if (nOk) {
            customerInfo = nValue;
            table = AustraliaPostCustomerEncodingTable.N;
            return true;
        }

        if (cOk) {
            customerInfo = cValue;
            table = AustraliaPostCustomerEncodingTable.C;
            return true;
        }

        return false;
    }

    private static bool TryDecodeDigits(List<int> bars, out string digits) {
        digits = string.Empty;
        if (bars.Count % 2 != 0) return false;
        var output = new StringBuilder(bars.Count / 2);
        for (var i = 0; i < bars.Count; i += 2) {
            var digit = AustraliaPostTables.NDecodePairs[bars[i] * 4 + bars[i + 1]];
            if (digit < 0) return false;
            output.Append((char)('0' + digit));
        }
        digits = output.ToString();
        return true;
    }

    private static bool TryDecodeN(List<int> bars, bool dropTrailingFiller, out string digits) {
        digits = string.Empty;
        var count = bars.Count;
        if (dropTrailingFiller) {
            if (count == 0 || bars[count - 1] != 3) return false;
            count -= 1;
        }
        if (count % 2 != 0) return false;

        var output = new StringBuilder(count / 2);
        for (var i = 0; i < count; i += 2) {
            var digit = AustraliaPostTables.NDecodePairs[bars[i] * 4 + bars[i + 1]];
            if (digit < 0) return false;
            output.Append((char)('0' + digit));
        }

        digits = output.ToString();
        return true;
    }

    private static bool TryDecodeC(List<int> bars, bool dropTrailingFiller, out string text) {
        text = string.Empty;
        var count = bars.Count;
        if (dropTrailingFiller) {
            if (count == 0 || bars[count - 1] != 3) return false;
            count -= 1;
        }
        if (count % 3 != 0) return false;

        var output = new StringBuilder(count / 3);
        for (var i = 0; i < count; i += 3) {
            var value = AustraliaPostTables.BarDigitsToValue(bars[i], bars[i + 1], bars[i + 2]);
            if ((uint)value >= AustraliaPostTables.CDecodeValues.Length) return false;
            var ch = AustraliaPostTables.CDecodeValues[value];
            if (ch == '\0') return false;
            output.Append(ch);
        }

        text = output.ToString();
        return true;
    }

    private static List<int> ExtractBars(BitMatrix modules) {
        var bars = new List<int>(modules.Width / 2);
        var height = modules.Height;

        var firstBar = -1;
        var lastBar = -1;
        for (var x = 0; x < modules.Width; x++) {
            if (HasBar(modules, x)) {
                if (firstBar < 0) firstBar = x;
                lastBar = x;
            }
        }

        if (firstBar < 0 || lastBar < 0) return bars;

        var runs = new List<(bool isBar, int start, int length)>(modules.Width / 2);
        var current = HasBar(modules, firstBar);
        var runStart = firstBar;
        for (var x = firstBar + 1; x <= lastBar; x++) {
            var isBar = HasBar(modules, x);
            if (isBar == current) continue;
            runs.Add((current, runStart, x - runStart));
            current = isBar;
            runStart = x;
        }
        runs.Add((current, runStart, lastBar - runStart + 1));

        foreach (var run in runs) {
            if (!run.isBar) continue;
            var asc = false;
            var desc = false;
            var tracker = false;
            for (var x = run.start; x < run.start + run.length; x++) {
                if (!tracker && (modules[x, AustraliaPostTables.TrackerRowTop] || modules[x, AustraliaPostTables.TrackerRowBottom])) tracker = true;
                if (!asc && (modules[x, 0] || modules[x, 1] || modules[x, 2])) asc = true;
                if (!desc && (modules[x, height - 1] || modules[x, height - 2] || modules[x, height - 3])) desc = true;
            }

            if (!tracker && !asc && !desc) continue;

            var value = asc ? (desc ? 0 : 1) : (desc ? 2 : 3);
            bars.Add(value);
        }

        return bars;
    }

    private static bool HasBar(BitMatrix modules, int x) {
        for (var y = 0; y < modules.Height; y++) {
            if (modules[x, y]) return true;
        }
        return false;
    }
}
