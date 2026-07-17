using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodeGlyphX.Gs1Data;

internal static class Gs1DigitalLinkCodec {
    private const string CanonicalStem = "https://id.gs1.org";
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

    internal static global::CodeGlyphX.Gs1DigitalLinkValidationResult Validate(
        string input,
        Gs1ValidationOptions? validationOptions) {
        if (input is null) throw new ArgumentNullException(nameof(input));

        var issues = new List<global::CodeGlyphX.Gs1DigitalLinkIssue>();
        if (!ValidateRawUri(input, issues, out var uri)) {
            return Result(null, null, issues);
        }

        var escapedPath = GetRawEscapedPath(input);
        if (escapedPath.Length == 0 || escapedPath.EndsWith("/", StringComparison.Ordinal)) {
            issues.Add(Issue(
                global::CodeGlyphX.Gs1DigitalLinkIssueCode.InvalidPath,
                "path",
                "A GS1 Digital Link path must end with an Application Identifier value, not a trailing slash."));
            return Result(null, null, issues);
        }

        var segments = escapedPath.Split('/');
        var primaryIndex = FindPrimaryIndex(segments);
        if (primaryIndex < 0) {
            issues.Add(Issue(
                global::CodeGlyphX.Gs1DigitalLinkIssueCode.MissingPrimaryIdentifier,
                "path",
                "The URI path does not contain a GS1 Digital Link primary Application Identifier followed by its value."));
            return Result(null, null, issues);
        }

        var pathElements = new List<global::CodeGlyphX.Gs1Element>();
        var qualifiers = new List<global::CodeGlyphX.Gs1Element>();
        var seenAis = new HashSet<string>(StringComparer.Ordinal);
        if (!ParseGs1Path(segments, primaryIndex, pathElements, qualifiers, seenAis, issues)) {
            return Result(null, null, issues);
        }

        var attributes = new List<global::CodeGlyphX.Gs1Element>();
        var extensions = new Dictionary<string, string>(StringComparer.Ordinal);
        ParseQuery(uri, pathElements[0].Definition!, attributes, extensions, seenAis, issues);

        string? fragment = null;
        var escapedFragment = uri.GetComponents(UriComponents.Fragment, UriFormat.UriEscaped);
        if (escapedFragment.Length > 0 && !TryDecode(escapedFragment, out fragment)) {
            issues.Add(Issue(
                global::CodeGlyphX.Gs1DigitalLinkIssueCode.InvalidPercentEncoding,
                "fragment",
                "The URI fragment contains malformed percent encoding or invalid UTF-8."));
        }

        var elements = new List<global::CodeGlyphX.Gs1Element>(pathElements.Count + attributes.Count);
        elements.AddRange(pathElements);
        elements.AddRange(attributes);
        var elementValidation = ValidateElements(elements, validationOptions, issues);
        if (issues.Count > 0 || elementValidation is null || !elementValidation.IsValid) {
            return Result(null, elementValidation, issues);
        }

        var stem = BuildStem(uri, segments, primaryIndex);
        var canonical = BuildUriString(
            CanonicalStem,
            elements,
            pathElements[0].Ai,
            extensionParameters: null,
            fragment: null,
            canonical: true);
        var value = new global::CodeGlyphX.Gs1DigitalLinkUri(
            uri,
            input,
            stem,
            pathElements[0],
            qualifiers.ToArray(),
            attributes.ToArray(),
            elements.ToArray(),
            extensions,
            fragment,
            canonical);
        return Result(value, elementValidation, issues);
    }

    internal static global::CodeGlyphX.Gs1DigitalLinkUri Build(
        string uriStem,
        IReadOnlyList<global::CodeGlyphX.Gs1Element> elements,
        string? primaryIdentifierAi,
        IReadOnlyDictionary<string, string>? extensionParameters,
        string? fragment,
        Gs1ValidationOptions? validationOptions) {
        if (uriStem is null) throw new ArgumentNullException(nameof(uriStem));
        if (elements is null) throw new ArgumentNullException(nameof(elements));
        if (elements.Count == 0) throw new ArgumentException("At least one GS1 element is required.", nameof(elements));

        var normalizedStem = NormalizeStem(uriStem);
        var uri = BuildUriString(
            normalizedStem,
            elements,
            primaryIdentifierAi,
            extensionParameters,
            fragment,
            canonical: string.Equals(normalizedStem, CanonicalStem, StringComparison.Ordinal)
                       && extensionParameters is null
                       && fragment is null);
        var result = Validate(uri, validationOptions);
        if (result.IsValid) return result.Value!;
        if (result.Issues.Count > 0) throw new FormatException(result.Issues[0].Message);
        if (result.ElementValidation is not null && result.ElementValidation.Issues.Count > 0) {
            throw new FormatException(result.ElementValidation.Issues[0].Message);
        }
        throw new FormatException("The GS1 elements cannot be represented as a GS1 Digital Link URI.");
    }

    private static bool ValidateRawUri(
        string input,
        ICollection<global::CodeGlyphX.Gs1DigitalLinkIssue> issues,
        out Uri uri) {
        uri = null!;
        if (input.Length == 0 || !string.Equals(input, input.Trim(), StringComparison.Ordinal)) {
            issues.Add(Issue(
                global::CodeGlyphX.Gs1DigitalLinkIssueCode.MalformedUri,
                null,
                "A GS1 Digital Link URI cannot be empty or surrounded by whitespace."));
            return false;
        }
        if (!HasOnlyRfc3986Characters(input)) {
            issues.Add(Issue(
                global::CodeGlyphX.Gs1DigitalLinkIssueCode.MalformedUri,
                null,
                "A GS1 Digital Link URI must use RFC 3986 ASCII characters; Unicode and other characters must be UTF-8 percent encoded."));
            return false;
        }
        if (!HasValidPercentEncoding(input)) {
            issues.Add(Issue(
                global::CodeGlyphX.Gs1DigitalLinkIssueCode.InvalidPercentEncoding,
                null,
                "The URI contains a percent sign that is not followed by two hexadecimal digits."));
            return false;
        }
        var queryIndex = input.IndexOf('?');
        var fragmentIndex = input.IndexOf('#');
        if (queryIndex >= 0
            && (fragmentIndex < 0 || queryIndex < fragmentIndex)
            && queryIndex + 1 == (fragmentIndex < 0 ? input.Length : fragmentIndex)) {
            issues.Add(Issue(
                global::CodeGlyphX.Gs1DigitalLinkIssueCode.InvalidQuery,
                "query",
                "A GS1 Digital Link URI cannot contain an empty query string."));
            return false;
        }
        if (!Uri.TryCreate(input, UriKind.Absolute, out uri!) || string.IsNullOrEmpty(uri.Host)) {
            issues.Add(Issue(
                global::CodeGlyphX.Gs1DigitalLinkIssueCode.MalformedUri,
                null,
                "A GS1 Digital Link must be an absolute Web URI with a host name."));
            return false;
        }
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) {
            issues.Add(Issue(
                global::CodeGlyphX.Gs1DigitalLinkIssueCode.UnsupportedScheme,
                "scheme",
                "GS1 Digital Link URI Syntax permits only HTTP and HTTPS schemes."));
            return false;
        }
        return true;
    }

    private static string GetRawEscapedPath(string input) {
        var authorityStart = input.IndexOf("://", StringComparison.Ordinal);
        if (authorityStart < 0) return string.Empty;
        var pathStart = input.IndexOf('/', authorityStart + 3);
        if (pathStart < 0) return string.Empty;
        var queryStart = input.IndexOf('?', authorityStart + 3);
        var fragmentStart = input.IndexOf('#', authorityStart + 3);
        if ((queryStart >= 0 && queryStart < pathStart) || (fragmentStart >= 0 && fragmentStart < pathStart)) return string.Empty;
        var pathEnd = input.Length;
        if (queryStart >= 0 && queryStart < pathEnd) pathEnd = queryStart;
        if (fragmentStart >= 0 && fragmentStart < pathEnd) pathEnd = fragmentStart;
        return input.Substring(pathStart + 1, pathEnd - pathStart - 1);
    }

    private static int FindPrimaryIndex(string[] segments) {
        for (var i = segments.Length - 2; i >= 0; i--) {
            if (!IsAiToken(segments[i])) continue;
            if (!Gs1ApplicationIdentifierCatalog.TryGet(segments[i], out var definition)) continue;
            if (definition.IsDigitalLinkPrimaryKey) return i;
        }
        return -1;
    }

    private static bool ParseGs1Path(
        string[] segments,
        int primaryIndex,
        ICollection<global::CodeGlyphX.Gs1Element> pathElements,
        ICollection<global::CodeGlyphX.Gs1Element> qualifiers,
        ISet<string> seenAis,
        ICollection<global::CodeGlyphX.Gs1DigitalLinkIssue> issues) {
        if ((segments.Length - primaryIndex) % 2 != 0) {
            issues.Add(Issue(
                global::CodeGlyphX.Gs1DigitalLinkIssueCode.InvalidPath,
                "path",
                "The GS1 path must contain alternating Application Identifier and value segments."));
            return false;
        }

        var primaryAi = segments[primaryIndex];
        var primaryDefinition = Gs1ApplicationIdentifierCatalog.Get(primaryAi);
        var groups = GetQualifierGroups(primaryDefinition);
        var lastGroup = -1;
        for (var i = primaryIndex; i < segments.Length; i += 2) {
            var ai = segments[i];
            if (!IsAiToken(ai) || !Gs1ApplicationIdentifierCatalog.TryGet(ai, out var definition)) {
                issues.Add(Issue(
                    global::CodeGlyphX.Gs1DigitalLinkIssueCode.InvalidPath,
                    ai,
                    "Every GS1 path key must be an assigned numeric Application Identifier."));
                return false;
            }
            if (!seenAis.Add(ai)) {
                issues.Add(Issue(
                    global::CodeGlyphX.Gs1DigitalLinkIssueCode.DuplicateApplicationIdentifier,
                    ai,
                    $"Application Identifier '{ai}' occurs more than once."));
                return false;
            }
            if (!TryDecode(segments[i + 1], out var data) || data.Length == 0) {
                issues.Add(Issue(
                    global::CodeGlyphX.Gs1DigitalLinkIssueCode.InvalidPercentEncoding,
                    ai,
                    $"The value for Application Identifier '{ai}' is empty or is not valid percent-encoded UTF-8."));
                return false;
            }

            if (i == primaryIndex) {
                pathElements.Add(CreateElement(definition, data));
                continue;
            }

            var group = FindQualifierGroup(groups, ai);
            if (group < 0 || group <= lastGroup) {
                issues.Add(Issue(
                    global::CodeGlyphX.Gs1DigitalLinkIssueCode.InvalidPath,
                    ai,
                    $"Application Identifier '{ai}' is not a key qualifier in the required order for primary identifier '{primaryAi}'."));
                return false;
            }
            lastGroup = group;
            var element = CreateElement(definition, data);
            pathElements.Add(element);
            qualifiers.Add(element);
        }
        return true;
    }

    private static void ParseQuery(
        Gs1ApplicationIdentifier primary,
        ICollection<global::CodeGlyphX.Gs1Element> attributes,
        IDictionary<string, string> extensions,
        ISet<string> seenAis,
        ICollection<global::CodeGlyphX.Gs1DigitalLinkIssue> issues,
        string query) {
        if (query.Length == 0) return;
        var qualifierGroups = GetQualifierGroups(primary);
        var parameters = query.Split(new[] { '&', ';' }, StringSplitOptions.None);
        for (var i = 0; i < parameters.Length; i++) {
            var parameter = parameters[i];
            var equals = parameter.IndexOf('=');
            if (parameter.Length == 0 || equals <= 0 || equals != parameter.LastIndexOf('=')) {
                issues.Add(Issue(
                    global::CodeGlyphX.Gs1DigitalLinkIssueCode.InvalidQuery,
                    "query",
                    "Every query component must contain exactly one non-empty key followed by '=' and a value."));
                continue;
            }

            var rawKey = parameter.Substring(0, equals);
            var rawValue = parameter.Substring(equals + 1);
            if (IsAiToken(rawKey)) {
                if (!Gs1ApplicationIdentifierCatalog.TryGet(rawKey, out var definition)) {
                    issues.Add(Issue(
                        global::CodeGlyphX.Gs1DigitalLinkIssueCode.UnknownApplicationIdentifier,
                        rawKey,
                        $"Application Identifier '{rawKey}' is not assigned in dictionary release {Gs1ApplicationIdentifierCatalog.Release}."));
                    continue;
                }
                if (FindQualifierGroup(qualifierGroups, rawKey) >= 0) {
                    issues.Add(Issue(
                        global::CodeGlyphX.Gs1DigitalLinkIssueCode.InvalidQuery,
                        rawKey,
                        $"Application Identifier '{rawKey}' is a key qualifier for primary identifier '{primary.Ai}' and must appear in the URI path."));
                    continue;
                }
                if (!definition.IsDigitalLinkDataAttribute) {
                    issues.Add(Issue(
                        global::CodeGlyphX.Gs1DigitalLinkIssueCode.InvalidQuery,
                        rawKey,
                        $"Application Identifier '{rawKey}' is not permitted as a GS1 Digital Link data attribute."));
                    continue;
                }
                if (!seenAis.Add(rawKey)) {
                    issues.Add(Issue(
                        global::CodeGlyphX.Gs1DigitalLinkIssueCode.DuplicateApplicationIdentifier,
                        rawKey,
                        $"Application Identifier '{rawKey}' occurs more than once."));
                    continue;
                }
                if (!TryDecode(rawValue, out var data) || data.Length == 0) {
                    issues.Add(Issue(
                        global::CodeGlyphX.Gs1DigitalLinkIssueCode.InvalidPercentEncoding,
                        rawKey,
                        $"The value for Application Identifier '{rawKey}' is empty or is not valid percent-encoded UTF-8."));
                    continue;
                }
                attributes.Add(CreateElement(definition, data));
                continue;
            }

            if (IsAllDigits(rawKey)) {
                issues.Add(Issue(
                    global::CodeGlyphX.Gs1DigitalLinkIssueCode.InvalidExtensionParameter,
                    rawKey,
                    "An extension parameter key cannot be all numeric because it may collide with a current or future GS1 Application Identifier."));
                continue;
            }
            if (!TryDecode(rawKey, out var key) || key.Length == 0 || IsAllDigits(key)
                || !TryDecode(rawValue, out var value)) {
                issues.Add(Issue(
                    global::CodeGlyphX.Gs1DigitalLinkIssueCode.InvalidExtensionParameter,
                    rawKey,
                    "The extension parameter key or value is not valid percent-encoded UTF-8."));
                continue;
            }
            if (extensions.ContainsKey(key)) {
                issues.Add(Issue(
                    global::CodeGlyphX.Gs1DigitalLinkIssueCode.InvalidExtensionParameter,
                    key,
                    $"Extension parameter '{key}' occurs more than once."));
                continue;
            }
            extensions.Add(key, value);
        }
    }

    private static void ParseQuery(
        Uri uri,
        Gs1ApplicationIdentifier primary,
        ICollection<global::CodeGlyphX.Gs1Element> attributes,
        IDictionary<string, string> extensions,
        ISet<string> seenAis,
        ICollection<global::CodeGlyphX.Gs1DigitalLinkIssue> issues) {
        ParseQuery(
            primary,
            attributes,
            extensions,
            seenAis,
            issues,
            uri.GetComponents(UriComponents.Query, UriFormat.UriEscaped));
    }

    private static Gs1ValidationResult? ValidateElements(
        IReadOnlyList<global::CodeGlyphX.Gs1Element> elements,
        Gs1ValidationOptions? options,
        ICollection<global::CodeGlyphX.Gs1DigitalLinkIssue> issues) {
        if (elements.Count == 0) return null;
        Gs1ValidationResult validation;
        try {
            validation = Gs1Validator.Validate(Gs1Validator.ToElementString(elements), options);
        } catch (FormatException exception) {
            issues.Add(Issue(
                global::CodeGlyphX.Gs1DigitalLinkIssueCode.InvalidApplicationIdentifier,
                null,
                exception.Message));
            return null;
        }

        for (var i = 0; i < validation.Issues.Count; i++) {
            var item = validation.Issues[i];
            issues.Add(Issue(
                global::CodeGlyphX.Gs1DigitalLinkIssueCode.InvalidApplicationIdentifier,
                item.Ai,
                item.Message));
        }
        return validation;
    }

    private static string BuildUriString(
        string stem,
        IReadOnlyList<global::CodeGlyphX.Gs1Element> elements,
        string? requestedPrimaryAi,
        IReadOnlyDictionary<string, string>? extensionParameters,
        string? fragment,
        bool canonical) {
        var normalized = NormalizeElements(elements);
        var primary = SelectPrimary(normalized, requestedPrimaryAi);
        var groups = GetQualifierGroups(primary.Definition!);
        var qualifiers = SelectQualifiers(normalized, primary, groups);
        var pathAis = new HashSet<string>(StringComparer.Ordinal) { primary.Ai };
        var builder = new StringBuilder(stem.TrimEnd('/'));
        AppendPathElement(builder, primary);
        for (var i = 0; i < qualifiers.Count; i++) {
            pathAis.Add(qualifiers[i].Ai);
            AppendPathElement(builder, qualifiers[i]);
        }

        var query = new List<KeyValuePair<string, string>>();
        for (var i = 0; i < normalized.Count; i++) {
            var element = normalized[i];
            if (pathAis.Contains(element.Ai)) continue;
            if (!element.Definition!.IsDigitalLinkDataAttribute) {
                throw new FormatException($"Application Identifier '{element.Ai}' is not permitted as a GS1 Digital Link data attribute.");
            }
            query.Add(new KeyValuePair<string, string>(element.Ai, element.Data));
        }
        query.Sort((left, right) => string.CompareOrdinal(left.Key, right.Key));

        if (!canonical && extensionParameters is not null) {
            foreach (var pair in extensionParameters.OrderBy(pair => pair.Key, StringComparer.Ordinal)) {
                if (string.IsNullOrEmpty(pair.Key) || IsAllDigits(pair.Key)) {
                    throw new FormatException("A GS1 Digital Link extension parameter key must be non-empty and contain a non-digit character.");
                }
                query.Add(new KeyValuePair<string, string>(pair.Key, pair.Value ?? string.Empty));
            }
        }
        for (var i = 0; i < query.Count; i++) {
            builder.Append(i == 0 ? '?' : '&');
            builder.Append(IsAiToken(query[i].Key) ? query[i].Key : Escape(query[i].Key));
            builder.Append('=').Append(Escape(query[i].Value));
        }
        if (!canonical && fragment is not null) builder.Append('#').Append(Escape(fragment));
        return builder.ToString();
    }

    private static List<global::CodeGlyphX.Gs1Element> NormalizeElements(
        IReadOnlyList<global::CodeGlyphX.Gs1Element> elements) {
        if (elements.Count == 0) throw new ArgumentException("At least one GS1 element is required.", nameof(elements));
        var normalized = new List<global::CodeGlyphX.Gs1Element>(elements.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < elements.Count; i++) {
            var element = elements[i];
            if (!Gs1ApplicationIdentifierCatalog.TryGet(element.Ai, out var definition)) {
                throw new FormatException($"Application Identifier '{element.Ai}' is not assigned in dictionary release {Gs1ApplicationIdentifierCatalog.Release}.");
            }
            if (!seen.Add(element.Ai)) throw new FormatException($"Application Identifier '{element.Ai}' occurs more than once.");
            normalized.Add(CreateElement(definition, element.Data ?? string.Empty));
        }
        return normalized;
    }

    private static global::CodeGlyphX.Gs1Element SelectPrimary(
        IReadOnlyList<global::CodeGlyphX.Gs1Element> elements,
        string? requestedPrimaryAi) {
        if (requestedPrimaryAi is not null) {
            for (var i = 0; i < elements.Count; i++) {
                if (elements[i].Ai != requestedPrimaryAi) continue;
                if (!elements[i].Definition!.IsDigitalLinkPrimaryKey) {
                    throw new FormatException($"Application Identifier '{requestedPrimaryAi}' is not a GS1 Digital Link primary key.");
                }
                return elements[i];
            }
            throw new FormatException($"Primary Application Identifier '{requestedPrimaryAi}' is not present in the GS1 elements.");
        }

        for (var i = 0; i < elements.Count; i++) {
            if (elements[i].Definition!.IsDigitalLinkPrimaryKey) return elements[i];
        }
        throw new FormatException("At least one GS1 Digital Link primary Application Identifier is required.");
    }

    private static List<global::CodeGlyphX.Gs1Element> SelectQualifiers(
        IReadOnlyList<global::CodeGlyphX.Gs1Element> elements,
        global::CodeGlyphX.Gs1Element primary,
        IReadOnlyList<string[]> groups) {
        var result = new List<global::CodeGlyphX.Gs1Element>();
        for (var group = 0; group < groups.Count; group++) {
            global::CodeGlyphX.Gs1Element? match = null;
            for (var i = 0; i < elements.Count; i++) {
                if (elements[i].Ai == primary.Ai || Array.IndexOf(groups[group], elements[i].Ai) < 0) continue;
                if (match.HasValue) {
                    throw new FormatException(
                        $"Only one of the alternative key qualifiers '{string.Join("|", groups[group])}' can occur for primary identifier '{primary.Ai}'.");
                }
                match = elements[i];
            }
            if (match.HasValue) result.Add(match.Value);
        }
        return result;
    }

    private static string NormalizeStem(string value) {
        if (value.IndexOf('?') >= 0 || value.IndexOf('#') >= 0) {
            throw new ArgumentException("The URI stem cannot contain a query string or fragment.", nameof(value));
        }
        if (value.Length == 0
            || !string.Equals(value, value.Trim(), StringComparison.Ordinal)
            || !HasOnlyRfc3986Characters(value)
            || !HasValidPercentEncoding(value)) {
            throw new ArgumentException(
                "The URI stem must use RFC 3986 ASCII characters and valid percent encoding.",
                nameof(value));
        }
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Host)) {
            throw new ArgumentException("The URI stem must be an absolute Web URI with a host name.", nameof(value));
        }
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) {
            throw new ArgumentException("The URI stem must use HTTP or HTTPS.", nameof(value));
        }
        if (!string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)) {
            throw new ArgumentException("The URI stem cannot contain a query string or fragment.", nameof(value));
        }
        var path = uri.GetComponents(UriComponents.Path, UriFormat.UriEscaped).TrimEnd('/');
        return path.Length == 0
            ? uri.GetLeftPart(UriPartial.Authority).TrimEnd('/')
            : uri.GetLeftPart(UriPartial.Authority).TrimEnd('/') + "/" + path.TrimStart('/');
    }

    private static string BuildStem(Uri uri, string[] segments, int primaryIndex) {
        var builder = new StringBuilder(uri.GetLeftPart(UriPartial.Authority).TrimEnd('/'));
        for (var i = 0; i < primaryIndex; i++) builder.Append('/').Append(segments[i]);
        return builder.ToString();
    }

    private static IReadOnlyList<string[]> GetQualifierGroups(Gs1ApplicationIdentifier primary) {
        var qualifiers = primary.DigitalLinkPrimaryKeyQualifiers;
        if (string.IsNullOrEmpty(qualifiers)) return Array.Empty<string[]>();
        return qualifiers!
            .Split(',')
            .Select(group => group.Split('|'))
            .ToArray();
    }

    private static int FindQualifierGroup(IReadOnlyList<string[]> groups, string ai) {
        for (var i = 0; i < groups.Count; i++) {
            if (Array.IndexOf(groups[i], ai) >= 0) return i;
        }
        return -1;
    }

    private static global::CodeGlyphX.Gs1Element CreateElement(Gs1ApplicationIdentifier definition, string data) {
        return new global::CodeGlyphX.Gs1Element(definition.Ai, data, definition.RequiresFnc1Separator, definition);
    }

    private static void AppendPathElement(StringBuilder builder, global::CodeGlyphX.Gs1Element element) {
        builder.Append('/').Append(element.Ai).Append('/').Append(EscapePathValue(element.Data));
    }

    private static string EscapePathValue(string value) {
        if (string.Equals(value, ".", StringComparison.Ordinal)) return "%2E";
        if (string.Equals(value, "..", StringComparison.Ordinal)) return "%2E%2E";
        return Escape(value);
    }

    private static string Escape(string value) {
        if (value is null) throw new ArgumentNullException(nameof(value));
        var bytes = StrictUtf8.GetBytes(value);
        var builder = new StringBuilder(bytes.Length);
        for (var i = 0; i < bytes.Length; i++) {
            var valueByte = bytes[i];
            if ((valueByte >= (byte)'A' && valueByte <= (byte)'Z')
                || (valueByte >= (byte)'a' && valueByte <= (byte)'z')
                || (valueByte >= (byte)'0' && valueByte <= (byte)'9')
                || valueByte == (byte)'-' || valueByte == (byte)'.'
                || valueByte == (byte)'_' || valueByte == (byte)'~') {
                builder.Append((char)valueByte);
            } else {
                builder.Append('%').Append(valueByte.ToString("X2"));
            }
        }
        return builder.ToString();
    }

    private static bool TryDecode(string value, out string decoded) {
        decoded = string.Empty;
        if (!HasValidPercentEncoding(value)) return false;
        var bytes = new List<byte>(value.Length);
        for (var i = 0; i < value.Length; i++) {
            if (value[i] == '%') {
                bytes.Add((byte)((HexValue(value[i + 1]) << 4) | HexValue(value[i + 2])));
                i += 2;
            } else if (value[i] <= 0x7f) {
                bytes.Add((byte)value[i]);
            } else {
                return false;
            }
        }
        try {
            decoded = StrictUtf8.GetString(bytes.ToArray());
            return true;
        } catch (DecoderFallbackException) {
            return false;
        }
    }

    private static bool HasValidPercentEncoding(string value) {
        for (var i = 0; i < value.Length; i++) {
            if (value[i] != '%') continue;
            if (i + 2 >= value.Length || !IsHex(value[i + 1]) || !IsHex(value[i + 2])) return false;
            i += 2;
        }
        return true;
    }

    private static bool HasOnlyRfc3986Characters(string value) {
        for (var i = 0; i < value.Length; i++) {
            var character = value[i];
            if ((character >= 'A' && character <= 'Z')
                || (character >= 'a' && character <= 'z')
                || (character >= '0' && character <= '9')) {
                continue;
            }
            switch (character) {
                case '-':
                case '.':
                case '_':
                case '~':
                case ':':
                case '/':
                case '?':
                case '#':
                case '[':
                case ']':
                case '@':
                case '!':
                case '$':
                case '&':
                case '\'':
                case '(':
                case ')':
                case '*':
                case '+':
                case ',':
                case ';':
                case '=':
                case '%':
                    continue;
                default:
                    return false;
            }
        }
        return true;
    }

    private static bool IsHex(char value) {
        return (value >= '0' && value <= '9')
               || (value >= 'A' && value <= 'F')
               || (value >= 'a' && value <= 'f');
    }

    private static int HexValue(char value) {
        if (value >= '0' && value <= '9') return value - '0';
        if (value >= 'A' && value <= 'F') return value - 'A' + 10;
        return value - 'a' + 10;
    }

    private static bool IsAiToken(string value) {
        return value.Length >= 2 && value.Length <= 4 && IsAllDigits(value);
    }

    private static bool IsAllDigits(string value) {
        if (value.Length == 0) return false;
        for (var i = 0; i < value.Length; i++) {
            if (value[i] < '0' || value[i] > '9') return false;
        }
        return true;
    }

    private static global::CodeGlyphX.Gs1DigitalLinkIssue Issue(
        global::CodeGlyphX.Gs1DigitalLinkIssueCode code,
        string? component,
        string message) {
        return new global::CodeGlyphX.Gs1DigitalLinkIssue(code, component, message);
    }

    private static global::CodeGlyphX.Gs1DigitalLinkValidationResult Result(
        global::CodeGlyphX.Gs1DigitalLinkUri? value,
        Gs1ValidationResult? elementValidation,
        ICollection<global::CodeGlyphX.Gs1DigitalLinkIssue> issues) {
        return new global::CodeGlyphX.Gs1DigitalLinkValidationResult(value, elementValidation, issues.ToArray());
    }
}
