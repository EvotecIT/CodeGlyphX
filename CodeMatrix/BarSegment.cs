using System;

namespace CodeMatrix;

public readonly struct BarSegment {
    public bool IsBar { get; }
    public int Modules { get; }

    public BarSegment(bool isBar, int modules) {
        if (modules <= 0) throw new ArgumentOutOfRangeException(nameof(modules));
        IsBar = isBar;
        Modules = modules;
    }
}

