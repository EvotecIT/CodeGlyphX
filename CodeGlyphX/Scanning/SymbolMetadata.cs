using System.Collections.Generic;
using CodeGlyphX.DataMatrix;
using CodeGlyphX.Pdf417;

namespace CodeGlyphX;

/// <summary>Format-specific structural information recovered by a decoder.</summary>
public abstract class SymbolMetadata {
    internal SymbolMetadata() { }

    internal static IReadOnlyList<int> Copy(IReadOnlyList<int> values) {
        var copy = new int[values.Count];
        for (var i = 0; i < copy.Length; i++) copy[i] = values[i];
        return System.Array.AsReadOnly(copy);
    }
}

/// <summary>QR Code version, error correction, and control information.</summary>
public sealed class QrSymbolMetadata : SymbolMetadata {
    /// <summary>Gets the version from 1 through 40.</summary>
    public int Version { get; }
    /// <summary>Gets the error correction level.</summary>
    public QrErrorCorrectionLevel ErrorCorrectionLevel { get; }
    /// <summary>Gets the selected mask.</summary>
    public int Mask { get; }
    /// <summary>Gets structured-append information when present.</summary>
    public QrStructuredAppend? StructuredAppend { get; }
    /// <summary>Gets the FNC1 control mode.</summary>
    public QrFnc1Mode Fnc1Mode { get; }
    /// <summary>Gets the second-position FNC1 application indicator when present.</summary>
    public int? Fnc1ApplicationIndicator { get; }
    internal QrSymbolMetadata(QrDecoded value) {
        Version = value.Version; ErrorCorrectionLevel = value.ErrorCorrectionLevel; Mask = value.Mask;
        StructuredAppend = value.StructuredAppend; Fnc1Mode = value.Fnc1Mode;
        Fnc1ApplicationIndicator = value.Fnc1ApplicationIndicator;
    }
}

/// <summary>Micro QR version, error correction, and mask information.</summary>
public sealed class MicroQrSymbolMetadata : SymbolMetadata {
    /// <summary>Gets the Micro QR version from 1 through 4.</summary>
    public int Version { get; }
    /// <summary>Gets the error correction level.</summary>
    public QrErrorCorrectionLevel ErrorCorrectionLevel { get; }
    /// <summary>Gets the selected mask.</summary>
    public int Mask { get; }
    internal MicroQrSymbolMetadata(MicroQrDecoded value) {
        Version = value.Version; ErrorCorrectionLevel = value.ErrorCorrectionLevel; Mask = value.Mask;
    }
}

/// <summary>Rectangular Micro QR version, error correction, and control information.</summary>
public sealed class RmQrSymbolMetadata : SymbolMetadata {
    /// <summary>Gets the version from 1 through 32.</summary>
    public int Version { get; }
    /// <summary>Gets the version name, for example R11x27.</summary>
    public string VersionName { get; }
    /// <summary>Gets the error correction level.</summary>
    public QrErrorCorrectionLevel ErrorCorrectionLevel { get; }
    /// <summary>Gets whether FNC1 identified a GS1 payload.</summary>
    public bool IsGs1 { get; }
    /// <summary>Gets the last ECI assignment when present.</summary>
    public int? EciAssignmentNumber { get; }
    internal RmQrSymbolMetadata(RmQrDecoded value) {
        Version = value.Version; VersionName = value.VersionName; ErrorCorrectionLevel = value.ErrorCorrectionLevel;
        IsGs1 = value.IsGs1; EciAssignmentNumber = value.EciAssignmentNumber;
    }
}

/// <summary>Data Matrix dimensions and payload control information.</summary>
public sealed class DataMatrixSymbolMetadata : SymbolMetadata {
    /// <summary>Gets whether FNC1 identified a GS1 payload.</summary>
    public bool IsGs1 { get; }
    /// <summary>Gets structured-append information when present.</summary>
    public DataMatrixStructuredAppend? StructuredAppend { get; }
    /// <summary>Gets whether reader programming was declared.</summary>
    public bool ReaderProgramming { get; }
    /// <summary>Gets the Macro 05/06 control mode.</summary>
    public DataMatrixMacro Macro { get; }
    /// <summary>Gets ECI assignments in payload order.</summary>
    public IReadOnlyList<int> EciAssignments { get; }
    /// <summary>Gets the symbol row count.</summary>
    public int Rows { get; }
    /// <summary>Gets the symbol column count.</summary>
    public int Columns { get; }
    /// <summary>Gets whether the symbol belongs to the DMRE family.</summary>
    public bool IsDmre { get; }
    internal DataMatrixSymbolMetadata(DataMatrixDecoded value) {
        IsGs1 = value.IsGs1; StructuredAppend = value.StructuredAppend; ReaderProgramming = value.ReaderProgramming;
        Macro = value.Macro; EciAssignments = Copy(value.EciAssignments); Rows = value.Rows; Columns = value.Columns; IsDmre = value.IsDmre;
    }
}

/// <summary>PDF417 Macro information when declared in the payload.</summary>
public sealed class Pdf417SymbolMetadata : SymbolMetadata {
    /// <summary>Gets Macro PDF417 information when present.</summary>
    public Pdf417MacroMetadata? Macro { get; }
    internal Pdf417SymbolMetadata(Pdf417Decoded value) { Macro = value.Macro; }
}

/// <summary>MaxiCode carrier, mode, and payload control information.</summary>
public sealed class MaxiCodeSymbolMetadata : SymbolMetadata {
    /// <summary>Gets the MaxiCode mode.</summary>
    public MaxiCodeMode Mode { get; }
    /// <summary>Gets the carrier postal code when present.</summary>
    public string? PostalCode { get; }
    /// <summary>Gets the carrier country code when present.</summary>
    public int? CountryCode { get; }
    /// <summary>Gets the carrier service class when present.</summary>
    public int? ServiceClass { get; }
    /// <summary>Gets ECI assignments in payload order.</summary>
    public IReadOnlyList<int> EciAssignments { get; }
    /// <summary>Gets the one-based structured-append index when present.</summary>
    public int? StructuredAppendIndex { get; }
    /// <summary>Gets the structured-append count when present.</summary>
    public int? StructuredAppendCount { get; }
    /// <summary>Gets whether this is a reader-programming symbol.</summary>
    public bool IsReaderProgramming => Mode == MaxiCodeMode.ReaderProgramming;
    internal MaxiCodeSymbolMetadata(MaxiCodeDecoded value) {
        Mode = value.Mode; PostalCode = value.PostalCode; CountryCode = value.CountryCode; ServiceClass = value.ServiceClass;
        EciAssignments = Copy(value.EciAssignments); StructuredAppendIndex = value.StructuredAppendIndex; StructuredAppendCount = value.StructuredAppendCount;
    }
}

/// <summary>DotCode mask and payload control information.</summary>
public sealed class DotCodeSymbolMetadata : SymbolMetadata {
    /// <summary>Gets whether the payload contains FNC1.</summary>
    public bool HasFnc1 { get; }
    /// <summary>Gets whether reader initialization was declared.</summary>
    public bool ReaderInitialization { get; }
    /// <summary>Gets the selected mask.</summary>
    public int Mask { get; }
    /// <summary>Gets ECI assignments in payload order.</summary>
    public IReadOnlyList<int> EciAssignments { get; }
    /// <summary>Gets the one-based structured-append index when present.</summary>
    public int? StructuredAppendIndex { get; }
    /// <summary>Gets the structured-append count when present.</summary>
    public int? StructuredAppendCount { get; }
    internal DotCodeSymbolMetadata(DotCodeDecoded value) {
        HasFnc1 = value.HasFnc1; ReaderInitialization = value.ReaderInitialization; Mask = value.Mask;
        EciAssignments = Copy(value.EciAssignments); StructuredAppendIndex = value.StructuredAppendIndex; StructuredAppendCount = value.StructuredAppendCount;
    }
}

/// <summary>Han Xin version, mask, and error correction information.</summary>
public sealed class HanXinSymbolMetadata : SymbolMetadata {
    /// <summary>Gets the Han Xin version.</summary>
    public int Version { get; }
    /// <summary>Gets the error correction level.</summary>
    public int ErrorCorrectionLevel { get; }
    /// <summary>Gets the selected mask.</summary>
    public int Mask { get; }
    /// <summary>Gets ECI assignments in payload order.</summary>
    public IReadOnlyList<int> EciAssignments { get; }
    internal HanXinSymbolMetadata(HanXinDecoded value) {
        Version = value.Version; ErrorCorrectionLevel = value.ErrorCorrectionLevel; Mask = value.Mask;
        EciAssignments = Copy(value.EciAssignments);
    }
}

/// <summary>Both component messages recovered from a GS1 Composite symbol.</summary>
public sealed class Gs1CompositeSymbolMetadata : SymbolMetadata {
    /// <summary>Gets the linear component's GS1 element string.</summary>
    public string LinearText { get; }
    /// <summary>Gets the two-dimensional component's GS1 element string.</summary>
    public string CompositeText { get; }
    /// <summary>Gets the composite component type.</summary>
    public Gs1CompositeComponent Component { get; }
    internal Gs1CompositeSymbolMetadata(Gs1CompositeDecoded value) {
        LinearText = value.LinearText; CompositeText = value.CompositeText; Component = value.Component;
    }
}
