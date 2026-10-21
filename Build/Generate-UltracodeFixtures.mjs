// Independent colour-cell vectors. Normal builds consume the committed output.
// node Build/Generate-UltracodeFixtures.mjs <pinned-bwip-js-directory> <conformance-directory>
import { createRequire } from 'node:module';
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { resolve, join } from 'node:path';
import { createHash } from 'node:crypto';
const [moduleDirectory, outputDirectory] = process.argv.slice(2);
if (!moduleDirectory || !outputDirectory) throw new Error('Specify the pinned reference module and output directory.');
const require = createRequire(import.meta.url);
const source = resolve(moduleDirectory);
const bwip = require(source);
const metadata = JSON.parse(readFileSync(join(source, 'package.json'), 'utf8'));
const mapping = { 0: 1, 9: 2, 1: 3, 3: 4, 5: 5, 6: 6 };
const records = [];
for (let level = 1; level <= 5; level++) {
    const maximum = [0, 263, 252, 234, 220, 202][level];
    const payloads = [Buffer.from('HELLO'), Buffer.from('Case 123!'), Buffer.from([0, 1, 13, 31, 127, 128, 200, 255]),
        Buffer.from(Array.from({ length: 128 }, (_, i) => i)), Buffer.from(Array.from({ length: 128 }, (_, i) => i + 128)),
        Buffer.alloc(40, 0x41), Buffer.alloc(maximum, 0x41)];
    for (const bytes of payloads) for (const revision of [1, 2]) {
        const text = [...bytes].map(value => `^${String(value).padStart(3, '0')}`).join('');
        const raw = bwip.raw({ bcid: 'ultracode', text, parse: true, eclevel: `EC${level}`, rev: revision })[0];
        const cells = raw.pixs.map(value => {
            if (!(value in mapping)) throw new Error(`Unrecognized reference colour ${value}.`);
            return mapping[value];
        }).join('');
        records.push([level, revision, bytes.toString('hex'), raw.pixx, raw.pixy, cells].join('|'));
    }
}
const output = resolve(outputDirectory);
mkdirSync(output, { recursive: true });
const hash = createHash('sha256').update(readFileSync(require.resolve(source))).digest('hex');
writeFileSync(join(output, 'bwipp-ultracode.txt'),
    `# bwip-js ${metadata.version} / BWIPP ${bwip.BWIPP_VERSION}\n# engineSha256 ${hash}\n`
    + '# Logical colours: 1 white, 2 black, 3 cyan, 4 magenta, 5 yellow, 6 green\n'
    + records.join('\n') + '\n');
for (const revision of [1, 2]) {
    const png = await bwip.toBuffer({ bcid: 'ultracode', text: 'Case 123!', eclevel: 'EC3', rev: revision, scale: 3 });
    writeFileSync(join(output, `bwipp-ultracode-rev${revision}.png`), png);
}
