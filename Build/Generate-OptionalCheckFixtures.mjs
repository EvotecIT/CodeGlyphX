// Disposable, test-only reference generator; never required to build or use CodeGlyphX.
// node Build/Generate-OptionalCheckFixtures.mjs <bwip-js 4.11.4 directory> <output.txt>
import { createRequire } from 'node:module';
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve, join } from 'node:path';
import { createHash } from 'node:crypto';
const [directory, output] = process.argv.slice(2);
if (!directory || !output) throw new Error('Supply the comparison module and output.');
const require = createRequire(import.meta.url);
const bwip = require(resolve(directory));
const metadata = JSON.parse(readFileSync(join(resolve(directory), 'package.json'), 'utf8'));
if (metadata.version !== '4.11.4') throw new Error('Qualify a changed reference before regeneration.');
const cases = [
    ...['code39', 'code11', 'interleaved2of5', 'industrial2of5', 'matrix2of5', 'iata2of5', 'coop2of5', 'datalogic2of5']
        .flatMap(bcid => ['12345', '123456789', '1234567890'].map(text => [bcid, text])),
    ...['A12345B', 'C-$/.:+D', 'A0A'].map(text => ['rationalizedCodabar', text]),
    ...['1', '12345', 'A1B2', 'FFFFFFFF', '0000'].map(text => ['plessey', text]),
    ...['abc', 'a+b', 'z%/', 'aX', 'aY', 'aW'].map(text => ['code39ext', text]),
    ...['BRC412AB', '1234567', '123456789012345678'].map(text => ['bc412', text]),
];
const lines = cases.map(([bcid, text]) => {
    const [raw] = bwip.raw({ bcid, text, includecheck: bcid !== 'bc412', includecheckintext: true, includetext: true, ...(bcid === 'bc412' ? { semi: true } : {}) });
    // Preserve integer run widths. Tests compare exact widths or legal narrow/wide classes, as appropriate.
    return [bcid, text, raw.sbs.join(','), raw.txt.map(line => line[0]).join('')].join('\t');
});
const hash = createHash('sha256').update(readFileSync(require.resolve(resolve(directory)))).digest('hex');
writeFileSync(resolve(output), [
    `# bwip-js ${metadata.version} / BWIPP ${bwip.BWIPP_VERSION}`,
    `# Engine SHA256: ${hash}`,
    '# Source: https://github.com/metafloor/bwip-js',
    '# bcid<TAB>data<TAB>run widths<TAB>human readable text with checks', ...lines,
].join('\n') + '\n');
