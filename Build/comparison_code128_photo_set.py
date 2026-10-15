"""Validate and project the separately selected Code128/DataBar photo set."""
import re

def _physical_payload(text,format):
    # This set uses only Code128 and Omni DataBar. Preserve the existing
    # CorpusScoring.cs presentation rules; raw text remains in every outcome.
    if format=='Gs1DataBarOmnidirectional':
        if text.startswith('(01)'):text=text[4:]
        elif len(text)==16 and text.startswith('01'):text=text[2:]
        if re.fullmatch(r'[0-9]{13}',text):
            total=sum(int(c)*(3 if i%2==0 else 1) for i,c in enumerate(text))
            text+=str((10-total%10)%10)
    return text

def validate_photographs(root,proof,checked,baseline_counts=(20,15,5)):
    photo=proof['PhotographSet']
    if any(photo[key]!=proof[key] for key in ('Source','CoreSha256','PreviousSource','PreviousCoreSha256')):
        raise ValueError('Photograph binary identity differs')
    manifest=checked(photo['Manifest']);provenance=checked(photo['Provenance'])
    samples={r['Id']:r for r in manifest['Samples']}
    frozen=checked(proof['FrozenCorpusManifest'])
    if (len(manifest['Samples'])!=len(samples) or len(samples)!=52
        or sum(r['Format']=='Code128' for r in samples.values())!=34
        or sum(r['Format']=='Gs1DataBarOmnidirectional' for r in samples.values())!=18
        or set(samples)&{r['Id'] for r in frozen['Samples']}
        or provenance['Revision']!=manifest['Revision']
        or provenance['ManifestSha256'].upper()!=photo['Manifest']['OriginalInputSha256']
        or provenance['OriginalFrozenManifestSha256'].upper()!=proof['FrozenCorpusManifest']['OriginalInputSha256']
        or not provenance['AllGitBlobHashesVerified']
        or len(provenance['Samples'])!=52 or {r['Id'] for r in provenance['Samples']}!=set(samples)):
        raise ValueError('Additional photograph selection, provenance or frozen separation differs')
    for row in samples.values():
        if row['File']!=row['Id'] or not re.fullmatch(r'(?:code128-2|rss14-2)/[0-9]+\.png',row['Id']) or not re.fullmatch(r'[a-f0-9]{64}',row['Sha256']) or len(row['Expected'])!=1:
            raise ValueError('Invalid photo input identity')
    for row in provenance['Samples']:
        folder,name=row['Id'].split('/')
        expected=f"https://raw.githubusercontent.com/zxing/zxing/{manifest['Revision']}/core/src/test/resources/blackbox/{folder}/{name[:-4]}.txt"
        if row['OracleUrl']!=expected or any(not re.fullmatch(r'[a-f0-9]{40}',row[k]) for k in ('ImageGitBlob','OracleGitBlob')) or not re.fullmatch(r'[a-f0-9]{64}',row['OracleSha256']):
            raise ValueError('Invalid immutable upstream photograph provenance')
    lanes={key:checked(photo[key]) for key in ('Before','Current')}
    engines=('CodeGlyphX','ZXing.Net','ZXing-C++')
    for rows in lanes.values():
        if len(rows)!=156 or {(r['Id'],r['Engine']) for r in rows}!={(id,e) for id in samples for e in engines}:
            raise ValueError('Missing or duplicate photograph outcome')
        for row in rows:
            sample=samples[row['Id']]
            expected={_physical_payload(t,sample['Format']) for t in sample['Expected']}
            actual={_physical_payload(t,sample['Format']) for t in row['Actual']}
            # CorpusSession scores an expected positive payload even when the
            # single-result scanner stopped at its symbol limit. Completion is
            # decisive for an empty negative scan; this set has no negatives.
            correct=expected==actual
            if row['Expected']!=sample['Expected'] or row['Format']!=sample['Format'] or set(row['Missing'])!=expected-actual or set(row['Unexpected'])!=actual-expected or (row['Status']=='Correct')!=correct:
                raise ValueError('Photograph oracle, hints or scored outcome differs')
    regression=checked(proof['BaselineRegressions'])
    cases=regression['Cases']
    total,failed,passed=baseline_counts
    if regression['CoreSha256']!=proof['PreviousCoreSha256'] or len(cases)!=total or len({r['Name'] for r in cases})!=total or sum(r['Outcome']=='Failed' for r in cases)!=failed or sum(r['Outcome']=='Passed' for r in cases)!=passed or (regression['Total'],regression['Failed'],regression['Passed'])!=baseline_counts:
        raise ValueError('Baseline photo regression evidence differs')
    table=[]
    for family,hint,count in [('Code128','Code128',34),('DataBar','Gs1DataBarOmnidirectional',18),('Combined',None,52)]:
        for engine in engines:
            a=[r for r in lanes['Before'] if r['Engine']==engine and (hint is None or r['Format']==hint)]
            b=[r for r in lanes['Current'] if r['Engine']==engine and (hint is None or r['Format']==hint)]
            table.append({'family':family,'engine':engine,'images':count,'before':sum(r['Status']=='Correct' for r in a),'correct':sum(r['Status']=='Correct' for r in b),'unexpected':sum(bool(r['Unexpected']) for r in b),'inconclusive':sum(r['Status']=='Inconclusive' for r in b)})
    a={r['Id']:r for r in lanes['Before'] if r['Engine']=='CodeGlyphX'}
    b={r['Id']:r for r in lanes['Current'] if r['Engine']=='CodeGlyphX'}
    return {'rows':table,'scope':photo['Scope'],'limits':photo['Limits'],'gained':[id for id,r in b.items() if r['Status']=='Correct' and a[id]['Status']!='Correct'],'lost':[id for id,r in a.items() if r['Status']=='Correct' and b[id]['Status']!='Correct'],'new_unexpected':[id for id,r in b.items() if r['Unexpected'] and not a[id]['Unexpected']],'manifest':photo['Manifest']['File'],'before_file':photo['Before']['File'],'current_file':photo['Current']['File']}

def append_photograph_section(lines,projected):
    if not projected or projected['historical']:return
    photos=projected['photographs']
    lines += ['', '## Additional Code128 and DataBar photographs', '', photos['scope'], '', photos['limits'], '',
        '| Family | Reader | Previous correct | Current correct | Images | Current unexpected | Current inconclusive |',
        '| --- | --- | ---: | ---: | ---: | ---: | ---: |']
    for r in photos['rows']:
        lines.append(f"| {r['family']} | {r['engine']} | {r['before']} | {r['correct']} | {r['images']} | {r['unexpected']} | {r['inconclusive']} |")
    lines += ['',f"CodeGlyphX gains {len(photos['gained'])} images, loses {len(photos['lost'])}, and adds unexpected outputs on {len(photos['new_unexpected'])} images in this set.",'',f"[Inputs](evidence/{photos['manifest']}) · [Previous outcomes](evidence/{photos['before_file']}) · [Current outcomes](evidence/{photos['current_file']})."]
