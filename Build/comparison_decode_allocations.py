"""Project exact-core allocation measurements without hiding completion losses."""
import json
from comparison_packet_identity import validate_paired_packet
from comparison_qualification_history import resolve_proof_owner


def project_decode_allocations(root, qualification):
    proof, owner, historical = resolve_proof_owner(root, qualification, 'DecodeAllocationProof')
    if not proof:
        return None
    if (proof['Source'] != owner['MeasurementSource']
            or proof['CoreSha256'] != owner['CandidateCoreSha256']
            or proof['PreviousSource'] != owner['NegativeRotationSources']['previous']['Source']
            or proof['PreviousCoreSha256'] != owner['NegativeRotationSources']['previous']['CoreSha256']
            or proof['RetainedScannerContracts'] != {'net10': 58, 'net8': 58, 'net472': 58}
            or proof['CallerOwnershipContracts'] != {'net10': 4, 'net8': 4, 'net472': 4}):
        raise ValueError('Allocation proof differs from the qualified core or regression contracts')
    rows = project_retail_costs(root, proof, 'decode-allocation')
    return {'historical': historical, 'source': proof['Source'], 'changes': proof['Changes'], 'scope': proof['Scope'], 'limits': proof['Limits'], 'costs': rows,
        'contracts': ('On that core, all 58' if historical else 'All 58') + ' retained scanner-margin contracts and four caller-ownership tests pass on each supported test framework. Caller modules stay unchanged through padding, inversion, reversal and repeated decoding.',
        'profile_scope': proof['ProfileScope']}


def project_retail_costs(root, proof, prefix):
    """Validate both fixed domains and project every measured outcome range."""
    if len(proof['RetailCosts']) != 4 or {(r['Kind'], r['Domain']) for r in proof['RetailCosts']} != {
            (kind, domain) for kind in ('positive', 'negative') for domain in ('ccd0', 'ccd1')}:
        raise ValueError('Incomplete allocation cost groups')
    rows = []
    for row in proof['RetailCosts']:
        kind, domain = row['Kind'], row['Domain']
        if row['Requests'] != (87 if kind == 'positive' else 9):
            raise ValueError('Retail request count differs from the frozen batch')
        packet = json.loads((root / f'{prefix}-{kind}-{domain}-result.json').read_text(encoding='utf-8-sig'))
        validate_paired_packet(packet, proof['Source'], proof['CoreSha256'], proof['PreviousCoreSha256'],
                              domain, row['Requests'], row['ManifestSha256'])
        lanes = {r['Engine']: r for r in packet['Summary']}
        before, current = lanes['Baseline'], lanes['Candidate']
        def span(engine, metric):
            values = [s['Metrics'][metric] for s in packet['Samples'] if s['Engine'] == engine and s['Iteration'] >= 0]
            return f'{min(values):g}' if min(values) == max(values) else f'{min(values):g}–{max(values):g}'
        rows.append({'kind': kind.title(), 'requests': row['Requests'], 'domain': domain.upper(),
            'before_ms': f'{before["MedianMs"]:.2f}', 'current_ms': f'{current["MedianMs"]:.2f}',
            'ratio': f'{current["MedianMs"] / before["MedianMs"]:.3f}',
            'before_mib': f'{before["Metrics"]["AllocatedBytes"] / 1048576:.2f}',
            'current_mib': f'{current["Metrics"]["AllocatedBytes"] / 1048576:.2f}',
            'allocation_reduction': f'{(1 - current["Metrics"]["AllocatedBytes"] / before["Metrics"]["AllocatedBytes"]) * 100:.1f}%',
            **{prefix + '_' + name: span(engine, metric)
               for prefix, engine in (('before','Baseline'), ('current','Candidate'))
               for name, metric in (('correct','CorrectImages'), ('inconclusive','InconclusiveImages'), ('unexpected','UnexpectedImages'))}})
    return rows

def append_decode_allocation_section(lines, proof):
    if not proof:
        return
    lines += ['', '## Historical linear decoder allocations' if proof['historical'] else '## Linear decoder allocations', '',
        f'These allocation measurements and regression contracts belong to source {proof["source"]}; current decoding costs appear separately.' if proof['historical'] else '', '', proof['changes'], '', proof['contracts'], '', proof['scope'], '',
        '| Batch | Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Allocation reduction | Previous → current correct range | Previous → current inconclusive range | Previous → current unexpected range |',
        '| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |']
    for row in proof['costs']:
        lines.append(f'| {row["kind"]}: {row["requests"]} | {row["domain"]} | {row["before_ms"]} → {row["current_ms"]} | {row["ratio"]} | {row["before_mib"]} → {row["current_mib"]} | {row["allocation_reduction"]} | {row["before_correct"]} → {row["current_correct"]} | {row["before_inconclusive"]} → {row["current_inconclusive"]} | {row["before_unexpected"]} → {row["current_unexpected"]} |')
    lines += ['', proof['limits'], '', proof['profile_scope'], '',
        '[Allocation and regression proof](evidence/decode-allocation-proof.json) · [Positive CCD0 samples](evidence/decode-allocation-positive-ccd0-result.json) · [Positive CCD1 samples](evidence/decode-allocation-positive-ccd1-result.json) · [Negative CCD0 samples](evidence/decode-allocation-negative-ccd0-result.json) · [Negative CCD1 samples](evidence/decode-allocation-negative-ccd1-result.json).']
