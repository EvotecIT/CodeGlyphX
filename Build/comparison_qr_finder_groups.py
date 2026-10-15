"""Project multi-QR costs from qualified packets, including every outcome range."""
import json
from comparison_packet_identity import project_paired_cost
from comparison_qualification_history import resolve_proof_owner
from comparison_decode_allocations import project_retail_costs


def project_qr_finder_groups(root, qualification):
    proof, owner, historical = resolve_proof_owner(root, qualification, 'QrFinderGroupProof')
    if not proof:
        return None
    previous = owner['NegativeRotationSources']['previous']
    if (proof['Source'] != owner['MeasurementSource']
            or proof['CoreSha256'] != owner['CandidateCoreSha256']
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
        costs.append(project_paired_cost(packet, proof['Source'], proof['CoreSha256'], proof['PreviousCoreSha256'], domain, 1, row['ManifestSha256']))
    controls = {**proof, 'RetailCosts': proof['RetailControlCosts']}
    return {'historical': historical, 'source': proof['Source'], 'changes': proof['Changes'], 'scope': proof['Scope'], 'limits': proof['Limits'],
        'contracts': ('On that core, four' if historical else 'Four') + ' generated-image cases pass on .NET 8 and .NET 10. They cover four versions, quarter-turn rotations, repeated payloads and mixed polarity with tile scanning disabled. Repeated-payload assertions require all four decoded versions.',
        'costs': costs, 'controls': project_retail_costs(root, controls, 'qr-finder-groups-retail')}


def append_qr_finder_group_section(lines, proof):
    if not proof:
        return
    lines += ['', '## Historical multi-QR finder grouping' if proof['historical'] else '## Multi-QR finder grouping', '',
        f'These finder-group measurements belong to source {proof["source"]}; current costs appear separately.' if proof['historical'] else '', '', proof['changes'], '', proof['contracts'], '', proof['scope'], '',
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
