namespace CodeGlyphX;

/// <summary>An application identifier whose validation and payload preparation are layered on a linear carrier.</summary>
public enum LinearBarcodeProfile {
    /// <summary>GTIN-14 encoded with GS1 application identifier 01.</summary>
    Ean14,
    /// <summary>Serial Shipping Container Code encoded with GS1 application identifier 00.</summary>
    Sscc18,
    /// <summary>ISBN-10 or ISBN-13, encoded as an ISBN-13 EAN barcode.</summary>
    Isbn,
    /// <summary>Legacy or current International Standard Music Number.</summary>
    Ismn,
    /// <summary>International Standard Serial Number, encoded with the default 00 issue variant.</summary>
    Issn,
    /// <summary>Seven-digit Pharmazentralnummer.</summary>
    Pzn7,
    /// <summary>Eight-digit Pharmazentralnummer.</summary>
    Pzn8,
    /// <summary>Deutsche Post Identcode.</summary>
    Identcode,
    /// <summary>Deutsche Post Leitcode.</summary>
    Leitcode,
    /// <summary>Marks and Spencer identifier carried by EAN-8.</summary>
    MarksAndSpencer
}
