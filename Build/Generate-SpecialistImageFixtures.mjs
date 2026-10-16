// Test-only PNG generation; the separately installed reference engine is never a product dependency.
// Usage: node Build/Generate-SpecialistImageFixtures.mjs <bwip-js-directory> <output-directory>
import { createRequire } from 'node:module';
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { resolve, join } from 'node:path';
import { createHash } from 'node:crypto';

const [moduleDirectory, outputDirectory] = process.argv.slice(2);
if (!moduleDirectory || !outputDirectory) throw new Error('Specify the pinned bwip-js directory and an output directory.');
const require = createRequire(import.meta.url);
const bwip = require(resolve(moduleDirectory));
const metadata = JSON.parse(readFileSync(join(resolve(moduleDirectory), 'package.json'), 'utf8'));
mkdirSync(outputDirectory, { recursive: true });
const cases = [
    ['RmQrCode', 'rectangularmicroqrcode', 'RMQR-42', { version: 'R13x77' }],
    ['HanXin', 'hanxin', '1234567890', { version: 1, eclevel: 'L1' }],
    ['DotCode', 'dotcode', 'ABC123'],
    ['Code16K', 'code16k', 'HELLO'],
    ['Code49', 'code49', 'ABC123'],
    ['CodablockF', 'codablockf', 'AB1234567890xy'],
    ['MicroPdf417', 'micropdf417', 'ABC123'],
    ['Gs1Composite', 'gs1-128composite', '(01)03212345678906|(21)A1B2C3D4E5F6G7H8', { ccversion: 'a' }],
    ['UspsIntelligentMail', 'onecode', '0123456709498765432101234567891'],
    ['RoyalMail4State', 'royalmail', 'LE28HS9Z'],
    ['AustraliaPost', 'auspost', '5956439111ABA9X'],
    ['JapanPost', 'japanpost', '15400233-16-4'],
    ['KixCode', 'kix', '1231FZ13XHS'],
    ['Postnet', 'postnet', '012345678'],
    ['Planet', 'planet', '01234567890'],
    ['PharmacodeTwoTrack', 'pharmacode2', '117480'],
    ['FacingIdentificationMark', 'symbol', 'fima'],
    ['Daft', 'daft', 'FATDAFTD'],
    // The reference renderer trims blank edge runs. Preserve the two leading and
    // trailing zero cells with its padding options, then record the actual data grid.
    ['Flattermarken', 'flattermarken', '001109900', { paddingleft: 30, paddingright: 30 }],
];
const fixtures = [];
for (const [format, bcid, text, options = {}] of cases) {
    const parameters = { bcid, text, scale: 3, padding: 12, backgroundcolor: 'FFFFFF', ...options };
    const png = await bwip.toBuffer(parameters);
    const file = `bwipp-${format}.png`;
    writeFileSync(join(outputDirectory, file), png);
    const samplingGrid = format === 'Flattermarken'
        ? { columns: 81, rows: 1, region: { x: 36, y: 36, width: 243, height: png.readUInt32BE(20) - 72 } }
        : undefined;
    fixtures.push({ file, format, text, options, samplingGrid, sha256: createHash('sha256').update(png).digest('hex') });
}
writeFileSync(join(outputDirectory, 'bwipp-specialist-images.json'), JSON.stringify({
    generator: `bwip-js ${metadata.version} / BWIPP ${bwip.BWIPP_VERSION}`,
    source: 'https://github.com/metafloor/bwip-js',
    engineSha256: createHash('sha256').update(readFileSync(require.resolve(resolve(moduleDirectory)))).digest('hex'),
    fixtures,
}, null, 2) + '\n');
