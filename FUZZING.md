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

## Environment variables

- `CODEGLYPHX_FUZZ_TIMEOUT_MS`: Per-call timeout (ms). Default: `2000`. Set to `0` to disable.
- `CODEGLYPHX_FUZZ_MAX_MB`: Soft memory limit (MB) checked after each decode. Default: `256`. Set to `0` to disable.
- `CODEGLYPHX_FUZZ_LOG`: Set to `1` to log expected exceptions.

Example:

```
CODEGLYPHX_FUZZ_TIMEOUT_MS=1500 CODEGLYPHX_FUZZ_MAX_MB=256 dotnet run --project CodeGlyphX.Fuzz -- path/to/input.bin
```

## Integration examples

- Drive the harness with your fuzzer of choice (AFL++, libFuzzer via a .NET bridge, SharpFuzz, or CI corpus replays).
- Simple corpus sweep:

```
find corpus -type f -print0 | xargs -0 -n1 dotnet run --project CodeGlyphX.Fuzz --
```

## Notes

- The harness swallows expected `FormatException`/`ArgumentException` inputs and allows unexpected exceptions to crash the process.
- Decode calls use `ImageDecodeOptions.UltraSafe()` and apply `CODEGLYPHX_FUZZ_TIMEOUT_MS` as a `MaxMilliseconds` budget.
- Use your preferred fuzzer (AFL++, libFuzzer via a .NET bridge, or your CI fuzzing pipeline) to drive the harness.
