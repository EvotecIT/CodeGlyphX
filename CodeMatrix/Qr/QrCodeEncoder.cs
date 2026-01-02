using System;
using System.Text;
using CodeMatrix.Qr;

namespace CodeMatrix;

public static class QrCodeEncoder {
    public static QrCode EncodeText(
        string text,
        QrErrorCorrectionLevel ecc = QrErrorCorrectionLevel.M,
        int minVersion = 1,
        int maxVersion = 10,
        int? forceMask = null) {
        if (text is null) throw new ArgumentNullException(nameof(text));
        var data = Encoding.UTF8.GetBytes(text);
        return EncodeBytes(data, ecc, minVersion, maxVersion, forceMask);
    }

    public static QrCode EncodeBytes(
        byte[] data,
        QrErrorCorrectionLevel ecc = QrErrorCorrectionLevel.M,
        int minVersion = 1,
        int maxVersion = 10,
        int? forceMask = null) {
        if (data is null) throw new ArgumentNullException(nameof(data));
        return QrEncoder.EncodeByteMode(data, ecc, minVersion, maxVersion, forceMask);
    }

#if NET8_0_OR_GREATER
    public static QrCode EncodeBytes(
        ReadOnlySpan<byte> data,
        QrErrorCorrectionLevel ecc = QrErrorCorrectionLevel.M,
        int minVersion = 1,
        int maxVersion = 10,
        int? forceMask = null) {
        return QrEncoder.EncodeByteMode(data.ToArray(), ecc, minVersion, maxVersion, forceMask);
    }
#endif
}

