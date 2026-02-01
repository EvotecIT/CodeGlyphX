# Fuzzing

This repo includes a small fuzzing harness for image decoders: `CodeGlyphX.Fuzz`.

## Build

```
dotnet build CodeGlyphX.Fuzz/CodeGlyphX.Fuzz.csproj
```

## Run on a single input

```
dotnet run --project CodeGlyphX.Fuzz -- path/to/input.bin
```

## Run via stdin

```
cat path/to/input.bin | dotnet run --project CodeGlyphX.Fuzz
```

## Notes

- The harness swallows expected `FormatException`/`ArgumentException` inputs and allows unexpected exceptions to crash the process.
- Use your preferred fuzzer (AFL++, libFuzzer via a .NET bridge, or your CI fuzzing pipeline) to drive the harness.
