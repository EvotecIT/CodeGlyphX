"""Project PDF417 cache costs with exact run-equivalence and retained-session evidence."""
import json
from comparison_retained_contexts import project_context_costs, project_packet_costs
from comparison_qualification_history import resolve_proof_owner


def project_pdf417_segment_cache(root, qualification):
    proof, owner, historical = resolve_proof_owner(root, qualification, 'Pdf417SegmentCacheProof')
    if not proof:
        return None
    actual = json.loads((root / 'pdf417-segment-cache-equivalence.json').read_text(encoding='utf-8-sig'))
    if (actual != proof['RunEquivalence']
            or actual['BaselineCoreSha256'] != proof['PreviousCoreSha256']
            or actual['CandidateCoreSha256'] != proof['CoreSha256']
            or actual['Requests'] != 95100 or actual['ShapePatternCases'] != 30
            or actual['Mismatches'] != 0 or actual['PrecisePolarityAndRunWidths'] is not True
            or actual['LaterCancellationOnCachedRowsStopped'] is not True
            or actual['ByteAllowance'] != 8 * 1024 * 1024 or actual['EntryAllowance'] != 32768
            or actual['BaselineEntryPressureCount'] != 32768 or actual['CandidateEntryPressureCount'] != 32768
            or not 0 <= actual['BaselinePressureBytes'] <= actual['ByteAllowance']
            or not 0 <= actual['CandidatePressureBytes'] <= actual['ByteAllowance']
            or any(proof['Contracts'][target] <= 150 for target in ('net10', 'net8', 'net472'))):
        raise ValueError('PDF417 cache proof differs from its binaries or run contracts')
    if {row['Label'] for row in proof['Experiments']} != {'cache', 'cursor'} or len(proof['Experiments']) != 2:
        raise ValueError('Missing exploratory PDF417 candidate')
    for experiment in proof['Experiments']:
        rows = experiment['Packets']
        if (len(rows) != 6 or {(row['Kind'], row['Domain']) for row in rows} !=
                {(kind, domain) for kind in ('fabric', 'texture', 'whole') for domain in ('ccd0', 'ccd1')}
                or len({(row['Source'], row['CoreSha256'], row['HostSha256']) for row in rows}) != 1
                or any(row['PreviousSource'] != proof['PreviousSource'] or row['PreviousCoreSha256'] != proof['PreviousCoreSha256']
                       or row['PreviousHostSha256'] != proof['PreviousHostSha256']
                       or row['Requests'] != (19 if row['Kind'] == 'whole' else 1) for row in rows)):
            raise ValueError('Exploratory PDF417 workload or binary changed')
        for row in rows:
            project_packet_costs(root, row, [row])
    projected = project_context_costs(root, qualification, 'Pdf417SegmentCacheProof')
    if (len(proof['Followups']) != 1 or proof['Followups'][0]['Kind'] != 'whole followup'
            or proof['Followups'][0]['Domain'] != 'ccd0' or proof['Followups'][0]['Requests'] != 19
            or proof['Followups'][0]['ManifestSha256'] != owner['LatencyManifestSha256']):
        raise ValueError('Whole-batch follow-up differs from the original workload')
    projected['costs'] += project_packet_costs(root, proof, proof['Followups'])
    projected.update(title='Historical PDF417 scanline cache costs' if historical else 'PDF417 scanline cache costs',
                     anchor='pdf417-segment-cache', evidence='pdf417-segment-cache-proof.json', evidence_label='PDF417 cache proof',
                     contracts=proof['ContractNote'],
                     exploration_note='The proof also retains six exploratory pairs for segmented storage and six for a rejected recent-segment cursor. The cursor had no consistent latency benefit and produced slower fabric/texture medians and completion losses; it is absent from this source. Exploratory and final qualification costs remain separate.')
    return projected
