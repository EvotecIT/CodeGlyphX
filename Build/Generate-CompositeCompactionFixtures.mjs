// Independent GS1 Composite compaction fixtures; test-only pinned bwip-js input.
// node Build/Generate-CompositeCompactionFixtures.mjs <module-directory> <output-file>
import { createRequire } from 'node:module';
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve, join } from 'node:path';
import { createHash } from 'node:crypto';
const [moduleDirectory, output] = process.argv.slice(2);
if (!moduleDirectory || !output) throw new Error('Specify the pinned bwip-js directory and output file.');
const require = createRequire(import.meta.url);
const source = resolve(moduleDirectory);
const bwip = require(source);
const version = JSON.parse(readFileSync(join(source, 'package.json'), 'utf8')).version;
const hash = createHash('sha256').update(readFileSync(require.resolve(source))).digest('hex');
const data = [
    '(10)ABC123', '(10)123456', '(17)251231',
    '(11)240229(10)ABC123', '(17)251200(21)SERIAL1',
    '(17)251231(21)123456', '(17)251231(10)LOT(21)SERIAL',
    '(90)1BABCDEF', '(90)123A1234567890', '(90)B12AB',
    '(90)31ZABCDEFGHIJKLM', '(90)ABCDEF(21)SERIAL123',
    '(90)12BHELLO(8004)0123456ASSET123', '(90)1BMIXed.12(21)SERIAL',
    '(90)B', '(90)0BABC', '(90)1234BABC', '(90)Z123456'
];
const records = [];
for (const kind of ['a', 'b', 'c']) {
    for (const text of data) {
        const raw = bwip.raw({ bcid: 'gs1-cc', text, ccversion: kind, cccolumns: 4 });
        if (raw.length !== 1 || raw[0].pixs.length % raw[0].pixx)
            throw new Error('Unexpected independent component grid.');
        const item = raw[0];
        records.push([kind, text, item.pixx, item.pixs.length / item.pixx, item.pixs.join('')].join('|'));
    }
}
writeFileSync(resolve(output),
    `# bwip-js ${version} / BWIPP ${bwip.BWIPP_VERSION}\n# engineSha256 ${hash}\n# https://github.com/metafloor/bwip-js\n`
    + records.join('\n') + '\n');
