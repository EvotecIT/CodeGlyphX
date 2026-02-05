---
title: Payload Helpers - CodeGlyphX
description: Payload helpers for WiFi, vCards, OTP, and more.
slug: payloads
collection: docs
layout: docs
meta.raw_html: true
---

{{< edit-link >}}

<h1>Payload Helpers</h1>
<p>CodeGlyphX includes built-in helpers for generating QR codes with structured payloads that mobile devices can interpret.</p>

<h2>WiFi Configuration</h2>
<pre class="code-block">using CodeGlyphX;
using CodeGlyphX.Payloads;

// WPA/WPA2 network
QR.Save(QrPayloads.Wifi("NetworkName", "Password123"), "wifi.png");

// Open network (no password)
QR.Save(QrPayloads.Wifi("PublicNetwork", "", "nopass"), "wifi-open.png");</pre>

<h2>Contact Cards</h2>
<pre class="code-block">// vCard format (widely supported)
QR.Save(QrPayloads.Contact(
    QrContactOutputType.VCard3,
    firstname: "Przemyslaw",
    lastname: "Klys",
    email: "contact@evotec.pl",
    phone: "+48123456789",
    website: "https://evotec.xyz",
    org: "Evotec Services"
), "contact.png");</pre>

<h2>OTP / 2FA</h2>
<pre class="code-block">// TOTP (Time-based One-Time Password)
QR.Save(QrPayloads.OneTimePassword(
    OtpAuthType.Totp,
    secretBase32: "JBSWY3DPEHPK3PXP",
    label: "user@example.com",
    issuer: "MyApp"
), "totp.png");</pre>

<h2>SEPA Girocode</h2>
<pre class="code-block">QR.Save(QrPayloads.Girocode(
    iban: "DE89370400440532013000",
    bic: "COBADEFFXXX",
    name: "Evotec Services",
    amount: 99.99m,
    remittanceInformation: "Invoice-2024-001"
), "sepa.png");</pre>

<p>Additional helpers include PayPal.Me, UPI, BezahlCode variants, Swiss QR Bill, Slovenian UPN, Russia payment order, crypto URIs, and social/app store links. See the API reference for the full list.</p>
<p>Auto-detect helper: <code>QrPayloads.Detect("...")</code> builds the best-known payload for mixed inputs.</p>
