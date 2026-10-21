// Independent test vectors. Normal builds consume the committed output.
// node Build/Generate-CodeOneFixtures.mjs <pinned-bwip-js-directory> <conformance-directory>
import { createRequire } from 'node:module';
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { resolve, join } from 'node:path';
import { createHash } from 'node:crypto';
const [moduleDirectory, outputDirectory] = process.argv.slice(2);
if (!moduleDirectory || !outputDirectory) throw new Error('Specify the pinned bwip-js module and output directory.');
const require = createRequire(import.meta.url);
const source = resolve(moduleDirectory);
const bwip = require(source);
const metadata = JSON.parse(readFileSync(join(source, 'package.json'), 'utf8'));
const versions = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'S-10', 'S-20', 'S-30', 'T-16', 'T-32', 'T-48'];
const records = [];
for (const [index, version] of versions.entries()) {
    const numeric = version.startsWith('S');
    const payloads = numeric ? ['0', '37', '9'.repeat((index - 7) * 6)] : [
        'HELLO', 'ABCDEFGH', 'abcdefgh', 'ABC>123\r', '12345678901234567890',
        String.fromCharCode(128, 129, 130, 131, 132), 'A\x00b\x7fC'
    ];
    for (const text of payloads) {
        const bytes = Buffer.from(text, 'latin1');
        const escaped = [...bytes].map(value => `^${String(value).padStart(3, '0')}`).join('');
        const raw = bwip.raw({ bcid: 'codeone', text: numeric ? text : escaped, parse: !numeric, version });
        if (raw.length !== 1 || raw[0].pixs.length % raw[0].pixx) throw new Error(`Unexpected Code One ${version} geometry.`);
        records.push([index + 1, bytes.toString('hex'), raw[0].pixx, raw[0].pixs.length / raw[0].pixx, raw[0].pixs.join('')].join('|'));
    }
}
for (const [version, text] of [['A', 'A'.repeat(13)], ['A', 'a'.repeat(13)], ['A', '1'.repeat(22)],
    ['B', 'A'.repeat(26)], ['B', 'A'.repeat(27)], ['H', 'A'.repeat(2218)], ['H', '1'.repeat(3550)]]) {
    const raw = bwip.raw({ bcid: 'codeone', text, version })[0];
    records.push([versions.indexOf(version) + 1, Buffer.from(text, 'latin1').toString('hex'), raw.pixx, raw.pixs.length / raw.pixx, raw.pixs.join('')].join('|'));
}
for (const [sourceText, expected, gs1, eci] of [
    ['^FNC10103212345678906', Buffer.from('0103212345678906'), true, ''],
    ['^FNC110LOT^FNC121SERIAL', Buffer.from('10LOT\x1d21SERIAL'), true, ''],
    ['^ECI000026' + [...Buffer.from('Case \\ 世界')].map(value => `^${String(value).padStart(3, '0')}`).join(''), Buffer.from('Case \\ 世界'), false, '26']
]) {
    const raw = bwip.raw({ bcid: 'codeone', text: sourceText, parse: true, parsefnc: true, version: 'C' })[0];
    records.push([3, expected.toString('hex'), raw.pixx, raw.pixs.length / raw.pixx, raw.pixs.join(''), gs1 ? '1' : '0', eci].join('|'));
}
const output = resolve(outputDirectory);
mkdirSync(output, { recursive: true });
const hash = createHash('sha256').update(readFileSync(require.resolve(source))).digest('hex');
writeFileSync(join(output, 'bwipp-code-one.txt'),
    `# bwip-js ${metadata.version} / BWIPP ${bwip.BWIPP_VERSION}\n# engineSha256 ${hash}\n# https://github.com/metafloor/bwip-js\n`
    + records.join('\n') + '\n');
