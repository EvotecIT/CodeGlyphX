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

Decode with `BarcodeProfiles.TryDecode(profile, modules, out identifier)` when the application knows the expected profile, or call `TryParse` on an already decoded carrier value. An EAN or Code 39 symbol alone cannot uniquely identify the business application. The M&S carrier is EAN-8; a seven-digit input has an implied leading zero, and eight-digit input keeps all eight checked digits. Its physical print layout positions M and S outside the data area and leaves the centre guards at digit height. These renderers produce symbols and captions, without a complete retail label template.

ISSN accepts an optional space followed by a two-digit issue variant, such as `0317-8471 12`.
The variant defaults to `00` and affects the EAN-13 bars. `Prepare` and `HumanReadable` retain the
canonical ISSN. Use `TryDecodeDetailed` or `TryParseDetailed` to preserve `IssueVariant`,
`Identifier` and the complete `CarrierText` in a `BarcodeProfileData` result.

```csharp
var periodical = BarcodeProfiles.Encode(LinearBarcodeProfile.Issn, "0317-8471 12");
if (BarcodeProfiles.TryParseDetailed(LinearBarcodeProfile.Issn, "9770317847124", out var issue))
{
    Console.WriteLine(issue.IssueVariant); // 12
}
```

## MSI checks and Channel Code finders

`MsiEncoder.Encode` accepts no check, Mod10, Mod10Mod10, IBM Mod11/Mod11Mod10 and NCR
Mod11/Mod11Mod10. Select the same algorithm through `BarcodeDecodeOptions.MsiChecksumAlgorithm`
and a checksum policy when decoding. An omitted algorithm preserves the existing Mod10/Mod10Mod10
handling. The default decode policy retains the full encoded text.

Mod11 check value ten occupies two decimal digits. Some symbols therefore admit two valid data
lengths. `StripIfValid` retains the complete text when that happens; `RequireValid` fails until the
application supplies `MsiExpectedDataLength`. The same options work through `ScanOptions.Barcode`.

```csharp
using CodeGlyphX.Msi;

var checkedMsi = MsiEncoder.Encode("12345678", MsiChecksumType.NcrMod11Mod10);
var scanOptions = new ScanOptions
{
    Formats = new[] { SymbolFormat.Msi },
    Barcode = new BarcodeDecodeOptions().WithMsiChecksum(
        MsiChecksumPolicy.RequireValid, MsiChecksumType.NcrMod11Mod10, expectedDataLength: 8)
};
```

`ChannelCodeEncoder.Encode("26", shortFinder: true)` selects the three-bar finder. The existing
overload keeps its five-bar finder. Module and image decoders accept both and preserve the
channel count as leading decimal zeros.

## HIBC

`Hibc.Prepare` adds the leading plus and Mod-43 check. `TryParse` validates them and returns the unframed data. Supported carriers are Code 39, Code 128, QR, Aztec, Data Matrix, PDF417, MicroPDF417 and Codablock F. Explicit Data Matrix options also select rectangular or DMRE models. HIBC Aztec uses the recommended 23 percent error-correction setting.

```csharp
var linear = Hibc.EncodeLinear(SymbolFormat.Code39, "A999BJC5D6E71");
linear.Save("hibc.png", new BarcodeOptions { LabelText = Hibc.Prepare("A999BJC5D6E71") });

var matrix = Hibc.EncodeMatrix(SymbolFormat.MicroPdf417, "A999BJC5D6E71");
matrix.Save("hibc-micro.pdf", new MatrixOptions { LabelText = Hibc.Prepare("A999BJC5D6E71") });

bool valid = Hibc.TryDecodeMatrix(SymbolFormat.MicroPdf417, matrix.Modules, out string data);
```

Use `PrepareStructured` to check supplier data fields before framing, or pass `validateStructure: true` to `EncodeLinear` or `EncodeMatrix`. `TryParseStructured` validates a checked payload and returns primary, secondary and supplemental fields. It retains two-digit date years without inventing a century, accepts and marks historical bare-Julian lot fields when decoding, and checks calendar dates, identifier lengths and supplemental-field order. Strict preparation does not produce historical forms.

`Hibc.IsLinked` checks the link character between separate primary and secondary payloads. `HumanReadable` formats checked text with readable delimiters, underscores for spaces, slashed zeros and a line break between concatenated primary and secondary data. These display changes do not change encoded content.

Assignment of labeler identifiers and regulatory acceptance remain the application's responsibility. Literal HIBC Code 39 uses its plus character directly; `Hibc.TryDecodeLinear` selects that interpretation rather than Full ASCII shift pairs.

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

## Australia Post fields and printed lines

The existing `AustraliaPostEncoder.Encode(string)` accepts a DPID and its fixed-width customer
field. Use `EncodeSymbol` for an explicit two-digit FCC followed by an eight-digit DPID and a
variable customer field. It supports FCC `00`, `11`, `59` and `62`; `00` requires an all-zero DPID.
Select the C or N table explicitly. Customer 2 holds up to five C characters or eight N digits;
Customer 3 holds up to ten C characters or fifteen N digits. Trailing customer spaces are retained.

```csharp
using CodeGlyphX.AustraliaPost;
using CodeGlyphX.Printing;

var postal = AustraliaPostEncoder.EncodeSymbol("5956439111ABz", AustraliaPostCustomerEncodingTable.C);
var mailLayout = BarcodePrint.Layout(postal, new BarcodePrintOptions
{
    Font = BarcodeFont.FromBytes(File.ReadAllBytes("label-font.ttf")),
    FontSizePoints = 8
});
mailLayout.Save("postal-label.pdf");
```

The physical layout positions the FCC, DPID, customer field and four decimal parity values above
the bars. It measures that line with the supplied font and reports text over eight points.

A C-table trailing `z` has the same bars as padding, and some fields fit both tables. Decoded
`CustomerInfoAlternatives` retains those interpretations. The default preserves the previous
table/longest-field interpretation and marks unresolved ambiguity. Supply the table and customer
character count to the four-argument `AustraliaPostDecoder.TryDecode` overload, or set
`ScanOptions.AustraliaPostCustomerTable` and `AustraliaPostCustomerInfoLength` for image scanning.
Unified results retain the selected fields, parity and alternatives in `AustraliaPostSymbolMetadata.Data`.

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

`RawLinear.Encode("9123456789")` treats each ASCII digit as a bar/space width from one through nine
modules. It retains an even-length definition's final white run. `GetWidths` returns that definition,
and `BarcodePrint.LayoutRaw` produces a measured layout with no required quiet zone or inferred text.
The default raw bar height is 15 percent of the width; physical options can set it explicitly.

`RawLinear.TryDecode` accepts exact module samples or an `ImageFrame` with the complete known
symbol bounds, module count, rotation and polarity. Calibrated bounds must include trailing white
modules and exclude captions. It recovers run widths and does no automatic symbol identification
or application-text inference. Raw definitions are limited to 2500 runs and 22500 modules.

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

## Optional check characters

`BarcodeEncodingOptions` selects `None`, `Compute` or `Validate` for Code 39, Code 11,
Codabar, ITF, discrete 2 of 5 and MSI. `Validate` checks characters supplied at the
end of the input and encodes them once. Plessey always carries its mandatory CRC;
its explicit options accept computation or validation. Other mandatory carrier
checks remain part of their encoders.

```csharp
var builder = Barcode.Create(SymbolFormat.Code39, "aX")
    .WithEncodingOptions(new BarcodeEncodingOptions {
        Code39FullAscii = true,
        CheckMode = BarcodeCheckMode.Compute,
        PrintCheckCharacters = false
    });
var symbol = builder.Encode();
var layout = builder.LayoutPrint(new BarcodePrintOptions { Font = suppliedFont });
layout.Save("checked-label.pdf");
```

The check is calculated over the encoded alphabet. `PrintCheckCharacters` changes
the preferred text for physical printing without changing bars. An explicit
`BarcodePrintOptions.HumanReadableText` overrides that preference. Existing
encoding overloads and pixel-render labels keep their defaults.

On decoding, select `Code39Checksum`, `Code11Checksum`, `MsiChecksum`,
`CodabarChecksum` or `TwoOfFiveChecksum` explicitly when the application knows
that an optional check is present. Defaults retain optional characters as data.
ITF may add a leading zero to form pairs; decoding retains it because the bars
cannot identify whether it was padding. MSI Mod11 value ten can occupy two
digits; set the algorithm and known data length when either interpretation is
valid. Codabar accepts alternate framing `T/N/*/E` and normalizes it to `A/B/C/D`.
