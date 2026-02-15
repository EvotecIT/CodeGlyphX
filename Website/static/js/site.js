// Theme management functions for Blazor interop
function getTheme() {
    return localStorage.getItem('theme') || 'dark';
}

function setTheme(theme) {
    localStorage.setItem('theme', theme);
    document.documentElement.dataset.theme = theme;
    globalThis.CodeGlyphX?.renderMermaidDiagrams?.();
}

// Expose to global scope for Blazor JS interop
globalThis.getTheme = getTheme;
globalThis.setTheme = setTheme;

(() => {
    const mermaidScriptUrl = 'https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.min.js';
    let mermaidLoadPromise = null;
    let mermaidThemeObserver = null;

    function copyText(text) {
        if (!text) return;
        if (navigator.clipboard?.writeText) {
            navigator.clipboard.writeText(text).catch(() => { });
            return;
        }
        const textarea = document.createElement('textarea');
        textarea.value = text;
        textarea.setAttribute('readonly', 'readonly');
        textarea.style.position = 'absolute';
        textarea.style.left = '-9999px';
        document.body.appendChild(textarea);
        textarea.select();
        try {
            document.execCommand('copy'); // Fallback for older browsers
        } catch {
            // ignore
        }
        textarea.remove();
    }

    function loadPrism(callback) {
        if (globalThis.__codeGlyphPrismLoaded) {
            callback?.();
            return;
        }
        const css = document.createElement('link');
        css.rel = 'stylesheet';
        css.href = '/css/prism-theme.css';
        document.head.appendChild(css);

        const loadScript = (src, done) => {
            const el = document.createElement('script');
            el.src = src;
            el.onload = () => done?.();
            document.head.appendChild(el);
        };

        const script = document.createElement('script');
        script.src = '/vendor/prism/prism.min.js';
        script.onload = () => {
            loadScript('/vendor/prism/prism-csharp.min.js', () => {
                // VB.NET grammar depends on the BASIC grammar.
                loadScript('/vendor/prism/prism-basic.min.js', () => {
                    loadScript('/vendor/prism/prism-vbnet.min.js', () => {
                        globalThis.__codeGlyphPrismLoaded = true;
                        callback?.();
                    });
                });
            });
        };
        document.head.appendChild(script);
    }

    function loadDynamicScript(src) {
        return new Promise((resolve) => {
            const existing = document.querySelector(`script[data-dynamic-src="${src}"]`);
            if (existing) {
                if (existing.getAttribute('data-loaded') === 'true') {
                    resolve(true);
                    return;
                }
                existing.addEventListener('load', () => resolve(true), { once: true });
                existing.addEventListener('error', () => resolve(false), { once: true });
                return;
            }

            const script = document.createElement('script');
            script.src = src;
            script.async = true;
            script.defer = true;
            script.setAttribute('data-dynamic-src', src);
            script.addEventListener('load', () => {
                script.setAttribute('data-loaded', 'true');
                resolve(true);
            }, { once: true });
            script.addEventListener('error', () => resolve(false), { once: true });
            document.head.appendChild(script);
        });
    }

    function resolveThemeKind() {
        const theme = document.documentElement.dataset.theme || localStorage.getItem('theme') || 'auto';
        if (theme === 'light') return 'light';
        if (theme === 'dark') return 'dark';
        return window.matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark';
    }

    function getCssVar(name, fallback) {
        const value = getComputedStyle(document.documentElement).getPropertyValue(name);
        if (!value) return fallback;
        const trimmed = value.trim();
        return trimmed || fallback;
    }

    function getMermaidConfig() {
        const isLight = resolveThemeKind() === 'light';
        return {
            startOnLoad: false,
            securityLevel: 'strict',
            theme: 'base',
            flowchart: { curve: 'basis' },
            themeVariables: {
                background: getCssVar('--bg-code', isLight ? '#f6f8fa' : '#0d0d12'),
                primaryColor: getCssVar('--bg-card', isLight ? '#ffffff' : '#12121a'),
                primaryTextColor: getCssVar('--text', isLight ? '#1e293b' : '#f4f4f5'),
                primaryBorderColor: getCssVar('--primary', isLight ? '#6d28d9' : '#8b5cf6'),
                lineColor: getCssVar('--text-muted', isLight ? '#475569' : '#c1c7d6'),
                secondaryColor: getCssVar('--bg-input', isLight ? '#f1f5f9' : '#1e1e2a'),
                tertiaryColor: getCssVar('--bg-card-hover', isLight ? '#e2e8f0' : '#1a1a25'),
                clusterBkg: getCssVar('--bg-input', isLight ? '#f1f5f9' : '#1e1e2a'),
                clusterBorder: getCssVar('--border', isLight ? '#e2e8f0' : '#27272a'),
                fontFamily: getCssVar('--font-body', 'Inter, Segoe UI, sans-serif'),
                fontSize: '15px'
            }
        };
    }

    function ensureMermaidLoaded() {
        if (window.mermaid) {
            return Promise.resolve(true);
        }
        if (!mermaidLoadPromise) {
            mermaidLoadPromise = loadDynamicScript(mermaidScriptUrl);
        }
        return mermaidLoadPromise;
    }

    function normalizeMermaidBlocks() {
        const blocks = Array.from(document.querySelectorAll('pre code.language-mermaid'));
        blocks.forEach((code) => {
            const pre = code.closest('pre');
            if (!pre) return;
            if (pre.parentElement?.classList.contains('mermaid-diagram')) return;
            const source = (code.textContent || '').trim();
            if (!source) return;

            const host = document.createElement('div');
            host.className = 'mermaid-diagram';
            host.setAttribute('data-mermaid-source', source);
            pre.replaceWith(host);
        });
    }

    async function renderMermaidDiagrams() {
        normalizeMermaidBlocks();
        const hosts = Array.from(document.querySelectorAll('.mermaid-diagram[data-mermaid-source]'));
        if (!hosts.length) return;

        const loaded = await ensureMermaidLoaded();
        if (!loaded || !window.mermaid) return;

        window.mermaid.initialize(getMermaidConfig());
        const nodes = [];
        hosts.forEach((host) => {
            const source = host.getAttribute('data-mermaid-source') || '';
            host.classList.remove('mermaid-diagram-failed');
            host.innerHTML = '';

            const diagram = document.createElement('div');
            diagram.className = 'mermaid';
            diagram.textContent = source;
            host.appendChild(diagram);
            nodes.push(diagram);
        });

        try {
            await window.mermaid.run({ nodes });
        } catch (err) {
            console.warn('Mermaid render failed', err);
            hosts.forEach((host) => {
                if (host.querySelector('svg')) return;
                host.classList.add('mermaid-diagram-failed');
                const fallback = document.createElement('pre');
                const code = document.createElement('code');
                code.className = 'language-mermaid';
                code.textContent = host.getAttribute('data-mermaid-source') || '';
                fallback.appendChild(code);
                host.innerHTML = '';
                host.appendChild(fallback);
            });
        }
    }

    function createCopyButton(textProvider, options) {
        const btn = document.createElement('button');
        btn.type = 'button';
        btn.className = options?.className || 'copy-btn';
        btn.title = options?.title || 'Copy to clipboard';
        btn.setAttribute('aria-label', options?.title || 'Copy to clipboard');
        btn.innerHTML = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="9" y="9" width="13" height="13" rx="2"/><path d="M5 15H4a2 2 0 01-2-2V4a2 2 0 012-2h9a2 2 0 012 2v1"/></svg>';
        btn.addEventListener('click', () => {
            const code = textProvider() || '';
            copyText(code);
            btn.innerHTML = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M20 6L9 17l-5-5"/></svg>';
            btn.classList.add('copied');
            setTimeout(() => {
                btn.innerHTML = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="9" y="9" width="13" height="13" rx="2"/><path d="M5 15H4a2 2 0 01-2-2V4a2 2 0 012-2h9a2 2 0 012 2v1"/></svg>';
                btn.classList.remove('copied');
            }, 2000);
        });
        return btn;
    }

    function addCopyButton(block) {
        if (block.querySelector('.copy-btn')) return;
        block.classList.add('copyable');
        block.style.position = 'relative';
        const codeEl = block.querySelector('code');
        const rawText = (codeEl ? codeEl.textContent : block.textContent) || '';
        const btn = createCopyButton(() => rawText);
        block.appendChild(btn);
    }

    function initCodeBlocks() {
        let defaultCodeLanguage = document.body?.dataset?.codeLanguage
            || document.documentElement?.dataset?.codeLanguage
            || 'language-csharp';
        if (defaultCodeLanguage && !defaultCodeLanguage.startsWith('language-')) {
            defaultCodeLanguage = `language-${defaultCodeLanguage}`;
        }
        const blocks = Array.from(document.querySelectorAll('pre.code-block, .docs-content pre, .docs-static pre, .type-detail pre'))
            .filter((block) => !block.classList.contains('initialized'))
            .filter((block) => !block.querySelector('code.language-mermaid'));
        const signatures = Array.from(document.querySelectorAll('code.signature'))
            .filter((sig) => !sig.classList.contains('initialized'));

        if (!blocks.length && !signatures.length) return;

        // Separate blocks that need highlighting from those that don't
        const blocksNeedingHighlight = [];
        const blocksReady = [];

        blocks.forEach((block) => {
            block.classList.add('initialized');
            if (!block.classList.contains('code-block')) {
                block.classList.add('code-block');
            }
            let codeEl = block.querySelector('code');
            if (!codeEl) {
                if (block.childElementCount === 0) {
                    const wrapper = document.createElement('code');
                    wrapper.textContent = block.textContent || '';
                    block.textContent = '';
                    block.appendChild(wrapper);
                    codeEl = wrapper;
                } else {
                    blocksReady.push({ block, codeEl: null });
                    return;
                }
            }
            const hasHighlight = block.querySelector('.token, .keyword, .string, .comment');
            if (codeEl && !hasHighlight) {
                blocksNeedingHighlight.push({ block, codeEl });
            } else {
                blocksReady.push({ block, codeEl });
            }
        });

        // Add copy buttons to blocks that don't need highlighting
        blocksReady.forEach(({ block }) => addCopyButton(block));

        signatures.forEach((sig) => {
            sig.classList.add('initialized', 'copyable');
            sig.style.position = 'relative';
            sig.style.paddingRight = '2.75rem';
            const rawText = sig.textContent || '';
            const btn = createCopyButton(() => rawText, { className: 'copy-btn copy-btn-inline' });
            sig.appendChild(btn);
        });

        // Highlight blocks first, then add copy buttons
        if (blocksNeedingHighlight.length) {
            loadPrism(() => {
                blocksNeedingHighlight.forEach(({ block, codeEl }) => {
                    if (!codeEl) {
                        addCopyButton(block);
                        return;
                    }
                    if (!codeEl.classList.contains('prism-highlighted')) {
                        const codeLanguage = Array.from(codeEl.classList).find((cls) => cls.startsWith('language-'));
                        const blockLanguage = Array.from(block.classList).find((cls) => cls.startsWith('language-'));
                        const targetLanguage = codeLanguage || blockLanguage || defaultCodeLanguage;
                        if (!codeLanguage) {
                            codeEl.classList.add(targetLanguage);
                        }
                        codeEl.classList.add('prism-highlighted');
                        globalThis.Prism?.highlightElement?.(codeEl);
                    }
                    // Add copy button AFTER highlighting
                    addCopyButton(block);
                });
            });
        }
    }

    let benchSummaryPromise = null;
    function loadBenchmarkSummary() {
        if (!benchSummaryPromise) {
            benchSummaryPromise = fetch('/data/benchmark-summary.json', { cache: 'no-store' })
                .then((res) => (res.ok ? res.json() : null))
                .catch(() => null);
        }
        return benchSummaryPromise;
    }

    function pickBenchmarkSummary(data) {
        if (!data) return null;
        const order = [
            ['windows', 'quick'],
            ['windows', 'full'],
            ['linux', 'quick'],
            ['linux', 'full'],
            ['macos', 'quick'],
            ['macos', 'full'],
        ];
        for (const [os, mode] of order) {
            const entry = data?.[os]?.[mode];
            if (entry?.summary?.length) return entry;
        }
        for (const osKey of Object.keys(data)) {
            const osEntry = data[osKey];
            for (const modeKey of Object.keys(osEntry || {})) {
                const entry = osEntry?.[modeKey];
                if (entry?.summary?.length) return entry;
            }
        }
        return null;
    }

    function appendVendorCell(td, vendor, delta) {
        if (!vendor) return;
        if (vendor.mean) {
            const mean = document.createElement('div');
            mean.textContent = vendor.mean;
            td.appendChild(mean);
        }
        if (vendor.allocated) {
            const alloc = document.createElement('div');
            alloc.className = 'bench-dim';
            alloc.textContent = vendor.allocated;
            td.appendChild(alloc);
        }
        if (delta) {
            const d = document.createElement('div');
            d.className = 'bench-delta';
            d.textContent = delta;
            td.appendChild(d);
        }
    }

    function renderBenchmarkSummary() {
        const container = document.querySelector('[data-benchmark-summary]');
        if (!container || container.dataset.loaded === 'true') return;

        loadBenchmarkSummary().then((data) => {
            const entry = pickBenchmarkSummary(data);
            if (!entry || !entry.summary?.length) {
                container.textContent = 'Benchmark summary unavailable.';
                container.dataset.loaded = 'true';
                return;
            }

            const table = document.createElement('table');
            table.className = 'bench-table bench-summary-table';

            const thead = document.createElement('thead');
            thead.innerHTML = '<tr>' +
                '<th>Benchmark</th>' +
                '<th>Scenario</th>' +
                '<th>Fastest</th>' +
                '<th>CodeGlyphX</th>' +
                '<th>ZXing.Net</th>' +
                '<th>QRCoder</th>' +
                '<th>Barcoder</th>' +
                '<th>CodeGlyphX vs Fastest</th>' +
                '<th>Alloc vs Fastest</th>' +
                '<th>Rating</th>' +
                '</tr>';
            table.appendChild(thead);

            const tbody = document.createElement('tbody');
            entry.summary.forEach((item) => {
                const row = document.createElement('tr');
                const vendors = item.vendors || {};
                const deltas = item.deltas || {};

                const cells = [
                    item.benchmark || '',
                    item.scenario || '',
                    item.fastestVendor ? `${item.fastestVendor} ${item.fastestMean || ''}`.trim() : (item.fastestMean || ''),
                ];

                cells.forEach((text) => {
                    const td = document.createElement('td');
                    td.textContent = text;
                    row.appendChild(td);
                });

                const cgxTd = document.createElement('td');
                appendVendorCell(cgxTd, vendors['CodeGlyphX'], '');
                row.appendChild(cgxTd);

                const zxTd = document.createElement('td');
                appendVendorCell(zxTd, vendors['ZXing.Net'], deltas['ZXing.Net']);
                row.appendChild(zxTd);

                const qrcTd = document.createElement('td');
                appendVendorCell(qrcTd, vendors['QRCoder'], deltas['QRCoder']);
                row.appendChild(qrcTd);

                const barTd = document.createElement('td');
                appendVendorCell(barTd, vendors['Barcoder'], deltas['Barcoder']);
                row.appendChild(barTd);

                const ratioTd = document.createElement('td');
                ratioTd.textContent = item.codeGlyphXVsFastestText || '';
                row.appendChild(ratioTd);

                const allocTd = document.createElement('td');
                allocTd.textContent = item.codeGlyphXAllocVsFastestText || '';
                row.appendChild(allocTd);

                const ratingTd = document.createElement('td');
                ratingTd.textContent = item.rating || '';
                row.appendChild(ratingTd);

                tbody.appendChild(row);
            });
            table.appendChild(tbody);

            container.innerHTML = '';
            container.appendChild(table);
            container.dataset.loaded = 'true';
        });
    }

    function initDocsNav() {
        const sidebar = document.querySelector('.docs-sidebar');
        const toggle = document.querySelector('.docs-sidebar-toggle');
        const overlay = document.querySelector('.docs-sidebar-overlay');

        if (toggle && sidebar && !toggle.dataset.bound) {
            toggle.dataset.bound = 'true';
            toggle.addEventListener('click', () => {
                sidebar.classList.toggle('sidebar-open');
                overlay?.classList.toggle('active');
            });
        }

        if (overlay && sidebar && !overlay.dataset.bound) {
            overlay.dataset.bound = 'true';
            overlay.addEventListener('click', () => {
                sidebar.classList.remove('sidebar-open');
                overlay.classList.remove('active');
            });
        }

        const links = Array.from(document.querySelectorAll('.docs-nav a'));
        if (!links.length) return;

        const normalizePath = (value) => {
            if (!value) return '/';
            const clean = value.split('#')[0].split('?')[0];
            return clean.endsWith('/') ? clean : `${clean}/`;
        };

        const currentPath = normalizePath(window.location.pathname);
        links.forEach((link) => {
            const href = link.getAttribute('href');
            if (!href || href.startsWith('http')) return;
            const linkPath = normalizePath(href);
            link.classList.toggle('active', linkPath === currentPath);
        });
    }

    function initShowcaseCarousel() {
        const galleries = Array.from(document.querySelectorAll('.showcase-gallery'))
            .filter((gallery) => gallery.dataset.bound !== 'true');

        galleries.forEach((gallery) => {
            gallery.dataset.bound = 'true';
            const carouselId = gallery.dataset.carousel;
            const dataScript = document.querySelector(`script.carousel-data[data-carousel="${carouselId}"]`);
            if (!dataScript) return;

            let captions = null;
            try {
                captions = JSON.parse(dataScript.textContent || '{}');
            } catch {
                return;
            }

            const themeTabs = Array.from(gallery.querySelectorAll('.showcase-gallery-tab'));
            const slides = Array.from(gallery.querySelectorAll('.showcase-carousel-slide'));
            const prevBtn = gallery.querySelector('.showcase-carousel-nav.prev');
            const nextBtn = gallery.querySelector('.showcase-carousel-nav.next');
            const dots = Array.from(gallery.querySelectorAll('.showcase-carousel-dot'));
            const thumbContainers = Array.from(gallery.querySelectorAll('.showcase-carousel-thumbs'));
            const captionEl = gallery.querySelector('.showcase-carousel-caption');
            const counterEl = gallery.querySelector('.showcase-carousel-counter');

            if (!themeTabs.length || !slides.length) return;

            let currentTheme = themeTabs.find((tab) => tab.classList.contains('active'))?.dataset.theme
                || themeTabs[0]?.dataset.theme
                || 'dark';
            let currentSlide = 0;

            const updateCarousel = () => {
                const themeCaptions = captions?.[currentTheme] || [];
                const totalSlides = themeCaptions.length || 0;

                slides.forEach((slide) => {
                    const isCurrentTheme = slide.dataset.theme === currentTheme;
                    const isCurrentSlide = Number.parseInt(slide.dataset.index || '0', 10) === currentSlide;
                    slide.style.display = isCurrentTheme ? '' : 'none';
                    slide.classList.toggle('active', isCurrentTheme && isCurrentSlide);
                });

                dots.forEach((dot, idx) => {
                    dot.classList.toggle('active', idx === currentSlide);
                });

                thumbContainers.forEach((container) => {
                    const isCurrentTheme = container.dataset.themeContainer === currentTheme;
                    container.style.display = isCurrentTheme ? '' : 'none';
                    if (isCurrentTheme) {
                        Array.from(container.querySelectorAll('.showcase-carousel-thumb')).forEach((thumb, idx) => {
                            thumb.classList.toggle('active', idx === currentSlide);
                        });
                    }
                });

                if (captionEl && totalSlides > 0) {
                    captionEl.textContent = themeCaptions[currentSlide] || '';
                }
                if (counterEl && totalSlides > 0) {
                    counterEl.textContent = `${currentSlide + 1} / ${totalSlides}`;
                }
            };

            const goToSlide = (index) => {
                const totalSlides = (captions?.[currentTheme] || []).length || 0;
                if (!totalSlides) return;
                currentSlide = ((index % totalSlides) + totalSlides) % totalSlides;
                updateCarousel();
            };

            themeTabs.forEach((tab) => {
                tab.addEventListener('click', () => {
                    currentTheme = tab.dataset.theme || currentTheme;
                    currentSlide = 0;
                    themeTabs.forEach((t) => t.classList.remove('active'));
                    tab.classList.add('active');
                    updateCarousel();
                });
            });

            if (prevBtn) prevBtn.addEventListener('click', () => goToSlide(currentSlide - 1));
            if (nextBtn) nextBtn.addEventListener('click', () => goToSlide(currentSlide + 1));

            dots.forEach((dot) => {
                dot.addEventListener('click', () => {
                    const idx = Number.parseInt(dot.dataset.index || '0', 10);
                    goToSlide(idx);
                });
            });

            thumbContainers.forEach((container) => {
                Array.from(container.querySelectorAll('.showcase-carousel-thumb')).forEach((thumb) => {
                    thumb.addEventListener('click', () => {
                        const idx = Number.parseInt(thumb.dataset.index || '0', 10);
                        goToSlide(idx);
                    });
                });
            });

            updateCarousel();
        });
    }

    document.addEventListener('click', (event) => {
        const target = event.target;
        if (!(target instanceof Element)) return;
        const button = target.closest('[data-copy]');
        if (!button) return;
        const text = button.dataset.copy;
        if (!text) return;
        copyText(text);
    });

    let initTimer = 0;
    function scheduleInit() {
        if (initTimer) return;
        initTimer = globalThis.setTimeout(() => {
            initTimer = 0;
            renderMermaidDiagrams();
            initCodeBlocks();
            initDocsNav();
            initShowcaseCarousel();
            renderBenchmarkSummary();
        }, 100);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', scheduleInit);
    } else {
        scheduleInit();
    }

    const observer = new MutationObserver(scheduleInit);
    observer.observe(document.body, { childList: true, subtree: true });

    if (!mermaidThemeObserver) {
        mermaidThemeObserver = new MutationObserver((mutations) => {
            mutations.forEach((mutation) => {
                if (mutation.type === 'attributes' && mutation.attributeName === 'data-theme') {
                    renderMermaidDiagrams();
                }
            });
        });
        mermaidThemeObserver.observe(document.documentElement, {
            attributes: true,
            attributeFilter: ['data-theme']
        });
    }

    globalThis.CodeGlyphX = globalThis.CodeGlyphX || {};
    globalThis.CodeGlyphX.initCodeBlocks = initCodeBlocks;
    globalThis.CodeGlyphX.renderBenchmarkSummary = renderBenchmarkSummary;
    globalThis.CodeGlyphX.renderMermaidDiagrams = renderMermaidDiagrams;
})();

// File drop handling for Blazor InputFile
globalThis.CodeGlyphX = globalThis.CodeGlyphX || {};
globalThis.CodeGlyphX.setupDropZone = function(dropZoneElement, inputFileElement) {
    if (!dropZoneElement || !inputFileElement) return;

    // Find the actual input element inside the InputFile component
    const inputElement = inputFileElement.querySelector('input[type="file"]') || inputFileElement;

    function handleDrop(e) {
        e.preventDefault();
        e.stopPropagation();

        if (e.dataTransfer?.files?.length > 0) {
            // Create a new DataTransfer to set files on the input
            const dt = new DataTransfer();
            for (let i = 0; i < e.dataTransfer.files.length; i++) {
                dt.items.add(e.dataTransfer.files[i]);
            }
            inputElement.files = dt.files;

            // Trigger change event
            inputElement.dispatchEvent(new Event('change', { bubbles: true }));
        }
    }

    function handleDragOver(e) {
        e.preventDefault();
        e.stopPropagation();
    }

    dropZoneElement.addEventListener('drop', handleDrop);
    dropZoneElement.addEventListener('dragover', handleDragOver);

    return {
        dispose: function() {
            dropZoneElement.removeEventListener('drop', handleDrop);
            dropZoneElement.removeEventListener('dragover', handleDragOver);
        }
    };
};

// Theme toggle + cycle button (static pages)
(function() {
    var themeButtons = Array.prototype.slice.call(document.querySelectorAll('.theme-toggle button[data-theme]'));
    var cycleButton = document.querySelector('.theme-cycle-btn');
    var themeOrder = ['auto', 'light', 'dark'];
    var themeIcons = {
        auto: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true" focusable="false"><rect x="2" y="3" width="20" height="14" rx="2"/><path d="M8 21h8M12 17v4"/></svg>',
        light: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true" focusable="false"><circle cx="12" cy="12" r="5"/><path d="M12 1v2M12 21v2M4.22 4.22l1.42 1.42M18.36 18.36l1.42 1.42M1 12h2M21 12h2M4.22 19.78l1.42-1.42M18.36 5.64l1.42-1.42"/></svg>',
        dark: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true" focusable="false"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/></svg>'
    };

    function getThemeLocal() {
        return document.documentElement.dataset.theme || localStorage.getItem('theme') || 'auto';
    }

    function getCycleTitle(theme) {
        if (theme === 'light') return 'Light mode (click to switch to dark)';
        if (theme === 'dark') return 'Dark mode (click to switch to auto)';
        return 'Auto mode (click to switch to light)';
    }

    function updateActiveTheme(theme) {
        if (!themeButtons.length) return;
        themeButtons.forEach(function(btn) {
            btn.classList.toggle('active', btn.dataset.theme === theme);
        });
    }

    function updateCycleButton(theme) {
        if (!cycleButton) return;
        cycleButton.innerHTML = themeIcons[theme] || themeIcons.auto;
        var title = getCycleTitle(theme);
        cycleButton.setAttribute('title', title);
        cycleButton.setAttribute('aria-label', title);
    }

    function setThemeLocal(theme) {
        document.documentElement.dataset.theme = theme;
        localStorage.setItem('theme', theme);
        updateActiveTheme(theme);
        updateCycleButton(theme);
        globalThis.CodeGlyphX?.renderMermaidDiagrams?.();
    }

    var currentTheme = getThemeLocal();
    updateActiveTheme(currentTheme);
    updateCycleButton(currentTheme);

    themeButtons.forEach(function(btn) {
        btn.addEventListener('click', function() {
            var theme = this.dataset.theme;
            if (!theme) return;
            setThemeLocal(theme);
        });
    });

    if (cycleButton) {
        cycleButton.addEventListener('click', function() {
            var theme = getThemeLocal();
            var idx = themeOrder.indexOf(theme);
            var next = themeOrder[(idx + 1) % themeOrder.length];
            setThemeLocal(next);
        });
    }
})();
