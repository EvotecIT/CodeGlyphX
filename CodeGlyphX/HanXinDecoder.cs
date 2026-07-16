using System;
using CodeGlyphX.HanXin;

namespace CodeGlyphX;

/// <summary>Decodes exact sampled Han Xin Code grids with structural and data Reed-Solomon correction.</summary>
public static class HanXinDecoder {
    /// <summary>Attempts to decode a Han Xin Code grid and preserve structural metadata.</summary>
    public static bool TryDecodeDetailed(BitMatrix modules, out HanXinDecoded decoded) {
        decoded = null!;
        if (!HanXinMatrixCodec.TryDecode(modules, out var data, out var version, out var eccLevel, out var mask)) return false;
        if (!HanXinPayloadCodec.TryDecode(data, out var payload)) return false;
        decoded = new HanXinDecoded(payload.Text, payload.Bytes, version, eccLevel, mask, payload.EciAssignments);
        return true;
    }

    /// <summary>Attempts to decode a Han Xin Code grid and return only its text.</summary>
    public static bool TryDecode(BitMatrix modules, out string text) {
        if (TryDecodeDetailed(modules, out var decoded)) { text = decoded.Text; return true; }
        text = string.Empty;
        return false;
    }
}
