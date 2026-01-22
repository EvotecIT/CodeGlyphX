using System;
using System.Collections.Generic;
using System.Text;
using CodeGlyphX;

namespace CodeGlyphX.Pdf417;

/// <summary>
/// Collects Macro PDF417 segments and assembles the original payload when complete.
/// </summary>
public sealed class Pdf417MacroAssembler {
    private readonly Dictionary<int, string> _segments = new Dictionary<int, string>();
    private string? _fileId;
    private int? _segmentCount;
    private int? _lastSegmentIndex;
    private bool _hasLastSegment;
    private string? _fileName;
    private long? _timestamp;
    private string? _sender;
    private string? _addressee;
    private long? _fileSize;
    private int? _checksum;

    /// <summary>
    /// Gets the Macro PDF417 file identifier when known.
    /// </summary>
    public string? FileId => _fileId;

    /// <summary>
    /// Gets the expected segment count when present in metadata.
    /// </summary>
    public int? SegmentCount => _segmentCount;

    /// <summary>
    /// Gets whether a segment flagged as the last segment was received.
    /// </summary>
    public bool HasLastSegment => _hasLastSegment;

    /// <summary>
    /// Gets the index of the last segment when known.
    /// </summary>
    public int? LastSegmentIndex => _lastSegmentIndex;

    /// <summary>
    /// Gets the number of segments collected.
    /// </summary>
    public int ReceivedCount => _segments.Count;

    /// <summary>
    /// Gets the file name when present.
    /// </summary>
    public string? FileName => _fileName;

    /// <summary>
    /// Gets the timestamp when present.
    /// </summary>
    public long? Timestamp => _timestamp;

    /// <summary>
    /// Gets the sender when present.
    /// </summary>
    public string? Sender => _sender;

    /// <summary>
    /// Gets the addressee when present.
    /// </summary>
    public string? Addressee => _addressee;

    /// <summary>
    /// Gets the file size when present.
    /// </summary>
    public long? FileSize => _fileSize;

    /// <summary>
    /// Gets the checksum when present.
    /// </summary>
    public int? Checksum => _checksum;

    /// <summary>
    /// Gets whether all expected segments have been collected.
    /// </summary>
    public bool IsComplete {
        get {
            if (_segments.Count == 0) return false;
            if (!TryGetExpectedCount(out var expected)) return false;
            if (_segments.Count != expected) return false;
            for (var i = 0; i < expected; i++) {
                if (!_segments.ContainsKey(i)) return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Attempts to add a decoded Macro PDF417 segment.
    /// </summary>
    public bool TryAdd(Pdf417Decoded decoded) {
        if (decoded is null) throw new ArgumentNullException(nameof(decoded));
        if (decoded.Macro is null) return false;
        return TryAdd(decoded.Text, decoded.Macro);
    }

    /// <summary>
    /// Attempts to add a decoded Macro PDF417 segment from a <see cref="CodeGlyphDecoded"/> wrapper.
    /// </summary>
    public bool TryAdd(CodeGlyphDecoded decoded) {
        if (decoded is null) throw new ArgumentNullException(nameof(decoded));
        if (decoded.Kind != CodeGlyphKind.Pdf417) return false;
        if (decoded.Pdf417 is null) return false;
        return TryAdd(decoded.Pdf417);
    }

    /// <summary>
    /// Attempts to add a Macro PDF417 segment with explicit metadata.
    /// </summary>
    public bool TryAdd(string text, Pdf417MacroMetadata macro) {
        if (macro is null) throw new ArgumentNullException(nameof(macro));
        if (!TryAcceptFileId(macro.FileId)) return false;
        if (_segments.ContainsKey(macro.SegmentIndex)) return false;

        _segments[macro.SegmentIndex] = text ?? string.Empty;
        MergeOptionalFields(macro);
        return true;
    }

    /// <summary>
    /// Adds a decoded Macro PDF417 segment.
    /// </summary>
    public void Add(Pdf417Decoded decoded) {
        if (!TryAdd(decoded)) {
            throw new InvalidOperationException("Segment does not contain Macro PDF417 metadata or does not match the current file.");
        }
    }

    /// <summary>
    /// Adds a decoded Macro PDF417 segment from a <see cref="CodeGlyphDecoded"/> wrapper.
    /// </summary>
    public void Add(CodeGlyphDecoded decoded) {
        if (!TryAdd(decoded)) {
            throw new InvalidOperationException("Segment does not contain Macro PDF417 metadata or does not match the current file.");
        }
    }

    /// <summary>
    /// Attempts to assemble the full payload when all segments are present.
    /// </summary>
    public bool TryAssemble(out string text) {
        text = string.Empty;
        if (!IsComplete) return false;

        TryGetExpectedCount(out var expected);
        var sb = new StringBuilder();
        for (var i = 0; i < expected; i++) {
            sb.Append(_segments[i]);
        }
        text = sb.ToString();
        return true;
    }

    /// <summary>
    /// Assembles the full payload when all segments are present.
    /// </summary>
    public string Assemble() {
        if (!TryAssemble(out var text)) {
            throw new InvalidOperationException("Macro PDF417 assembly is incomplete.");
        }
        return text;
    }

    private bool TryAcceptFileId(string fileId) {
        if (_fileId is null) {
            _fileId = fileId ?? string.Empty;
            return true;
        }
        return string.Equals(_fileId, fileId, StringComparison.Ordinal);
    }

    private void MergeOptionalFields(Pdf417MacroMetadata macro) {
        if (macro.SegmentCount.HasValue && !_segmentCount.HasValue) _segmentCount = macro.SegmentCount;
        if (macro.IsLastSegment) {
            _hasLastSegment = true;
            _lastSegmentIndex = macro.SegmentIndex;
        }
        if (_fileName is null && macro.FileName is not null) _fileName = macro.FileName;
        if (!_timestamp.HasValue && macro.Timestamp.HasValue) _timestamp = macro.Timestamp;
        if (_sender is null && macro.Sender is not null) _sender = macro.Sender;
        if (_addressee is null && macro.Addressee is not null) _addressee = macro.Addressee;
        if (!_fileSize.HasValue && macro.FileSize.HasValue) _fileSize = macro.FileSize;
        if (!_checksum.HasValue && macro.Checksum.HasValue) _checksum = macro.Checksum;
    }

    private bool TryGetExpectedCount(out int expected) {
        if (_segmentCount.HasValue) {
            expected = _segmentCount.Value;
            return true;
        }
        if (_hasLastSegment && _lastSegmentIndex.HasValue) {
            expected = _lastSegmentIndex.Value + 1;
            return true;
        }
        expected = 0;
        return false;
    }
}
