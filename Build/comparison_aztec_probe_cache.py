"""Project compact Aztec probe costs with exact voting and retained-session evidence."""
import json
from comparison_retained_contexts import project_context_costs, project_packet_costs
from comparison_qualification_history import resolve_proof_owner


def project_aztec_probe_cache(root, qualification):
    proof, _, historical = resolve_proof_owner(root, qualification, 'AztecProbeCacheProof')
    if not proof:
        return None
    actual = json.loads((root / 'aztec-probe-cache-equivalence.json').read_text(encoding='utf-8-sig'))
    if (actual != proof['ProbeEquivalence']
            or actual['BaselineCoreSha256'] != proof['PreviousCoreSha256']
            or actual['CandidateCoreSha256'] != proof['CoreSha256']
            or actual['Requests'] != 194168 or actual['Cases'] != 100
            or actual['AcceptedProbes'] != 5848 or actual['AcceptedViewsCompared'] != 457
            or actual['Mismatches'] != 0
            or actual['LaterCancellationOnCachedAcceptedProbesStopped'] is not True
            or actual['LaterClockExpiryOnCachedAcceptedProbesStopped'] is not True
            or actual['ClockScopeDisposalRestoresAcceptance'] is not True
            or actual['MaximumWindows'] != 256 or actual['MaximumCellsPerWindow'] != 81
            or actual['PressureWindows'] != 256 or not 0 < actual['PressureStateCells'] <= 256 * 81
            or actual['UncachedAcceptedProbesAfterCapacity'] != 2
            or proof['Contracts'] != {'net10': 60, 'net8': 60, 'net472': 24}):
        raise ValueError('Aztec probe proof differs from its binaries or voting contracts')
    if len(proof['Experiments']) != 1 or proof['Experiments'][0]['Label'] != 'probe':
        raise ValueError('Missing exploratory Aztec candidate')
    experiment = proof['Experiments'][0]
    rows = experiment['Packets']
    if (len(rows) != 6 or {(row['Kind'], row['Domain']) for row in rows} !=
            {(kind, domain) for kind in ('fabric', 'texture', 'whole') for domain in ('ccd0', 'ccd1')}
            or any(any(row[key] != proof[key] for key in
                       ('Source', 'CoreSha256', 'HostSha256', 'PreviousSource', 'PreviousCoreSha256', 'PreviousHostSha256'))
                   or row['Requests'] != (19 if row['Kind'] == 'whole' else 1) for row in rows)):
        raise ValueError('Exploratory Aztec workload or binary changed')
    project_packet_costs(root, proof, rows)
    projected = project_context_costs(root, qualification, 'AztecProbeCacheProof')
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
    projected.update(title='Historical Aztec probe cache costs' if historical else 'Aztec probe cache costs',
                     anchor='aztec-probe-cache', evidence='aztec-probe-cache-proof.json', evidence_label='Aztec probe cache proof',
                     contracts=proof['ContractNote'], completion_note=' '.join(losses),
                     exploration_note='The proof also retains six exploratory pairs from the same candidate before full qualification. Their complete priming and measured outcomes remain separate from the table below, including inconclusive results and latency losses.')
    return projected
