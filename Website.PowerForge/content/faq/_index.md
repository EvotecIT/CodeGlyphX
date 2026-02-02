---
title: FAQ - CodeGlyphX
description: Common questions about CodeGlyphX and QR/barcode generation in .NET.
content_engine: theme
render: html
---

<div class="faq-page">
    <div class="faq-hero">
        <span class="section-label">Support</span>
        <h1>{{ data.faq.title }}</h1>
        <p>{{ data.faq.description }}</p>
    </div>

    <div class="faq-content">
        {{ for section in data.faq.sections }}
        <div class="faq-section">
            <h2>{{ section.title }}</h2>
            {{ for item in section.items }}
            <div class="faq-item" id="{{ item.id }}">
                <h3>{{ item.question }}</h3>
                {{ item.answer }}
            </div>
            {{ end }}
        </div>
        {{ end }}
    </div>
</div>
