using System;

namespace CodeGlyphX;

/// <summary>Shared identifier check-digit rules, independent of carrier check characters.</summary>
internal static class IdentifierChecks {
    internal static string WithCheck(string value, int length, Func<string, char> calculate) {
        if (value.Length != length - 1 && value.Length != length)
            throw new ArgumentException($"The identifier requires {length - 1} data digits or {length} characters including its check digit.", nameof(value));
        var body = value.Length == length ? value.Substring(0, length - 1) : value;
        RequireDigits(body);
        var check = calculate(body);
        if (value.Length == length && value[length - 1] != check)
            throw new ArgumentException("The identifier check digit is incorrect.", nameof(value));
        return body + check;
    }

    internal static void RequireDigits(string value) {
        foreach (var character in value)
            if ((uint)(character - '0') > 9) throw new ArgumentException("The identifier requires decimal digits.", nameof(value));
    }

    internal static char Gs1(string body) {
        var sum = 0;
        for (var i = body.Length - 1; i >= 0; i--)
            sum += (body[i] - '0') * (((body.Length - 1 - i) & 1) == 0 ? 3 : 1);
        return (char)('0' + (10 - sum % 10) % 10);
    }

    internal static char Pzn(string body, int initialWeight) {
        var sum = 0;
        for (var i = 0; i < body.Length; i++) sum += (body[i] - '0') * (i + initialWeight);
        var check = sum % 11;
        if (check == 10) throw new ArgumentException("The digit sequence cannot form a PZN because its check digit would be ten.", nameof(body));
        return (char)('0' + check);
    }

    internal static char DeutschePost(string body) {
        var sum = 0;
        for (var i = 0; i < body.Length; i++) sum += (body[i] - '0') * ((i & 1) == 0 ? 4 : 9);
        return (char)('0' + (10 - sum % 10) % 10);
    }

    internal static char Mod11(string body, int firstWeight) {
        var sum = 0;
        for (var i = 0; i < body.Length; i++) sum += (body[i] - '0') * (firstWeight - i);
        var check = (11 - sum % 11) % 11;
        return check == 10 ? 'X' : (char)('0' + check);
    }
}
