// Test-only independent vectors. Normal builds consume the committed fixtures.
// node Build/Generate-AztecRuneFixtures.mjs <pinned-bwip-js-directory> <fixture-directory>
import { createRequire } from 'node:module';
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { resolve, join } from 'node:path';
import { createHash } from 'node:crypto';

const [moduleDirectory, outputDirectory] = process.argv.slice(2);
if (!moduleDirectory || !outputDirectory) throw new Error('Specify the pinned bwip-js module and fixture directory.');
const require = createRequire(import.meta.url);
const source = resolve(moduleDirectory);
const bwip = require(source);
const metadata = JSON.parse(readFileSync(join(source, 'package.json'), 'utf8'));
const output = resolve(outputDirectory);
mkdirSync(join(output, 'Conformance'), { recursive: true });
mkdirSync(join(output, 'Aztec'), { recursive: true });
const fixtures = Array.from({ length: 256 }, (_, value) => {
    const raw = bwip.raw({ bcid: 'aztecrune', text: String(value) });
    if (raw.length !== 1 || raw[0].pixx !== 11 || raw[0].pixs.length !== 121)
        throw new Error('Unexpected independent Rune geometry.');
    return { value, modules: raw[0].pixs.join('') };
});
const hash = createHash('sha256').update(readFileSync(require.resolve(source))).digest('hex');
// A line-oriented fixture is readable on every supported target without a JSON package.
writeFileSync(join(output, 'Conformance', 'bwipp-aztec-runes.txt'),
    `# bwip-js ${metadata.version} / BWIPP ${bwip.BWIPP_VERSION}\n# engineSha256 ${hash}\n# https://github.com/metafloor/bwip-js\n`
    + fixtures.map(item => `${item.value}:${item.modules}`).join('\n') + '\n');
for (const value of [0, 37, 255])
    writeFileSync(join(output, 'Aztec', `bwipp-rune-${value}.png`),
        await bwip.toBuffer({ bcid: 'aztecrune', text: String(value), scale: 5, padding: 4 }));
