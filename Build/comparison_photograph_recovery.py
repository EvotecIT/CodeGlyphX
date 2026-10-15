"""Validate photograph recovery proof owners and complete scan costs."""
import hashlib
import json
from comparison_decoder_contracts import validate_native_contracts, validate_profile_diagnostics, validate_expanded_packets
from comparison_retained_contexts import project_context_costs, project_packet_costs
from comparison_qualification_history import resolve_proof_owner
from comparison_code128_photo_set import validate_photographs


def project_photograph_recovery(root, qualification, proof_key, prefix, contract_count, native_checks, baseline_counts, title, contract_note):
    proof, owner, historical = resolve_proof_owner(root, qualification, proof_key)
    if not proof:
        return None

    def read(name):
        return json.loads((root / (prefix + name + '.json')).read_text(encoding='utf-8-sig'))

    def checked(row, key='File', digest='Sha256'):
        name = row[key]
        if '/' in name or '\\' in name:
            raise ValueError('Workspace proof file must stay in its evidence root')
        raw = (root / name).read_bytes()
        if hashlib.sha256(raw).hexdigest().upper() != row[digest]:
            raise ValueError('Photograph recovery evidence bytes differ')
        return json.loads(raw.decode('utf-8-sig'))

    expected_contracts = contract_count if isinstance(contract_count, dict) else {target:contract_count for target in ('net10','net8','net472')}
    if read('proof') != proof or proof['Contracts'] != expected_contracts:
        raise ValueError('Photograph recovery download or framework contracts differ')
    review = proof['ReviewCoverage']
    if review['Head'] != proof['Source'] or review['BoundaryState'] not in ('current', 'candidate-reviewed') or review['UnresolvedFindings']:
        raise ValueError('Workspace review does not cover its source')
    photo_set = validate_photographs(root,proof,checked,baseline_counts)
    frozen = checked(proof['FrozenCorpusManifest'])
    negatives = checked(proof['FrozenNegativeManifest'])
    validate_native_contracts(proof, read('native-arm64'), native_checks)
    if proof['NativeArm64']['DecisionProof']['AdditionalDiagnosticAndAllResultPhotoCalls'] != 20:
        raise ValueError('Photograph native public API controls differ')
    frozen_samples, means, completions = validate_profile_diagnostics(proof, owner, frozen, negatives, checked, read)
    supplemental = validate_expanded_packets(proof, frozen, frozen_samples, checked)
    projected = project_context_costs(root,qualification,proof_key)
    projected['costs'] += project_packet_costs(root,proof,supplemental)
    losses = []
    for row in proof['Packets'] + supplemental:
        packet = json.loads((root/row['PacketFile']).read_text(encoding='utf-8-sig'))
        counts = {engine:sum(s['Metrics']['CorrectImages'] for s in packet['Samples'] if s['Engine']==engine and s['Iteration']>=0) for engine in ('Baseline','Candidate')}
        if counts['Candidate'] < counts['Baseline']:
            losses.append(f"{row['Kind']} on {row['Domain'].upper()}: {counts['Baseline']:g} to {counts['Candidate']:g} correct requests out of {12*row['Requests']} across twelve measured calls.")
    projected.update(title=('Historical ' if historical else '')+title,
        anchor=prefix.rstrip('-'), evidence=prefix+'proof.json', evidence_label=title+' proof',
        contracts=contract_note,
        exploration_note=f"The separate traced nine-image diagnostic records {completions['before']}/27 to {completions['after']}/27 correct rejections after priming, with mean managed allocation {means['before']/1048576:.2f} to {means['after']/1048576:.2f} MiB per call. Every completion and unexpected output is retained. Diagnostic counters include tracing overhead; overlapping stack samples do not establish CPU utilization or comparative latency.",
        completion_note='', completion_notes=losses)
    projected['photographs']=photo_set
    projected['history_note']='These tables retain one decoder session per binary. Earlier fresh-context costs remain separate in the historical context inventory.'
    return projected
