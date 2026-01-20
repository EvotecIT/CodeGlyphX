#if NET8_0_OR_GREATER
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading;
using CodeGlyphX;

namespace CodeGlyphX.Qr;

internal static partial class QrPixelDecoder {
    private static global::CodeGlyphX.QrDecodeDiagnostics Better(global::CodeGlyphX.QrDecodeDiagnostics a, global::CodeGlyphX.QrDecodeDiagnostics b) {
        if (IsEmpty(a)) return b;
        if (IsEmpty(b)) return a;

        var sa = Score(a);
        var sb = Score(b);
        if (sb > sa) return b;
        if (sa > sb) return a;

        var da = a.FormatBestDistance;
        var db = b.FormatBestDistance;
        if (db >= 0 && (da < 0 || db < da)) return b;
        return a;
    }

    private static bool IsEmpty(QrPixelDecodeDiagnostics d) {
        return d.Scale == 0 && d.Dimension == 0 && d.CandidateCount == 0 && d.CandidateTriplesTried == 0 &&
               d.ModuleDiagnostics.Version == 0 && d.ModuleDiagnostics.Failure == global::CodeGlyphX.QrDecodeFailure.None;
    }

    private static bool IsEmpty(global::CodeGlyphX.QrDecodeDiagnostics d) {
        return d.Version == 0 && d.Failure == global::CodeGlyphX.QrDecodeFailure.None;
    }

    private static int Score(global::CodeGlyphX.QrDecodeDiagnostics d) {
        return d.Failure switch {
            global::CodeGlyphX.QrDecodeFailure.None => 5,
            global::CodeGlyphX.QrDecodeFailure.Payload => 4,
            global::CodeGlyphX.QrDecodeFailure.ReedSolomon => 3,
            global::CodeGlyphX.QrDecodeFailure.FormatInfo => 2,
            global::CodeGlyphX.QrDecodeFailure.InvalidSize => 1,
            _ => 0,
        };
    }
}
#endif
