#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Tests website pages for mobile layout issues (horizontal overflow).
.DESCRIPTION
    Uses Playwright to load pages at mobile viewport sizes and checks
    that no elements overflow horizontally.
#>
param(
    [string]$BaseUrl = "http://localhost:5051",
    [string[]]$Pages = @("/", "/showcase/", "/faq/"),
    [int[]]$Viewports = @(375, 390, 414),  # iPhone SE, iPhone 12, iPhone Plus
    [int]$MaxOverflow = 5  # Allow small tolerance in pixels
)

$ErrorActionPreference = "Stop"

# Check if playwright is installed
$playwrightInstalled = $null -ne (Get-Command npx -ErrorAction SilentlyContinue)
if (-not $playwrightInstalled) {
    Write-Error "npx not found. Please install Node.js."
    exit 1
}

# Create test script
$testScript = @"
const { chromium } = require('playwright');

(async () => {
    const baseUrl = '$BaseUrl';
    const pages = $($Pages | ConvertTo-Json -Compress);
    const viewports = $($Viewports | ConvertTo-Json -Compress);
    const maxOverflow = $MaxOverflow;

    const browser = await chromium.launch();
    const context = await browser.newContext();
    const page = await context.newPage();

    let failures = [];

    for (const viewportWidth of viewports) {
        await page.setViewportSize({ width: viewportWidth, height: 667 });

        for (const pagePath of pages) {
            const url = baseUrl + pagePath;
            console.log('Testing: ' + url + ' at ' + viewportWidth + 'px');

            try {
                await page.goto(url, { waitUntil: 'networkidle', timeout: 30000 });

                // Check for horizontal overflow
                const result = await page.evaluate((vw, maxOv) => {
                    const docWidth = document.documentElement.scrollWidth;
                    const overflow = docWidth - vw;

                    if (overflow > maxOv) {
                        // Find overflowing elements
                        const overflowing = [];
                        document.querySelectorAll('*').forEach(el => {
                            const rect = el.getBoundingClientRect();
                            if (rect.width > vw + maxOv && rect.width < 2000) {
                                overflowing.push({
                                    tag: el.tagName,
                                    class: el.className.substring(0, 50),
                                    width: Math.round(rect.width)
                                });
                            }
                        });
                        return { overflow, overflowing: overflowing.slice(0, 5) };
                    }
                    return null;
                }, viewportWidth, maxOverflow);

                if (result) {
                    failures.push({
                        page: pagePath,
                        viewport: viewportWidth,
                        overflow: result.overflow,
                        elements: result.overflowing
                    });
                }
            } catch (err) {
                failures.push({
                    page: pagePath,
                    viewport: viewportWidth,
                    error: err.message
                });
            }
        }
    }

    await browser.close();

    if (failures.length > 0) {
        console.error('\n--- FAILURES ---');
        console.error(JSON.stringify(failures, null, 2));
        process.exit(1);
    } else {
        console.log('\nAll pages passed mobile layout checks!');
        process.exit(0);
    }
})();
"@

$tempFile = [System.IO.Path]::GetTempFileName() -replace '\.tmp$', '.js'
$testScript | Set-Content -Path $tempFile -Encoding UTF8

try {
    Write-Host "Running mobile layout tests..." -ForegroundColor Cyan
    npx playwright install chromium --with-deps 2>$null
    node $tempFile
    $exitCode = $LASTEXITCODE
} finally {
    Remove-Item -Path $tempFile -Force -ErrorAction SilentlyContinue
}

exit $exitCode
