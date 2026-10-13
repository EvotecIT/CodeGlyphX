"""Project corroborated DataBar recovery from qualified binaries and complete outcomes."""
import hashlib
import json
from comparison_retained_contexts import project_context_costs, project_packet_costs
from comparison_qualification_history import resolve_proof_owner


def project_databar_character_recovery(root, qualification):
    proof, _, historical = resolve_proof_owner(root, qualification, 'DataBarCharacterRecoveryProof')
    if not proof:
        return None

    def read(name):
        return json.loads((root / ('databar-character-' + name + '.json')).read_text(encoding='utf-8-sig'))

    if read('proof') != proof:
        raise ValueError('DataBar recovery download differs from its qualification owner')
    if proof['Contracts'] != {'net10': 11, 'net8': 11, 'net472': 11}:
        raise ValueError('DataBar recovery framework contracts differ')
    fixture = proof['Fixture']
    if (fixture['Sha256'] != '0436CD131A7CACFE5A51FA69B714C5015A5B6A8697BBC7152EC93977E4A5AAB3'
            or fixture['UpstreamRevision'] != '33dfdefcb35576612841e614d44ba9edc9aee2b5'
            or fixture['ExpectedCorpusText'] != '02001234567893' or fixture['ExpectedCoreText'] != '0200123456789'):
        raise ValueError('DataBar photo or oracle identity differs')
    review = proof['ReviewCoverage']
    if (review['Head'] != proof['Source'] or review['BoundaryState'] != 'candidate-reviewed'
            or review['UnresolvedFindings'] or review['TargetedConfirmationUsed'] is not True):
        raise ValueError('DataBar recovery review does not cover its repaired source')
    control = read('baseline-regression-proof')
    if (control != proof['BaselineRegression'] or control['BaselineCoreSha256'] != proof['PreviousCoreSha256']
            or control['Failed'] != 6 or control['Passed'] != 2):
        raise ValueError('DataBar baseline regression control differs')
    overlap = read('databar-overlap-remediation')
    if (overlap != proof['OverlapRemediation'] or overlap['FinalSource'] != proof['Source']
            or overlap['Before']['Failed'] != 2 or overlap['Before']['Passed'] != 1
            or overlap['After']['Failed'] != 0 or overlap['After']['Passed'] != 150):
        raise ValueError('DataBar overlap remediation proof differs')
    oracle = read('databar-oracle-verification')
    if (oracle != proof['OracleVerification'] or oracle['Status'] != 'passed'
            or oracle['UpstreamRevision'] != fixture['UpstreamRevision'] or len(oracle['Checks']) != 4
            or {r['Image'] for r in oracle['Checks']} != {'11.png', '18.png', '19.png', '7.png'}
            or any(r['Text'] != fixture['ExpectedCorpusText'] for r in oracle['Checks'])):
        raise ValueError('DataBar immutable oracle verification differs')
    native = read('native-arm64')
    if (native != proof['NativeArm64'] or native['Source'] != proof['Source']
            or native['CoreSha256'] != proof['CoreSha256'] or native['InputFiles']['CodeGlyphX.dll'] != proof['CoreSha256']
            or native['InputFiles']['photo.png'] != fixture['Sha256'] or native['Architecture'] != 'arm64'
            or native['FrameworkRollForward'] != 'Disable' or not native['SelectedFrameworkVersion'] or not native['SDKVersion']
            or native['DecisionProof']['Status'] != 'passed' or native['DecisionProof']['Cases'] != 8
            or native['DecisionProof']['Checks'] != 32
            or native['DecisionProof']['CoreInformationalVersion'].split('+')[-1] != proof['Source']):
        raise ValueError('DataBar native ARM64 functional proof differs from its binary')
    run = read('diagnostic-run')
    if (run != proof['DiagnosticRun'] or run['Status'] != 'passed' or run['Source'] != proof['Source']
            or run['CoreSha256'] != proof['CoreSha256'] or run['HostSha256'] != proof['HostSha256']):
        raise ValueError('DataBar diagnostic execution identity differs')
    summaries = []
    for kind, requests in (('positive', 12), ('negative', 31)):
        diagnostic = proof['Diagnostics'][kind]
        manifest = root / ('databar-character-databar14-' + kind + '-manifest.json')
        if hashlib.sha256(manifest.read_bytes()).hexdigest().upper() != diagnostic['ManifestSha256']:
            raise ValueError('DataBar diagnostic manifest differs')
        lanes = {}
        for lane, source, core in (('baseline', proof['PreviousSource'], proof['PreviousCoreSha256']),
                                   ('current', proof['Source'], proof['CoreSha256'])):
            data = diagnostic['Lanes'][lane]
            raw = (root / data['File']).read_bytes()
            if (data['Source'] != source or data['CoreSha256'] != core or data['Rows'] != requests
                    or hashlib.sha256(raw).hexdigest().upper() != data['Sha256']
                    or run['Outputs'][kind + '-' + lane] != data['Sha256']
                    or run['RawOutputs'][kind + '-' + lane] != data['RawSha256']):
                raise ValueError('DataBar diagnostic outcomes differ from their binary')
            lanes[lane] = json.loads(raw.decode('utf-8-sig'))
        if ([(r['Engine'], r['Id'], r['Expected']) for r in lanes['baseline']]
                != [(r['Engine'], r['Id'], r['Expected']) for r in lanes['current']]):
            raise ValueError('DataBar diagnostic oracles differ between binaries')
        for engine in ('CodeGlyphX', 'ZXing.Net', 'ZXing-C++'):
            before = [r for r in lanes['baseline'] if r['Engine'] == engine]
            current = [r for r in lanes['current'] if r['Engine'] == engine]
            if len(before) != requests or len(current) != requests:
                raise ValueError('Incomplete DataBar diagnostic engine rows')
            summaries.append(f"{kind.title()} {engine}: {sum(r['Status'] == 'Correct' for r in before)} → "
                             f"{sum(r['Status'] == 'Correct' for r in current)}/{requests} correct, "
                             f"{sum(bool(r['Unexpected']) for r in current)} current unexpected images.")
    rows = proof['SupplementalPackets']
    if (len(rows) != 4 or {(r['Kind'], r['Domain']) for r in rows} !=
            {(kind, domain) for kind in ('databar-positive', 'databar-negative') for domain in ('ccd0', 'ccd1')}
            or any(r['Requests'] != (12 if r['Kind'] == 'databar-positive' else 31)
                   or r['ManifestSha256'] != proof['Diagnostics'][r['Kind'].split('-')[1]]['ManifestSha256'] for r in rows)):
        raise ValueError('DataBar supplemental workload changed')
    projected = project_context_costs(root, qualification, 'DataBarCharacterRecoveryProof')
    projected['costs'] += project_packet_costs(root, proof, rows)
    losses = []
    for row in proof['Packets'] + rows:
        packet = json.loads((root / row['PacketFile']).read_text(encoding='utf-8-sig'))
        counts = {engine: sum(s['Metrics']['CorrectImages'] for s in packet['Samples']
                              if s['Engine'] == engine and s['Iteration'] >= 0) for engine in ('Baseline', 'Candidate')}
        if counts['Candidate'] < counts['Baseline']:
            losses.append(f"{row['Kind']} on {row['Domain'].upper()}: {counts['Baseline']:g} → "
                          f"{counts['Candidate']:g} correct requests out of {12 * row['Requests']} across twelve measured calls.")
    projected.update(title=('Historical ' if historical else '') + 'Corroborated DataBar photo recovery and costs',
                     anchor='databar-character-recovery', evidence='databar-character-proof.json',
                     evidence_label='DataBar photo recovery proof',
                     contracts=' '.join(summaries) + ' Eleven regression cases pass on each of .NET 10, .NET 8 and .NET Framework 4.7.2, with span paths exercised on modern targets. The exact managed binary passes 32 native ARM64 public decoding checks; timing remains specific to Windows.',
                     exploration_note='', completion_note=' '.join(losses))
    projected['history_note']='These tables retain one decoder session per binary. Earlier fresh-context measurements remain separately recorded in the historical context inventory and cannot be compared directly with these reused sessions.'
    return projected
