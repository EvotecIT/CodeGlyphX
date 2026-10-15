"""Project DataBar workspace reuse with exact candidates, allocations and retained costs."""
import hashlib
import json
from comparison_retained_contexts import project_context_costs, project_packet_costs
from comparison_qualification_history import resolve_proof_owner


def project_databar_recovery_buffers(root, qualification):
    proof, _, historical = resolve_proof_owner(root, qualification, 'DataBarRecoveryBufferProof')
    if not proof:
        return None

    def read(name):
        return json.loads((root / ('databar-buffers-' + name + '.json')).read_text(encoding='utf-8-sig'))

    def checked(row, key='File', digest='Sha256'):
        raw = (root / row[key]).read_bytes()
        if hashlib.sha256(raw).hexdigest().upper() != row[digest]:
            raise ValueError('DataBar buffer evidence bytes differ')
        return json.loads(raw.decode('utf-8-sig'))

    if read('proof') != proof:
        raise ValueError('DataBar buffer download differs from its qualification owner')
    if proof['Contracts'] != {'net10': 11, 'net8': 11, 'net472': 11}:
        raise ValueError('DataBar buffer framework contracts differ')
    review = proof['ReviewCoverage']
    if review['Head'] != proof['Source'] or review['BoundaryState'] != 'current' or review['UnresolvedFindings']:
        raise ValueError('DataBar buffer review does not cover its source')
    eq = read('equivalence')
    if (eq != proof['BufferEquivalence'] or eq['Source'] != proof['Source']
            or eq['CandidateCoreSha256'] != proof['CoreSha256'] or eq['BaselineCoreSha256'] != proof['PreviousCoreSha256']
            or eq['Cases'] != 344 or eq['LocatedCandidates'] != 732 or eq['Mismatches'] != 0
            or eq['InputImages'] != 43 or eq['RotationPolarityVariants'] != 4
            or eq['PixelLayouts'] != ['PackedRgba32', 'PaddedBgra32'] or eq['PaddedStrideBytes'] != 12
            or eq['DirtyIntPoolBucketsSeededBeforeEveryCall'] is not True):
        raise ValueError('DataBar dirty-pool equivalence proof differs')
    packets = proof['CollectorPackets']
    if len(packets) != 2 or {r['Kind'] for r in packets} != {'positive', 'negative'}:
        raise ValueError('Missing DataBar collector workloads')
    cases = candidates = 0
    for packet in packets:
        expected = (12 if packet['Kind'] == 'positive' else 31) * 8
        if packet['Cases'] != expected:
            raise ValueError('DataBar collector variant count differs')
        before, current = (checked(packet['Lanes'][lane]) for lane in ('before', 'final'))
        for lane, data, core in (('before', before, proof['PreviousCoreSha256']), ('final', current, proof['CoreSha256'])):
            if (data['CoreSha256'] != core or data['CoreSha256'] != packet['Lanes'][lane]['CoreSha256']
                    or data['ManifestSha256'] != packet['Lanes'][lane]['ManifestSha256']
                    or data['ManifestSha256'] != proof['Diagnostics'][packet['Kind']]['ManifestSha256']
                    or len(data['Rows']) != expected or data['Mode'] != 'candidates'):
                raise ValueError('DataBar collector inputs or binary differ')
        if before['Rows'] != current['Rows']:
            raise ValueError('DataBar collector candidates or metadata changed')
        cases += len(current['Rows'])
        candidates += sum(len(r['Candidates']) for r in current['Rows'])
    if cases != eq['Cases'] or candidates != eq['LocatedCandidates']:
        raise ValueError('DataBar actual collector totals differ')
    native = read('native-arm64')
    if (native != proof['NativeArm64'] or native['Source'] != proof['Source'] or native['CoreSha256'] != proof['CoreSha256']
            or native['InputFiles']['CodeGlyphX.dll'] != proof['CoreSha256'] or native['Architecture'] != 'arm64'
            or not native['SDKVersion'] or not native['SelectedFrameworkVersion'] or native['FrameworkRollForward'] != 'Disable'
            or native['DecisionProof']['CoreSha256'] != proof['CoreSha256']
            or native['DecisionProof']['FixtureSha256'] != native['InputFiles']['photo.png']
            or native['InputFiles']['photo.png'] != '0436CD131A7CACFE5A51FA69B714C5015A5B6A8697BBC7152EC93977E4A5AAB3'
            or native['DecisionProof']['Status'] != 'passed' or native['DecisionProof']['Cases'] != 8
            or native['DecisionProof']['Checks'] != 32
            or native['DecisionProof']['CoreInformationalVersion'].split('+')[-1] != proof['Source']):
        raise ValueError('DataBar buffer native proof differs from its binary')
    diagnostic = read('allocation-diagnostic')
    if (diagnostic != proof['AllocationDiagnostic'] or diagnostic['Source'] != proof['Source']
            or diagnostic['CoreSha256'] != proof['CoreSha256'] or diagnostic['PreviousCoreSha256'] != proof['PreviousCoreSha256']
            or diagnostic['CallsPerBinary'] != 5 or diagnostic['ImagesPerCall'] != 31
            or diagnostic['CorrectEveryCall'] != 31 or diagnostic['UnexpectedEveryCall'] != 0):
        raise ValueError('DataBar allocation diagnostic identity differs')
    rows = proof['DiagnosticPackets']
    if len(rows) != 2 or {r['Lane'] for r in rows} != {'before', 'after'}:
        raise ValueError('Missing DataBar allocation diagnostic lane')
    means = {}
    for row in rows:
        data = checked(row); profile = checked(row, 'ProfileFile', 'ProfileSha256')
        expected_core = proof['PreviousCoreSha256'] if row['Lane'] == 'before' else proof['CoreSha256']
        if (data['CoreSha256'] != expected_core or row['CoreSha256'] != expected_core or len(data['Rows']) != 5
                or any(len(r['Outcomes']) != 31 or any(x['Status'] != 'Correct' or x['Unexpected'] for x in r['Outcomes']) for r in data['Rows'])
                or profile['CoreSha256'] != expected_core or profile['ProbeOutcomeSha256'] != row['Sha256']
                or len(profile['DerivedFromTraceSha256']) != 64
                or profile['AllocationTicks'] < 1 or profile['ThreadTimeSamples'] < 1):
            raise ValueError('Incomplete DataBar allocation outcomes or profile')
        means[row['Lane']] = sum(r['AllocatedBytes'] for r in data['Rows']) / 5
    if (means['before'] != diagnostic['BeforeMeanAllocatedBytes'] or means['after'] != diagnostic['CurrentMeanAllocatedBytes']
            or means['after'] / means['before'] != diagnostic['AllocationRatio']):
        raise ValueError('DataBar actual allocation counters differ')
    for key in ('SupplementalPackets', 'ExploratoryPackets'):
        rows = proof[key]
        if (len(rows) != 4 or {(r['Kind'], r['Domain']) for r in rows} !=
                {(kind, domain) for kind in ('databar-positive', 'databar-negative') for domain in ('ccd0', 'ccd1')}
                or any(r['Requests'] != (12 if r['Kind'] == 'databar-positive' else 31)
                       or r['ManifestSha256'] != proof['Diagnostics'][r['Kind'].split('-')[1]]['ManifestSha256'] for r in rows)):
            raise ValueError('DataBar retained comparison workload changed')
    projected = project_context_costs(root, qualification, 'DataBarRecoveryBufferProof')
    projected['costs'] += project_packet_costs(root, proof, proof['SupplementalPackets'])
    project_packet_costs(root, proof, proof['ExploratoryPackets'])
    losses = []
    for label, rows in (('Final', proof['Packets'] + proof['SupplementalPackets']), ('Exploratory', proof['ExploratoryPackets'])):
        for row in rows:
            packet = json.loads((root / row['PacketFile']).read_text(encoding='utf-8-sig'))
            counts = {engine: sum(s['Metrics']['CorrectImages'] for s in packet['Samples']
                                  if s['Engine'] == engine and s['Iteration'] >= 0) for engine in ('Baseline', 'Candidate')}
            if counts['Candidate'] < counts['Baseline']:
                losses.append(f"{label} {row['Kind']} on {row['Domain'].upper()}: {counts['Baseline']:g} to "
                              f"{counts['Candidate']:g} correct requests out of {12 * row['Requests']} across twelve measured calls.")
    databar_costs = [row for row in projected['costs'] if row['kind'].startswith('databar-')]
    reductions = [float(row['allocation_reduction'].rstrip('%')) for row in databar_costs]
    ratios = [float(row['ratio']) for row in databar_costs]
    result_note = (f'Managed allocation falls by {min(reductions):.1f}% to {max(reductions):.1f}% on the unchanged DataBar workloads. '
                   f'Median elapsed time ranges from {min(ratios):.3f} to {max(ratios):.3f} times the preceding binary across the two processor domains; some passes are slower. ')
    projected.update(title=('Historical ' if historical else '') + 'DataBar recovery buffer allocation and costs',
                     anchor='databar-recovery-buffers', evidence='databar-buffers-proof.json', evidence_label='DataBar buffer reuse proof',
                     contracts=result_note + 'The actual recovery collector matches all 732 located candidates across 344 variants, including dirty pooled storage, rotation, inversion, packed RGBA and padded BGRA. Existing DataBar regressions pass on all three frameworks, and the exact binary passes 32 native ARM64 public decoding checks.',
                     exploration_note='Four exploratory DataBar pairs remain separately retained from the final table. Every priming and measured per-image outcome is included. Five traced negative batches per binary provide allocation diagnostics; their overlapping stack samples do not establish CPU use or competitive latency.',
                     completion_note=' '.join(losses))
    projected['history_note']='These tables retain one decoder session per binary. Earlier fresh-context costs remain separate in the historical context inventory.'
    return projected
