"""Project DPM retry ownership, shared morphology and complete scan costs."""
import hashlib
import json
from comparison_decoder_contracts import validate_native_contracts, validate_profile_diagnostics, validate_expanded_packets
from comparison_retained_contexts import project_context_costs, project_packet_costs
from comparison_qualification_history import resolve_proof_owner
from comparison_dpm_equivalence import validate_morphology, validate_variants


def project_dpm_workspace(root, qualification):
    proof, owner, historical = resolve_proof_owner(root, qualification, 'DpmWorkspaceProof')
    if not proof:
        return None

    def read(name):
        return json.loads((root / ('dpm-workspace-' + name + '.json')).read_text(encoding='utf-8-sig'))

    def checked(row, key='File', digest='Sha256'):
        name = row[key]
        if '/' in name or '\\' in name:
            raise ValueError('Workspace proof file must stay in its evidence root')
        raw = (root / name).read_bytes()
        if hashlib.sha256(raw).hexdigest().upper() != row[digest]:
            raise ValueError('DPM workspace evidence bytes differ')
        return json.loads(raw.decode('utf-8-sig'))

    if read('proof') != proof or proof['Contracts'] != {'net10':15, 'net8':15, 'net472':15}:
        raise ValueError('DPM workspace download or framework contracts differ')
    review = proof['ReviewCoverage']
    if review['Head'] != proof['Source'] or review['BoundaryState'] != 'current' or review['UnresolvedFindings']:
        raise ValueError('Workspace review does not cover its source')
    validate_morphology(checked(proof['MorphologyEquivalence']), proof)
    validate_variants(checked(proof['VariantEquivalence']), proof, checked(proof['ProfileManifest']))
    if proof['InputMaterializerCoreSha256'] != proof['PreviousCoreSha256']:
        raise ValueError('DPM input materialization differs from the preceding core')
    frozen = checked(proof['FrozenCorpusManifest'])
    negatives = checked(proof['FrozenNegativeManifest'])
    validate_native_contracts(proof, read('native-arm64'), 105)
    frozen_samples, means, completions = validate_profile_diagnostics(proof, owner, frozen, negatives, checked, read)
    supplemental = validate_expanded_packets(proof, frozen, frozen_samples, checked)
    projected = project_context_costs(root,qualification,'DpmWorkspaceProof')
    projected['costs'] += project_packet_costs(root,proof,supplemental)
    losses = []
    for row in proof['Packets'] + supplemental:
        packet = json.loads((root/row['PacketFile']).read_text(encoding='utf-8-sig'))
        counts = {engine:sum(s['Metrics']['CorrectImages'] for s in packet['Samples'] if s['Engine']==engine and s['Iteration']>=0) for engine in ('Baseline','Candidate')}
        if counts['Candidate'] < counts['Baseline']:
            losses.append(f"{row['Kind']} on {row['Domain'].upper()}: {counts['Baseline']:g} to {counts['Candidate']:g} correct requests out of {12*row['Requests']} across twelve measured calls.")
    projected.update(title=('Historical ' if historical else '')+'DPM preprocessing storage and scan costs',
        anchor='dpm-workspace', evidence='dpm-workspace-proof.json', evidence_label='DPM workspace proof',
        contracts='All 19,240 actual square-filter cases preserve the two border policies. Across 3,414 retry cases, all 14,886 returned images match the preceding binary, including retained and mutated outputs. Seventeen cancellation, early-disposal and consumer-exception cases preserve ownership. Fifteen regression contracts pass on each framework; the exact core and test assembly pass 105 native ARM64 checks. These private preprocessing checks do not establish image recognition or comparative speed.',
        exploration_note=f"The separate traced nine-image diagnostic records {completions['before']}/27 to {completions['after']}/27 correct rejections after priming, with mean managed allocation {means['before']/1048576:.2f} to {means['after']/1048576:.2f} MiB per call. Every completion and unexpected output is retained. Diagnostic counters include tracing overhead; overlapping stack samples do not establish CPU utilization or comparative latency.",
        completion_note='', completion_notes=losses)
    projected['history_note']='These tables retain one decoder session per binary. Earlier fresh-context costs remain separate in the historical context inventory.'
    return projected
