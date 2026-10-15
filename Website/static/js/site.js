// Theme management functions for Blazor interop
function getTheme() {
    return localStorage.getItem('theme') || 'auto';
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
                        loadScript('/vendor/prism/prism-powershell.min.js', () => {
                            globalThis.__codeGlyphPrismLoaded = true;
                            callback?.();
                        });
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

    // Site colors are light-dark() pairs, which Mermaid cannot parse, so it gets the resolved values here.
    function getMermaidConfig() {
        const isLight = resolveThemeKind() === 'light';
        return {
            startOnLoad: false,
            securityLevel: 'strict',
            theme: 'base',
            flowchart: { curve: 'basis' },
            themeVariables: {
                background: isLight ? '#fcfbff' : '#0e0d15',
                primaryColor: isLight ? '#ffffff' : '#13121b',
                primaryTextColor: isLight ? '#16131f' : '#f4f2fa',
                primaryBorderColor: isLight ? '#6d28d9' : '#8b5cf6',
                lineColor: isLight ? '#4b4660' : '#bcb7cc',
                secondaryColor: isLight ? '#efedf5' : '#1b1a25',
                tertiaryColor: isLight ? '#e2dfeb' : '#25232f',
                clusterBkg: isLight ? '#efedf5' : '#1b1a25',
                clusterBorder: isLight ? '#e2dfeb' : '#25232f',
                fontFamily: 'Inter, Segoe UI, sans-serif',
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
        const copyIcon = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true"><rect x="9" y="9" width="13" height="13" rx="2"/><path d="M5 15H4a2 2 0 01-2-2V4a2 2 0 012-2h9a2 2 0 012 2v1"/></svg>';
        const doneIcon = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true"><path d="M20 6L9 17l-5-5"/></svg>';
        // Code blocks show a text label in their caption bar; inline signatures keep the icon only.
        const render = (done) => {
            btn.innerHTML = (done ? doneIcon : copyIcon) + (options?.text ? `<span>${done ? 'Copied' : 'Copy'}</span>` : '');
        };
        render(false);
        btn.addEventListener('click', () => {
            const code = textProvider() || '';
            copyText(code);
            render(true);
            btn.classList.add('copied');
            setTimeout(() => {
                render(false);
                btn.classList.remove('copied');
            }, 2000);
        });
        return btn;
    }

    function addCopyButton(block) {
        if (block.querySelector(':scope > .copy-btn')) return;
        block.classList.add('copyable');
        const codeEl = block.querySelector('code');
        const rawText = (codeEl ? codeEl.textContent : block.textContent) || '';
        const btn = createCopyButton(() => rawText, { text: true });
        block.appendChild(btn);
    }

    // Markdown tables scroll inside their own frame instead of widening the page.
    function initTables() {
        document.querySelectorAll('.markdown-body > table').forEach((table) => {
            const wrap = document.createElement('div');
            wrap.className = 'table-scroll';
            table.replaceWith(wrap);
            wrap.appendChild(table);
        });
    }

    // FAQ answers are <details>; open the one a link points at.
    function openTargetedDetails() {
        const id = decodeURIComponent((window.location.hash || '').slice(1));
        if (!id) return;
        const target = document.getElementById(id);
        if (target instanceof HTMLDetailsElement && !target.open) target.open = true;
    }

    // Style board: the preset gallery on the home page and the showcase.
    function initStyleBoards() {
        document.querySelectorAll('[data-style-board]').forEach((board) => {
            if (board.dataset.loaded) return;
            board.dataset.loaded = 'true';
            const src = board.dataset.styleBoardSrc || '/data/style-board.json';
            const base = board.dataset.styleBoardBase || '/assets/style-board/';
            fetch(src)
                .then((res) => {
                    if (!res.ok) throw new Error('Failed to load style board');
                    return res.json();
                })
                .then((items) => {
                    if (!Array.isArray(items)) return;
                    const cards = items.map((item) => {
                        const card = document.createElement('figure');
                        card.className = 'style-board-card';
                        const img = document.createElement('img');
                        img.loading = 'lazy';
                        img.decoding = 'async';
                        img.alt = item.name || 'QR style';
                        img.width = item.width || 400;
                        img.height = item.height || 400;
                        img.src = base + item.file;
                        const label = document.createElement('figcaption');
                        label.className = 'style-board-label';
                        label.textContent = item.name || 'QR style';
                        card.append(img, label);
                        return card;
                    });
                    board.replaceChildren(...cards);
                })
                .catch(() => {
                    board.innerHTML = '<div class="style-board-fallback">Style board assets are not available yet.</div>';
                });
        });
    }

    function initCodeBlocks() {
        let defaultCodeLanguage = document.body?.dataset?.codeLanguage
            || document.documentElement?.dataset?.codeLanguage
            || 'language-csharp';
        if (defaultCodeLanguage && !defaultCodeLanguage.startsWith('language-')) {
            defaultCodeLanguage = `language-${defaultCodeLanguage}`;
        }
        const blocks = Array.from(document.querySelectorAll('pre.code-block, .docs-content pre, .docs-static pre, .type-detail pre'))
            .filter((block) => !block.classList.contains('initialized') && !block.closest('.cgx-fig, .cgx-run'))
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
                    themeTabs.forEach((t) => { t.classList.toggle('active', t === tab); t.setAttribute('aria-pressed', t === tab ? 'true' : 'false'); });
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

    function getFriendlyReleaseUrlInfo(url) {
        const prMatch = url.match(/^https?:\/\/github\.com\/[^/]+\/[^/]+\/pull\/(\d+)(?:[/?#].*)?$/i);
        if (prMatch) {
            return { label: `PR #${prMatch[1]}`, className: 'pf-release-link--pr' };
        }

        const issueMatch = url.match(/^https?:\/\/github\.com\/[^/]+\/[^/]+\/issues\/(\d+)(?:[/?#].*)?$/i);
        if (issueMatch) {
            return { label: `Issue #${issueMatch[1]}`, className: 'pf-release-link--issue' };
        }

        const compareMatch = url.match(/^https?:\/\/github\.com\/[^/]+\/[^/]+\/compare\/[^?#]+$/i);
        if (compareMatch) {
            return { label: 'Full Changelog', className: 'pf-release-link--compare' };
        }

        return { label: url.replace(/^https?:\/\//i, ''), className: '' };
    }

    function linkifyReleaseBodyUrls() {
        const containers = Array.from(document.querySelectorAll('.pf-release-body'))
            .filter((container) => container.dataset.linkified !== 'true');

        containers.forEach((container) => {
            const walker = document.createTreeWalker(
                container,
                NodeFilter.SHOW_TEXT,
                {
                    acceptNode(node) {
                        if (!node?.nodeValue || !/(https?:\/\/|@[A-Za-z0-9-]{1,39})/i.test(node.nodeValue)) {
                            return NodeFilter.FILTER_REJECT;
                        }
                        const parent = node.parentElement;
                        if (!parent) {
                            return NodeFilter.FILTER_ACCEPT;
                        }
                        const tag = parent.tagName;
                        if (tag === 'A' || tag === 'CODE' || tag === 'PRE' || tag === 'SCRIPT' || tag === 'STYLE') {
                            return NodeFilter.FILTER_REJECT;
                        }
                        return NodeFilter.FILTER_ACCEPT;
                    }
                }
            );

            const textNodes = [];
            let current = walker.nextNode();
            while (current) {
                textNodes.push(current);
                current = walker.nextNode();
            }

            textNodes.forEach((node) => {
                const source = node.nodeValue || '';
                const tokenPattern = /(https?:\/\/[^\s<>()]+)|@([A-Za-z0-9-]{1,39})/gi;
                let lastIndex = 0;
                let match = tokenPattern.exec(source);
                if (!match) return;

                const fragment = document.createDocumentFragment();
                while (match) {
                    const rawUrl = match[1];
                    const mentionUser = match[2];
                    const token = match[0];

                    if (match.index > lastIndex) {
                        fragment.appendChild(document.createTextNode(source.slice(lastIndex, match.index)));
                    }

                    if (rawUrl) {
                        let url = rawUrl;
                        let suffix = '';
                        while (/[.,;:!?)]$/.test(url)) {
                            suffix = url.slice(-1) + suffix;
                            url = url.slice(0, -1);
                        }

                        if (url) {
                            const linkInfo = getFriendlyReleaseUrlInfo(url);
                            const anchor = document.createElement('a');
                            anchor.href = url;
                            anchor.textContent = linkInfo.label;
                            anchor.target = '_blank';
                            anchor.rel = 'noopener';
                            anchor.className = `pf-release-link ${linkInfo.className}`.trim();
                            if (linkInfo.label !== url) {
                                anchor.title = url;
                            }
                            fragment.appendChild(anchor);
                        } else {
                            fragment.appendChild(document.createTextNode(token));
                        }

                        if (suffix) {
                            fragment.appendChild(document.createTextNode(suffix));
                        }
                    } else {
                        const mentionToken = `@${mentionUser}`;
                        const previousChar = match.index > 0 ? source.charAt(match.index - 1) : '';
                        const isLikelyEmail = previousChar && /[A-Za-z0-9._-]/.test(previousChar);

                        if (isLikelyEmail) {
                            fragment.appendChild(document.createTextNode(mentionToken));
                        } else {
                            const anchor = document.createElement('a');
                            anchor.href = `https://github.com/${mentionUser}`;
                            anchor.textContent = mentionToken;
                            anchor.target = '_blank';
                            anchor.rel = 'noopener';
                            anchor.className = 'pf-release-link pf-release-link--mention';
                            anchor.title = `GitHub profile: ${mentionUser}`;
                            fragment.appendChild(anchor);
                        }
                    }

                    lastIndex = match.index + token.length;
                    match = tokenPattern.exec(source);
                }

                if (lastIndex < source.length) {
                    fragment.appendChild(document.createTextNode(source.slice(lastIndex)));
                }

                node.parentNode?.replaceChild(fragment, node);
            });

            container.dataset.linkified = 'true';
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
        if (button.textContent.trim() === 'Copy') {
            button.textContent = 'Copied';
            setTimeout(() => { button.textContent = 'Copy'; }, 2000);
        }
    });

    globalThis.addEventListener('hashchange', openTargetedDetails);

    let initTimer = 0;
    function scheduleInit() {
        if (initTimer) return;
        initTimer = globalThis.setTimeout(() => {
            initTimer = 0;
            renderMermaidDiagrams();
            initCodeBlocks();
            initTables();
            openTargetedDetails();
            initStyleBoards();
            initDocsNav();
            initShowcaseCarousel();
            linkifyReleaseBodyUrls();
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

// Header navigation. Each group button discloses its night panel; one panel is open at a time on wide screens, and
// Escape, a click outside or tabbing out of the group closes it. The phone sheet lists every group open. Pages the
// site generator did not render (the API reference) get their current destination marked here from the address.
(function () {
    var header = document.querySelector('[data-cgx-header]');
    if (!header) return;
    var nav = header.querySelector('.cgx-nav');
    var toggle = document.getElementById('nav-toggle');
    var compact = window.matchMedia('(max-width: 960px)');
    var groups = Array.prototype.slice.call(header.querySelectorAll('[data-cgx-navgroup]'));
    var triggerOf = function (group) { return group.querySelector('.cgx-navgroup__trigger'); };

    if (!header.querySelector('.cgx-navitem.is-current')) {
        var path = window.location.pathname;
        var best = null;
        header.querySelectorAll('[data-cgx-navitem]').forEach(function (item) {
            var url = item.getAttribute('data-cgx-navitem');
            if (!url || url === '/' || path.indexOf(url) !== 0) return;
            if (!best || url.length > best.getAttribute('data-cgx-navitem').length) best = item;
        });
        if (best) {
            var link = best.querySelector('.cgx-navitem__link');
            var group = best.closest('[data-cgx-navgroup]');
            best.classList.add('is-current');
            if (link) link.setAttribute('aria-current', path === best.getAttribute('data-cgx-navitem') ? 'page' : 'true');
            if (group) {
                group.classList.add('is-current');
                var here = header.querySelector('[data-cgx-here]');
                var title = header.querySelector('[data-cgx-here-title]');
                if (title && link) title.textContent = link.textContent;
                if (here) here.hidden = false;
            }
        }
    }

    var setOpen = function (group, open) { triggerOf(group).setAttribute('aria-expanded', open ? 'true' : 'false'); };
    var closeAll = function (except) { groups.forEach(function (group) { if (group !== except) setOpen(group, false); }); };

    groups.forEach(function (group) {
        var trigger = triggerOf(group);
        var links = Array.prototype.slice.call(group.querySelectorAll('.cgx-navitem__link'));
        trigger.addEventListener('click', function () {
            if (compact.matches) return;
            var open = trigger.getAttribute('aria-expanded') !== 'true';
            closeAll(group);
            setOpen(group, open);
        });
        trigger.addEventListener('keydown', function (event) {
            if (event.key !== 'ArrowDown' || compact.matches || !links.length) return;
            event.preventDefault();
            closeAll(group);
            setOpen(group, true);
            links[0].focus();
        });
        group.addEventListener('keydown', function (event) {
            if (compact.matches) return;
            var index = links.indexOf(document.activeElement);
            if (event.key === 'Escape' && trigger.getAttribute('aria-expanded') === 'true') {
                setOpen(group, false);
                trigger.focus();
            } else if (index >= 0 && /^Arrow(Left|Right|Up|Down)$/.test(event.key)) {
                event.preventDefault();
                var step = event.key === 'ArrowLeft' || event.key === 'ArrowUp' ? -1 : 1;
                links[(index + step + links.length) % links.length].focus();
            }
        });
        group.addEventListener('focusout', function (event) {
            if (!compact.matches && event.relatedTarget && !group.contains(event.relatedTarget)) setOpen(group, false);
        });
    });

    document.addEventListener('click', function (event) {
        if (!compact.matches && !header.contains(event.target)) closeAll(null);
    });
    var reset = function () {
        if (compact.matches) groups.forEach(function (group) { setOpen(group, true); });
        else closeAll(null);
    };
    if (compact.addEventListener) compact.addEventListener('change', reset);
    reset();
    if (nav) nav.setAttribute('data-cgx-wired', '');

    if (toggle) {
        var sync = function () { document.documentElement.classList.toggle('cgx-nav-open', toggle.checked && compact.matches); };
        toggle.addEventListener('change', sync);
        document.addEventListener('keydown', function (event) {
            if (event.key === 'Escape' && toggle.checked) { toggle.checked = false; sync(); toggle.focus(); }
        });
        header.addEventListener('click', function (event) {
            if (toggle.checked && event.target.closest && event.target.closest('.cgx-navitem__link')) { toggle.checked = false; sync(); }
        });
    }
})();

// Tab sets ([data-cgx-tabs]): arrow keys move between tabs, Home and End jump to the ends, and each tab shows the
// panel named by aria-controls.
(function () {
    document.querySelectorAll('[data-cgx-tabs]').forEach(function (set) {
        var tabs = Array.prototype.slice.call(set.querySelectorAll('[role="tab"]'));
        var select = function (active) {
            tabs.forEach(function (tab) {
                var on = tab === active;
                tab.setAttribute('aria-selected', on ? 'true' : 'false');
                tab.tabIndex = on ? 0 : -1;
                var panel = document.getElementById(tab.getAttribute('aria-controls'));
                if (panel) panel.hidden = !on;
            });
        };
        tabs.forEach(function (tab, index) {
            tab.addEventListener('click', function () { select(tab); });
            tab.addEventListener('keydown', function (event) {
                var next = null;
                if (event.key === 'ArrowRight' || event.key === 'ArrowDown') next = tabs[(index + 1) % tabs.length];
                if (event.key === 'ArrowLeft' || event.key === 'ArrowUp') next = tabs[(index - 1 + tabs.length) % tabs.length];
                if (event.key === 'Home') next = tabs[0];
                if (event.key === 'End') next = tabs[tabs.length - 1];
                if (next) { event.preventDefault(); next.focus(); select(next); }
            });
        });
        // Panels render visible so the page reads without script; show only the selected one from here on.
        var initial = tabs.filter(function (tab) { return tab.getAttribute('aria-selected') === 'true'; })[0] || tabs[0];
        if (initial) select(initial);
    });

    // Filter sets ([data-cgx-filter-set]): toggle buttons that show the items whose data-family matches.
    document.querySelectorAll('[data-cgx-filter-set]').forEach(function (set) {
        var target = document.querySelector(set.getAttribute('data-cgx-filter-set'));
        if (!target) return;
        set.querySelectorAll('button[data-filter]').forEach(function (button) {
            button.addEventListener('click', function () {
                var value = button.getAttribute('data-filter');
                set.querySelectorAll('button[data-filter]').forEach(function (other) {
                    other.setAttribute('aria-pressed', other === button ? 'true' : 'false');
                });
                target.querySelectorAll('[data-family]').forEach(function (item) {
                    item.hidden = value !== 'all' && item.getAttribute('data-family') !== value;
                });
            });
        });
    });
})();

// Changelog: put each release highlight's code beside that release's notes.
(function () {
    var drawn = document.querySelector('[data-cgx-drawn]');
    if (!drawn) return;
    drawn.querySelectorAll('li[data-release-tag]').forEach(function (item) {
        var tag = item.getAttribute('data-release-tag');
        var entry = document.querySelector('.pf-release-entry[data-release-tag="' + tag + '"] .pf-release-entry-header');
        var img = item.querySelector('img');
        if (!entry || !img) return;
        var holder = entry.querySelector('.cgx-entry-codes');
        if (!holder) {
            holder = document.createElement('div');
            holder.className = 'cgx-entry-codes';
            entry.appendChild(holder);
        }
        var copy = img.cloneNode(false);
        copy.alt = item.querySelector('strong') ? item.querySelector('strong').textContent : img.alt;
        copy.title = copy.alt;
        holder.appendChild(copy);
    });
})();

// Not found: show the address that failed and the closest page, with the QR code CodeGlyphX drew for that page.
(function () {
    var lost = document.querySelector('[data-cgx-lost]');
    if (!lost || !window.fetch) return;
    var path = decodeURIComponent(window.location.pathname || '/');
    var pathEl = lost.querySelector('[data-cgx-lost-path]');
    if (pathEl && path !== '/404/' && path !== '/404.html') pathEl.textContent = path;
    var tokens = function (value) { return value.toLowerCase().split(/[^a-z0-9]+/).filter(function (t) { return t.length > 1; }); };
    var wanted = tokens(path);
    fetch(lost.getAttribute('data-index')).then(function (r) { return r.ok ? r.json() : Promise.reject(); }).then(function (index) {
        var best = null, bestScore = 0;
        index.pages.forEach(function (page) {
            var have = tokens(page.url + ' ' + page.title);
            var score = 0;
            wanted.forEach(function (w) {
                have.forEach(function (h) {
                    if (h === w) score += 3;
                    else if (h.indexOf(w) === 0 || w.indexOf(h) === 0) score += 1;
                });
            });
            if (path.indexOf(page.url) === 0 && page.url !== '/') score += 2;
            if (score > bestScore || (score === bestScore && best && score > 0 && page.url.length < best.url.length)) { best = page; bestScore = score; }
        });
        if (!best) best = index.pages.filter(function (p) { return p.url === '/'; })[0];
        if (!best) return;
        var full = 'https://codeglyphx.com' + best.url;
        var match = lost.querySelector('[data-cgx-lost-match]');
        var url = lost.querySelector('[data-cgx-lost-url]');
        var img = lost.querySelector('[data-cgx-lost-img]');
        var caption = lost.querySelector('[data-cgx-lost-caption]');
        var link = '<a href="' + best.url + '">' + best.title.replace(/[&<>"]/g, '') + '</a>';
        if (match) match.innerHTML = (best.section && best.section !== 'Pages' && best.section !== 'CodeGlyphX' ? best.section + ': ' : '') + link;
        if (url) url.textContent = full;
        if (img) { img.src = best.src; img.alt = 'QR code that opens ' + full; }
        if (caption) caption.innerHTML = 'Scan, or open ' + link;
    }, function () { /* the default suggestion stays */ });
})();

// FAQ: the section in view is marked in the index.
(function () {
    var index = document.querySelector('.cgx-faq__index');
    if (!index || !('IntersectionObserver' in window)) return;
    var links = Array.prototype.slice.call(index.querySelectorAll('a[href^="#"]'));
    var sections = links.map(function (link) { return document.getElementById(link.getAttribute('href').slice(1)); });
    var visible = new Map();
    var observer = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) { visible.set(entry.target, entry.isIntersecting); });
        var active = null;
        sections.forEach(function (section, i) { if (!active && section && visible.get(section)) active = links[i]; });
        if (!active) return;
        links.forEach(function (link) { if (link === active) link.setAttribute('aria-current', 'true'); else link.removeAttribute('aria-current'); });
    }, { rootMargin: '-80px 0px -55% 0px' });
    sections.forEach(function (section) { if (section) observer.observe(section); });
})();
