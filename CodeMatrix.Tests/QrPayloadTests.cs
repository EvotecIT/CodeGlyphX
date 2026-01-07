using System;
using CodeMatrix.Payloads;
using Xunit;

namespace CodeMatrix.Tests;

public sealed class QrPayloadTests {
    [Fact]
    public void Geo_Uses_Invariant_Format() {
        var payload = QrPayload.Geo(47.6205, -122.3493);
        Assert.Equal("geo:47.6205,-122.3493", payload);
    }

    [Fact]
    public void MeCard_Encodes_And_Escapes() {
        var payload = QrPayload.MeCard(
            "John",
            "Doe",
            phone: "+123",
            email: "john@example.com",
            url: "https://example.com",
            address: "123; Main",
            note: "Hello",
            organization: "ACME");

        Assert.Equal(
            "MECARD:N:Doe,John;TEL:+123;EMAIL:john@example.com;URL:https\\://example.com;ADR:123\\; Main;NOTE:Hello;ORG:ACME;;",
            payload);
    }

    [Fact]
    public void CalendarEvent_Formats_Zulu_Time() {
        var start = new DateTime(2026, 1, 2, 10, 30, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 1, 2, 11, 0, 0, DateTimeKind.Utc);

        var payload = QrPayload.CalendarEvent(
            "Meet",
            start,
            end,
            location: "Room 1",
            description: "Plan",
            organizer: "alice@example.com",
            uid: "evt-1");

        var expected = "BEGIN:VCALENDAR\r\n" +
                       "VERSION:2.0\r\n" +
                       "BEGIN:VEVENT\r\n" +
                       "UID:evt-1\r\n" +
                       "SUMMARY:Meet\r\n" +
                       "DTSTART:20260102T103000Z\r\n" +
                       "DTEND:20260102T110000Z\r\n" +
                       "LOCATION:Room 1\r\n" +
                       "DESCRIPTION:Plan\r\n" +
                       "ORGANIZER:alice@example.com\r\n" +
                       "END:VEVENT\r\n" +
                       "END:VCALENDAR";

        Assert.Equal(expected, payload);
    }

    [Fact]
    public void CalendarEvent_AllDay_Uses_Date_Only() {
        var start = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Unspecified);
        var end = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Unspecified);

        var payload = QrPayload.CalendarEvent("Holiday", start, end, allDay: true);
        Assert.Contains("DTSTART:20260102", payload, StringComparison.Ordinal);
        Assert.Contains("DTEND:20260103", payload, StringComparison.Ordinal);
    }

    [Fact]
    public void CalendarEvent_Uses_Tzid_When_Provided() {
        var start = new DateTime(2026, 1, 2, 10, 30, 0, DateTimeKind.Unspecified);
        var end = new DateTime(2026, 1, 2, 11, 0, 0, DateTimeKind.Unspecified);

        var payload = QrPayload.CalendarEvent(
            "Meet",
            start,
            end,
            timeZoneId: "America/Los_Angeles");

        Assert.Contains("DTSTART;TZID=America/Los_Angeles:20260102T103000", payload, StringComparison.Ordinal);
        Assert.Contains("DTEND;TZID=America/Los_Angeles:20260102T110000", payload, StringComparison.Ordinal);
        Assert.DoesNotContain("DTSTART;TZID=America/Los_Angeles:20260102T103000Z", payload, StringComparison.Ordinal);
    }

    [Fact]
    public void CalendarEvent_Folds_Long_Lines() {
        var summary = new string('A', 100);
        var payload = QrPayload.CalendarEvent("X" + summary, DateTime.UtcNow);

        Assert.Contains("\r\n ", payload, StringComparison.Ordinal);
        Assert.Contains("SUMMARY:", payload, StringComparison.Ordinal);
    }

    [Fact]
    public void CalendarEvent_Adds_Alarm() {
        var start = new DateTime(2026, 1, 2, 10, 30, 0, DateTimeKind.Utc);
        var payload = QrPayload.CalendarEvent(
            "Meet",
            start,
            alarmMinutesBefore: 15,
            alarmDescription: "Ping");

        Assert.Contains("BEGIN:VALARM", payload, StringComparison.Ordinal);
        Assert.Contains("TRIGGER:-PT15M", payload, StringComparison.Ordinal);
        Assert.Contains("DESCRIPTION:Ping", payload, StringComparison.Ordinal);
        Assert.Contains("END:VALARM", payload, StringComparison.Ordinal);
    }

    [Fact]
    public void VCard4_Emits_Multiple_Fields_And_Escapes() {
        var payload = QrPayload.VCard4(
            "John",
            "Doe",
            phones: new[] { "+123", "+456" },
            emails: new[] { "john@example.com", "alt@example.com" },
            organization: "ACME",
            title: "Lead;Dev",
            url: "https://example.com",
            address: ";;123 Main;Seattle;WA;98101;USA",
            note: "Line1\nLine2",
            birthday: "1980-01-02",
            photoUri: "https://example.com/photo.jpg",
            logoUri: "https://example.com/logo.png");

        var expected = "BEGIN:VCARD\r\n" +
                       "VERSION:4.0\r\n" +
                       "N:Doe;John;;;\r\n" +
                       "FN:John Doe\r\n" +
                       "TEL:+123\r\n" +
                       "TEL:+456\r\n" +
                       "EMAIL:john@example.com\r\n" +
                       "EMAIL:alt@example.com\r\n" +
                       "ORG:ACME\r\n" +
                       "TITLE:Lead\\;Dev\r\n" +
                       "URL:https://example.com\r\n" +
                       "ADR:;;123 Main;Seattle;WA;98101;USA\r\n" +
                       "NOTE:Line1\\nLine2\r\n" +
                       "BDAY:1980-01-02\r\n" +
                       "PHOTO:https://example.com/photo.jpg\r\n" +
                       "LOGO:https://example.com/logo.png\r\n" +
                       "END:VCARD";

        Assert.Equal(expected, payload);
    }
}
