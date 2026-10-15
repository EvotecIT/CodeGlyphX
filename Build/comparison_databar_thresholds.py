"""Keep decoder-only follow-ups distinct from the earlier full comparison."""
import hashlib
import json
from comparison_retained_contexts import project_packet_costs


def project_databar_thresholds(root, qualification):
    reference = qualification.get('DecoderFollowups', {}).get('DataBarThresholdMasks')
    if not reference:
        return None

    def checked(row):
        name = row['File']
        if '/' in name or '\\' in name:
            raise ValueError('DataBar follow-up evidence leaves its root')
        raw = (root/name).read_bytes()
        if hashlib.sha256(raw).hexdigest().upper() != row['Sha256']:
            raise ValueError('DataBar follow-up evidence changed')
        return json.loads(raw.decode('utf-8-sig'))

    proof = checked(reference)
    previous = checked(proof['BaselineQualification'])
    if (reference['Source'] != proof['Source'] or reference['CoreSha256'] != proof['CoreSha256']
            or previous['MeasurementSource'] != proof['PreviousSource']
            or previous['CandidateCoreSha256'] != proof['PreviousCoreSha256']):
        raise ValueError('DataBar follow-up source or qualified baseline differs')
    review = proof['ReviewCoverage']
    if review['Head'] != proof['Source'] or review['BoundaryState'] != 'current' or review['UnresolvedFindings']:
        raise ValueError('DataBar follow-up lacks its independent review')
    tests = checked(proof['Tests'])
    totals = {'net10.0':2754,'net8.0':2732,'net472':1177}
    if set(tests) != set(totals) or any(
            row['Source'] != proof['Source'] or row['ContractsPassed'] != 6
            or row['DistinctPassed'] != row['Total'] or row['UnresolvedFailures'] or row['Skipped']
            or row['Total'] != totals[target] or len(row['ContractCases']) != 6
            or row['FixtureRerunPassed'] != row['InitialMissingFixtureFailures']
            for target,row in tests.items()) or tests['net10.0']['CoreSha256'] != proof['CoreSha256']:
        raise ValueError('DataBar follow-up framework proof is incomplete')
    native = checked(proof['NativeArm64'])
    if (native['Source'] != proof['Source'] or native['CoreSha256'] != proof['CoreSha256']
            or native['TestAssemblySha256'] != proof['TestAssemblySha256']
            or native['Architecture'] != 'arm64' or native['FrameworkRollForward'] != 'Disable'
            or native['DecisionProof']['Status'] != 'passed' or native['DecisionProof']['Checks'] != 199
            or native['InputFiles']['CodeGlyphX.dll'] != proof['CoreSha256']
            or native['InputFiles']['CodeGlyphX.Tests.dll'] != proof['TestAssemblySha256']
            or native['DecisionProof']['CoreSha256'] != proof['CoreSha256']
            or native['DecisionProof']['TestAssemblySha256'] != proof['TestAssemblySha256']
            or native['DecisionProof']['CoreInformationalVersion'].split('+')[-1] != proof['Source']
            or len(native['DecisionProof']['Cases']) != 199
            or any(r['Status'] != 'passed' for r in native['DecisionProof']['Cases'])):
        raise ValueError('DataBar follow-up native contracts differ')
    kinds = {'databar-positive':12,'databar-negative':31,'fabric':1}
    packets = proof['Packets']
    if (len(packets) != 6 or {(r['Kind'],r['Domain']) for r in packets} !=
            {(kind,domain) for kind in kinds for domain in ('ccd0','ccd1')}
            or any(r['Requests'] != kinds[r['Kind']] for r in packets)):
        raise ValueError('DataBar follow-up workload matrix differs')
    manifests = {kind:checked(row) for kind,row in proof['Manifests'].items()}
    if set(manifests) != set(kinds) or any(r['ManifestSha256'] != proof['Manifests'][r['Kind']]['OriginalInputSha256'] for r in packets):
        raise ValueError('DataBar follow-up measured input differs')
    loads = [checked(row) for row in proof['HostLoads']]
    if len(loads) != 6 or any(len(r['Samples']) != 3 or r['Masks'] != ['0xFFFF','0xFFFF0000'] for r in loads):
        raise ValueError('DataBar follow-up load observations are incomplete')
    corpora = {key:checked(row) for key,row in proof['Corpus'].items()}
    frozen = {key:checked(row) for key,row in proof['FrozenManifests'].items()}
    if (set(corpora) != {'main-before','main-current','negative-before','negative-current'}
            or proof['FrozenManifests']['main']['OriginalInputSha256'] != previous['CorpusManifestSha256']
            or proof['FrozenManifests']['negative']['OriginalInputSha256'] != previous['NegativeManifestSha256']):
        raise ValueError('DataBar follow-up frozen corpus differs')
    for kind,key in (('main','FrozenCorpusManifest'),('negative','FrozenNegativeManifest')):
        if frozen[kind] != checked(previous['Pdf417GridSamplingProof'][key]):
            raise ValueError('DataBar follow-up changes the qualified frozen inputs')
    samples = {r['Id']:r for manifest in frozen.values() for r in manifest['Samples']}
    expected = {
        'databar-positive':{r['Id']:r for r in frozen['main']['Samples'] if r['Format']=='Gs1DataBarOmnidirectional'},
        'databar-negative':{r['Id']:dict(r,Format='Gs1DataBarOmnidirectional') for r in samples.values() if not r['Expected']},
        'fabric':{'falsepositives/15.png':samples['falsepositives/15.png']}}
    if any({r['Id']:r for r in manifests[kind]['Samples']} != expected[kind] for kind in kinds):
        raise ValueError('DataBar follow-up diagnostic images, hints or oracles differ')
    for kind,count in (('main',279),('negative',25)):
        schema = [(r['Id'],r['Format'],r['Expected']) for r in frozen[kind]['Samples']]
        for lane in ('before','current'):
            rows = corpora[kind+'-'+lane]
            if len(rows) != count*3 or any([(r['Id'],r['Format'],r['Expected']) for r in rows if r['Engine']==engine] != schema for engine in ('CodeGlyphX','ZXing.Net','ZXing-C++')):
                raise ValueError('DataBar follow-up corpus oracles, hints or readers differ')
    costs = project_packet_costs(root, proof, packets)
    improvements, completion = [], []
    for row in packets:
        packet = checked({'File':row['PacketFile'],'Sha256':row['PacketSha256']})
        outcomes = checked({'File':row['OutcomesFile'],'Sha256':row['OutcomesSha256']})
        expected_order = [(r['Id'],r['Format'],r['Expected']) for r in manifests[row['Kind']]['Samples']]
        if any([(r['Id'],r['Format'],r['Expected']) for r in call['Outcomes']] != expected_order for call in outcomes['Calls']):
            raise ValueError('DataBar follow-up measured order differs from its input')
        lanes = {r['Engine']:r for r in packet['Summary']}
        if row['Kind']=='databar-positive':
            if any(r['Metrics']['CorrectImages'] != 10 for r in packet['Samples'] if r['Iteration']>=0):
                raise ValueError('DataBar follow-up positive outcomes changed')
            improvements.append(100*(1-lanes['Candidate']['MedianMs']/lanes['Baseline']['MedianMs']))
        if row['Kind']=='fabric':
            totals = {engine:sum(r['Metrics']['CorrectImages'] for r in packet['Samples'] if r['Engine']==engine and r['Iteration']>=0) for engine in lanes}
            completion.append(f"{row['Domain'].upper()}: {totals['Baseline']:g}/12 to {totals['Candidate']:g}/12 completed rejections")
    def counts(rows):
        rows = [r for r in rows if r['Engine']=='CodeGlyphX']
        return (sum(bool(r['Expected']) and r['Status']=='Correct' for r in rows),sum(not r['Expected'] and r['Status']=='Correct' for r in rows),sum(bool(r['Expected']) and bool(r['Unexpected']) for r in rows))
    before, current = counts(corpora['main-before']), counts(corpora['main-current'])
    neg_before, neg_current = counts(corpora['negative-before']), counts(corpora['negative-current'])
    return {'historical':qualification['MeasurementSource'] not in (proof['PreviousSource'],proof['Source']),'source':proof['Source'],'title':'DataBar recovery threshold follow-up',
        'anchor':'databar-thresholds','evidence':'databar-threshold-proof.json','evidence_label':'Decoder follow-up proof',
        'summary':f'The twelve-image DataBar positive batch takes {min(improvements):.1f}–{max(improvements):.1f}% less median time in this paired run. Every measured call retains 10/12 decoded images. The default negative-image workload is slower on both CPU domains and completes fewer times.',
        'completion_note':'; '.join(completion)+f'. The separate frozen corpus rerun retains {before[0]}/267 → {current[0]}/267 correct positives and {before[2]} → {current[2]} unexpected positive-image outputs. Main negative rejections are {before[1]}/12 → {current[1]}/12; supplementary rejections are {neg_before[1]}/25 → {neg_current[1]}/25. These conflicting completion changes do not establish a reliability gain.',
        'changes':proof['Changes'],'scope':proof['Scope'],'limits':proof['Limits'],'history_note':proof['HistoryNote'],
        'contracts':'All 6,663 distinct framework tests pass, including eighteen fixture-dependent reruns after correcting scratch fixture lookup. Six threshold contracts pass per framework; the exact core and test assembly also pass 199 native ARM64 functional checks and twenty additional Code128 pixel-API calls.',
        'costs':costs}
