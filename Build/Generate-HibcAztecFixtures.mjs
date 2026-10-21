// Test-only independent HIBC Aztec inputs, consumed without external tools by normal tests.
// node Build/Generate-HibcAztecFixtures.mjs <pinned-bwip-js-directory> <output-file>
import { createRequire } from 'node:module';
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve, join } from 'node:path';
import { createHash } from 'node:crypto';
const [moduleDirectory, output] = process.argv.slice(2);
if (!moduleDirectory || !output) throw new Error('Specify a pinned bwip-js directory and output file.');
const require = createRequire(import.meta.url);
const source = resolve(moduleDirectory);
const bwip = require(source);
const version = JSON.parse(readFileSync(join(source, 'package.json'), 'utf8')).version;
const hash = createHash('sha256').update(readFileSync(require.resolve(source))).digest('hex');
const data = [
    'A123BJC5D6E71',
    '$$52001510X3G',
    'A123BJC5D6E71/$$52001510X3',
    'A99912349/$10X3/16D20111231/14D20200131/Q500'
];
const records = data.map(text => {
    const raw = bwip.raw({ bcid: 'hibcazteccode', text });
    if (raw.length !== 1 || raw[0].pixs.length !== raw[0].pixx * raw[0].pixx)
        throw new Error('Unexpected independent HIBC Aztec geometry.');
    return text + '|' + raw[0].pixx + '|' + raw[0].pixs.join('');
});
writeFileSync(resolve(output),
    `# bwip-js ${version} / BWIPP ${bwip.BWIPP_VERSION}\n# engineSha256 ${hash}\n# https://github.com/metafloor/bwip-js\n`
    + records.join('\n') + '\n');
