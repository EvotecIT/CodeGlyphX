namespace CodeMatrix.Rendering.Png;

public readonly struct Rgba32 {
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }
    public byte A { get; }

    public Rgba32(byte r, byte g, byte b, byte a = 255) {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public static Rgba32 Black => new(0, 0, 0, 255);
    public static Rgba32 White => new(255, 255, 255, 255);
}

