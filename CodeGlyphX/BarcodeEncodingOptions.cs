using System;

namespace CodeGlyphX;

/// <summary>Controls optional linear check characters and their physical-print interpretation.</summary>
/// <remarks>Existing encoding overloads keep their defaults. A selected check mode never removes mandatory carrier checks.</remarks>
public sealed class BarcodeEncodingOptions {
    /// <summary>Gets or sets optional-check handling. Null retains the selected format's existing encoder default.</summary>
    public BarcodeCheckMode? CheckMode { get; set; }
    /// <summary>Gets or sets whether the preferred human readable line includes optional check characters.</summary>
    public bool PrintCheckCharacters { get; set; } = true;
    /// <summary>Gets or sets Code 39 Full ASCII expansion. The check is calculated over its expanded symbol alphabet.</summary>
    public bool Code39FullAscii { get; set; }
    /// <summary>Gets or sets the MSI check algorithm.</summary>
    public MsiChecksumType MsiChecksum { get; set; } = MsiChecksumType.Mod10;
    /// <summary>Gets or sets known MSI data length for validating an ambiguous supplied Mod11 check.</summary>
    public int? MsiExpectedDataLength { get; set; }
    /// <summary>Gets or sets the short three-bar Channel Code finder.</summary>
    public bool ChannelShortFinder { get; set; }
    /// <summary>Gets or sets explicit Channel Code channels, or null for digit-width selection.</summary>
    public int? ChannelCount { get; set; }
    /// <summary>Gets or sets a Codabar start when the input omits its framing characters.</summary>
    public char CodabarStart { get; set; } = 'A';
    /// <summary>Gets or sets a Codabar stop when the input omits its framing characters.</summary>
    public char CodabarStop { get; set; } = 'B';

    internal BarcodeEncodingOptions Snapshot() {
        if (CheckMode.HasValue && !Enum.IsDefined(typeof(BarcodeCheckMode), CheckMode.Value)) throw new ArgumentOutOfRangeException(nameof(CheckMode));
        if (!Enum.IsDefined(typeof(MsiChecksumType), MsiChecksum)) throw new ArgumentOutOfRangeException(nameof(MsiChecksum));
        if (MsiExpectedDataLength.HasValue && (MsiExpectedDataLength < 1 || MsiExpectedDataLength > 1048576)) throw new ArgumentOutOfRangeException(nameof(MsiExpectedDataLength));
        return new BarcodeEncodingOptions {
            CheckMode = CheckMode, PrintCheckCharacters = PrintCheckCharacters, Code39FullAscii = Code39FullAscii,
            MsiChecksum = MsiChecksum, MsiExpectedDataLength = MsiExpectedDataLength, ChannelShortFinder = ChannelShortFinder,
            ChannelCount = ChannelCount, CodabarStart = CodabarStart, CodabarStop = CodabarStop
        };
    }
}

/// <summary>How an optional symbol check is supplied.</summary>
public enum BarcodeCheckMode {
    /// <summary>All input characters are data and no optional check is added.</summary>
    None,
    /// <summary>Compute and append the selected format's check character or characters.</summary>
    Compute,
    /// <summary>Validate check characters already supplied in the input and encode them exactly once.</summary>
    Validate
}
