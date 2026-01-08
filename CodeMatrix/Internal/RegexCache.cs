using System.Text.RegularExpressions;

namespace CodeMatrix.Internal;

internal static partial class RegexCache {
#if NET7_0_OR_GREATER
    [GeneratedRegex("^[0-9]*$", RegexOptions.CultureInvariant)]
    internal static partial Regex DigitsOptional();

    [GeneratedRegex("^[0-9]+$", RegexOptions.CultureInvariant)]
    internal static partial Regex DigitsRequired();

    [GeneratedRegex("^[A-Za-z]{2}$", RegexOptions.CultureInvariant)]
    internal static partial Regex CountryCode();

    [GeneratedRegex("^[a-zA-Z]{2}[0-9]{2}([a-zA-Z0-9]?){16,30}$", RegexOptions.CultureInvariant)]
    internal static partial Regex IbanBasic();

    [GeneratedRegex("^([a-zA-Z]{4}[a-zA-Z]{2}[a-zA-Z0-9]{2}([a-zA-Z0-9]{3})?)$", RegexOptions.CultureInvariant)]
    internal static partial Regex Bic();

    [GeneratedRegex("\\A\\b[0-9a-fA-F]+\\b\\Z", RegexOptions.CultureInvariant)]
    internal static partial Regex HexPlain();

    [GeneratedRegex("\\A\\b(0[xX])?[0-9a-fA-F]+\\b\\Z", RegexOptions.CultureInvariant)]
    internal static partial Regex HexWithPrefix();

    [GeneratedRegex("^[0+]+|[ ()-]", RegexOptions.CultureInvariant)]
    internal static partial Regex WhatsappSanitize();
#else
    private static readonly Regex DigitsOptionalRegex = new("^[0-9]*$", RegexOptions.CultureInvariant);
    private static readonly Regex DigitsRequiredRegex = new("^[0-9]+$", RegexOptions.CultureInvariant);
    private static readonly Regex CountryCodeRegex = new("^[A-Za-z]{2}$", RegexOptions.CultureInvariant);
    private static readonly Regex IbanBasicRegex = new("^[a-zA-Z]{2}[0-9]{2}([a-zA-Z0-9]?){16,30}$", RegexOptions.CultureInvariant);
    private static readonly Regex BicRegex = new("^([a-zA-Z]{4}[a-zA-Z]{2}[a-zA-Z0-9]{2}([a-zA-Z0-9]{3})?)$", RegexOptions.CultureInvariant);
    private static readonly Regex HexPlainRegex = new("\\A\\b[0-9a-fA-F]+\\b\\Z", RegexOptions.CultureInvariant);
    private static readonly Regex HexWithPrefixRegex = new("\\A\\b(0[xX])?[0-9a-fA-F]+\\b\\Z", RegexOptions.CultureInvariant);
    private static readonly Regex WhatsappSanitizeRegex = new("^[0+]+|[ ()-]", RegexOptions.CultureInvariant);

    internal static Regex DigitsOptional() => DigitsOptionalRegex;
    internal static Regex DigitsRequired() => DigitsRequiredRegex;
    internal static Regex CountryCode() => CountryCodeRegex;
    internal static Regex IbanBasic() => IbanBasicRegex;
    internal static Regex Bic() => BicRegex;
    internal static Regex HexPlain() => HexPlainRegex;
    internal static Regex HexWithPrefix() => HexWithPrefixRegex;
    internal static Regex WhatsappSanitize() => WhatsappSanitizeRegex;
#endif
}
