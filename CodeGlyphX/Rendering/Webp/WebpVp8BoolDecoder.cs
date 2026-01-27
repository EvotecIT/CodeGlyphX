using System;

namespace CodeGlyphX.Rendering.Webp;

/// <summary>
/// Minimal VP8 boolean decoder scaffold for parsing the control header.
/// This is intentionally small and focused; it will be extended as VP8 decoding grows.
/// </summary>
internal sealed class WebpVp8BoolDecoder
{
    private readonly byte[] _data;
    private int _offset;
    private int _range;
    private int _value;
    private int _count;
    private readonly bool _valid;

    public WebpVp8BoolDecoder(ReadOnlySpan<byte> data)
    {
        _data = data.ToArray();
        _valid = _data.Length >= 2;

        if (!_valid)
        {
            return;
        }

        _range = 255;
        _value = (_data[0] << 8) | _data[1];
        _offset = 2;
        _count = 0;
    }

    public int BytesConsumed => _offset;

    public bool TryReadBool(int probability, out bool bit)
    {
        bit = false;

        if (!_valid)
        {
            return false;
        }

        if ((uint)probability > 255)
        {
            return false;
        }

        // VP8 boolean arithmetic decoding step.
        var split = 1 + (((_range - 1) * probability) >> 8);
        var bigSplit = split << 8;

        if (_value >= bigSplit)
        {
            bit = true;
            _range -= split;
            _value -= bigSplit;
        }
        else
        {
            _range = split;
        }

        // Renormalize.
        while (_range < 128)
        {
            _range <<= 1;
            _value <<= 1;
            _count++;

            if (_count == 8)
            {
                _count = 0;
                _value |= ReadByte();
            }
        }

        return true;
    }

    public bool TryReadLiteral(int bits, out int value)
    {
        value = 0;
        if (bits <= 0 || bits > 31)
        {
            return false;
        }

        var result = 0;
        for (var i = bits - 1; i >= 0; i--)
        {
            if (!TryReadBool(probability: 128, out var bit))
            {
                return false;
            }

            if (bit)
            {
                result |= 1 << i;
            }
        }

        value = result;
        return true;
    }

    public bool TryReadSignedLiteral(int bits, out int value)
    {
        value = 0;
        if (!TryReadLiteral(bits, out var magnitude))
        {
            return false;
        }

        if (!TryReadBool(probability: 128, out var signBit))
        {
            return false;
        }

        value = signBit ? -magnitude : magnitude;
        return true;
    }

    private int ReadByte()
    {
        if (_offset >= _data.Length)
        {
            return 0;
        }

        return _data[_offset++];
    }
}
