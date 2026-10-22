// Independent linked carrier fixtures; bwip-js is isolated test tooling.
// node Build/Generate-CompositeCarrierFixtures.mjs <bwip-js-directory> <output-file>
import { createRequire } from 'node:module';
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { resolve, join } from 'node:path';
import { createHash } from 'node:crypto';
const [moduleDirectory, output, imageDirectory] = process.argv.slice(2);
if (!moduleDirectory || !output) throw new Error('Specify the pinned bwip-js directory and output file.');
const require = createRequire(import.meta.url);
const source = resolve(moduleDirectory);
const bwip = require(source);
const version = JSON.parse(readFileSync(join(source, 'package.json'), 'utf8')).version;
const hash = createHash('sha256').update(readFileSync(require.resolve(source))).digest('hex');
const carriers = [
    ['Ean13', 'ean13composite', '9520123456788'],
    ['Ean8', 'ean8composite', '95200002'],
    ['UpcA', 'upcacomposite', '012345000058'],
    ['UpcE', 'upcecomposite', '01234558'],
    ['DataBarOmnidirectional', 'databaromnicomposite', '(01)09521234543213'],
    ['DataBarTruncated', 'databartruncatedcomposite', '(01)09521234543213'],
    ['DataBarStacked', 'databarstackedcomposite', '(01)09521234543213'],
    ['DataBarStackedOmnidirectional', 'databarstackedomnicomposite', '(01)09521234543213'],
    ['DataBarLimited', 'databarlimitedcomposite', '(01)09521234543213'],
    ['DataBarExpanded', 'databarexpandedcomposite', '(01)09521234543213(3103)001234'],
    ['DataBarExpandedStacked', 'databarexpandedstackedcomposite', '(01)09521234543213(10)ABCDEF(21)12345678'],
    ['DataBarExpanded', 'databarexpandedcomposite', '(01)09521234543213(10)Abcdef(21)12345678'],
    ['DataBarExpandedStacked', 'databarexpandedstackedcomposite', '(01)09521234543213(10)Abcdef(21)12345678']
];
for (const carrier of carriers.slice(0, 4)) {
    for (const addOn of ['12', '12345']) carriers.push([carrier[0], carrier[1], carrier[2] + '+' + addOn]);
}
const records = [];
if (imageDirectory) mkdirSync(resolve(imageDirectory), { recursive: true });
for (const [carrier, bcid, primary] of carriers) {
    for (const component of ['a', 'b']) {
        const secondary = '(11)990102';
        const item = bwip.raw({ bcid, text: primary.replace('+', ' ') + '|' + secondary, ccversion: component,
            segments: 4, addongap: 9, permitaddon: carrier === 'Ean8' })[0];
        if (!item?.pixs || item.pixs.length % item.pixx) throw new Error('Unexpected linked carrier grid.');
        records.push([carrier, component, primary, secondary, item.pixx, item.pixs.length / item.pixx, item.pixs.join('')].join('|'));
        if (imageDirectory) {
            const image = await bwip.toBuffer({ bcid, text: primary.replace('+', ' ') + '|' + secondary,
                ccversion: component, segments: 4, addongap: 9, permitaddon: carrier === 'Ean8',
                scale: 3, paddingwidth: 10, paddingheight: 10, backgroundcolor: 'FFFFFF', includetext: false });
            writeFileSync(join(resolve(imageDirectory), `bwipp-composite-${records.length - 1}.png`), image);
        }
    }
}
writeFileSync(resolve(output), `# bwip-js ${version} / BWIPP ${bwip.BWIPP_VERSION}\n# engineSha256 ${hash}\n`
    + '# https://github.com/metafloor/bwip-js\n# Compact logical rows; physical row heights are qualified separately.\n'
    + records.join('\n') + '\n');
