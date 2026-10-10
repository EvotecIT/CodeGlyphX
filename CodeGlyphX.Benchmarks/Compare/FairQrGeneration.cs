using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Benchmarks;

/// <summary>Comparison-only adapters with fixed payload, M error correction, version, scale and quiet zone.</summary>
public sealed class FairQrGeneration {
    public const int BatchSize = 100;
    public string Payload { get; }
    public int Version { get; }
    private const int Scale = 8;
    private const int QuietZone = 4;
    public FairQrGeneration(string payload, int version) { Payload = payload; Version = version; }

    public GenerationBatch Generate(string engine, string operation) {
        var allocated = GC.GetTotalAllocatedBytes(precise: true);
        var outputs = new byte[BatchSize][];
        for (var i = 0; i < outputs.Length; i++) {
            outputs[i] = operation == "Png" ? GeneratePng(engine) : SerializeModules(EncodeModules(engine));
        }
        return new GenerationBatch(outputs, GC.GetTotalAllocatedBytes(precise: true) - allocated);
    }

    private BitMatrix EncodeModules(string engine) {
        if (engine == "CodeGlyphX") return QR.Encode(Payload, EncodingOptions()).Modules;
#if COMPARE_ZXING
        if (engine == "ZXing.Net") {
            var hints = new Dictionary<ZXing.EncodeHintType, object> {
                [ZXing.EncodeHintType.ERROR_CORRECTION] = ZXing.QrCode.Internal.ErrorCorrectionLevel.M,
                [ZXing.EncodeHintType.CHARACTER_SET] = "UTF-8", [ZXing.EncodeHintType.QR_VERSION] = Version,
                [ZXing.EncodeHintType.MARGIN] = 0
            };
            var source = new ZXing.QrCode.QRCodeWriter().encode(Payload, ZXing.BarcodeFormat.QR_CODE, 0, 0, hints);
            var modules = new BitMatrix(source.Width, source.Height);
            for (var y = 0; y < modules.Height; y++) for (var x = 0; x < modules.Width; x++) modules[x, y] = source[x, y];
            return modules;
        }
#endif
#if COMPARE_QRCODER
        if (engine == "QRCoder") {
            using var generator = new QRCoder.QRCodeGenerator();
            using var data = generator.CreateQrCode(Payload, QRCoder.QRCodeGenerator.ECCLevel.M,
                forceUtf8: Payload.Any(c => c > 127), utf8BOM: false, requestedVersion: Version);
            var modules = new BitMatrix(data.ModuleMatrix.Count - 8, data.ModuleMatrix.Count - 8);
            for (var y = 0; y < modules.Height; y++) for (var x = 0; x < modules.Width; x++) modules[x, y] = data.ModuleMatrix[y + 4][x + 4];
            return modules;
        }
#endif
#if COMPARE_ZXINGCPP
        if (engine == "ZXing-C++") {
            using var options = new ZXingCpp.CreatorOptions(ZXingCpp.BarcodeFormat.QRCode, $"ecLevel=M,version={Version},eci=26");
            // The binding's string overload passes UTF-16 Length to a UTF-8 native API. Use explicit UTF-8 bytes.
            using var barcode = new ZXingCpp.Barcode(System.Text.Encoding.UTF8.GetBytes(Payload), options);
            using var writer = new ZXingCpp.WriterOptions { Scale = 1, AddQuietZones = false };
            using var image = barcode.ToImage(writer);
            var source = image.ToArray();
            var modules = new BitMatrix(image.Width, image.Height);
            for (var y = 0; y < modules.Height; y++) for (var x = 0; x < modules.Width; x++) modules[x, y] = source[y * modules.Width + x] == 0;
            return modules;
        }
#endif
        throw new NotSupportedException($"Generation engine '{engine}' is not compiled in.");
    }

    /// <summary>Materializes one PNG using the fixed comparison contract.</summary>
    public byte[] GeneratePng(string engine) {
        if (engine == "CodeGlyphX") return QR.Render(Payload, OutputFormat.Png,
            new QrRenderOptions { ModuleSize = Scale, QuietZone = QuietZone }, EncodingOptions()).ToArray();
#if COMPARE_QRCODER
        if (engine == "QRCoder") {
            using var generator = new QRCoder.QRCodeGenerator();
            using var data = generator.CreateQrCode(Payload, QRCoder.QRCodeGenerator.ECCLevel.M,
                forceUtf8: Payload.Any(c => c > 127), utf8BOM: false, requestedVersion: Version);
            using var png = new QRCoder.PngByteQRCode(data);
            return png.GetGraphic(Scale, drawQuietZones: true);
        }
#endif
        // Neither ZXing binding provides a PNG codec. Both use this same managed RGBA PNG adapter.
        var modules = EncodeModules(engine);
        var size = (modules.Width + QuietZone * 2) * Scale;
        var pixels = new byte[size * size * 4];
        for (var y = 0; y < size; y++) for (var x = 0; x < size; x++) {
            var mx = x / Scale - QuietZone; var my = y / Scale - QuietZone;
            var value = mx >= 0 && my >= 0 && mx < modules.Width && my < modules.Height && modules[mx, my] ? (byte)0 : (byte)255;
            var offset = (y * size + x) * 4;
            pixels[offset] = pixels[offset + 1] = pixels[offset + 2] = value; pixels[offset + 3] = 255;
        }
        return PngImageEncoder.EncodeRgba32(pixels, size, size);
    }

    private QrEncodingOptions EncodingOptions() => new() { MinVersion = Version, MaxVersion = Version, ErrorCorrectionLevel = QrErrorCorrectionLevel.M };
    private static byte[] SerializeModules(BitMatrix modules) {
        var bytes = new byte[modules.Width * modules.Height];
        for (var y = 0; y < modules.Height; y++) for (var x = 0; x < modules.Width; x++) bytes[y * modules.Width + x] = modules[x, y] ? (byte)0 : (byte)255;
        return bytes;
    }

    /// <summary>Validates every output and two independent readers before accepting a measured generation lane.</summary>
    public void Validate(GenerationBatch batch, string operation) {
        if (batch.Outputs.Length != BatchSize) throw new InvalidDataException("Incomplete generation batch.");
        var moduleCount = Version * 4 + 17;
        if (batch.Outputs.Length == 0) throw new InvalidDataException("Empty generation batch.");
        var reference = batch.Outputs[0];
        foreach (var candidate in batch.Outputs) {
            if (!candidate.AsSpan().SequenceEqual(reference)) throw new InvalidDataException("Generation batch contains unequal artifacts.");
        }
        // All 100 deterministic artifacts are byte-qualified above. Decode the representative
        // once per independent reader, outside timing, instead of repeating identical work.
        foreach (var output in batch.Outputs.Take(1)) {
            byte[] rgba; int width, height;
            if (operation == "Png") {
                if (!ImageReader.TryDecodeRgba32(output, out rgba, out width, out height)) throw new InvalidDataException("Invalid generated PNG.");
                if (width != (moduleCount + 8) * Scale || height != width) throw new InvalidDataException("PNG size differs from the fixed QR version/scale/quiet zone.");
            } else {
                if (output.Length != moduleCount * moduleCount) throw new InvalidDataException("Matrix size differs from the fixed QR version.");
                width = height = (moduleCount + 8) * Scale; rgba = new byte[width * height * 4];
                for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) {
                    var mx = x / Scale - 4; var my = y / Scale - 4;
                    var value = mx >= 0 && my >= 0 && mx < moduleCount && my < moduleCount ? output[my * moduleCount + mx] : (byte)255;
                    var offset = (y * width + x) * 4; rgba[offset] = rgba[offset + 1] = rgba[offset + 2] = value; rgba[offset + 3] = 255;
                }
            }
            var matrix = new BitMatrix(moduleCount, moduleCount);
            for (var y = 0; y < moduleCount; y++) for (var x = 0; x < moduleCount; x++)
                matrix[x, y] = rgba[((y + 4) * Scale + Scale / 2) * width * 4 + ((x + 4) * Scale + Scale / 2) * 4] < 128;
            if (!QrDecoder.TryDecode(matrix, out var metadata) || metadata.Text != Payload || metadata.Version != Version ||
                metadata.ErrorCorrectionLevel != QrErrorCorrectionLevel.M)
                throw new InvalidDataException("Generated QR version or error correction differs from the comparison contract.");
            foreach (var engine in ValidationReaders()) {
                var actual = CorpusReaders.Decode(rgba, width, height, "QrCode", engine, false, false);
                if (actual.Length != 1 || actual[0] != Payload) throw new InvalidDataException($"{engine} rejected generated payload.");
            }
        }
    }

    private static IEnumerable<string> ValidationReaders() {
#if COMPARE_ZXING
        yield return "ZXing.Net";
#endif
#if COMPARE_ZXINGCPP
        yield return "ZXing-C++";
#endif
        yield break;
    }
}

public sealed record GenerationBatch(byte[][] Outputs, long AllocatedBytes);
