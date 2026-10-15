"""Project qualified deadline costs with recognition and completion alongside timing."""
from comparison_decode_allocations import project_retail_costs
from comparison_qualification_history import resolve_proof_owner


def project_decode_deadlines(root, qualification):
    proof, owner, historical = resolve_proof_owner(root, qualification, 'DecodeDeadlineProof')
    if not proof:
        return None
    previous = owner['NegativeRotationSources']['previous']
    if (proof['Source'] != owner['MeasurementSource']
            or proof['CoreSha256'] != owner['CandidateCoreSha256']
            or proof['PreviousSource'] != previous['Source']
            or proof['PreviousCoreSha256'] != previous['CoreSha256']
            or proof['RetainedScannerContracts'] != {'net10': 58, 'net8': 58, 'net472': 58}
            or proof['CallerOwnershipContracts'] != {'net10': 4, 'net8': 4, 'net472': 4}
            or proof['DeadlineContracts'] != {'net10': 2, 'net8': 2, 'net472': 2}):
        raise ValueError('Deadline proof differs from the qualified core or regression contracts')
    return {'historical': historical, 'source': proof['Source'], 'changes': proof['Changes'], 'scope': proof['Scope'],
        'limits': proof['Limits'], 'costs': project_retail_costs(root, proof, 'decode-deadline'),
        'contracts': ('On that core, two' if historical else 'Two') + ' deadline-snapshot tests, all 58 retained scanner-margin contracts and four caller-ownership tests pass on each supported test framework. Captured deadlines still observe clock expiry and later caller cancellation.',
        'profile_scope': proof['ProfileScope']}


def append_decode_deadline_section(lines, proof):
    if not proof:
        return
    lines += ['', '## Historical recognition deadline polling' if proof['historical'] else '## Recognition deadline polling', '',
        f'These deadline measurements and regression contracts belong to source {proof["source"]}; current decoding costs appear separately.' if proof['historical'] else '', '', proof['changes'], '', proof['contracts'], '', proof['scope'], '',
        '| Batch | Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Previous → current correct range | Previous → current inconclusive range | Previous → current unexpected range |',
        '| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |']
    for row in proof['costs']:
        lines.append(f'| {row["kind"]}: {row["requests"]} | {row["domain"]} | {row["before_ms"]} → {row["current_ms"]} | {row["ratio"]} | {row["before_mib"]} → {row["current_mib"]} | {row["before_correct"]} → {row["current_correct"]} | {row["before_inconclusive"]} → {row["current_inconclusive"]} | {row["before_unexpected"]} → {row["current_unexpected"]} |')
    lines += ['', proof['limits'], '', proof['profile_scope'], '',
        '[Deadline and regression proof](evidence/decode-deadline-proof.json) · [Positive CCD0 samples](evidence/decode-deadline-positive-ccd0-result.json) · [Positive CCD1 samples](evidence/decode-deadline-positive-ccd1-result.json) · [Negative CCD0 samples](evidence/decode-deadline-negative-ccd0-result.json) · [Negative CCD1 samples](evidence/decode-deadline-negative-ccd1-result.json).']
