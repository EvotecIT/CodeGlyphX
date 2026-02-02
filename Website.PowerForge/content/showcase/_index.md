---
title: Showcase - CodeGlyphX
description: See CodeGlyphX in action powering real-world applications.
content_engine: theme
render: html
---

<div class="showcase-page">
    <div class="showcase-hero">
        <span class="section-label">Built with CodeGlyphX</span>
        <h1>{{ data.showcase.title }}</h1>
        <p>{{ data.showcase.description }}</p>
    </div>

    <div class="showcase-grid">
        {{ for item in data.showcase.items }}
        <div class="showcase-card showcase-card-large">
            <div class="showcase-header">
                <div class="showcase-icon">
                    {{ if item.icon == "info" }}
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>
                    {{ else if item.icon == "lock" }}
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"/></svg>
                    {{ end }}
                </div>
                <div class="showcase-title">
                    <h2>{{ item.name }}</h2>
                    <span class="showcase-badge">{{ item.badge }}</span>
                </div>
            </div>

            <div class="showcase-meta">
                {{ if item.license }}
                <span class="showcase-license">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/></svg>
                    {{ item.license }}
                </span>
                {{ end }}
                {{ for tech in item.tech }}
                <span class="showcase-tech">{{ tech }}</span>
                {{ end }}
            </div>

            <p class="showcase-description">{{ item.description }}</p>

            <div class="showcase-details">
                <h4>Key Features</h4>
                <ul>
                    {{ for feature in item.features }}
                    <li>{{ feature }}</li>
                    {{ end }}
                </ul>
            </div>

            <div class="showcase-features">
                {{ for highlight in item.highlights }}
                <span class="showcase-feature">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="16" height="16"><path d="M5 13l4 4L19 7"/></svg>
                    {{ highlight }}
                </span>
                {{ end }}
            </div>

            <div class="showcase-gallery" data-carousel="carousel-{{ item.id }}">
                <div class="showcase-gallery-tabs">
                    <button class="showcase-gallery-tab active" data-theme="dark">Dark Theme</button>
                    <button class="showcase-gallery-tab" data-theme="light">Light Theme</button>
                </div>

                <div class="showcase-carousel">
                    <div class="showcase-carousel-viewport">
                        {{ for image in item.images.dark }}
                        <div class="showcase-carousel-slide {{ if for.index == 0 }}active{{ end }}" data-theme="dark" data-index="{{ for.index }}">
                            <img src="{{ image.src }}" alt="{{ image.alt }}" loading="lazy" />
                        </div>
                        {{ end }}

                        {{ for image in item.images.light }}
                        <div class="showcase-carousel-slide" data-theme="light" data-index="{{ for.index }}" style="display:none">
                            <img src="{{ image.src }}" alt="{{ image.alt }}" loading="lazy" />
                        </div>
                        {{ end }}

                        <button class="showcase-carousel-nav prev" aria-label="Previous image">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M15 19l-7-7 7-7"/></svg>
                        </button>
                        <button class="showcase-carousel-nav next" aria-label="Next image">
                            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M9 5l7 7-7 7"/></svg>
                        </button>
                    </div>

                    <div class="showcase-carousel-footer">
                        <span class="showcase-carousel-caption">{{ item.images.dark[0].caption }}</span>
                        <div class="showcase-carousel-dots">
                            {{ for image in item.images.dark }}
                            <button class="showcase-carousel-dot {{ if for.index == 0 }}active{{ end }}" data-index="{{ for.index }}" aria-label="Go to slide {{ for.index + 1 }}"></button>
                            {{ end }}
                        </div>
                        <span class="showcase-carousel-counter">1 / {{ item.images.dark.size }}</span>
                    </div>

                    <div class="showcase-carousel-thumbs" data-theme-container="dark">
                        {{ for image in item.images.dark }}
                        <button class="showcase-carousel-thumb {{ if for.index == 0 }}active{{ end }}" data-index="{{ for.index }}" aria-label="{{ image.caption }}">
                            <img src="{{ image.src }}" alt="" loading="lazy" />
                        </button>
                        {{ end }}
                    </div>
                    <div class="showcase-carousel-thumbs" data-theme-container="light" style="display:none">
                        {{ for image in item.images.light }}
                        <button class="showcase-carousel-thumb {{ if for.index == 0 }}active{{ end }}" data-index="{{ for.index }}" aria-label="{{ image.caption }}">
                            <img src="{{ image.src }}" alt="" loading="lazy" />
                        </button>
                        {{ end }}
                    </div>
                </div>
            </div>

            <script type="application/json" class="carousel-data" data-carousel="carousel-{{ item.id }}">
            {
              "dark": [{{ for image in item.images.dark }}{{ if for.index > 0 }},{{ end }}"{{ image.caption }}"{{ end }}],
              "light": [{{ for image in item.images.light }}{{ if for.index > 0 }},{{ end }}"{{ image.caption }}"{{ end }}]
            }
            </script>

            <div class="showcase-actions">
                {{ if item.github }}
                <a href="{{ item.github }}" target="_blank" rel="noopener" class="btn btn-secondary">
                    <svg viewBox="0 0 24 24" fill="currentColor" class="btn-icon"><path d="M12 0c-6.626 0-12 5.373-12 12 0 5.302 3.438 9.8 8.207 11.387.599.111.793-.261.793-.577v-2.234c-3.338.726-4.033-1.416-4.033-1.416-.546-1.387-1.333-1.756-1.333-1.756-1.089-.745.083-.729.083-.729 1.205.084 1.839 1.237 1.839 1.237 1.07 1.834 2.807 1.304 3.492.997.107-.775.418-1.305.762-1.604-2.665-.305-5.467-1.334-5.467-5.931 0-1.311.469-2.381 1.236-3.221-.124-.303-.535-1.524.117-3.176 0 0 1.008-.322 3.301 1.23.957-.266 1.983-.399 3.003-.404 1.02.005 2.047.138 3.006.404 2.291-1.552 3.297-1.23 3.297-1.23.653 1.653.242 2.874.118 3.176.77.84 1.235 1.911 1.235 3.221 0 4.609-2.807 5.624-5.479 5.921.43.372.823 1.102.823 2.222v3.293c0 .319.192.694.801.576 4.765-1.589 8.199-6.086 8.199-11.386 0-6.627-5.373-12-12-12z"/></svg>
                    View on GitHub
                </a>
                {{ end }}
                {{ if item.download }}
                <a href="{{ item.download }}" target="_blank" rel="noopener" class="btn btn-outline">
                    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="btn-icon"><path d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4"/></svg>
                    Download
                </a>
                {{ end }}
                {{ if item.status == "released" }}
                <span class="showcase-status"><span class="status-dot" style="background: var(--success);"></span>Released</span>
                {{ else }}
                <span class="showcase-status"><span class="status-dot"></span>In Development</span>
                {{ end }}
            </div>
        </div>
        {{ end }}
    </div>

    <div class="showcase-submit">
        <div class="showcase-submit-content">
            <h3>{{ data.showcase.submit.title }}</h3>
            <p>{{ data.showcase.submit.description }}</p>
            <a href="{{ data.showcase.submit.link }}" target="_blank" rel="noopener" class="btn btn-primary">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="18" height="18"><path d="M12 5v14M5 12h14"/></svg>
                Submit Your Project
            </a>
        </div>
    </div>
</div>
