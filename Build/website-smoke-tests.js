const { chromium } = require('playwright');

const baseUrl = process.env.WEBSITE_BASE_URL || 'http://localhost:5051';
const trackedResourceTypes = new Set(['document', 'stylesheet', 'script', 'fetch']);

function attachResourceMonitor(page) {
    const badResponses = [];
    const failedRequests = [];

    const responseHandler = response => {
        const url = response.url();
        if (!url.startsWith(baseUrl)) return;
        const type = response.request().resourceType();
        if (!trackedResourceTypes.has(type)) return;
        const status = response.status();
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

async function testNavigation(page) {
    console.log('\n=== Testing Navigation ===');
    const failures = [];
    const navTests = [
        { label: 'Home', href: '/', expectedPath: '/', expectedContent: 'Generate QR Codes' },
        { label: 'Playground', href: '/playground/', expectedPath: '/playground/', expectedContent: null },
        { label: 'Docs', href: '/docs/', expectedPath: '/docs/', expectedContent: null },
        { label: 'Showcase', href: '/showcase/', expectedPath: '/showcase/', expectedContent: 'Showcase' },
    ];

    await page.goto(baseUrl, { waitUntil: 'networkidle', timeout: 30000 });

    for (const { label, href, expectedPath, expectedContent } of navTests) {
        try {
            console.log('Testing navigation to:', label);
            let link = page.locator(`.nav-links a[href="${href}"]`).first();
            if (!await link.isVisible().catch(() => false)) {
                link = page.locator('.nav-links a', { hasText: label }).first();
            }

            if (!await link.isVisible().catch(() => false)) {
                failures.push({ test: 'navigation', link: label, error: 'Link not visible' });
                continue;
            }

            await link.click();
            await page.waitForURL('**' + expectedPath + '**', { timeout: 10000 });

            if (expectedPath === '/playground/' || expectedPath === '/docs/') {
                await page.waitForTimeout(3000);
            }

            const url = page.url();
            if (!url.includes(expectedPath)) {
                failures.push({ test: 'navigation', link: label, error: 'Wrong URL: ' + url });
                continue;
            }

            if (expectedContent) {
                const content = await page.textContent('body');
                if (!content.includes(expectedContent)) {
                    failures.push({ test: 'navigation', link: label, error: 'Missing content: ' + expectedContent });
                    continue;
                }
            }

            console.log('  ✓', label, 'navigation works');
        } catch (err) {
            failures.push({ test: 'navigation', link: label, error: err.message });
        }
    }

    return failures;
}

async function testStaticPage(page, path, label, options = {}) {
    const failures = [];
    const monitor = attachResourceMonitor(page);

    try {
        await page.goto(baseUrl + path, { waitUntil: 'networkidle', timeout: 30000 });

        if (options.waitForSelector) {
            await page.waitForSelector(options.waitForSelector, { timeout: 15000 });
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

        if (typeof options.afterLoad === 'function') {
            await options.afterLoad(page, failures);
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

    const errors = [];
    page.on('console', msg => {
        if (msg.type() === 'error') errors.push(msg.text());
    });
    page.on('pageerror', err => errors.push(err.message));

    try {
        await page.goto(url, { timeout: 60000, waitUntil: 'networkidle' });

        await page.waitForFunction(() => {
            const loading = document.querySelector('.loading-progress, .loading, [class*="loading"]');
            const blazorError = document.body.innerText.includes('error') &&
                                document.body.innerText.includes('reload');
            return !loading && !blazorError;
        }, { timeout: 45000 });

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

async function testBenchmarks(page) {
    const path = '/benchmarks/';
    const label = 'Benchmarks';
    const failures = [];
    const monitor = attachResourceMonitor(page);

    try {
        await page.goto(baseUrl + path, { waitUntil: 'networkidle', timeout: 30000 });
        await page.waitForSelector('.benchmark-page', { timeout: 15000 });
        await expectStylesheet(page, 'app.css', failures, label);

        await page.waitForFunction(() => {
            const summary = document.querySelector('[data-benchmark-summary]');
            return summary && summary.querySelector('table');
        }, { timeout: 30000 });

        const rowCount = await page.$$eval('.bench-summary-table tbody tr', rows => rows.length);
        if (rowCount === 0) {
            failures.push({
                test: 'benchmarks',
                page: label,
                error: 'Benchmark summary table has no rows'
            });
        }

        const metaExists = await page.locator('.benchmark-meta-grid').count();
        if (!metaExists) {
            failures.push({
                test: 'benchmarks',
                page: label,
                error: 'Benchmark meta section did not render'
            });
        }

        const tableHandle = await page.$('.bench-table');
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
    const browser = await chromium.launch();
    const context = await browser.newContext();
    const page = await context.newPage();
    const allFailures = [];

    await page.setViewportSize({ width: 1280, height: 800 });
    allFailures.push(...await testNavigation(page));

    console.log('\n=== Testing Static Pages ===');
    allFailures.push(...await testStaticPage(page, '/', 'Home', {
        expectedText: 'Generate QR Codes',
        expectedSelector: '.hero',
        stylesheet: 'app.css'
    }));
    allFailures.push(...await testStaticPage(page, '/showcase/', 'Showcase', {
        expectedText: 'Showcase',
        expectedSelector: '.showcase-page',
        stylesheet: 'app.css'
    }));
    allFailures.push(...await testStaticPage(page, '/faq/', 'FAQ', {
        expectedText: 'Frequently Asked Questions',
        expectedSelector: '.faq-page',
        stylesheet: 'app.css'
    }));
    allFailures.push(...await testBenchmarks(page));

    console.log('\n=== Testing Mobile Layout ===');
    allFailures.push(...await testMobileLayout(
        page,
        ['/', '/showcase/', '/faq/', '/benchmarks/'],
        [375, 390, 414],
        5
    ));

    console.log('\n=== Testing Blazor Apps ===');
    await page.setViewportSize({ width: 1280, height: 800 });
    allFailures.push(...await testBlazorApp(page, '/docs/', 'Documentation', {
        expectedSelector: '.docs-layout',
        expectedText: 'CodeGlyphX Documentation',
        expectedBaseHref: '/docs/',
        stylesheet: 'app.css'
    }));
    allFailures.push(...await testBlazorApp(page, '/playground/', 'Playground', {
        expectedSelector: '.playground',
        expectedBaseHref: '/playground/',
        stylesheet: 'app.css'
    }));

    console.log('\n=== Testing Blazor Apps Mobile Layout ===');
    allFailures.push(...await testMobileLayout(
        page,
        ['/docs/', '/playground/'],
        [375, 414],
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
