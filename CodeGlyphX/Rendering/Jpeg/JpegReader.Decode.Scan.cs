using System;

namespace CodeGlyphX.Rendering.Jpeg;

public static partial class JpegReader {
    private static byte[] DecodeBaselineScan(
        ReadOnlySpan<byte> scanData,
        ScanHeader scan,
        JpegFrame frame,
        int[][] quantTables,
        HuffmanTable[] dcTables,
        HuffmanTable[] acTables,
        int restartInterval,
        int? adobeTransform,
        bool allowTruncated,
        bool highQualityChroma) {
        if (frame.ComponentCount == 0) throw new FormatException("Invalid JPEG frame.");
        if (frame.ComponentCount != 1 && frame.ComponentCount != 3 && frame.ComponentCount != 4) {
            throw new FormatException("Unsupported JPEG component count.");
        }
        if (scan.Ss != 0 || scan.Se != 63 || scan.Ah != 0 || scan.Al != 0) {
            throw new FormatException("Invalid baseline JPEG scan parameters.");
        }

        EnsureStandardHuffmanTables(dcTables, acTables);

        var maxH = frame.MaxH;
        var maxV = frame.MaxV;
        var mcuWidth = maxH * 8;
        var mcuHeight = maxV * 8;
        var mcuCols = (frame.Width + mcuWidth - 1) / mcuWidth;
        var mcuRows = (frame.Height + mcuHeight - 1) / mcuHeight;

        var states = new BaselineComponentState[frame.ComponentCount];
        for (var i = 0; i < frame.ComponentCount; i++) {
            var comp = frame.Components[i];
            if (comp.QuantId >= quantTables.Length || quantTables[comp.QuantId] is null) {
                throw new FormatException("Missing JPEG quantization table.");
            }
            if (comp.DcTable >= dcTables.Length || !dcTables[comp.DcTable].IsValid) {
                throw new FormatException("Missing JPEG DC Huffman table.");
            }
            if (comp.AcTable >= acTables.Length || !acTables[comp.AcTable].IsValid) {
                throw new FormatException("Missing JPEG AC Huffman table.");
            }
            states[i] = new BaselineComponentState(comp, mcuCols * comp.H, mcuRows * comp.V);
        }

        var reader = new JpegBitReader(scanData, allowTruncated);
        var mcuIndex = 0;
        var isSingle = scan.ComponentIndices.Length == 1;
        var scanMcuCols = isSingle ? states[scan.ComponentIndices[0]].BlocksPerRow : mcuCols;
        var scanMcuRows = isSingle ? states[scan.ComponentIndices[0]].BlocksPerCol : mcuRows;

        for (var my = 0; my < scanMcuRows; my++) {
            for (var mx = 0; mx < scanMcuCols; mx++) {
                if (restartInterval > 0 && mcuIndex > 0 && mcuIndex % restartInterval == 0) {
                    reader.ExpectRestartMarker();
                    for (var i = 0; i < scan.ComponentIndices.Length; i++) {
                        states[scan.ComponentIndices[i]].PrevDc = 0;
                    }
                }

                if (isSingle) {
                    var compIndex = scan.ComponentIndices[0];
                    var state = states[compIndex];
                    DecodeBlock(
                        ref reader,
                        dcTables[state.Component.DcTable],
                        acTables[state.Component.AcTable],
                        quantTables[state.Component.QuantId],
                        ref state.PrevDc,
                        state.BlockCoeffs,
                        state.BlockPixels);
                    WriteBlock(state.Buffer, state.Stride, mx, my, state.BlockPixels);
                } else {
                    for (var ci = 0; ci < scan.ComponentIndices.Length; ci++) {
                        var compIndex = scan.ComponentIndices[ci];
                        var state = states[compIndex];
                        var blocks = state.Component.H * state.Component.V;
                        for (var b = 0; b < blocks; b++) {
                            DecodeBlock(
                                ref reader,
                                dcTables[state.Component.DcTable],
                                acTables[state.Component.AcTable],
                                quantTables[state.Component.QuantId],
                                ref state.PrevDc,
                                state.BlockCoeffs,
                                state.BlockPixels);

                            var blockX = mx * state.Component.H + (b % state.Component.H);
                            var blockY = my * state.Component.V + (b / state.Component.H);
                            WriteBlock(state.Buffer, state.Stride, blockX, blockY, state.BlockPixels);
                        }
                    }
                }

                if (reader.RestartMarkerSeen) {
                    for (var i = 0; i < scan.ComponentIndices.Length; i++) {
                        states[scan.ComponentIndices[i]].PrevDc = 0;
                    }
                    reader.RestartMarkerSeen = false;
                }

                mcuIndex++;
            }
        }

        return ComposeRgba(frame, states, adobeTransform, highQualityChroma);
    }

    private static void DecodeProgressiveScan(
        ReadOnlySpan<byte> scanData,
        ScanHeader scan,
        JpegFrame frame,
        ProgressiveState state,
        int[][] quantTables,
        HuffmanTable[] dcTables,
        HuffmanTable[] acTables,
        int restartInterval,
        bool allowTruncated) {
        if (scan.Ss > scan.Se || scan.Se > 63 || (scan.Ss == 0 && scan.Se != 0) ||
            scan.Ah > 13 || scan.Al > 13 ||
            (scan.Ah != 0 && scan.Ah != scan.Al + 1)) {
            throw new FormatException("Invalid progressive JPEG scan parameters.");
        }
        EnsureStandardHuffmanTables(dcTables, acTables);
        var reader = new JpegBitReader(scanData, allowTruncated);
        var mcuIndex = 0;
        var eobRun = 0;
        var isSingle = scan.ComponentIndices.Length == 1;
        // A noninterleaved scan contains only real component blocks, not MCU edge padding.
        var component = frame.Components[scan.ComponentIndices[0]];
        var scanMcuCols = isSingle ? (frame.Width * component.H + frame.MaxH * 8 - 1) / (frame.MaxH * 8) : state.McuCols;
        var scanMcuRows = isSingle ? (frame.Height * component.V + frame.MaxV * 8 - 1) / (frame.MaxV * 8) : state.McuRows;

        for (var i = 0; i < scan.ComponentIndices.Length; i++) {
            var componentIndex = scan.ComponentIndices[i];
            // SOS table selectors can change between scans, unlike SOF sampling factors.
            state.Components[componentIndex].Component = frame.Components[componentIndex];
            state.Components[componentIndex].PrevDc = 0;
        }

        for (var my = 0; my < scanMcuRows; my++) {
            for (var mx = 0; mx < scanMcuCols; mx++) {
                if (restartInterval > 0 && mcuIndex > 0 && mcuIndex % restartInterval == 0) {
                    reader.ExpectRestartMarker();
                    for (var i = 0; i < scan.ComponentIndices.Length; i++) {
                        state.Components[scan.ComponentIndices[i]].PrevDc = 0;
                    }
                    eobRun = 0;
                }

                if (isSingle) {
                    var compIndex = scan.ComponentIndices[0];
                    DecodeProgressiveBlock(
                        ref reader,
                        scan,
                        state.Components[compIndex],
                        dcTables,
                        acTables,
                        quantTables,
                        mx,
                        my,
                        ref eobRun);
                } else {
                    for (var ci = 0; ci < scan.ComponentIndices.Length; ci++) {
                        var compIndex = scan.ComponentIndices[ci];
                        var compState = state.Components[compIndex];
                        var blocks = compState.Component.H * compState.Component.V;
                        for (var b = 0; b < blocks; b++) {
                            var blockX = mx * compState.Component.H + (b % compState.Component.H);
                            var blockY = my * compState.Component.V + (b / compState.Component.H);
                            DecodeProgressiveBlock(
                                ref reader,
                                scan,
                                compState,
                                dcTables,
                                acTables,
                                quantTables,
                                blockX,
                                blockY,
                                ref eobRun);
                        }
                    }
                }

                if (reader.RestartMarkerSeen) {
                    for (var i = 0; i < scan.ComponentIndices.Length; i++) {
                        state.Components[scan.ComponentIndices[i]].PrevDc = 0;
                    }
                    reader.RestartMarkerSeen = false;
                    eobRun = 0;
                }

                mcuIndex++;
            }
        }
    }

    private static void DecodeProgressiveBlock(
        ref JpegBitReader reader,
        ScanHeader scan,
        ProgressiveComponentState state,
        HuffmanTable[] dcTables,
        HuffmanTable[] acTables,
        int[][] quantTables,
        int blockX,
        int blockY,
        ref int eobRun) {
        var quant = quantTables[state.Component.QuantId];
        if (quant is null) throw new FormatException("Missing JPEG quantization table.");
        var baseIndex = (blockY * state.BlocksPerRow + blockX) * 64;

        if (scan.Ss == 0 && scan.Se == 0) {
            var dcTable = dcTables[state.Component.DcTable];
            if (!dcTable.IsValid) throw new FormatException("Missing JPEG DC Huffman table.");
            if (scan.Ah == 0) {
                var t = DecodeHuffman(ref reader, dcTable, useFast: false);
                var diff = t == 0 ? 0 : Extend(reader.ReadBits(t), t);
                var dc = state.PrevDc + (diff << scan.Al);
                state.PrevDc = dc;
                state.Coeffs[baseIndex] = dc * quant[0];
            } else {
                var bit = reader.ReadBit();
                if (bit != 0) {
                    var delta = (1 << scan.Al) * quant[0];
                    // DC refinement appends a two's-complement bit, also for negative DC values.
                    state.Coeffs[baseIndex] += delta;
                }
            }
            return;
        }

        if (scan.Ss > scan.Se || scan.Se > 63) throw new FormatException("Invalid JPEG spectral selection.");
        var acTable = acTables[state.Component.AcTable];
        if (!acTable.IsValid) throw new FormatException("Missing JPEG AC Huffman table.");

        if (scan.Ah == 0) {
            DecodeProgressiveAcFirst(ref reader, scan, state, acTable, quant, baseIndex, ref eobRun);
        } else {
            DecodeProgressiveAcRefine(ref reader, scan, state, acTable, quant, baseIndex, ref eobRun);
        }
    }

    private static void DecodeProgressiveAcFirst(
        ref JpegBitReader reader,
        ScanHeader scan,
        ProgressiveComponentState state,
        HuffmanTable acTable,
        int[] quant,
        int baseIndex,
        ref int eobRun) {
        if (eobRun > 0) {
            eobRun--;
            return;
        }

        var k = (int)scan.Ss;
        while (k <= scan.Se) {
            var rs = DecodeHuffman(ref reader, acTable, useFast: false);
            if (rs == 0) {
                eobRun = 0;
                break;
            }
            var r = rs >> 4;
            var s = rs & 0x0F;
            if (s == 0) {
                if (r == 15) {
                    k += 16;
                    continue;
                }
                eobRun = (1 << r) - 1;
                if (r > 0) eobRun += reader.ReadBits(r);
                break;
            }

            k += r;
            if (k > scan.Se) break;
            var ac = Extend(reader.ReadBits(s), s);
            var zig = ZigZag[k];
            state.Coeffs[baseIndex + zig] = (ac << scan.Al) * quant[zig];
            k++;
        }
    }

    private static void DecodeProgressiveAcRefine(
        ref JpegBitReader reader,
        ScanHeader scan,
        ProgressiveComponentState state,
        HuffmanTable acTable,
        int[] quant,
        int baseIndex,
        ref int eobRun) {
        var k = (int)scan.Ss;
        if (eobRun > 0) {
            for (; k <= scan.Se; k++) {
                RefineCoefficient(ref reader, state.Coeffs, baseIndex + ZigZag[k], scan.Al, quant[ZigZag[k]]);
            }
            eobRun--;
            return;
        }

        while (k <= scan.Se) {
            var rs = DecodeHuffman(ref reader, acTable, useFast: false);
            var r = rs >> 4;
            var s = rs & 0x0F;

            if (s == 0) {
                if (r == 15) {
                    var zeros = 16;
                    while (zeros > 0 && k <= scan.Se) {
                        var index = baseIndex + ZigZag[k];
                        RefineCoefficient(ref reader, state.Coeffs, index, scan.Al, quant[ZigZag[k]]);
                        if (state.Coeffs[index] == 0) zeros--;
                        k++;
                    }
                    continue;
                }

                eobRun = (1 << r) - 1;
                if (r > 0) eobRun += reader.ReadBits(r);
                for (; k <= scan.Se; k++) {
                    RefineCoefficient(ref reader, state.Coeffs, baseIndex + ZigZag[k], scan.Al, quant[ZigZag[k]]);
                }
                break;
            }

            if (s != 1) throw new FormatException("Invalid JPEG AC refinement symbol.");
            // The new coefficient's sign precedes correction bits for existing coefficients.
            var ac = reader.ReadBit() == 1 ? 1 : -1;
            while (k <= scan.Se) {
                var index = baseIndex + ZigZag[k];
                if (state.Coeffs[index] != 0) {
                    RefineCoefficient(ref reader, state.Coeffs, index, scan.Al, quant[ZigZag[k]]);
                } else {
                    if (r == 0) break;
                    r--;
                }
                k++;
            }

            if (k > scan.Se) throw new FormatException("Invalid JPEG AC refinement run.");
            var zig = ZigZag[k];
            state.Coeffs[baseIndex + zig] = (ac << scan.Al) * quant[zig];
            k++;
        }
    }

    private static void RefineCoefficient(ref JpegBitReader reader, int[] coeffs, int index, int al, int quant) {
        if (coeffs[index] == 0) return;
        var bit = reader.ReadBit();
        if (bit == 0) return;
        var delta = (1 << al) * quant;
        coeffs[index] += coeffs[index] > 0 ? delta : -delta;
    }

}
