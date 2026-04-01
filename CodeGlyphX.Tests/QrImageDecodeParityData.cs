using System;
using System.Collections.Generic;
using CodeGlyphX.Payloads;
using CodeGlyphX.Rendering;
using CodeGlyphX.Rendering.Png;

namespace CodeGlyphX.Tests;

public sealed class QrImageDecodeParityCase {
    public QrImageDecodeParityCase(string name, QrPayloadData payload, Rgba32 foreground, Rgba32 background, Action<QrEasyOptions>? configure = null) {
        Name = name;
        Payload = payload;
        Foreground = foreground;
        Background = background;
        Configure = configure;
    }

    public string Name { get; }
    public QrPayloadData Payload { get; }
    public Rgba32 Foreground { get; }
    public Rgba32 Background { get; }
    public Action<QrEasyOptions>? Configure { get; }

    public override string ToString() => Name;
}

public static class QrImageDecodeParityData {
    public static IEnumerable<object[]> CleanGeneratedCases() {
        yield return Case(
            "url",
            QrPayloads.Url("https://example.com/qr-parity?src=net472&v=1"));

        yield return Case(
            "wifi",
            QrPayloads.Wifi("ParityNet", "P@ssw0rd!234", authType: "WPA"));

        yield return Case(
            "vcard",
            QrPayloads.Contact(
                QrContactOutputType.VCard3,
                firstname: "Ada",
                lastname: "Lovelace",
                mobilePhone: "+48123456789",
                email: "ada@example.com",
                org: "Parity Labs",
                orgTitle: "Engineer",
                website: "https://example.com/contact",
                note: "net472 parity sample"));

        yield return Case(
            "calendar",
            QrPayloads.CalendarEvent(
                subject: "Parity Review",
                description: "QR decode parity validation event",
                location: "Warsaw",
                start: new DateTime(2026, 04, 01, 9, 30, 0, DateTimeKind.Utc),
                end: new DateTime(2026, 04, 01, 10, 15, 0, DateTimeKind.Utc),
                allDayEvent: false,
                encoding: QrCalendarEncoding.ICalComplete));

        yield return Case(
            "sms",
            QrPayloads.Sms("+48123456789", "Parity message body"));

        yield return Case(
            "email",
            QrPayloads.Email("team@example.com", "Parity subject", "Parity body", QrMailEncoding.Mailto));

        yield return Case(
            "geo",
            QrPayloads.Geo("52.2297", "21.0122", QrGeolocationEncoding.Geo));

        yield return Case(
            "bitcoin",
            QrPayloads.BitcoinLike(
                QrBitcoinLikeType.Bitcoin,
                "bc1qxy2kgdygjrsqtzq2n0yrf2493p83kkfjhx0wlh",
                amount: 0.015625,
                label: "Parity Wallet",
                message: "Invoice 42"));

        yield return Case(
            "url-transparent-background",
            QrPayloads.Url("https://example.com/qr-parity-transparent"),
            foreground: new Rgba32(12, 12, 12, 255),
            background: new Rgba32(255, 255, 255, 0));

        yield return Case(
            "wifi-semitransparent-foreground",
            QrPayloads.Wifi("ParityOverlay", "OverlayPass!9", authType: "WPA"),
            foreground: new Rgba32(20, 20, 20, 208),
            background: new Rgba32(255, 255, 255, 0));

        yield return Case(
            "url-foreground-gradient",
            QrPayloads.Url("https://example.com/qr-parity-gradient"),
            configure: options => {
                options.Foreground = new Rgba32(12, 45, 110, 255);
                options.ForegroundGradient = new QrPngGradientOptions {
                    Type = QrPngGradientType.DiagonalDown,
                    StartColor = new Rgba32(206, 32, 255, 255),
                    EndColor = new Rgba32(0, 194, 255, 255)
                };
                options.ModuleShape = QrPngModuleShape.Rounded;
                options.ModuleScale = 0.94;
            });

        yield return Case(
            "email-background-pattern",
            QrPayloads.Email("design@example.com", "Gradient parity", "Background pattern parity"),
            configure: options => {
                options.BackgroundPattern = new QrPngBackgroundPatternOptions {
                    Type = QrPngBackgroundPatternType.Dots,
                    Color = new Rgba32(0, 122, 255, 28),
                    SizePx = 10,
                    ThicknessPx = 1,
                    SnapToModuleSize = true,
                    ModuleStep = 2
                };
                options.BackgroundSupersample = 2;
            });
    }

    public static byte[] RenderPng(QrImageDecodeParityCase testCase) {
        var options = new QrEasyOptions {
            ModuleSize = 14,
            QuietZone = 6,
            Foreground = testCase.Foreground,
            Background = testCase.Background
        };
        testCase.Configure?.Invoke(options);

        return QrCode.Render(testCase.Payload, OutputFormat.Png, options).Data;
    }

    private static object[] Case(string name, QrPayloadData payload, Rgba32? foreground = null, Rgba32? background = null, Action<QrEasyOptions>? configure = null) {
        return new object[] {
            new QrImageDecodeParityCase(
                name,
                payload,
                foreground ?? new Rgba32(0, 0, 0, 255),
                background ?? new Rgba32(255, 255, 255, 255),
                configure)
        };
    }
}
