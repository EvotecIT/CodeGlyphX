const { chromium } = require('playwright');

const baseUrl = process.env.WEBSITE_BASE_URL || 'http://localhost:5051';

async function testNavigation(page) {
    console.log('\n=== Testing Navigation ===');
    const failures = [];
    const navTests = [
        ['Home', '/', 'Generate QR Codes'],
        ['Playground', '/playground/', null],
        ['Docs', '/docs/', null],
        ['Showcase', '/showcase/', 'Showcase'],
    ];

    await page.goto(baseUrl, { waitUntil: 'networkidle', timeout: 30000 });

    for (const [linkText, expectedPath, expectedContent] of navTests) {
        try {
            console.log('Testing navigation to:', linkText);
            const link = await page.locator('.nav-links a', { hasText: linkText }).first();
            if (!await link.isVisible()) {
                failures.push({ test: 'navigation', link: linkText, error: 'Link not visible' });
                continue;
            }

            await link.click();
            await page.waitForURL('**' + expectedPath + '**', { timeout: 10000 });

            if (expectedPath === '/playground/' || expectedPath === '/docs/') {
                await page.waitForTimeout(3000);
            }

            const url = page.url();
            if (!url.includes(expectedPath)) {
                failures.push({ test: 'navigation', link: linkText, error: 'Wrong URL: ' + url });
                continue;
            }

            if (expectedContent) {
                const content = await page.textContent('body');
                if (!content.includes(expectedContent)) {
                    failures.push({ test: 'navigation', link: linkText, error: 'Missing content: ' + expectedContent });
                    continue;
                }
            }

            console.log('  ✓', linkText, 'navigation works');
        } catch (err) {
            failures.push({ test: 'navigation', link: linkText, error: err.message });
        }
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

async function testBlazorApp(page, path, appName) {
    const url = baseUrl + path;
    console.log('Testing Blazor app:', appName, 'at', url);
    const failures = [];

    const errors = [];
    page.on('console', msg => {
        if (msg.type() === 'error') errors.push(msg.text());
    });
    page.on('pageerror', err => errors.push(err.message));

    try {
        await page.goto(url, { timeout: 60000 });

        await page.waitForFunction(() => {
            const loading = document.querySelector('.loading-progress, .loading, [class*="loading"]');
            const blazorError = document.body.innerText.includes('error') &&
                                document.body.innerText.includes('reload');
            return !loading && !blazorError;
        }, { timeout: 45000 });

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

        console.log('  ✓', appName, 'loaded successfully');
    } catch (err) {
        failures.push({
            test: 'blazor-load',
            app: appName,
            error: err.message,
        });
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

    console.log('\n=== Testing Mobile Layout ===');
    allFailures.push(...await testMobileLayout(
        page,
        ['/', '/showcase/', '/faq/'],
        [375, 390, 414],
        5
    ));

    console.log('\n=== Testing Blazor Apps ===');
    await page.setViewportSize({ width: 1280, height: 800 });
    allFailures.push(...await testBlazorApp(page, '/docs/', 'Documentation'));
    allFailures.push(...await testBlazorApp(page, '/playground/', 'Playground'));

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
