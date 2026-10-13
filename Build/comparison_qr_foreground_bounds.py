"""Project QR foreground decisions and costs with exact artifact identities."""
import hashlib
import json
from comparison_retained_contexts import project_context_costs, project_packet_costs
from comparison_qualification_history import resolve_proof_owner


def project_qr_foreground_bounds(root, qualification):
    proof, _, historical = resolve_proof_owner(root, qualification, 'QrForegroundBoundsProof')
    if not proof:
        return None

    def read(name):
        return json.loads((root / ('qr-foreground-bounds-' + name + '.json')).read_text(encoding='utf-8-sig'))

    if read('proof') != proof:
        raise ValueError('QR foreground downloadable proof differs from its qualification owner')
    links = read('history-links')
    if links != proof['HistoricalLinkRepair'] or len(links['Changes']) != 2:
        raise ValueError('Legacy qualification link repair proof differs')
    for row in links['Changes']:
        linked_path, original_path = root / row['File'], root / row['OriginalFile']
        linked = json.loads(linked_path.read_text(encoding='utf-8-sig'))
        original = json.loads(original_path.read_text(encoding='utf-8-sig'))
        prior = json.loads((root / row['PriorQualificationPacket']).read_text(encoding='utf-8-sig'))
        if (hashlib.sha256(linked_path.read_bytes()).hexdigest().upper() != row['LinkedSha256']
                or hashlib.sha256(original_path.read_bytes()).hexdigest().upper() != row['OriginalSha256']
                or 'PriorQualificationPacket' in original
                or linked != {**original, 'PriorQualificationPacket': row['PriorQualificationPacket']}
                or linked['MeasurementSource'] != row['Source'] or linked['CandidateCoreSha256'] != row['CoreSha256']
                or linked['PreviousMeasurementSource'] != prior['MeasurementSource']
                or linked['NegativeRotationSources']['previous'] !=
                   {'Source': prior['MeasurementSource'], 'CoreSha256': prior['CandidateCoreSha256']}):
            raise ValueError('Legacy qualification repair changed its original data or predecessor identity')
    for mode, flags in (('vector256', (True, True)), ('vector128', (False, True)), ('scalar', (False, False))):
        data = read('equivalence-' + mode)
        if (data != proof['Equivalence'][mode] or data['CoreSha256'] != proof['CoreSha256']
                or data['Cases'] != 286720 or data['PixelClassificationsCompared'] != 144252928
                or data['Mismatches'] != 0 or data['CancelledAndExpiredBoundsStop'] is not True
                or (data['Vector256HardwareAccelerated'], data['Vector128HardwareAccelerated']) != flags):
            raise ValueError('QR foreground decision proof differs from its core or execution mode')
    native = read('native-arm64')
    if (native != proof['NativeArm64'] or native['Source'] != proof['Source']
            or native['InputFiles']['CodeGlyphX.dll'] != proof['CoreSha256']
            or native['Architecture'] != 'arm64'
            or native['DecisionProof'] != proof['Equivalence']['vector128']):
        raise ValueError('QR foreground native ARM64 proof differs from its qualified binary')
    extraction = read('source-extraction')
    if (extraction != proof['SourceExtraction'] or extraction['Candidate'] != proof['Source']
            or len(extraction['Checks']) != 4 or any(r['VerbatimMoved'] is not True for r in extraction['Checks'])):
        raise ValueError('QR foreground source extraction proof differs')
    if proof['Contracts'] != {'net10': 5, 'net8': 5, 'net472': 0}:
        raise ValueError('QR foreground framework contracts differ')
    review = proof['ReviewCoverage']
    if review['Head'] != proof['Source'] or review['BoundaryState'] != 'current' or review['UnresolvedFindings']:
        raise ValueError('QR foreground independent review does not cover its source')
    if len(proof['Experiments']) != 1 or proof['Experiments'][0]['Label'] != 'foreground-bounds':
        raise ValueError('Missing exploratory QR foreground pairs')
    experiment = proof['Experiments'][0]
    rows = experiment['Packets']
    identity_keys = ('Source', 'CoreSha256', 'HostSha256', 'PreviousSource', 'PreviousCoreSha256', 'PreviousHostSha256')
    if (any(experiment[k] != proof[k] for k in identity_keys)
            or len(rows) != 8 or {(r['Kind'], r['Domain']) for r in rows} !=
            {(kind, domain) for kind in ('qr', 'texture', 'whole', 'fabric') for domain in ('ccd0', 'ccd1')}
            or any(any(r[k] != proof[k] for k in identity_keys)
                   or r['Requests'] != (19 if r['Kind'] == 'whole' else 1) for r in rows)):
        raise ValueError('Exploratory QR foreground workload or binary changed')
    project_packet_costs(root, proof, rows)
    projected = project_context_costs(root, qualification, 'QrForegroundBoundsProof')
    losses = []
    for label, packets in (('Final', proof['Packets']), ('Exploratory', rows)):
        for row in packets:
            packet = json.loads((root / row['PacketFile']).read_text(encoding='utf-8-sig'))
            counts = {engine: sum(sample['Metrics']['CorrectImages'] for sample in packet['Samples']
                                  if sample['Engine'] == engine and sample['Iteration'] >= 0)
                      for engine in ('Baseline', 'Candidate')}
            if counts['Candidate'] < counts['Baseline']:
                losses.append(f"{label} {row['Kind']} on {row['Domain'].upper()}: "
                              f"{counts['Baseline']:g} → {counts['Candidate']:g} correct image requests "
                              f"out of {12 * row['Requests']} across twelve measured calls.")
    projected.update(title='Historical QR foreground bounds and costs' if historical else 'QR foreground bounds and costs',
                     anchor='qr-foreground-bounds', evidence='qr-foreground-bounds-proof.json',
                     evidence_label='QR foreground bounds proof', contracts=proof['ContractNote'],
                     completion_note=' '.join(losses),
                     exploration_note='Eight exploratory pairs on this same production binary remain separate from the final table. They include QR, texture, whole-image and fabric workloads, with every completion loss retained. Native ARM64 results prove pixel decisions only; the timing tables describe the Windows host.')
    return projected
