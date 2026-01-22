(() => {
    function copyText(text) {
        if (!text) return;
        if (navigator.clipboard && navigator.clipboard.writeText) {
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
            document.execCommand('copy');
        } catch {
            // ignore
        }
        document.body.removeChild(textarea);
    }

    function loadPrism(callback) {
        if (globalThis.__codeGlyphPrismLoaded) {
            callback && callback();
            return;
        }
        const css = document.createElement('link');
        css.rel = 'stylesheet';
        css.href = '/vendor/prism/prism-tomorrow.min.css';
        document.head.appendChild(css);

        const script = document.createElement('script');
        script.src = '/vendor/prism/prism.min.js';
        script.onload = () => {
            const csharp = document.createElement('script');
            csharp.src = '/vendor/prism/prism-csharp.min.js';
            csharp.onload = () => {
                globalThis.__codeGlyphPrismLoaded = true;
                callback && callback();
            };
            document.head.appendChild(csharp);
        };
        document.head.appendChild(script);
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
        const blocks = Array.from(document.querySelectorAll('pre.code-block, .docs-content pre, .docs-static pre, .type-detail pre'))
            .filter((block) => !block.classList.contains('initialized'));
        const signatures = Array.from(document.querySelectorAll('code.signature'))
            .filter((sig) => !sig.classList.contains('initialized'));

        if (!blocks.length && !signatures.length) return;

        // Separate blocks that need highlighting from those that don't
        const blocksNeedingHighlight = [];
        const blocksReady = [];

        blocks.forEach((block) => {
            block.classList.add('initialized');
            const needsHighlight = block.classList.contains('code-block') && !block.querySelector('.keyword, .string, .comment');
            if (needsHighlight) {
                blocksNeedingHighlight.push(block);
            } else {
                blocksReady.push(block);
            }
        });

        // Add copy buttons to blocks that don't need highlighting
        blocksReady.forEach(addCopyButton);

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
                blocksNeedingHighlight.forEach((block) => {
                    if (!block.classList.contains('prism-highlighted')) {
                        block.classList.add('language-csharp', 'prism-highlighted');
                        if (globalThis.Prism && globalThis.Prism.highlightElement) {
                            globalThis.Prism.highlightElement(block);
                        }
                    }
                    // Add copy button AFTER highlighting
                    addCopyButton(block);
                });
            });
        }
    }

    document.addEventListener('click', (event) => {
        const target = event.target;
        if (!(target instanceof Element)) return;
        const button = target.closest('[data-copy]');
        if (!button) return;
        const text = button.getAttribute('data-copy');
        if (!text) return;
        copyText(text);
    });

    let initTimer = 0;
    function scheduleInit() {
        if (initTimer) return;
        initTimer = globalThis.setTimeout(() => {
            initTimer = 0;
            initCodeBlocks();
        }, 100);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', scheduleInit);
    } else {
        scheduleInit();
    }

    const observer = new MutationObserver(scheduleInit);
    observer.observe(document.body, { childList: true, subtree: true });

    globalThis.CodeGlyphX = globalThis.CodeGlyphX || {};
    globalThis.CodeGlyphX.initCodeBlocks = initCodeBlocks;
})();
