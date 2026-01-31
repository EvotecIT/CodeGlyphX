const { chromium } = require('playwright');
const path = require('path');
const fs = require('fs');

const baseUrl = process.env.WEBSITE_BASE_URL || 'http://localhost:5051';
const trackedResourceTypes = new Set(['document', 'stylesheet', 'script', 'fetch']);
const configPath = process.env.WEBSITE_SMOKE_CONFIG || path.join(__dirname, 'website-smoke-config.json');

function getTimeout(name, fallback) {
    const raw = process.env[`WEBSITE_TIMEOUT_${name}`];
    if (!raw) return fallback;
    const value = parseInt(raw, 10);
    return Number.isFinite(value) && value > 0 ? value : fallback;
}

const timeouts = {
    page: getTimeout('PAGE', 30000),
    nav: getTimeout('NAV', 10000),
    blazorLoad: getTimeout('BLAZOR_LOAD', 60000),
    blazorReady: getTimeout('BLAZOR_READY', 45000),
    selector: getTimeout('SELECTOR', 15000),
    networkIdle: getTimeout('NETWORK_IDLE', 30000)
};

let expectedNav = [
    { href: '/', text: 'Home' },
    { href: '/playground/', text: 'Playground' },
    { href: '/docs/', text: 'Docs' },
    { href: '/benchmarks/', text: 'Benchmarks' },
    { href: '/showcase/', text: 'Showcase' },
    { href: '/pricing/', text: 'Pricing' }
];

function loadSmokeConfig() {
    try {
        if (!fs.existsSync(configPath)) {
            return null;
        }
        const raw = fs.readFileSync(configPath, 'utf8');
        return raw ? JSON.parse(raw) : null;
    } catch (err) {
        console.warn(`Failed to load smoke test config: ${err.message}`);
        return null;
    }
}

const smokeConfig = loadSmokeConfig() || {};
const defaultNavTests = [
    { label: 'Home', href: '/', expectedPath: '/', expectedContent: 'Generate QR Codes' },
    { label: 'Playground', href: '/playground/', expectedPath: '/playground/', expectedContent: null },
    { label: 'Docs', href: '/docs/', expectedPath: '/docs/', expectedContent: null },
    { label: 'Benchmarks', href: '/benchmarks/', expectedPath: '/benchmarks/', expectedContent: 'Benchmarks' },
    { label: 'Showcase', href: '/showcase/', expectedPath: '/showcase/', expectedContent: 'Showcase' },
    { label: 'Pricing', href: '/pricing/', expectedPath: '/pricing/', expectedContent: 'Pricing' },
];

const defaultStaticPages = [
    {
        path: '/',
        label: 'Home',
        expectedText: 'Generate QR Codes',
        expectedSelector: '.hero',
        stylesheet: 'app.css',
        checkNav: true
    },
    {
        path: '/showcase/',
        label: 'Showcase',
        expectedText: 'Showcase',
        expectedSelector: '.showcase-page',
        stylesheet: 'app.css',
        checkNav: true
    },
    {
        path: '/faq/',
        label: 'FAQ',
        expectedText: 'Frequently Asked Questions',
        expectedSelector: '.faq-page',
        stylesheet: 'app.css',
        checkNav: true
    },
    {
        path: '/pricing/',
        label: 'Pricing',
        expectedText: 'Simple, Transparent Pricing',
        expectedSelector: '.pricing-page',
        stylesheet: 'app.css',
        checkNav: true,
        afterLoad: 'pricing'
    },
    {
        path: '/docs/',
        label: 'Documentation',
        expectedText: 'CodeGlyphX Documentation',
        expectedSelector: '.docs-layout',
        stylesheet: 'app.css',
        checkNav: true
    },
    {
        path: '/docs/api',
        label: 'API Reference',
        expectedText: 'API Reference',
        expectedSelector: '.api-layout',
        stylesheet: 'api.css',
        checkNav: true
    }
];

const defaultBlazorApps = [
    {
        path: '/playground/',
        label: 'Playground',
        expectedSelector: '.playground',
        expectedBaseHref: '/playground/',
        stylesheet: 'app.css',
        checkNav: true,
        afterLoad: 'playground'
    }
];

const navTests = Array.isArray(smokeConfig.navTests) && smokeConfig.navTests.length
    ? smokeConfig.navTests
    : defaultNavTests;
const staticPages = Array.isArray(smokeConfig.staticPages) && smokeConfig.staticPages.length
    ? smokeConfig.staticPages
    : defaultStaticPages;
const blazorApps = Array.isArray(smokeConfig.blazorApps) && smokeConfig.blazorApps.length
    ? smokeConfig.blazorApps
    : defaultBlazorApps;
const mobileStaticPages = smokeConfig.mobile?.staticPages || ['/', '/showcase/', '/faq/', '/benchmarks/', '/pricing/', '/docs/', '/docs/api'];
const mobileStaticWidths = smokeConfig.mobile?.staticWidths || [375, 390, 414];
const mobileBlazorPages = smokeConfig.mobile?.blazorPages || ['/playground/'];
const mobileBlazorWidths = smokeConfig.mobile?.blazorWidths || [375, 414];
const benchmarkConfig = smokeConfig.benchmarks || { enabled: true, path: '/benchmarks/' };

async function loadNavConfig() {
    if (Array.isArray(smokeConfig.navLinks) && smokeConfig.navLinks.length) {
        expectedNav = smokeConfig.navLinks;
        return;
    }
    try {
        const res = await fetch(`${baseUrl}/data/site-nav.json`, { cache: 'no-store' });
        if (!res.ok) return;
        const data = await res.json();
        if (!data) return;
        if (Array.isArray(data.primary) && data.primary.length) {
            expectedNav = data.primary;
            return;
        }
        if (Array.isArray(data.links) && data.links.length) {
            expectedNav = data.links;
        }
    } catch {
        // Keep defaults if config isn't available
    }
}

async function withRetries(label, attempts, fn) {
    let lastError;
    for (let attempt = 1; attempt <= attempts; attempt++) {
        try {
            return await fn();
        } catch (err) {
            lastError = err;
            if (attempt < attempts) {
                console.warn(`Retrying ${label} (attempt ${attempt + 1}/${attempts})...`);
                await new Promise(resolve => setTimeout(resolve, 500));
            }
        }
    }
    throw lastError;
}

function attachResourceMonitor(page) {
    const badResponses = [];
    const failedRequests = [];

    const responseHandler = response => {
        const url = response.url();
        if (!url.startsWith(baseUrl)) return;
        const type = response.request().resourceType();
        if (!trackedResourceTypes.has(type)) return;
        const status = response.status();
        if (status === 404 && url.endsWith('/data/style-board.json')) return;
        if (status >= 400) {
            badResponses.push({ url, status, type });
        }
    };

    const requestFailedHandler = request => {
        const url = request.url();
        if (!url.startsWith(baseUrl)) return;
        const type = request.resourceType();
        if (!trackedResourceTypes.has(type)) return;
        const failure = request.failure();
        failedRequests.push({ url, type, error: failure ? failure.errorText : 'request failed' });
    };

    page.on('response', responseHandler);
    page.on('requestfailed', requestFailedHandler);

    return {
        badResponses,
        failedRequests,
        detach: () => {
            page.off('response', responseHandler);
            page.off('requestfailed', requestFailedHandler);
        }
    };
}

function reportResourceFailures(monitor, label, failures) {
    if (monitor.badResponses.length > 0) {
        failures.push({
            test: 'resource-response',
            page: label,
            errors: monitor.badResponses.slice(0, 6)
        });
    }
    if (monitor.failedRequests.length > 0) {
        failures.push({
            test: 'resource-request',
            page: label,
            errors: monitor.failedRequests.slice(0, 6)
        });
    }
}

async function expectStylesheet(page, contains, failures, label) {
    const hrefs = await page.$$eval('link[rel="stylesheet"]', links =>
        links.map(link => link.getAttribute('href') || link.href).filter(Boolean)
    );
    if (!hrefs.some(href => href.includes(contains))) {
        failures.push({
            test: 'styles',
            page: label,
            error: `Missing stylesheet containing "${contains}"`
        });
    }
}

async function expectSelector(page, selector, failures, label) {
    const visible = await page.locator(selector).first().isVisible().catch(() => false);
    if (!visible) {
        failures.push({
            test: 'content',
            page: label,
            error: `Missing expected selector: ${selector}`
        });
    }
}

async function expectText(page, text, failures, label) {
    const content = await page.textContent('body');
    if (!content || !content.includes(text)) {
        failures.push({
            test: 'content',
            page: label,
            error: `Missing expected text: "${text}"`
        });
    }
}

async function expectNavConsistency(page, failures, label) {
    const navLinks = await page.$$eval('.nav-links a', links => links.map(link => ({
        href: link.getAttribute('href'),
        text: (link.textContent || '').trim()
    })));

    expectedNav.forEach(expected => {
        const match = navLinks.find(link => link.href === expected.href);
        if (!match) {
            failures.push({
                test: 'nav',
                page: label,
                error: `Missing nav link: ${expected.href}`
            });
            return;
        }
        if (match.text !== expected.text) {
            failures.push({
                test: 'nav',
                page: label,
                error: `Nav text mismatch for ${expected.href}: "${match.text}" vs "${expected.text}"`
            });
        }
    });
}

function resolveAfterLoadHandler(afterLoad) {
    if (!afterLoad) return null;
    if (typeof afterLoad === 'function') return afterLoad;
    if (typeof afterLoad === 'string' && afterLoadHandlers[afterLoad]) {
        return afterLoadHandlers[afterLoad];
    }
    return null;
}

async function verifyPricingPage(page, failures) {
    const cardCount = await page.$$eval('.pricing-card', cards => cards.length);
    if (cardCount < 3) {
        failures.push({
            test: 'pricing-cards',
            page: 'Pricing',
            error: `Expected at least 3 pricing cards, got ${cardCount}`
        });
    }
    // Check for free tier price label.
    const hasFreePrice = await page.evaluate(() => {
        const cards = Array.from(document.querySelectorAll('.pricing-card'));
        const freeCard = cards.find(card => {
            const title = card.querySelector('h2');
            return title && title.textContent && title.textContent.trim() === 'Free';
        });
        if (!freeCard) {
            return false;
        }
        const price = freeCard.querySelector('.pricing-amount .pricing-currency');
        if (!price || !price.textContent) {
            return false;
        }
        const value = price.textContent.trim();
        return value === 'Free' || value === '$0';
    });
    if (!hasFreePrice) {
        failures.push({
            test: 'pricing-free',
            page: 'Pricing',
            error: 'Missing free tier price'
        });
    }
}

const afterLoadHandlers = {
    pricing: verifyPricingPage,
    playground: testPlaygroundInteractions
};

async function testNavigation(page) {
    console.log('\n=== Testing Navigation ===');
    const failures = [];
    const tests = navTests;

    await page.goto(baseUrl, { waitUntil: 'networkidle', timeout: timeouts.page });

    for (const { label, href, expectedPath, expectedContent } of tests) {
        try {
            console.log('Testing navigation to:', label);
            await withRetries(`navigation ${label}`, 2, async () => {
                let link = page.locator(`.nav-links a[href="${href}"]`).first();
                if (!await link.isVisible().catch(() => false)) {
                    link = page.locator('.nav-links a', { hasText: label }).first();
                }

                if (!await link.isVisible().catch(() => false)) {
                    throw new Error('Link not visible');
                }

                await link.click();
                await page.waitForURL('**' + expectedPath + '**', { timeout: timeouts.nav });

                if (expectedPath === '/playground/' || expectedPath === '/docs/') {
                    await page.waitForTimeout(3000);
                }

                const url = page.url();
                if (!url.includes(expectedPath)) {
                    throw new Error('Wrong URL: ' + url);
                }

                if (expectedContent) {
                    const content = await page.textContent('body');
                    if (!content.includes(expectedContent)) {
                        throw new Error('Missing content: ' + expectedContent);
                    }
                }
            });

            console.log('  ✓', label, 'navigation works');
        } catch (err) {
            failures.push({ test: 'navigation', link: label, error: err.message });
            await page.goto(baseUrl, { waitUntil: 'networkidle', timeout: timeouts.page }).catch(() => {});
        }
    }

    return failures;
}

async function testStaticPage(page, path, label, options = {}) {
    const failures = [];
    const monitor = attachResourceMonitor(page);
    const afterLoadHandler = resolveAfterLoadHandler(options.afterLoad);

    try {
        await page.goto(baseUrl + path, { waitUntil: 'networkidle', timeout: timeouts.page });

        if (options.waitForSelector) {
            await page.waitForSelector(options.waitForSelector, { timeout: timeouts.selector });
        }

        if (options.expectedSelector) {
            await expectSelector(page, options.expectedSelector, failures, label);
        }

        if (options.expectedText) {
            await expectText(page, options.expectedText, failures, label);
        }

        if (options.stylesheet) {
            await expectStylesheet(page, options.stylesheet, failures, label);
        }

        if (options.checkNav) {
            await expectNavConsistency(page, failures, label);
        }

        if (afterLoadHandler) {
            await afterLoadHandler(page, failures);
        }
    } catch (err) {
        failures.push({
            test: 'page-load',
            page: label,
            error: err.message
        });
    } finally {
        reportResourceFailures(monitor, label, failures);
        monitor.detach();
    }

    return failures;
}

async function testMobileLayout(page, pages, viewports, maxOverflow) {
    const failures = [];
    for (const vw of viewports) {
        await page.setViewportSize({ width: vw, height: 667 });
        for (const path of pages) {
            const url = baseUrl + path;
            console.log('Testing layout:', url, 'at', vw + 'px');
            try {
                await page.goto(url, { waitUntil: 'networkidle', timeout: 30000 });
                const overflow = await page.evaluate(({ vw, max }) => {
                    const docWidth = document.documentElement.scrollWidth;
                    return docWidth - vw > max ? docWidth - vw : 0;
                }, { vw, max: maxOverflow });
                if (overflow > 0) {
                    failures.push({ test: 'mobile-layout', page: path, viewport: vw, overflow });
                }
            } catch (err) {
                failures.push({ test: 'mobile-layout', page: path, viewport: vw, error: err.message });
            }
        }
    }
    return failures;
}

async function testBlazorApp(page, path, appName, options = {}) {
    const url = baseUrl + path;
    console.log('Testing Blazor app:', appName, 'at', url);
    const failures = [];
    const monitor = attachResourceMonitor(page);
    const afterLoadHandler = resolveAfterLoadHandler(options.afterLoad);

    const errors = [];
    page.on('console', msg => {
        if (msg.type() === 'error') errors.push(msg.text());
    });
    page.on('pageerror', err => errors.push(err.message));

    try {
        await page.goto(url, { timeout: timeouts.blazorLoad, waitUntil: 'networkidle' });

        await page.waitForFunction(() => {
            const loading = document.querySelector('.loading-progress, .loading, [class*="loading"]');
            const blazorError = document.body.innerText.includes('error') &&
                                document.body.innerText.includes('reload');
            return !loading && !blazorError;
        }, { timeout: timeouts.blazorReady });

        const baseHref = await page.locator('base').getAttribute('href').catch(() => null);
        const expectedBase = options.expectedBaseHref || path;
        if (baseHref !== expectedBase) {
            failures.push({
                test: 'blazor-base',
                app: appName,
                error: `Expected base href "${expectedBase}", got "${baseHref}"`
            });
        }

        const hasBlazorScript = await page.locator('script[src*="blazor.webassembly"]').count();
        if (!hasBlazorScript) {
            failures.push({
                test: 'blazor-script',
                app: appName,
                error: 'Missing blazor.webassembly.js script reference'
            });
        }

        const criticalErrors = errors.filter(e =>
            e.includes('blazor') ||
            e.includes('WebAssembly') ||
            e.includes('Failed to fetch') ||
            e.includes('404')
        );

        if (criticalErrors.length > 0) {
            failures.push({
                test: 'blazor-init',
                app: appName,
                errors: criticalErrors.slice(0, 3),
            });
        }

        const hasContent = await page.evaluate(() => {
            const main = document.querySelector('main, .main, [role="main"], .app');
            return main && main.children.length > 0;
        });

        if (!hasContent) {
            failures.push({
                test: 'blazor-render',
                app: appName,
                error: 'App did not render content',
            });
        }

        if (options.expectedSelector) {
            await expectSelector(page, options.expectedSelector, failures, appName);
        }

        if (options.expectedText) {
            await expectText(page, options.expectedText, failures, appName);
        }

        if (options.stylesheet) {
            await expectStylesheet(page, options.stylesheet, failures, appName);
        }

        if (options.checkNav) {
            await expectNavConsistency(page, failures, appName);
        }

        if (afterLoadHandler) {
            await afterLoadHandler(page, failures);
        }

        console.log('  ✓', appName, 'loaded successfully');
    } catch (err) {
        failures.push({
            test: 'blazor-load',
            app: appName,
            error: err.message,
        });
    } finally {
        reportResourceFailures(monitor, appName, failures);
        monitor.detach();
    }

    return failures;
}

async function testPlaygroundInteractions(page, failures) {
    console.log('\n=== Testing Playground Interactions ===');

    try {
        // Accordion toggles
        const accordionHeaders = page.locator('.accordion-header');
        const headerCount = await accordionHeaders.count();
        if (headerCount > 1) {
            const target = accordionHeaders.nth(1);
            const section = target.locator('..');
            const wasOpen = await section.evaluate(el => el.classList.contains('open'));
            await target.click();
            await page.waitForTimeout(300);
            const isOpen = await section.evaluate(el => el.classList.contains('open'));
            if (wasOpen === isOpen) {
                failures.push({
                    test: 'playground-accordion',
                    page: 'Playground',
                    error: 'Accordion did not toggle open/closed'
                });
            }
        }

        // Preview remains in view on scroll (desktop only)
        await page.setViewportSize({ width: 1280, height: 800 });
        const previewSelector = '.playground-preview-card';
        const previewVisible = await page.locator(previewSelector).isVisible().catch(() => false);
        if (previewVisible) {
            const before = await page.$eval(previewSelector, el => el.getBoundingClientRect());
            await page.evaluate(() => window.scrollBy(0, 800));
            await page.waitForTimeout(300);
            const after = await page.$eval(previewSelector, el => el.getBoundingClientRect());
            const viewportHeight = await page.evaluate(() => window.innerHeight);
            const visibleHeight = Math.min(after.bottom, viewportHeight) - Math.max(after.top, 0);
            const visibleRatio = visibleHeight / Math.max(after.height, 1);

            if (visibleRatio < 0.5) {
                failures.push({
                    test: 'playground-sticky',
                    page: 'Playground',
                    error: `Preview not sufficiently visible after scroll (visible=${(visibleRatio * 100).toFixed(0)}%)`
                });
            }
            // Reset scroll for subsequent checks
            await page.evaluate(() => window.scrollTo(0, 0));
        }

        // Decode drag/drop via InputFile
        const modeSelect = page.locator('.card:has-text("Configuration") select').first();
        await modeSelect.selectOption('Decode');
        await page.waitForSelector('.dropzone', { timeout: timeouts.selector });

        const samplePath = path.join(__dirname, '..', 'Assets', 'DecodingSamples', 'qr-clean-small.png');
        const input = page.locator('.dropzone input[type="file"]').first();
        await input.setInputFiles(samplePath);

        await page.waitForFunction(() => {
            const hasImage = document.querySelector('.output-preview img');
            const text = document.body.textContent || '';
            return hasImage || text.includes('No codes detected') || text.includes('No codes found');
        }, { timeout: timeouts.page });
    } catch (err) {
        failures.push({
            test: 'playground-interactions',
            page: 'Playground',
            error: err.message
        });
    }
}

async function testBenchmarks(page, config = {}) {
    const route = config.path || '/benchmarks/';
    const label = config.label || 'Benchmarks';
    const expectedSelector = config.expectedSelector || '.benchmark-page';
    const summarySelector = config.summarySelector || '[data-benchmark-summary]';
    const summaryRowsSelector = config.summaryRowsSelector || '.bench-summary-table tbody tr';
    const metaSelector = config.metaSelector || '.benchmark-meta-grid';
    const tableSelector = config.tableSelector || '.bench-table';
    const stylesheet = config.stylesheet || 'app.css';
    const failures = [];
    const monitor = attachResourceMonitor(page);

    try {
        await page.goto(baseUrl + route, { waitUntil: 'networkidle', timeout: 30000 });
        await page.waitForSelector(expectedSelector, { timeout: timeouts.selector });
        await expectStylesheet(page, stylesheet, failures, label);

        if (summarySelector) {
            await page.waitForFunction((selector) => {
                const summary = document.querySelector(selector);
                return summary && summary.querySelector('table');
            }, summarySelector, { timeout: timeouts.page });
        }

        const rowCount = await page.$$eval(summaryRowsSelector, rows => rows.length);
        if (rowCount === 0) {
            failures.push({
                test: 'benchmarks',
                page: label,
                error: 'Benchmark summary table has no rows'
            });
        }

        const metaExists = await page.locator(metaSelector).count();
        if (!metaExists) {
            failures.push({
                test: 'benchmarks',
                page: label,
                error: 'Benchmark meta section did not render'
            });
        }

        const tableHandle = await page.$(tableSelector);
        if (tableHandle) {
            const borderCollapse = await tableHandle.evaluate(el => getComputedStyle(el).borderCollapse);
            if (borderCollapse !== 'collapse') {
                failures.push({
                    test: 'styles',
                    page: label,
                    error: 'Benchmark table styles not applied (border-collapse mismatch)'
                });
            }
        }

        await expectNavConsistency(page, failures, label);
    } catch (err) {
        failures.push({
            test: 'benchmarks',
            page: label,
            error: err.message
        });
    } finally {
        reportResourceFailures(monitor, label, failures);
        monitor.detach();
    }

    return failures;
}

(async () => {
    await loadNavConfig();
    const browser = await chromium.launch();
    const context = await browser.newContext();
    const page = await context.newPage();
    const allFailures = [];

    await page.setViewportSize({ width: 1280, height: 800 });
    allFailures.push(...await testNavigation(page));

    console.log('\n=== Testing Static Pages ===');
    for (const entry of staticPages) {
        const { path, label, ...options } = entry;
        allFailures.push(...await testStaticPage(page, path, label, options));
    }
    if (benchmarkConfig && benchmarkConfig.enabled !== false) {
        allFailures.push(...await testBenchmarks(page, benchmarkConfig));
    }

    console.log('\n=== Testing Mobile Layout ===');
    allFailures.push(...await testMobileLayout(
        page,
        mobileStaticPages,
        mobileStaticWidths,
        5
    ));

    console.log('\n=== Testing Blazor Apps ===');
    await page.setViewportSize({ width: 1280, height: 800 });
    for (const entry of blazorApps) {
        const { path, label, ...options } = entry;
        allFailures.push(...await testBlazorApp(page, path, label, options));
    }

    console.log('\n=== Testing Blazor Apps Mobile Layout ===');
    allFailures.push(...await testMobileLayout(
        page,
        mobileBlazorPages,
        mobileBlazorWidths,
        10
    ));

    await browser.close();

    if (allFailures.length > 0) {
        console.error('\n=== FAILURES ===');
        console.error(JSON.stringify(allFailures, null, 2));
        process.exit(1);
    }
    console.log('\n✓ Website smoke tests passed!');
})();
