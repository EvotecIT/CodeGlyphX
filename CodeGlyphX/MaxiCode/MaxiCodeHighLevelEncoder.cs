// Portions adapted from the Zint backend, Copyright (c) Robin Stuart and contributors.
// Licensed under BSD-3-Clause; see THIRD-PARTY-NOTICES.md.

using System;
using System.Collections.Generic;
using System.Text;
using CodeGlyphX.Internal;

namespace CodeGlyphX.MaxiCode;

internal static class MaxiCodeHighLevelEncoder {
    private const int StateCount = 5;

    private sealed class PathNode {
        internal int Cost { get; }
        internal PathNode? Previous { get; }
        internal byte[] Emitted { get; }
        internal int Position { get; }
        internal int State { get; }

        internal PathNode(int cost, PathNode? previous, byte[] emitted, int position, int state) {
            Cost = cost;
            Previous = previous;
            Emitted = emitted;
            Position = position;
            State = state;
        }
    }

    internal static byte[] EncodeText(string text, MaxiCodeMode mode, MaxiCodeEncodingOptions options, int capacity) {
        if (text is null) throw new ArgumentNullException(nameof(text));
        ResolveEncoding(text, options, out var encoding, out var eciAssignment);
        var payload = EncodingUtils.GetBytesStrict(encoding, text, nameof(text));
        return EncodeBytes(payload, mode, options, capacity, eciAssignment);
    }

    private static byte[] EncodeBytes(byte[] payload, MaxiCodeMode mode, MaxiCodeEncodingOptions options, int capacity, int? eciAssignment) {
        var prefix = new List<byte>(16);
        AppendStructuredAppend(options, prefix);
        var scm = BuildScmPrefix(mode, options);
        var source = new byte[scm.Length + payload.Length];
        Array.Copy(scm, source, scm.Length);
        Array.Copy(payload, 0, source, scm.Length, payload.Length);

        if (prefix.Count > capacity) throw new InvalidOperationException("MaxiCode control data exceeds the selected mode capacity.");
        var nodes = new PathNode?[source.Length + 1, StateCount];
        nodes[0, 0] = new PathNode(0, null, Array.Empty<byte>(), 0, 0);

        for (var position = 0; position < source.Length; position++) {
            if (position == scm.Length && eciAssignment.HasValue) ApplyEciAtBoundary(nodes, position, eciAssignment.Value);
            for (var state = 0; state < StateCount; state++) {
                var node = nodes[position, state];
                if (node is null) continue;
                AddNumericTransition(source, position, state, node, nodes);
                AddCharacterTransitions(source[position], position, state, node, nodes);
                if (state == 1) AddMultiShiftATransitions(source, position, node, nodes);
            }
        }
        if (scm.Length == source.Length && eciAssignment.HasValue) ApplyEciAtBoundary(nodes, source.Length, eciAssignment.Value);

        PathNode? best = null;
        var bestFinalCost = int.MaxValue;
        for (var state = 0; state < StateCount; state++) {
            var candidate = nodes[source.Length, state];
            if (candidate is null) continue;
            var total = prefix.Count + candidate.Cost;
            if (total < capacity && state is 3 or 4) total++;
            if (total > capacity || total >= bestFinalCost) continue;
            best = candidate;
            bestFinalCost = total;
        }
        if (best is null) throw new InvalidOperationException($"Payload does not fit in MaxiCode Mode {(int)mode}.");

        var chunks = new List<byte[]>();
        for (var node = best; node.Previous is not null; node = node.Previous) chunks.Add(node.Emitted);
        chunks.Reverse();
        var output = new List<byte>(capacity);
        output.AddRange(prefix);
        for (var i = 0; i < chunks.Count; i++) output.AddRange(chunks[i]);

        var finalState = best.State;
        if (output.Count < capacity && finalState is 3 or 4) {
            output.Add(58); // Latch A from Code Set C or D.
            finalState = 0;
        }
        var pad = finalState == 2 ? (byte)28 : (byte)33;
        while (output.Count < capacity) output.Add(pad);
        return output.ToArray();
    }

    private static void AddCharacterTransitions(byte value, int position, int state, PathNode node, PathNode?[,] nodes) {
        for (var target = 0; target < StateCount; target++) {
            if (!MaxiCodeTables.CanEncode(target, value)) continue;
            var symbol = MaxiCodeTables.SymbolForState(target, value);
            if (target == state) {
                Update(nodes, position + 1, state, node, new[] { symbol });
                continue;
            }

            var latch = MaxiCodeTables.LatchSequences[target][state];
            var latched = new byte[latch.Length + 1];
            Array.Copy(latch, latched, latch.Length);
            latched[latched.Length - 1] = symbol;
            Update(nodes, position + 1, target, node, latched);

            var shift = MaxiCodeTables.GetShiftSymbol(state, target);
            if (shift >= 0) Update(nodes, position + 1, state, node, new[] { (byte)shift, symbol });
        }
    }

    private static void AddMultiShiftATransitions(byte[] payload, int position, PathNode node, PathNode?[,] nodes) {
        for (var count = 2; count <= 3; count++) {
            if (position + count > payload.Length) break;
            var canEncode = true;
            for (var i = 0; i < count; i++) {
                if (!MaxiCodeTables.CanEncode(0, payload[position + i])) { canEncode = false; break; }
            }
            if (!canEncode) continue;
            var emitted = new byte[count + 1];
            emitted[0] = count == 2 ? (byte)56 : (byte)57;
            for (var i = 0; i < count; i++) emitted[i + 1] = MaxiCodeTables.SymbolForState(0, payload[position + i]);
            Update(nodes, position + count, 1, node, emitted);
        }
    }

    private static void AddNumericTransition(byte[] payload, int position, int state, PathNode node, PathNode?[,] nodes) {
        if (position + 9 > payload.Length) return;
        var value = 0;
        for (var i = 0; i < 9; i++) {
            var digit = payload[position + i];
            if (digit < (byte)'0' || digit > (byte)'9') return;
            value = value * 10 + digit - (byte)'0';
        }
        Update(nodes, position + 9, state, node, new[] {
            (byte)31,
            (byte)((value >> 24) & 0x3F),
            (byte)((value >> 18) & 0x3F),
            (byte)((value >> 12) & 0x3F),
            (byte)((value >> 6) & 0x3F),
            (byte)(value & 0x3F)
        });
    }

    private static void Update(PathNode?[,] nodes, int position, int state, PathNode previous, byte[] emitted) {
        var cost = previous.Cost + emitted.Length;
        var current = nodes[position, state];
        if (current is not null && current.Cost <= cost) return;
        nodes[position, state] = new PathNode(cost, previous, emitted, position, state);
    }

    private static void AppendStructuredAppend(MaxiCodeEncodingOptions options, List<byte> prefix) {
        var index = options.StructuredAppendIndex;
        var count = options.StructuredAppendCount;
        if (!index.HasValue && !count.HasValue) return;
        if (!index.HasValue || !count.HasValue || count.Value is < 2 or > 8 || index.Value < 1 || index.Value > count.Value) {
            throw new InvalidOperationException("MaxiCode structured append requires a one-based index and a count from 2 through 8.");
        }
        prefix.Add(33); // PAD at the beginning signals structured append.
        prefix.Add((byte)((count.Value - 1) | ((index.Value - 1) << 3)));
    }

    private static byte[] BuildScmPrefix(MaxiCodeMode mode, MaxiCodeEncodingOptions options) {
        if (!options.StructuredCarrierMessageVersion.HasValue) return Array.Empty<byte>();
        if (mode is not (MaxiCodeMode.StructuredCarrierNumeric or MaxiCodeMode.StructuredCarrierAlphanumeric)) {
            throw new InvalidOperationException("The Structured Carrier Message prefix is available only in MaxiCode Modes 2 and 3.");
        }
        var version = options.StructuredCarrierMessageVersion.Value;
        if (version is < 0 or > 99) throw new InvalidOperationException("The Structured Carrier Message version must be between 0 and 99.");
        var scm = "[)>\u001e01\u001d" + version.ToString("00");
        var bytes = new byte[scm.Length];
        for (var i = 0; i < scm.Length; i++) bytes[i] = (byte)scm[i];
        return bytes;
    }

    private static void ApplyEciAtBoundary(PathNode?[,] nodes, int position, int assignment) {
        var control = new List<byte>(5);
        AppendEci(assignment, control);
        var emitted = control.ToArray();
        for (var state = 0; state < StateCount; state++) {
            var previous = nodes[position, state];
            if (previous is null) continue;
            nodes[position, state] = new PathNode(previous.Cost + emitted.Length, previous, emitted, position, state);
        }
    }

    private static void AppendEci(int assignment, List<byte> output) {
        if (assignment is < 0 or > 999999) throw new InvalidOperationException("MaxiCode ECI assignments must be between 0 and 999999.");
        output.Add(27);
        if (assignment <= 31) {
            output.Add((byte)assignment);
        } else if (assignment <= 1023) {
            output.Add((byte)(0x20 | (assignment >> 6)));
            output.Add((byte)(assignment & 0x3F));
        } else if (assignment <= 32767) {
            output.Add((byte)(0x30 | (assignment >> 12)));
            output.Add((byte)((assignment >> 6) & 0x3F));
            output.Add((byte)(assignment & 0x3F));
        } else {
            output.Add((byte)(0x38 | (assignment >> 18)));
            output.Add((byte)((assignment >> 12) & 0x3F));
            output.Add((byte)((assignment >> 6) & 0x3F));
            output.Add((byte)(assignment & 0x3F));
        }
    }

    private static void ResolveEncoding(string text, MaxiCodeEncodingOptions options, out Encoding encoding, out int? eciAssignment) {
        encoding = EncodingUtils.ResolveTextEncoding(
            text,
            options.TextEncoding,
            options.EciAssignmentNumber,
            "MaxiCode",
            out eciAssignment);
    }
}
