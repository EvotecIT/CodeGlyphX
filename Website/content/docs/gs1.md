---
title: GS1 data and Digital Link - CodeGlyphX
description: Parse and validate GS1 element strings and GS1 Digital Link URIs.
slug: gs1
collection: docs
layout: docs
---

{{< edit-link >}}

# GS1 data and Digital Link

CodeGlyphX includes a generated catalog of all Application Identifiers in the bundled GS1 Barcode Syntax Dictionary release. The validator uses that catalog for field lengths, character sets, check digits, dates, code lists, required associations, exclusions, and the semantic rules attached to each AI.

## Validate an element string

```csharp
using CodeGlyphX;
using CodeGlyphX.Gs1Data;

Gs1ValidationResult result = Gs1.Validate(
    "(01)09520123456788(10)ABC1(21)12345(17)180426");

if (!result.IsValid) {
    foreach (Gs1ValidationIssue issue in result.Issues) {
        Console.WriteLine(issue);
    }
}
```

`Gs1.Validate` accepts bracketed AI syntax and raw element strings. `Gs1Validator.ToElementString` inserts FNC1 separators from the catalog when you need the payload for GS1-128, GS1 DataMatrix, QR, or another GS1 carrier.

## Parse a Digital Link URI

The Digital Link engine implements the uncompressed [GS1 Digital Link URI Syntax 1.6.0](https://ref.gs1.org/standards/digital-link/uri-syntax/). It distinguishes the primary key, ordered key qualifiers, GS1 query attributes, extension parameters, and fragment data.

```csharp
Gs1DigitalLinkUri link = Gs1DigitalLink.Parse(
    "https://brand.example/products/01/09520123456788/10/ABC1/21/12345?17=180426");

Console.WriteLine(link.PrimaryIdentifier); // (01)09520123456788
Console.WriteLine(link.KeyQualifiers.Count); // 2
Console.WriteLine(link.CanonicalUri);
Console.WriteLine(link.ToElementString());
```

Use `Gs1DigitalLink.Validate` when you want all detectable URI and GS1 data issues instead of an exception. `Gs1.ValidateDigitalLink` and `Gs1.ParseDigitalLink` are thin aliases on the main GS1 facade.

## Build a canonical URI

```csharp
Gs1DigitalLinkUri link = Gs1DigitalLink.BuildCanonical(new[] {
    Gs1Element.Create("01", "09520123456788"),
    Gs1Element.Create("10", "ABC1"),
    Gs1Element.Create("21", "12345"),
    Gs1Element.Create("17", "180426")
});

Console.WriteLine(link.Uri);
// https://id.gs1.org/01/09520123456788/10/ABC1/21/12345?17=180426
```

Canonical output uses HTTPS and `id.gs1.org`, keeps qualifiers in the required path order, removes non-GS1 query controls and fragments, and sorts GS1 query attributes lexically by AI. Use `Gs1DigitalLink.Build` with your own HTTP or HTTPS stem when the brand or resolver controls the domain.

URI compression and online resolver behavior are separate GS1 standards. CodeGlyphX does not perform network resolution, and this API does not claim support for compressed Digital Link payloads.
