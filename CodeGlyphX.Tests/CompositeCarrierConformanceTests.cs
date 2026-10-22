using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CodeGlyphX.DataBar;
using Xunit;

namespace CodeGlyphX.Tests;

public sealed class CompositeCarrierConformanceTests {
    [Fact]
    public void DecodesCompleteWideExpandedSeparatorsFromZintWithoutAcceptingArbitraryDamage() {
        using var stream = typeof(CompositeCarrierConformanceTests).Assembly.GetManifestResourceStream(
            "CodeGlyphX.Tests.Fixtures.Conformance.zint-wide-composite.txt");
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream!);
        var errors = new List<string>();
        var count = 0;
        while (reader.ReadLine() is { } line) {
            if (line.Length == 0 || line[0] == '#') continue;
            var parts = line.Split('|');
            var carrier = (Gs1CompositeCarrier)Enum.Parse(typeof(Gs1CompositeCarrier), parts[0]);
            var grid = new BitMatrix(int.Parse(parts[4], CultureInfo.InvariantCulture), int.Parse(parts[5], CultureInfo.InvariantCulture));
            for (var y = 0; y < grid.Height; y++) for (var x = 0; x < grid.Width; x++) grid[x, y] = parts[6][y * grid.Width + x] == '1';
            if (!Gs1CompositeDecoder.TryDecode(grid, carrier, out var decoded)) errors.Add($"{carrier}/{parts[1]}/{grid.Width}");
            else {
                Assert.Equal(Gs1.ElementString(parts[2]), decoded.LinearText);
                Assert.Equal(Gs1.ElementString(parts[3]), decoded.CompositeText);
                var damaged = grid.Clone();
                var separator = parts[1] == "a" ? 3 : 4;
                damaged[grid.Width - 5, separator] = !damaged[grid.Width - 5, separator];
                Assert.False(Gs1CompositeDecoder.TryDecode(damaged, carrier, out _));
            }
            count++;
        }
        Assert.Equal(6, count);
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
    }

    [Fact]
    public void CapacitySelectionAndCarrierRestrictionsAreAppliedBeforeAssembly() {
        var options = new Gs1CompositeEncodingOptions { Carrier = Gs1CompositeCarrier.UpcE };
        var small = Gs1CompositeEncoder.Encode("01234558", "(11)990102", options);
        Assert.Equal(Gs1CompositeComponent.CcA, small.Component);
        var larger = Gs1CompositeEncoder.Encode("01234558", "(91)" + new string('A', 50), options);
        Assert.Equal(Gs1CompositeComponent.CcB, larger.Component);
        var tooLarge = "(91)" + new string('A', 90) + "(92)" + new string('A', 90) + "(93)" + new string('A', 90);
        Assert.Throws<ArgumentException>(() => Gs1CompositeEncoder.Encode("01234558", tooLarge, options));
        options.Component = Gs1CompositeComponent.CcC;
        Assert.Throws<ArgumentException>(() => Gs1CompositeEncoder.Encode("01234558", "(11)990102", options));
        options.Carrier = Gs1CompositeCarrier.Ean13;
        options.Component = Gs1CompositeComponent.CcA;
        Assert.Throws<ArgumentException>(() => Gs1CompositeEncoder.Encode("95200002", "(11)990102", options));
        Assert.Throws<ArgumentException>(() => Gs1CompositeEncoder.Encode("01234559", "(11)990102",
            new Gs1CompositeEncodingOptions { Carrier = Gs1CompositeCarrier.UpcE }));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.DataBarExpandedColumns = 1);
    }

    [Fact]
    public void AnUnlinkedDataBarAndDamagedRetailSeparatorDoNotBecomeCompleteComposites() {
        const string primary = "(01)09521234543213";
        var linked = Gs1CompositeEncoder.Encode(primary, "(11)990102",
            new Gs1CompositeEncodingOptions { Carrier = Gs1CompositeCarrier.DataBarOmnidirectional });
        var unlinked = linked.Modules.Clone();
        var barcode = DataBar14Encoder.EncodeOmnidirectional("0952123454321");
        var column = 4;
        foreach (var segment in barcode.Segments)
            for (var module = 0; module < segment.Modules; module++) unlinked[column++, unlinked.Height - 1] = segment.IsBar;
        Assert.False(Gs1CompositeDecoder.TryDecode(unlinked, Gs1CompositeCarrier.DataBarOmnidirectional, out _));
        var retail = Gs1CompositeEncoder.Encode("9520123456788", "(11)990102",
            new Gs1CompositeEncodingOptions { Carrier = Gs1CompositeCarrier.Ean13 });
        var damaged = retail.Modules.Clone();
        damaged[3, retail.ComponentRows] = false;
        Assert.False(Gs1CompositeDecoder.TryDecode(damaged, Gs1CompositeCarrier.Ean13, out _));
        Assert.False(Gs1CompositeDecoder.TryDecode(retail.Modules, Gs1CompositeCarrier.Ean8, out _));
    }

    [Fact]
    public void DecodesIndependentLinkedCarriersAndRetainsBothMessages() {
        var errors = new List<string>();
        foreach (var parts in Vectors()) {
            var carrier = (Gs1CompositeCarrier)Enum.Parse(typeof(Gs1CompositeCarrier), parts[0]);
            var expectedComponent = parts[1] == "a" ? Gs1CompositeComponent.CcA : Gs1CompositeComponent.CcB;
            var matrix = new BitMatrix(int.Parse(parts[4], CultureInfo.InvariantCulture), int.Parse(parts[5], CultureInfo.InvariantCulture));
            for (var y = 0; y < matrix.Height; y++) for (var x = 0; x < matrix.Width; x++) matrix[x, y] = parts[6][y * matrix.Width + x] == '1';
            if (!Gs1CompositeDecoder.TryDecode(matrix, carrier, out var decoded)) {
                errors.Add($"Decode {carrier}/{expectedComponent}");
                continue;
            }
            Assert.Equal(carrier, decoded.Carrier);
            Assert.Equal(expectedComponent, decoded.Component);
            Assert.Equal(Gs1.ElementString(parts[2]), decoded.LinearText);
            Assert.Equal(Gs1.ElementString(parts[3]), decoded.CompositeText);
        }
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
    }

    internal static IEnumerable<string[]> Vectors() {
        foreach (var source in new[] { "bwipp", "zint" }) {
            using var stream = typeof(CompositeCarrierConformanceTests).Assembly.GetManifestResourceStream(
                "CodeGlyphX.Tests.Fixtures.Conformance." + source + "-composite-carriers.txt");
            Assert.NotNull(stream);
            using var reader = new StreamReader(stream!);
            while (reader.ReadLine() is { } line) {
                if (line.Length == 0 || line[0] == '#') continue;
                var parts = line.Split('|');
                Array.Resize(ref parts, 8);
                parts[7] = source;
                yield return parts;
            }
        }
    }

    [Fact]
    public void LinkedRetailAndDataBarCarriersMatchIndependentModules() {
        var count = 0;
        var errors = new List<string>();
        foreach (var parts in Vectors()) {
            var carrier = (Gs1CompositeCarrier)Enum.Parse(typeof(Gs1CompositeCarrier), parts[0]);
            var component = parts[1] == "a" ? Gs1CompositeComponent.CcA : Gs1CompositeComponent.CcB;
            var encoded = Gs1CompositeEncoder.Encode(parts[2], parts[3], new Gs1CompositeEncodingOptions {
                Carrier = carrier, Component = component
            });
            Assert.Equal(carrier, encoded.Carrier);
            Assert.Equal(component, encoded.Component);
            var matrix = encoded.Modules;
            if (matrix.Width != int.Parse(parts[4], CultureInfo.InvariantCulture)
                || matrix.Height != int.Parse(parts[5], CultureInfo.InvariantCulture)) {
                errors.Add($"Dimensions {carrier}/{component}: {matrix.Width}x{matrix.Height}, independent {parts[4]}x{parts[5]}");
                count++;
                continue;
            }
            var differences = 0;
            var firstDifference = string.Empty;
            var rowDifferences = new List<string>();
            for (var y = 0; y < matrix.Height; y++) {
                var rowCount = 0;
                for (var x = 0; x < matrix.Width; x++) {
                    // The published engines differ in this single Limited boundary module.
                    // Encoding follows Zint; both complete independent layouts must decode.
                    if (parts[7] == "bwipp" && carrier == Gs1CompositeCarrier.DataBarLimited
                        && y == matrix.Height - 2 && x == (component == Gs1CompositeComponent.CcA ? 3 : 12)) {
                        Assert.False(matrix[x, y]);
                        continue;
                    }
                    if (matrix[x, y] != (parts[6][y * matrix.Width + x] == '1')) {
                        if (differences++ == 0) firstDifference = $"{x},{y}";
                        rowCount++;
                    }
                }
                if (rowCount != 0) rowDifferences.Add($"row {y}:{rowCount}");
            }
            if (differences != 0) errors.Add($"Modules {carrier}/{component}: {differences} differences, first at {firstDifference}, {string.Join(", ", rowDifferences)}");
            count++;
        }
        Assert.Equal(47, count);
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
    }
}
