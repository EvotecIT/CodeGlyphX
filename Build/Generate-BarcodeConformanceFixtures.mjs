// Test-only fixture generation. Pass a separately installed, pinned bwip-js module.
// Usage: node Build/Generate-BarcodeConformanceFixtures.mjs <module-directory> <output.json>
import { createRequire } from 'node:module';
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve, join } from 'node:path';

const [moduleDirectory, output] = process.argv.slice(2);
if (!moduleDirectory || !output) throw new Error('Specify the independent bwip-js directory and fixture output path.');
const require = createRequire(import.meta.url);
const bwip = require(resolve(moduleDirectory));
const metadata = JSON.parse(readFileSync(join(resolve(moduleDirectory), 'package.json'), 'utf8'));
const cases = [
    ['ean2', '00'], ['ean2', '01'], ['ean2', '02'], ['ean2', '03'], ['ean2', '99'],
    ['ean5', '00000'], ['ean5', '12345'], ['ean5', '51234'], ['ean5', '99999'],
    ['telepennumeric', '00'], ['telepennumeric', '1234'], ['telepennumeric', '9999'],
    ['telepennumeric', '0X1X9X'], ['telepennumeric', '0123456789'],
    ['bc412', 'BRC412AB', { semi: true }], ['bc412', '0000000', { semi: true }],
    ['bc412', '123456789ABCDEFGHI', { semi: true }],
    ['flattermarken', '11099'], ['flattermarken', '01234567890'], ['flattermarken', '000'],
    ...['00', '26', '292', '3493', '44072', '576688', '7742862'].map(text => ['channelcode', text]),
    ['ean14', '(01)09506000134352'], ['sscc18', '(00)376123450000010039'],
    ['isbn', '978-1-56581-231-4'], ['ismn', '979-0-2306-7118-7'], ['issn', '0317-8471'],
    ['pzn', '123456'], ['pzn', '1234567', { pzn8: true }],
    ['identcode', '56310243031'], ['leitcode', '2134807501640'], ['mands', '0642118'],
    ['hibccode39', 'A999BJC5D6E71'], ['hibccode128', 'A999BJC5D6E71'],
    ['hibcdatamatrix', 'A999BJC5D6E71'], ['hibcdatamatrixrectangular', 'A999BJC5D6E71'],
    ['hibcqrcode', 'A999BJC5D6E71'], ['hibcpdf417', 'A999BJC5D6E71'], ['hibcmicropdf417', 'A999BJC5D6E71'],
    ...['HELLO', '1234567890', 'A123456', 'AB123456', '\u0001ABC', 'ab\u0001cd'].map(text => ['code16k', text]),
    ...['HELLO', '12345', '123456', '1234567', '12345678', '123456789', 'A1234567B', '\u0001ABC', 'lowercase', '1'.repeat(81)].map(text => ['code49', text]),
    ...['industrial2of5', 'iata2of5', 'matrix2of5', 'coop2of5', 'datalogic2of5'].flatMap(bcid => [
        [bcid, '1234'], [bcid, '0123456789'], [bcid, '1234', { includecheck: true }],
    ]),
];
const fixtures = cases.map(([bcid, text, options = {}]) => {
    const raw = bwip.raw({ bcid, text, ...options });
    if (raw.length !== 1) throw new Error(`${bcid}: unexpected number of symbols`);
    const symbol = raw[0];
    if (symbol.pixs) {
        const logicalHeight = symbol.pixs.length / symbol.pixx;
        if (!Number.isInteger(logicalHeight)) throw new Error(`${bcid}: inconsistent module grid`);
        return { bcid, text, options, width: symbol.pixx, height: logicalHeight, modules: symbol.pixs.join('') };
    }
    if (!symbol.sbs || symbol.sbs.some(width => !Number.isInteger(width) || width < 0))
        throw new Error(`${bcid}: linear fixture requires integer module widths`);
    return {
        bcid, text, options,
        runs: symbol.sbs,
        modules: symbol.sbs.map((width, index) => (index % 2 === 0 ? '1' : '0').repeat(width)).join(''),
    };
});
writeFileSync(resolve(output), JSON.stringify({
    generator: `bwip-js ${metadata.version} / BWIPP`,
    source: 'https://github.com/metafloor/bwip-js',
    fixtures,
}, null, 2) + '\n');
