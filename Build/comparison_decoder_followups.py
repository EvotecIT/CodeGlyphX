"""Validate decoder follow-ups without relabeling the full comparison owner."""
import hashlib
import json
from comparison_retained_contexts import project_packet_costs


def load_decoder_followup(root, qualification, slot, kinds, positive_format, contracts, totals, native_checks):
    reference = qualification.get('DecoderFollowups', {}).get(slot)
    if not reference:
        return None

    def checked(row):
        name = row['File']
        if '/' in name or '\\' in name:
            raise ValueError('Decoder follow-up evidence leaves its root')
        raw = (root/name).read_bytes()
        if hashlib.sha256(raw).hexdigest().upper() != row['Sha256']:
            raise ValueError('Decoder follow-up evidence changed')
        return json.loads(raw.decode('utf-8-sig'))

    proof = checked(reference)
    previous = checked(proof['BaselineQualification'])
    if (reference['Source'] != proof['Source'] or reference['CoreSha256'] != proof['CoreSha256']
            or previous.get('MeasurementSource', previous.get('Source')) != proof['PreviousSource']
            or previous.get('CandidateCoreSha256', previous.get('CoreSha256')) != proof['PreviousCoreSha256']):
        raise ValueError('Decoder follow-up source or qualified baseline differs')
    review = proof['ReviewCoverage']
    if review['Head'] != proof['Source'] or review['BoundaryState'] != 'current' or review['UnresolvedFindings']:
        raise ValueError('Decoder follow-up lacks its independent review')
    tests = checked(proof['Tests'])
    if set(tests) != set(totals) or any(
            row['Source'] != proof['Source'] or row['ContractsPassed'] != contracts[target]
            or row['DistinctPassed'] != row['Total'] or row['UnresolvedFailures'] or row['Skipped']
            or row['Total'] != totals[target] or len(row['ContractCases']) != contracts[target]
            or row['FixtureRerunPassed'] != row['InitialMissingFixtureFailures']
            for target,row in tests.items()) or tests['net10.0']['CoreSha256'] != proof['CoreSha256']:
        raise ValueError('Decoder follow-up framework proof is incomplete')
    native = checked(proof['NativeArm64'])
    if (native['Source'] != proof['Source'] or native['CoreSha256'] != proof['CoreSha256']
            or native['TestAssemblySha256'] != proof['TestAssemblySha256']
            or native['Architecture'] != 'arm64' or native['FrameworkRollForward'] != 'Disable'
            or native['DecisionProof']['Status'] != 'passed' or native['DecisionProof']['Checks'] != native_checks
            or native['InputFiles']['CodeGlyphX.dll'] != proof['CoreSha256']
            or native['InputFiles']['CodeGlyphX.Tests.dll'] != proof['TestAssemblySha256']
            or native['DecisionProof']['CoreSha256'] != proof['CoreSha256']
            or native['DecisionProof']['TestAssemblySha256'] != proof['TestAssemblySha256']
            or native['DecisionProof']['CoreInformationalVersion'].split('+')[-1] != proof['Source']
            or len(native['DecisionProof']['Cases']) != native_checks
            or any(r['Status'] != 'passed' for r in native['DecisionProof']['Cases'])):
        raise ValueError('Decoder follow-up native contracts differ')
    packets = proof['Packets']
    if (len(packets) != 6 or {(r['Kind'],r['Domain']) for r in packets} !=
            {(kind,domain) for kind in kinds for domain in ('ccd0','ccd1')}
            or any(r['Requests'] != kinds[r['Kind']] for r in packets)):
        raise ValueError('Decoder follow-up workload matrix differs')
    manifests = {kind:checked(row) for kind,row in proof['Manifests'].items()}
    if set(manifests) != set(kinds) or any(r['ManifestSha256'] != proof['Manifests'][r['Kind']]['OriginalInputSha256'] for r in packets):
        raise ValueError('Decoder follow-up measured input differs')
    loads = [checked(row) for row in proof['HostLoads']]
    if len(loads) != 6 or any(len(r['Samples']) != 3 or r['Masks'] != ['0xFFFF','0xFFFF0000'] for r in loads):
        raise ValueError('Decoder follow-up load observations are incomplete')
    corpora = {key:checked(row) for key,row in proof['Corpus'].items()}
    frozen = {key:checked(row) for key,row in proof['FrozenManifests'].items()}
    if (set(corpora) != {'main-before','main-current','negative-before','negative-current'}
            or proof['FrozenManifests']['main']['OriginalInputSha256'] != (previous['FrozenManifests']['main']['OriginalInputSha256'] if 'FrozenManifests' in previous else previous['CorpusManifestSha256'])
            or proof['FrozenManifests']['negative']['OriginalInputSha256'] != (previous['FrozenManifests']['negative']['OriginalInputSha256'] if 'FrozenManifests' in previous else previous['NegativeManifestSha256'])):
        raise ValueError('Decoder follow-up frozen corpus differs')
    for kind,key in (('main','FrozenCorpusManifest'),('negative','FrozenNegativeManifest')):
        frozen_reference = previous['FrozenManifests'][kind] if 'FrozenManifests' in previous else previous['Pdf417GridSamplingProof'][key]
        if frozen[kind] != checked(frozen_reference):
            raise ValueError('Decoder follow-up changes the qualified frozen inputs')
    samples = {r['Id']:r for manifest in frozen.values() for r in manifest['Samples']}
    positive_kind = next(k for k in kinds if k.endswith('positive'))
    negative_kind = next(k for k in kinds if k.endswith('negative'))
    expected = {
        positive_kind:{r['Id']:r for r in frozen['main']['Samples'] if r['Format']==positive_format},
        negative_kind:{r['Id']:dict(r,Format=positive_format) for r in samples.values() if not r['Expected']},
        'fabric':{'falsepositives/15.png':samples['falsepositives/15.png']}}
    if any({r['Id']:r for r in manifests[kind]['Samples']} != expected[kind] for kind in kinds):
        raise ValueError('Decoder follow-up diagnostic images, hints or oracles differ')
    for kind,count in (('main',279),('negative',25)):
        schema = [(r['Id'],r['Format'],r['Expected']) for r in frozen[kind]['Samples']]
        for lane in ('before','current'):
            rows = corpora[kind+'-'+lane]
            if len(rows) != count*3 or any([(r['Id'],r['Format'],r['Expected']) for r in rows if r['Engine']==engine] != schema for engine in ('CodeGlyphX','ZXing.Net','ZXing-C++')):
                raise ValueError('Decoder follow-up corpus oracles, hints or readers differ')
    return checked, proof, corpora, manifests, project_packet_costs(root, proof, packets)
