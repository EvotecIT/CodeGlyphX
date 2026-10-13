"""Project bounded local-window costs with exact pixel and binary identities."""
import json
from comparison_packet_identity import project_paired_cost
from comparison_decode_allocations import project_retail_costs


def project_dpm_window_means(root, qualification):
    proof = qualification.get('DpmWindowMeansProof')
    if not proof:
        return None
    previous = qualification['NegativeRotationSources']['previous']
    if (proof['Source'] != qualification['MeasurementSource']
            or proof['CoreSha256'] != qualification['CandidateCoreSha256']
            or proof['PreviousSource'] != previous['Source']
            or proof['PreviousCoreSha256'] != previous['CoreSha256']
            or proof['WindowContracts'] != {'net10': 9, 'net8': 9, 'net472': 9}
            or proof['DpmContracts'] != {'net10': 6, 'net8': 6, 'net472': 6}
            or len(proof['Costs']) != 4
            or {(r['Kind'], r['Domain']) for r in proof['Costs']} !=
               {(kind, domain) for kind in ('fabric', 'texture') for domain in ('ccd0', 'ccd1')}):
        raise ValueError('Local-window proof differs from the qualified binaries or contracts')
    masks = json.loads((root / 'dpm-window-means-mask-equivalence.json').read_text(encoding='utf-8-sig'))
    if (masks != proof['MaskEquivalence']
            or masks['BaselineCoreSha256'] != proof['PreviousCoreSha256']
            or masks['CandidateCoreSha256'] != proof['CoreSha256']
            or masks['Cases'] != 675 or masks['Variants'] != 2025
            or masks['IdenticalNormalizedPixelHashesAndOrder'] is not True
            or masks['PreCancelledAndLaterCancelledStopped'] is not True):
        raise ValueError('Local-window retry pixels belong to another binary or input set')
    costs = []
    for row in proof['Costs']:
        if row['Requests'] != 1:
            raise ValueError('Local-window cost requires a single frozen negative')
        packet = json.loads((root / f'dpm-window-means-{row["Kind"]}-{row["Domain"]}-result.json').read_text(encoding='utf-8-sig'))
        costs.append({'kind': row['Kind'], **project_paired_cost(packet, proof['Source'], proof['CoreSha256'],
            proof['PreviousCoreSha256'], row['Domain'], 1, row['ManifestSha256'])})
    return {'source': proof['Source'], 'changes': proof['Changes'], 'scope': proof['Scope'], 'limits': proof['Limits'],
        'contracts': 'Nine local-window cases and six DPM scanner cases pass on each of .NET 10, .NET 8 and .NET Framework 4.7.2. Window cases compare both adaptive polarities with output derived from independent clipped-neighborhood enumeration, including narrow images, radii larger than the image, clipped edges and a full-resolution boundary. All 2,025 retries across 675 diagnostic cases preserve the preceding binary’s normalized pixels and order.',
        'costs': costs, 'controls': project_retail_costs(root, {**proof, 'RetailCosts': proof['RetailControlCosts']}, 'dpm-window-means-retail')}


def append_dpm_window_means_section(lines, proof):
    if not proof:
        return
    lines += ['', '## DPM local-window means', '', proof['changes'], '', proof['contracts'], '', proof['scope'], '',
        '| Image | Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Allocation reduction | Previous → current completed negatives / 1 | Previous → current inconclusive range | Previous → current unexpected range |',
        '| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |']
    for row in proof['costs']:
        lines.append(f'| {row["kind"]} | {row["domain"]} | {row["before_ms"]} → {row["current_ms"]} | {row["ratio"]} | {row["before_mib"]} → {row["current_mib"]} | {row["allocation_reduction"]} | {row["before_correct"]} → {row["current_correct"]} | {row["before_inconclusive"]} → {row["current_inconclusive"]} | {row["before_unexpected"]} → {row["current_unexpected"]} |')
    lines += ['', 'Retail controls retain their original requests, hints and oracles:', '',
        '| Batch | Domain | Previous → current median ms | Current / previous | Previous → current mean managed MiB | Previous → current correct range | Previous → current inconclusive range | Previous → current unexpected range |',
        '| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |']
    for row in proof['controls']:
        lines.append(f'| {row["kind"]}: {row["requests"]} | {row["domain"]} | {row["before_ms"]} → {row["current_ms"]} | {row["ratio"]} | {row["before_mib"]} → {row["current_mib"]} | {row["before_correct"]} → {row["current_correct"]} | {row["before_inconclusive"]} → {row["current_inconclusive"]} | {row["before_unexpected"]} → {row["current_unexpected"]} |')
    lines += ['', proof['limits'], '', '[Local-window proof](evidence/dpm-window-means-proof.json) · [Pixel equivalence](evidence/dpm-window-means-mask-equivalence.json) · [Fabric CCD0 samples](evidence/dpm-window-means-fabric-ccd0-result.json) · [Fabric CCD1 samples](evidence/dpm-window-means-fabric-ccd1-result.json) · [Texture CCD0 samples](evidence/dpm-window-means-texture-ccd0-result.json) · [Texture CCD1 samples](evidence/dpm-window-means-texture-ccd1-result.json).']
