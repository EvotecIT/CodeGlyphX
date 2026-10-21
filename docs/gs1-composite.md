# GS1 Composite

A Composite symbol links a retail or GS1 linear message to a two-dimensional GS1 message.
Both messages remain available in the decoded result.

```csharp
var symbol = Gs1CompositeEncoder.Encode(
    "9520123456788", "(11)990102",
    new Gs1CompositeEncodingOptions { Carrier = Gs1CompositeCarrier.Ean13 });

if (Gs1CompositeDecoder.TryDecode(symbol.Modules, Gs1CompositeCarrier.Ean13, out var decoded))
{
    Console.WriteLine(decoded.LinearText);    // 9520123456788
    Console.WriteLine(decoded.CompositeText); // 11990102
}
```

`Carrier` defaults to GS1-128. It accepts a GS1 element string such as `(01)09521234543213`.
EAN and UPC carriers accept their ordinary retail digits and optional `+12` or `+12345` add-ons.
UPC-E accepts six compressed digits, seven digits including number system 0 or 1, or eight digits
including the check. Supplied checks are validated. Composite retail add-ons have a nine-module gap;
the existing ordinary pixel-rendering defaults remain available through their original APIs.

DataBar Omnidirectional, Truncated, Stacked, Stacked Omnidirectional and Limited accept a GTIN-14,
an `(01)` element, or up to thirteen data digits without the check. Expanded and Expanded Stacked
accept a GS1 element string. `DataBarExpandedColumns` selects two through ten finder/data blocks
per Expanded Stacked row.

`Component` selects CC-A or CC-B, or CC-C with GS1-128. `Auto` chooses the smallest fitting legal
component. It cannot escalate a retail or DataBar carrier to CC-C. The carrier determines the
component column count, alignment, linkage and separators.

```csharp
var scan = SymbolScanner.Scan(imageBytes, new ScanOptions
{
    Formats = new[] { SymbolFormat.Gs1Composite },
    CompositeCarrier = Gs1CompositeCarrier.UpcA
});

foreach (var found in scan.Symbols)
{
    var messages = (Gs1CompositeSymbolMetadata)found.Metadata!;
    Console.WriteLine(messages.LinearText);
    Console.WriteLine(messages.CompositeText);
}
```

The optional carrier hint resolves identical EAN-13/UPC-A module sequences and the identical
DataBar Omnidirectional/Truncated data patterns. A sampled module matrix does not establish its
intended physical height profile. Without a hint, decoding uses a canonical interpretation.

`Gs1CompositeComponents` handles standalone CC-A, CC-B and CC-C. A standalone result has no carrier
and sets `IsStandalone`; it must not be treated as a completely read linked label. A complete decode
validates the carrier linkage and separator instead of joining any nearby pair of barcodes.

Image results expose `ComponentGeometry` and `LinearGeometry` in source-image pixel coordinates.
They follow the printed row heights, reading order, rotation and source region, including image
downscaling. Sampled-module results leave these image geometries null. If a carrier is damaged or
cropped beyond recovery, a complete checked upper component can still be returned with
`IsStandalone`, an empty linear message and no linear geometry. The scanner searches for a complete
linked label before returning that component as its fallback.

Image sampling tolerates limited damage to known CC-A/B row-address marks while retaining
data correction, GS1 parsing, separator and linkage checks. Direct module decoding retains exact
row-address validation. Heavily damaged framing is rejected. Perspective recovery is bounded by
the scan deadline and a 16-million-pixel projection workspace; a skipped workspace is reported as
a resource limit.

Retail linkage is established by the printed Composite separator and component arrangement.
It cannot prove that two otherwise valid messages belong to the same business transaction.

The symbol's matrix contains compact logical rows. Their physical heights differ between the
two-dimensional rows, separators and linear bars; a uniform square-cell rendering is not a
standards-compliant retail or industrial print layout.
