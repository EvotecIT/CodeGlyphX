"""Project multi-QR costs from qualified packets, including every outcome range."""
import json
from comparison_packet_identity import validate_paired_packet
from comparison_decode_allocations import project_retail_costs


def project_qr_finder_groups(root, qualification):
    proof = qualification.get('QrFinderGroupProof')
    if not proof:
        return None
    previous = qualification['NegativeRotationSources']['previous']
    if (proof['Source'] != qualification['MeasurementSource']
            or proof['CoreSha256'] != qualification['CandidateCoreSha256']
            or proof['PreviousSource'] != previous['Source']
            or proof['PreviousCoreSha256'] != previous['CoreSha256']
            or proof['Contracts'] != {'net10': 4, 'net8': 4}
            or len(proof['Costs']) != 2 or {r['Domain'] for r in proof['Costs']} != {'ccd0', 'ccd1'}):
        raise ValueError('Finder-group proof differs from the qualified cores or contracts')
    costs = []
    for row in proof['Costs']:
        domain = row['Domain']
        if row['Requests'] != 1:
            raise ValueError('Finder-group cost must contain the frozen four-symbol image')
        packet = json.loads((root / f'qr-finder-groups-{domain}-result.json').read_text(encoding='utf-8-sig'))
        validate_paired_packet(packet, proof['Source'], proof['CoreSha256'], proof['PreviousCoreSha256'],
                               domain, 1, row['ManifestSha256'])
        lanes = {r['Engine']: r for r in packet['Summary']}
        before, current = lanes['Baseline'], lanes['Candidate']
        def span(engine, metric):
            values = [s['Metrics'][metric] for s in packet['Samples'] if s['Engine'] == engine and s['Iteration'] >= 0]
            return f'{min(values):g}' if min(values) == max(values) else f'{min(values):g}–{max(values):g}'
        costs.append({'domain': domain.upper(), 'before_ms': f'{before["MedianMs"]:.2f}',
            'current_ms': f'{current["MedianMs"]:.2f}', 'ratio': f'{current["MedianMs"] / before["MedianMs"]:.3f}',
            'before_mib': f'{before["Metrics"]["AllocatedBytes"] / 1048576:.2f}',
            'current_mib': f'{current["Metrics"]["AllocatedBytes"] / 1048576:.2f}',
            **{prefix + '_' + name: span(engine, metric)
               for prefix, engine in (('before', 'Baseline'), ('current', 'Candidate'))
               for name, metric in (('correct', 'CorrectImages'), ('inconclusive', 'InconclusiveImages'), ('unexpected', 'UnexpectedImages'))}})
    controls = {**proof, 'RetailCosts': proof['RetailControlCosts']}
    return {'source': proof['Source'], 'changes': proof['Changes'], 'scope': proof['Scope'], 'limits': proof['Limits'],
        'contracts': 'Four generated-image cases pass on .NET 8 and .NET 10. They cover four versions, quarter-turn rotations, repeated payloads and mixed polarity with tile scanning disabled. Repeated-payload assertions require all four decoded versions.',
        'costs': costs, 'controls': project_retail_costs(root, controls, 'qr-finder-groups-retail')}


def append_qr_finder_group_section(lines, proof):
    if not proof:
        return
    lines += ['', '## Multi-QR finder grouping', '', proof['changes'], '', proof['contracts'], '', proof['scope'], '',
        '| Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Previous → current complete images / 1 | Previous → current inconclusive range | Previous → current unexpected range |',
        '| --- | ---: | ---: | ---: | ---: | ---: | ---: |']
    for row in proof['costs']:
        lines.append(f'| {row["domain"]} | {row["before_ms"]} → {row["current_ms"]} | {row["ratio"]} | {row["before_mib"]} → {row["current_mib"]} | {row["before_correct"]} → {row["current_correct"]} | {row["before_inconclusive"]} → {row["current_inconclusive"]} | {row["before_unexpected"]} → {row["current_unexpected"]} |')
    lines += ['', 'The retail control batches keep their original hints and oracles:', '',
        '| Batch | Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Previous → current correct range | Previous → current inconclusive range | Previous → current unexpected range |',
        '| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |']
    for row in proof['controls']:
        lines.append(f'| {row["kind"]}: {row["requests"]} | {row["domain"]} | {row["before_ms"]} → {row["current_ms"]} | {row["ratio"]} | {row["before_mib"]} → {row["current_mib"]} | {row["before_correct"]} → {row["current_correct"]} | {row["before_inconclusive"]} → {row["current_inconclusive"]} | {row["before_unexpected"]} → {row["current_unexpected"]} |')
    lines += ['', proof['limits'], '',
        '[Finder-group proof](evidence/qr-finder-groups-proof.json) · [CCD0 samples](evidence/qr-finder-groups-ccd0-result.json) · [CCD1 samples](evidence/qr-finder-groups-ccd1-result.json).']
