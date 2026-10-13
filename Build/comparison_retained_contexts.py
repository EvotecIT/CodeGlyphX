"""Project retained decoder contexts only with complete per-request evidence."""
import hashlib
import json
from comparison_packet_identity import project_paired_cost, validate_paired_packet
from comparison_qualification_history import resolve_proof_owner


def validate_retained_packet(packet, outcomes, proof, domain, requests, manifest_hash):
    validate_paired_packet(packet, proof['Source'], proof['CoreSha256'], proof['PreviousCoreSha256'], domain, requests, manifest_hash)
    metadata = packet['Metadata']
    if (not metadata.get('benchmark.ContextLifetime', '').startswith('RetainedEngineContextsV1:')
            or metadata['benchmark.CandidateHostSha256'] != proof['HostSha256']
            or metadata['benchmark.BaselineHostSha256'] != proof['PreviousHostSha256']
            or metadata['benchmark.CandidateCoreInformationalVersion'].split('+')[-1] != proof['Source']
            or metadata['benchmark.BaselineCoreInformationalVersion'].split('+')[-1] != proof['PreviousSource']
            or metadata.get('warmupCount') != '3' or metadata.get('runOrder') != 'GroupedRotated'
            or outcomes['RunId'] != packet['RunId'] or len(outcomes['Calls']) != 30):
        raise ValueError('Retained decoder lifetime or outcome evidence differs')
    identities = []
    for engine in ('Baseline', 'Candidate'):
        samples = [r for r in packet['Samples'] if r['Engine'] == engine and r['Iteration'] >= 0]
        if {r['Iteration'] for r in samples} != set(range(12)) or any(r['Status'] != 0 for r in samples):
            raise ValueError('Missing, duplicated or failed measured call')
        ids = {r['Metrics']['ContextIdentity'] for r in samples}
        if len(ids) != 1:
            raise ValueError('Decoder session was recreated between measurements')
        identities.append(next(iter(ids)))
        calls = [r for r in outcomes['Calls'] if r['Engine'] == engine]
        if len(calls) != 15 or {r['Iteration'] for r in calls} != set(range(-3, 12)):
            raise ValueError('Missing or duplicated priming or measured outcome')
        for call in calls:
            rows = call['Outcomes']
            if len(rows) != requests:
                raise ValueError('Incomplete per-request evidence')
            if call['Iteration'] >= 0:
                sample = next(r for r in samples if r['Iteration'] == call['Iteration'])
                counts = {'CorrectImages': sum(r['Status'] == 'Correct' for r in rows),
                          'InconclusiveImages': sum(r['Status'] == 'Inconclusive' for r in rows),
                          'UnexpectedImages': sum(bool(r['Unexpected']) for r in rows)}
                if any(sample['Metrics'][key] != count for key, count in counts.items()):
                    raise ValueError('Recorded outcomes differ from measured recognition metrics')
    if identities[0] == identities[1]:
        raise ValueError('Both binaries share a decoder session')
    schemas = {json.dumps([(r['Id'], r['Format'], r['Expected']) for r in call['Outcomes']], sort_keys=True)
               for call in outcomes['Calls']}
    if len(schemas) != 1:
        raise ValueError('Request oracles differ between priming and measured calls')


def project_packet_costs(root, proof, packets):
    costs = []
    for row in packets:
        raw = (root / row['OutcomesFile']).read_bytes()
        if hashlib.sha256(raw).hexdigest().upper() != row['OutcomesSha256']:
            raise ValueError('Per-request outcome file changed')
        packet = json.loads((root / row['PacketFile']).read_text(encoding='utf-8-sig'))
        outcomes = json.loads(raw.decode('utf-8-sig'))
        validate_retained_packet(packet, outcomes, proof, row['Domain'], row['Requests'], row['ManifestSha256'])
        costs.append({'kind': row['Kind'], 'requests': row['Requests'], **project_paired_cost(packet,
            proof['Source'], proof['CoreSha256'], proof['PreviousCoreSha256'], row['Domain'], row['Requests'], row['ManifestSha256'])})
    return costs


def project_context_costs(root, qualification, key):
    proof, owner, historical = resolve_proof_owner(root, qualification, key)
    if not proof:
        return None
    previous = owner['NegativeRotationSources']['previous']
    if (proof['Source'] != owner['MeasurementSource']
            or proof['CoreSha256'] != owner['CandidateCoreSha256']
            or proof['PreviousSource'] != previous['Source'] or proof['PreviousCoreSha256'] != previous['CoreSha256']
            or len(proof['Packets']) != 10
            or {(r['Kind'], r['Domain']) for r in proof['Packets']} !=
               {(kind, domain) for kind in ('fabric', 'texture', 'whole', 'retail-positive', 'retail-negative') for domain in ('ccd0', 'ccd1')}
            or any(r['Requests'] != {'fabric': 1, 'texture': 1, 'whole': 19, 'retail-positive': 87, 'retail-negative': 9}[r['Kind']] for r in proof['Packets'])):
        raise ValueError('Retained context proof belongs to another candidate or workload')
    costs = project_packet_costs(root, proof, proof['Packets'])
    return {'historical': historical, 'source': proof['Source'], 'changes': proof['Changes'], 'scope': proof['Scope'], 'limits': proof['Limits'],
            'history_note': proof['HistoricalContextCorrection'], 'contracts': '', 'exploration_note': '', 'costs': costs}


def project_retained_contexts(root, qualification):
    proof = project_context_costs(root, qualification, 'RetainedContextProof')
    if proof:
        proof.update(title='Historical retained decoder sessions and deadline costs' if proof['historical'] else 'Retained decoder sessions and deadline costs',
                     anchor='retained-contexts', evidence='retained-contexts-proof.json', evidence_label='Session and deadline proof')
    return proof


def _append_scan_cost_table(lines, costs):
    lines += ['| Workload | Requests | Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Previous → current correct range | Previous → current inconclusive range | Previous → current unexpected range |',
              '| --- | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: |']
    for row in costs:
        lines.append(f'| {row["kind"]} | {row["requests"]} | {row["domain"]} | {row["before_ms"]} → {row["current_ms"]} | {row["ratio"]} | {row["before_mib"]} → {row["current_mib"]} | {row["before_correct"]} → {row["current_correct"]} | {row["before_inconclusive"]} → {row["current_inconclusive"]} | {row["before_unexpected"]} → {row["current_unexpected"]} |')


def append_retained_context_section(lines, proof):
    if not proof:
        return
    lines += ['', '## ' + proof['title'], '',
              f'These measurements retain their original source {proof["source"]}.' if proof['historical'] else '', '',
              proof['changes'], '', proof.get('contracts', ''), '', proof.get('exploration_note', ''), '',
              proof.get('completion_note', ''), '', proof['scope'], '', proof['history_note'], '']
    if proof.get('completion_notes'):
        lines += [''] + ['- ' + note for note in proof['completion_notes']] + ['']
    if proof.get('kernel_costs'):
        lines += ['### Component collection only', '', proof['kernel_note'], '',
                  '| Domain | Input variants | Identical components | Previous → current median ms | Current / previous | Previous → current mean managed MiB |',
                  '| --- | ---: | ---: | ---: | ---: | ---: |']
        for row in proof['kernel_costs']:
            lines.append(f'| {row["domain"]} | {row["variants"]} | {row["components"]} | {row["before_ms"]} → {row["current_ms"]} | {row["ratio"]} | {row["before_mib"]} → {row["current_mib"]} |')
        lines += ['', '### Complete scans', '']
    _append_scan_cost_table(lines, proof['costs'])
    if proof.get('exploratory_costs'):
        lines += ['', '### Exploratory complete scans', '', 'Every exploratory public-scan pass remains separate from the final table, with all priming and measured outcomes retained.', '']
        _append_scan_cost_table(lines, proof['exploratory_costs'])
    lines += ['', proof['limits'], '', f'[{proof["evidence_label"]}](evidence/{proof["evidence"]}) · [Historical context inventory](evidence/retained-contexts-history.json). Each listed packet links its complete priming and measured per-image outcomes in the proof.']
