---
title: Barcode profiles and specialist formats
description: Checked application identifiers, postal and payment helpers, and specialist barcode controls.
---

# Barcode profiles and specialist formats

Use an application helper when the label needs more than a physical barcode alphabet. These APIs prepare check characters, validate decoded values and keep application fields separate from the carrier. They reuse the same managed encoders and renderers as the rest of CodeGlyphX.

## Checked identifiers

`BarcodeProfiles` supports GTIN-14/EAN-14, SSCC-18, ISBN, ISMN, ISSN, PZN-7/PZN-8, Deutsche Post Identcode/Leitcode and Marks & Spencer identifiers. It accepts spaces and hyphens, calculates omitted checks where the identifier allows that, and rejects incorrect supplied checks. ISBN-10 and legacy ISMN are converted to their thirteen-digit forms.

```csharp
using CodeGlyphX;
using CodeGlyphX.Rendering;

string isbn = BarcodeProfiles.Prepare(LinearBarcodeProfile.Isbn, "978-1-56581-231-4");
var book = BarcodeProfiles.Encode(LinearBarcodeProfile.Isbn, isbn);
book.Save("book.svg");

var captioned = BarcodeProfiles.Render(LinearBarcodeProfile.Isbn, isbn, OutputFormat.Png);
OutputWriter.Write("book-with-text.png", captioned);
```

Decode with `BarcodeProfiles.TryDecode(profile, modules, out identifier)` when the application knows the expected profile, or call `TryParse` on an already decoded carrier value. An EAN or Code 39 symbol alone cannot uniquely identify the business application. ISSN uses the default `00` issue variant. The M&S carrier is EAN-8 with an initial zero; its caption shows the seven-digit identifier between M and S. These renderers produce symbols and captions, without a complete retail label template.

## HIBC

`Hibc.Prepare` adds the leading plus and Mod-43 check. `TryParse` validates them and returns the unframed data. Supported carriers are Code 39, Code 128, QR, Data Matrix, PDF417, MicroPDF417 and Codablock F. Explicit Data Matrix options also select rectangular or DMRE models.

```csharp
var linear = Hibc.EncodeLinear(SymbolFormat.Code39, "A999BJC5D6E71");
linear.Save("hibc.png", new BarcodeOptions { LabelText = Hibc.Prepare("A999BJC5D6E71") });

var matrix = Hibc.EncodeMatrix(SymbolFormat.MicroPdf417, "A999BJC5D6E71");
matrix.Save("hibc-micro.pdf", new MatrixOptions { LabelText = Hibc.Prepare("A999BJC5D6E71") });

bool valid = Hibc.TryDecodeMatrix(SymbolFormat.MicroPdf417, matrix.Modules, out string data);
```

The helper validates HIBC transport framing. Assignment of labeler identifiers and complete UDI field rules remain the application's responsibility. Literal HIBC Code 39 uses its plus character directly; `Hibc.TryDecodeLinear` selects that interpretation rather than Full ASCII shift pairs.

## Postal and coupon helpers

| Helper | Carrier and behavior |
| --- | --- |
| `IntelligentMailPackage` | GS1-128 with checked USPS PIC, optional ZIP routing and typed commercial, online, retail and legacy structures. `Render` prints the grouped PIC without the routing ZIP. |
| `NorthAmericanCoupon` | GS1 AI 8110 validation and DataBar Expanded Stacked encoding. `Prepare` builds mandatory fields and validates optional groups. `Render` adds the company-prefix/offer caption. |
| `Mailmark2D` | Mailmark types 7, 9 and 29 with positionally checked destination/return postcodes and the required basic-C40 header. The detailed result retains the header encodation evidence. |
| `MailmarkFourState` | Mailmark C/L fields and checked four-state bar grids. |

```csharp
var package = IntelligentMailPackage.Render("9101123456789000000013", OutputFormat.Svg, routingZip: "12345");
OutputWriter.Write("package.svg", package);

var coupon = NorthAmericanCoupon.Render("(8110)012345612345611110123", OutputFormat.Png);
OutputWriter.Write("coupon.png", coupon);
```

`TryDecode` and `TryParse` return application fields alongside the carrier payload. Structure and checksums do not establish postal identifier ownership, service eligibility, manifests or acceptance of a printed mailpiece. Four-state modules are logical bar states; qualify their final physical dimensions separately.

## Swiss QR payments

`SwissQrCode` uses `SwissQrCodePayload` for SPC version 0200, UTF-8, CHF/EUR, Swiss/Liechtenstein IBAN checks and QRR, SCOR or NON reference rules. The encoder selects the smallest fitting byte-mode QR with error correction M and renders the centered Swiss cross.

```csharp
using CodeGlyphX.Payloads;

var payment = new SwissQrCodePayload(
    new SwissQrCodePayload.Iban("CH9300762011623852957", SwissQrIbanType.Iban),
    SwissQrCurrency.CHF,
    SwissQrCodePayload.Contact.CreateStructured("Max Muster & Söhne", "Musterstrasse", "123", "8000", "Seldwyla", "CH"),
    new SwissQrCodePayload.Reference(SwissQrReferenceType.SCOR, "RF18539007547034"),
    amount: 50);

var swiss = SwissQrCode.Encode(payment);
swiss.Save("payment.svg");
bool valid = SwissQrCode.TryDecode(swiss.Modules, out var parsedPayment);
```

Current structured addresses are the default, and contact countries must be assigned ISO 3166-1 two-letter codes. Legacy combined addresses require explicit opt-in in both the encoder and parser. Optional billing and alternative-procedure fields retain their defined positions. The common QR result recognizes validated Swiss UTF-8 payloads without requiring an ECI.

For printing, scale the data-module area, excluding the quiet zone, to 46 by 46 mm; the cross is proportional to 7 mm within that area. Output includes four quiet-zone modules on each side. For a core width of `n` modules, scale the complete output to `46 * (n + 8) / n` mm, rather than 46 mm. The helper supplies the symbol; a complete Swiss payment part and receipt need their own layout and qualification under the [SIX QR-bill guidelines](https://www.six-group.com/dam/download/banking-services/standardization/qr-bill/ig-qr-bill-v2.3-en.pdf), sections 6.4–6.4.2.

## Specialist format controls

- `Code16KEncodingOptions` selects GS1, reader initialization, FNC2 message append, FNC2 ordered concatenation or sixteen-row extended-data blocks. Sequence metadata uses one-based indices. Extended data supports up to 107 blocks; ordinary FNC2 concatenation supports up to nine.
- `Code49EncodingOptions` selects GS1, continuation or an ordered group of up to nine symbols. The decoder retains these modes; FNC2 separates fields and reserved FNC3 is rejected.
- `PosiCodeEncoder` selects A/B or Limited A/B variants, two through five linked rows, GS1 and reader/message controls. Limited variants use their restricted alphabet and have no function characters.
- `Bc412Encoder` defaults to framed SEMI T1 identifiers. IBM mode permits optional trailing checks and guards. Raw IBM decoding requires exact module bounds and explicit check/guard choices; the generic image decoder supports framed, checked input.
- `ChannelCodeEncoder` accepts two through seven digits within the capacity of their digit width. Automatic selection preserves that width; an explicitly wider channel count adds leading zeros to the decoded value. For example, `"05"` in four channels decodes as `"005"`.
- `TelepenNumeric` requires a format hint because its bars also have a valid Telepen ASCII interpretation. DAFT and Flattermarken are raw mark encodings and also require explicit selection.
- `D3AqrEncoder` wraps a QR host in the provisional Rev C category frame. Its decoder retains the category and QR host metadata. The category is required when encoding; this is provisional-format support.

```csharp
using CodeGlyphX.Code16K;
using CodeGlyphX.PosiCode;

var part = Code16KEncoder.Encode("FIRST PART", new Code16KEncodingOptions {
    StructuredAppend = new Code16KStructuredAppend(index: 1, total: 2)
});
part.Save("part.svg", new MatrixOptions { LabelText = "Part 1 of 2" });

var linked = PosiCodeEncoder.EncodeStacked("ABC123DEF456", rows: 3, variant: PosiCodeVariant.B);
bool decoded = SymbolDecoder.TryDecode(linked.Modules, out var result, SymbolFormat.PosiCode);
```

`MatrixOptions.LabelText` adds a portable caption outside the symbol's quiet zone. Caption rendering keeps the encoded modules intact. Code 16K, Code 49, Codablock F, Mailmark 4-state, D3 AQR and linked PosiCode rows also decode from images through `SymbolScanner`. The [capability table](/docs/symbol-capabilities/) records default recognition, calibration, geometry and multiple-result support for every physical format.
