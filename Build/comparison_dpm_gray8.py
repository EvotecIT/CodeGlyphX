"""Project exact-core grayscale retry evidence and retain completion beside cost."""
import json
from comparison_packet_identity import project_paired_cost
from comparison_qualification_history import resolve_proof_owner
from comparison_decode_allocations import project_retail_costs


def project_dpm_gray8(root, qualification):
    proof, owner, historical = resolve_proof_owner(root, qualification, 'DpmGray8Proof')
    if not proof:
        return None
    previous = owner['NegativeRotationSources']['previous']
    if (proof['Source'] != owner['MeasurementSource']
            or proof['CoreSha256'] != owner['CandidateCoreSha256']
            or proof['PreviousSource'] != previous['Source']
            or proof['PreviousCoreSha256'] != previous['CoreSha256']
            or proof['GrayPixelContracts'] != {'net10': 10, 'net8': 10, 'net472': 10}
            or proof['PerspectiveGrayContracts'] != {'net10': 3, 'net8': 3, 'net472': 3}
            or proof['DpmContracts'] != {'net10': 6, 'net8': 6, 'net472': 6}
            or len(proof['Costs']) != 2 or {r['Domain'] for r in proof['Costs']} != {'ccd0', 'ccd1'}):
        raise ValueError('Grayscale retry proof differs from the qualified binaries or contracts')
    masks = json.loads((root / 'dpm-gray8-mask-equivalence.json').read_text(encoding='utf-8-sig'))
    if (masks['BaselineCoreSha256'] != proof['PreviousCoreSha256']
            or masks['CandidateCoreSha256'] != proof['CoreSha256']
            or masks['Cases'] != 225 or masks['Variants'] != 675
            or masks['IdenticalNormalizedPixelHashesAndOrder'] is not True
            or masks['PreCancelledAndLaterCancelledStopped'] is not True):
        raise ValueError('Retry pixel equivalence belongs to another binary or input set')
    costs = []
    for row in proof['Costs']:
        domain = row['Domain']
        if row['Requests'] != 1:
            raise ValueError('Texture cost must use the single frozen negative image')
        packet = json.loads((root / f'dpm-gray8-texture-{domain}-result.json').read_text(encoding='utf-8-sig'))
        costs.append(project_paired_cost(packet, proof['Source'], proof['CoreSha256'], proof['PreviousCoreSha256'], domain, 1, row['ManifestSha256']))
    return {'historical': historical, 'source': proof['Source'], 'changes': proof['Changes'], 'scope': proof['Scope'], 'limits': proof['Limits'],
        'contracts': ('On that core, on each of' if historical else 'On each of') + ' .NET 10, .NET 8 and .NET Framework 4.7.2, ten raw-pixel cases cover padded Gray8 rows, rotations, mirroring, inversion, GS1 metadata, cancellation and malformed layouts. Three Gray8 perspective cases and six DPM scanner cases also pass. All 675 retries across 225 diagnostic cases preserve the preceding binary’s normalized pixels and order.',
        'costs': costs, 'controls': project_retail_costs(root, {**proof, 'RetailCosts': proof['RetailControlCosts']}, 'dpm-gray8-retail')}


def append_dpm_gray8_section(lines, proof):
    if not proof:
        return
    lines += ['', '## Historical Data Matrix grayscale retries' if proof['historical'] else '## Data Matrix grayscale retries', '',
        f'These grayscale retry measurements belong to source {proof["source"]}; current costs appear separately.' if proof['historical'] else '', '', proof['changes'], '', proof['contracts'], '', proof['scope'], '',
        '| Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Allocation reduction | Previous → current completed negatives / 1 | Previous → current inconclusive range | Previous → current unexpected range |',
        '| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |']
    for row in proof['costs']:
        lines.append(f'| {row["domain"]} | {row["before_ms"]} → {row["current_ms"]} | {row["ratio"]} | {row["before_mib"]} → {row["current_mib"]} | {row["allocation_reduction"]} | {row["before_correct"]} → {row["current_correct"]} | {row["before_inconclusive"]} → {row["current_inconclusive"]} | {row["before_unexpected"]} → {row["current_unexpected"]} |')
    lines += ['', 'Retail controls retain their original requests, hints and oracles:', '',
        '| Batch | Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Previous → current correct range | Previous → current inconclusive range | Previous → current unexpected range |',
        '| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |']
    for row in proof['controls']:
        lines.append(f'| {row["kind"]}: {row["requests"]} | {row["domain"]} | {row["before_ms"]} → {row["current_ms"]} | {row["ratio"]} | {row["before_mib"]} → {row["current_mib"]} | {row["before_correct"]} → {row["current_correct"]} | {row["before_inconclusive"]} → {row["current_inconclusive"]} | {row["before_unexpected"]} → {row["current_unexpected"]} |')
    lines += ['', proof['limits'], '', '[Grayscale retry proof](evidence/dpm-gray8-proof.json) · [Pixel equivalence](evidence/dpm-gray8-mask-equivalence.json) · [CCD0 samples](evidence/dpm-gray8-texture-ccd0-result.json) · [CCD1 samples](evidence/dpm-gray8-texture-ccd1-result.json).']
