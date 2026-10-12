"""Project qualified physical-margin regressions into the report and website."""
import json


def project_scanner_boundaries(root, qualification):
    proof = qualification.get('ScannerBoundaryProof')
    if not proof:
        return None
    if (proof['Source'] != qualification['MeasurementSource']
            or proof['CoreSha256'] != qualification['CandidateCoreSha256']
            or proof['CurrentCoreProbe']['CoreSha256'] != proof['CoreSha256']):
        raise ValueError('Scanner boundary proof differs from the qualified core')
    if (proof['BaselineCases'] != 58 or proof['BaselinePassed'] != 27
            or proof['BaselineFailed'] != 31
            or proof['CurrentTargetedCases'] != {'net10': 58, 'net8': 58, 'net472': 58}
            or sum(r['Cases'] for r in proof['Contracts']) != 58
            or sum(r['PreviousPassed'] for r in proof['Contracts']) != 27
            or any(r['CurrentPassed'] != r['Cases'] for r in proof['Contracts'])):
        raise ValueError('Incomplete scanner boundary contract evidence')
    labels = {
        'Scan_CompleteSingleDigitSymbols_PreservesTypedAndDefaultTileResults': 'Industrial single-digit symbols',
        'TransparentTileMargins_PreserveBothOriginalPayloads': 'Neighboring symbols and transparent margins',
        'PharmacodePixelRoundedWideBars_PreserveCompletePayload': 'Pixel-rounded Pharmacode bars',
        'PharmacodeWidthRatios_PreserveCompletePayload': 'Pharmacode bar-width ratios',
        'PharmacodeWithoutQuietMargin_PreservesCompletePayload': 'Pharmacode tight and one-sided crops',
    }
    costs = []
    if {(row['Kind'], row['Domain']) for row in proof['RetailCosts']} != {
            (kind, domain) for kind in ('positive', 'negative') for domain in ('ccd0', 'ccd1')} or len(proof['RetailCosts']) != 4:
        raise ValueError('Incomplete scanner retail cost groups')
    for row in proof['RetailCosts']:
        domain, kind = row['Domain'], row['Kind']
        packet = json.loads((root / f'scanner-boundary-{kind}-{domain}-result.json').read_text(encoding='utf-8-sig'))
        meta = packet['Metadata']
        if (meta['benchmark.CandidateSource'] != proof['Source']
                or meta['benchmark.CandidateCoreSha256'] != proof['CoreSha256']
                or meta['benchmark.BaselineCoreSha256'] != proof['PreviousCoreSha256']
                or meta['processAffinityMask'] != ('0xFFFF' if domain == 'ccd0' else '0xFFFF0000')
                or meta['processPriority'] != 'Normal'
                or len(packet['Summary']) != 2
                or {r['Engine'] for r in packet['Summary']} != {'Baseline', 'Candidate'}
                or any(r['Status'] != 'Succeeded' or r['FailureCount'] or r['SampleCount'] != 12 for r in packet['Summary'])):
            raise ValueError('Scanner retail cost identity, placement or samples differ')
        def span(name):
            values = row[name + 'Range']
            return f'{values[0]:g}' if values[0] == values[1] else f'{values[0]:g}–{values[1]:g}'
        costs.append({'kind': kind.title(), 'requests': row['Requests'], 'domain': domain.upper(),
            'before_ms': f'{row["BaselineMedianMs"]:.2f}', 'current_ms': f'{row["CurrentMedianMs"]:.2f}',
            'ratio': f'{row["LatencyRatio"]:.3f}',
            'before_mib': f'{row["BaselineAllocatedBytes"] / 1048576:.2f}', 'current_mib': f'{row["CurrentAllocatedBytes"] / 1048576:.2f}',
            'before_correct': span('PreviousCorrectImages'), 'current_correct': span('CurrentCorrectImages'),
            'before_inconclusive': span('PreviousInconclusiveImages'), 'current_inconclusive': span('CurrentInconclusiveImages'),
            'before_unexpected': span('PreviousUnexpectedImages'), 'current_unexpected': span('CurrentUnexpectedImages')})
    return {'changes': proof['Changes'], 'scope': proof['Scope'], 'limits': proof['Limits'], 'cost_scope': proof['CostScope'],
            'contracts': [{'name': labels[r['Contract']], 'cases': r['Cases'], 'before': r['PreviousPassed'], 'current': r['CurrentPassed']} for r in proof['Contracts']],
            'before': proof['BaselinePassed'], 'current': 58, 'cases': 58, 'costs': costs}


def append_scanner_boundary_section(lines, proof):
    if not proof:
        return
    lines += ['', '## Linear barcode margins and neighboring symbols', '', proof['changes'], '', proof['scope'], '',
              '| Targeted contract | Previous passed | Current passed | Cases |', '| --- | ---: | ---: | ---: |']
    for row in proof['contracts']:
        lines.append(f'| {row["name"]} | {row["before"]} | {row["current"]} | {row["cases"]} |')
    lines += ['', proof['cost_scope'], '',
              '| Batch | Domain | Previous → current median ms | Current / previous | Previous → current correct range | Previous → current inconclusive range | Previous → current unexpected range | Previous → current mean managed MiB |',
              '| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |']
    for row in proof['costs']:
        lines.append(f'| {row["kind"]}: {row["requests"]} | {row["domain"]} | {row["before_ms"]} → {row["current_ms"]} | {row["ratio"]} | {row["before_correct"]} → {row["current_correct"]} | {row["before_inconclusive"]} → {row["current_inconclusive"]} | {row["before_unexpected"]} → {row["current_unexpected"]} | {row["before_mib"]} → {row["current_mib"]} |')
    lines += ['', proof['limits'], '', '[Regression and binary proof](evidence/scanner-boundary-proof.json) · [Exact-core diagnostics](evidence/scanner-final-core-probes.json) · [Current typed retail negatives](evidence/scanner-retail-negative-diagnostic.json) · [Positive CCD0 samples](evidence/scanner-boundary-positive-ccd0-result.json) · [Positive CCD1 samples](evidence/scanner-boundary-positive-ccd1-result.json) · [Negative CCD0 samples](evidence/scanner-boundary-negative-ccd0-result.json) · [Negative CCD1 samples](evidence/scanner-boundary-negative-ccd1-result.json).']
