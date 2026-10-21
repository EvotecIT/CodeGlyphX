// Test-only, independently generated controls. Requires a separately installed bwip-js 4.11.4.
// Usage: node Build/Generate-LinearControlFixtures.mjs <bwip-js-directory> <output.txt>
import { createRequire } from 'node:module';
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve, join } from 'node:path';
import { createHash } from 'node:crypto';

const [moduleDirectory, output] = process.argv.slice(2);
if (!moduleDirectory || !output) throw new Error('Supply the pinned comparison module and output path.');
const require = createRequire(import.meta.url);
const bwip = require(resolve(moduleDirectory));
const metadata = JSON.parse(readFileSync(join(resolve(moduleDirectory), 'package.json'), 'utf8'));
if (metadata.version !== '4.11.4') throw new Error('Qualify a changed reference version before regenerating fixtures.');
const cases = [
    ...['00', '26', '000', '292', '0000', '3493', '00000', '44072', '000000', '576688', '0000000', '7742862']
        .map(text => ['channelcode', text, { shortfinder: true }]),
    ...['00', '12', '99'].map(variant => ['issn', `0317-8471 ${variant}`, {}]),
    ...['mod10', 'mod1010', 'mod11', 'mod1110', 'ncrmod11', 'ncrmod1110'].flatMap(checktype =>
        ['6', '12345678', '00000001'].map(text => ['msi', text, { includecheck: true, includecheckintext: true, checktype, badmod11: checktype.includes('11') }])),
];
const fixtures = cases.map(([bcid, text, options]) => {
    const raw = bwip.raw({ bcid, text, ...options });
    if (raw.length !== 1 || !raw[0].sbs) throw new Error(`${bcid}: expected one linear symbol.`);
    const runs = raw[0].sbs;
    if (runs.some(width => !Number.isInteger(width) || width < 0)) throw new Error('Expected integer run widths.');
    return { bcid, text, options, runs, modules: runs.map((width, index) => ((index & 1) === 0 ? '1' : '0').repeat(width)).join('') };
});
const hash = createHash('sha256').update(readFileSync(require.resolve(resolve(moduleDirectory)))).digest('hex');
writeFileSync(resolve(output), [
    `# bwip-js ${metadata.version} / BWIPP ${bwip.BWIPP_VERSION}`,
    `# Engine SHA256: ${hash}`,
    '# Source: https://github.com/metafloor/bwip-js',
    '# bcid<TAB>input<TAB>modules<TAB>options(JSON)',
    ...fixtures.map(fixture => {
        if (/[\r\n\t]/.test(fixture.text)) throw new Error('Escape new text controls before extending this fixture format.');
        return [fixture.bcid, fixture.text, fixture.modules, JSON.stringify(fixture.options)].join('\t');
    }),
].join('\n') + '\n');
