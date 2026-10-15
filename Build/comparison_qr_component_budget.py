"""Project QR budget contracts with exact identities and retained completion losses."""
import json
from comparison_retained_contexts import project_context_costs, project_packet_costs
from comparison_qualification_history import resolve_proof_owner


def project_qr_component_budget(root, qualification):
    proof, _, historical = resolve_proof_owner(root, qualification, 'QrComponentBudgetProof')
    if not proof:
        return None
    def read(name):
        return json.loads((root / ('qr-component-budget-' + name + '.json')).read_text(encoding='utf-8-sig'))
    if read('proof') != proof:
        raise ValueError('QR downloadable proof differs from its qualification owner')
    actual = read('equivalence')
    if (actual != proof['ComponentEquivalence']
            or actual['BaselineCoreSha256'] != proof['PreviousCoreSha256']
            or actual['CandidateCoreSha256'] != proof['CoreSha256']
            or actual['ImagePolarityThresholdCases'] != 288
            or actual['PixelClassificationsCompared'] != 12279996
            or actual['AcceptedComponentsCompared'] != 133 or actual['Mismatches'] != 0
            or actual['CancelledAndExpiredTraversalStops'] is not True
            or actual['UnboundedTraversalPreserved'] is not True):
        raise ValueError('QR component proof differs from its binaries or traversal contracts')
    before, current = read('deadline-before'), read('deadline-current')
    if (before != proof['DeadlineDiagnosticBefore'] or current != proof['DeadlineDiagnosticCurrent']
            or current['CoreSha256'] != proof['CoreSha256']
            or before['InvalidBufferFailure'] != 'InvalidInput' or current['InvalidBufferFailure'] != 'InvalidInput'
            or len(before['Rows']) != 6 or len(current['Rows']) != 6
            or any(r['Found'] or not r['Expired'] or r['Failure'] != 'InvalidInput' for r in before['Rows'])
            or any(r['Found'] or not r['Expired'] or r['Failure'] != 'Payload' for r in current['Rows'])
            or any(r['LocalBudgetMilliseconds'] != 0 or r['AmbientDeadlineMilliseconds'] != 10 for r in (before, current))):
        raise ValueError('QR conversion deadline classification proof differs')
    for mode in ('single', 'all'):
        data = read('cancellation-' + mode)
        if (data != proof['CancellationDiagnostics'][mode] or data['CoreSha256'] != proof['CoreSha256']
                or data['CallKind'] != mode or len(data['Rows']) != 6
                or {(r['Side'], r['DelayMilliseconds']) for r in data['Rows']} !=
                   {(side, delay) for side in (2048, 4096) for delay in (5, 40, 120)}
                or any(r['Found'] or r['LocalBudgetMilliseconds'] != 0
                       or r['CancellationRequestedBeforeReturn'] != (r['CancelledAtMs'] <= r['FinishedAtMs'])
                       or abs(r['PostCancellationMs'] - max(0, r['FinishedAtMs'] - r['CancelledAtMs'])) > 1e-8
                       for r in data['Rows'])
                or not any(r['CancellationRequestedBeforeReturn'] for r in data['Rows'])):
            raise ValueError('Public QR cancellation diagnostic differs from its caller or result')
    if proof['Contracts'] != {'net10': 11, 'net8': 11, 'net472': 0}:
        raise ValueError('QR nested budget and partial-result contracts differ')
    review = proof['ReviewCoverage']
    if review['Head'] != proof['Source'] or review['BoundaryState'] != 'current' or review['UnresolvedFindings']:
        raise ValueError('QR remediation review does not cover its current boundary')
    if len(proof['Experiments']) != 1 or proof['Experiments'][0]['Label'] != 'component':
        raise ValueError('Missing exploratory QR candidate')
    experiment = proof['Experiments'][0]
    rows = experiment['Packets']
    if (experiment['Source'] == proof['Source'] or experiment['CoreSha256'] != before['CoreSha256']
            or len(rows) != 6 or {(r['Kind'], r['Domain']) for r in rows} !=
            {(kind, domain) for kind in ('fabric', 'texture', 'whole') for domain in ('ccd0', 'ccd1')}
            or any(any(r[k] != experiment[k] for k in ('Source', 'CoreSha256', 'HostSha256'))
                   or any(r[k] != proof[k] for k in ('PreviousSource', 'PreviousCoreSha256', 'PreviousHostSha256'))
                   or r['Requests'] != (19 if r['Kind'] == 'whole' else 1) for r in rows)):
        raise ValueError('Exploratory QR workload or binary changed')
    for row in rows:
        project_packet_costs(root, row, [row])
    projected = project_context_costs(root, qualification, 'QrComponentBudgetProof')
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
    projected.update(title='Historical QR cancellation and component costs' if historical else 'QR cancellation and component costs',
                     anchor='qr-component-budget', evidence='qr-component-budget-proof.json', evidence_label='QR budget and component proof',
                     contracts=proof['ContractNote'], completion_note=' '.join(losses),
                     exploration_note='Six exploratory pairs from the earlier candidate remain separate from the table below. They include completion losses and a whole-image latency increase. Public cancellation response samples are diagnostic evidence, with no timing threshold or portable latency claim.')
    return projected
