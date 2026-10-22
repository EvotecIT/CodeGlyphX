// Independent, test-only postal states. Requires a separately installed bwip-js 4.11.4.
// Usage: node Build/Generate-AustraliaPostControlFixtures.mjs <module-directory> <output.txt>
import { createRequire } from 'node:module';
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve, join } from 'node:path';
import { createHash } from 'node:crypto';
const [directory, output] = process.argv.slice(2);
if (!directory || !output) throw new Error('Supply the pinned module and output path.');
const require = createRequire(import.meta.url);
const bwip = require(resolve(directory));
const metadata = JSON.parse(readFileSync(join(resolve(directory), 'package.json'), 'utf8'));
if (metadata.version !== '4.11.4') throw new Error('Qualify a changed oracle before regenerating.');
const cases = [
    ['1156439111', 'character'], ['1100000000', 'numeric'],
    ...['', 'AB', 'ABz', 'ABCDE', 'ab #z', 'zzzzz'].map(text => ['5956439111' + text, 'character']),
    ...['', '12', '9', '12345678'].map(text => ['5956439111' + text, 'numeric']),
    ...['', 'AB', 'ABCDEFGHIJ', 'zz', 'zzzzzzzzzz'].map(text => ['6256439111' + text, 'character']),
    ...['', '12', '123456789012345'].map(text => ['6256439111' + text, 'numeric']),
];
const lines = cases.map(([text, table]) => {
    const raw = bwip.raw({ bcid: 'auspost', text, custinfoenc: table })[0];
    const maximum = Math.max(...raw.bhs); const minimum = Math.min(...raw.bhs);
    const states = raw.bhs.map((height, index) => Math.abs(height - maximum) < 1e-8 ? '0'
        : Math.abs(height - minimum) < 1e-8 ? '3' : raw.bbs[index] > 1e-8 ? '1' : '2').join('');
    return [text, table, states].join('\t');
});
const hash = createHash('sha256').update(readFileSync(require.resolve(resolve(directory)))).digest('hex');
writeFileSync(resolve(output), [
    `# bwip-js ${metadata.version} / BWIPP ${bwip.BWIPP_VERSION}`,
    `# Engine SHA256: ${hash}`,
    '# Source: https://github.com/metafloor/bwip-js',
    '# full FCC input<TAB>customer table<TAB>states (0 full, 1 ascender, 2 descender, 3 tracker)', ...lines,
].join('\n') + '\n');
