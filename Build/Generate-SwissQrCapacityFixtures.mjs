// Optional test-only fixture generation; the pinned comparison engine is installed separately.
// Usage: node Build/Generate-SwissQrCapacityFixtures.mjs <bwip-js-directory> <output-json>
import { createRequire } from 'node:module';
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve, join } from 'node:path';
import { createHash } from 'node:crypto';

const [moduleDirectory, output] = process.argv.slice(2);
if (!moduleDirectory || !output) throw new Error('Specify the pinned bwip-js directory and output JSON.');
const require = createRequire(import.meta.url);
const engineDirectory = resolve(moduleDirectory);
const bwip = require(engineDirectory);
const metadata = JSON.parse(readFileSync(join(engineDirectory, 'package.json'), 'utf8'));

function boundaryText(bytes, fill, separator) {
    const fields = Array(34).fill('');
    fields[0] = 'SPC'; fields[1] = '0200'; fields[2] = '1'; fields[3] = 'CH0607620AAAAAAAAAAAA';
    fields[4] = fields[20] = 'S';
    fields[5] = fields[9] = fields[21] = fields[25] = 'A';
    fields[8] = fields[24] = '8000'; fields[10] = fields[26] = 'CH';
    fields[19] = 'CHF'; fields[27] = 'NON'; fields[30] = 'EPD'; fields[32] = 'UV;1'; fields[33] = 'UV;2';
    let remaining = bytes - Buffer.byteLength(fields.join(separator), 'utf8');
    const fillBytes = Buffer.byteLength(fill, 'utf8');
    for (const [index, limit] of [[5, 70], [6, 70], [7, 16], [9, 35], [21, 70], [22, 70], [23, 16], [25, 35], [29, 140], [32, 100], [33, 100]]) {
        while (fields[index].length < limit && remaining > 0) {
            const next = remaining >= fillBytes ? fill : 'A';
            fields[index] += next;
            remaining -= Buffer.byteLength(next, 'utf8');
        }
    }
    if (remaining !== 0) throw new Error('Boundary fields could not hold the requested byte count.');
    return fields.join(separator);
}

const fixtures = [];
for (const fill of ['é', '€']) for (const separator of ['\n', '\r\n']) for (const bytes of [997, 998]) {
    const text = boundaryText(bytes, fill, separator);
    const fixture = { bcid: 'swissqrcode', text, utf8Bytes: Buffer.byteLength(text, 'utf8'), separator: separator === '\n' ? 'LF' : 'CRLF' };
    try {
        const raw = bwip.raw({ bcid: 'swissqrcode', text });
        const matrix = raw.find(item => item.pixs);
        if (!matrix) throw new Error('Reference engine did not return a module matrix.');
        Object.assign(fixture, { accepted: true, width: matrix.pixx, height: matrix.pixy, modules: matrix.pixs.join('') });
    } catch (error) {
        Object.assign(fixture, { accepted: false, error: String(error.message || error) });
    }
    if (fixture.accepted !== (bytes === 997)) throw new Error(`Unexpected reference capacity result: ${JSON.stringify(fixture)}`);
    fixtures.push(fixture);
}
writeFileSync(resolve(output), JSON.stringify({
    generator: `bwip-js ${metadata.version}`, source: 'https://github.com/metafloor/bwip-js',
    engineFile: 'src/bwipp.js', engineSha256: createHash('sha256').update(readFileSync(join(engineDirectory, 'src/bwipp.js'))).digest('hex'), fixtures
}, null, 2) + '\n');
process.stdout.write(`Generated ${fixtures.length} independent Swiss QR UTF-8 capacity vectors.\n`);
