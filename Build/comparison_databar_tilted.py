"""Project the current tilted DataBar recovery and paired costs for both public views."""
import json


def project_databar_tilted(root, qualification):
    proof = qualification.get('DataBarTiltedSeparatorProof')
    if not proof:
        return None
    if (proof['Source'] != qualification['MeasurementSource']
            or proof['CoreSha256'] != qualification['CandidateCoreSha256']):
        raise ValueError('Tilted DataBar proof differs from the qualified core')
    costs = []
    for kind, requests in (('positive', 12), ('negative', 31)):
        for domain in ('ccd0', 'ccd1'):
            packet = json.loads((root / f'databar-tilted-{kind}-{domain}-result.json').read_text(encoding='utf-8-sig'))
            metadata = packet['Metadata']
            if (metadata['benchmark.CandidateSource'] != proof['Source']
                    or metadata['benchmark.CandidateCoreSha256'] != proof['CoreSha256']
                    or metadata['benchmark.BaselineCoreSha256'] != proof['PreviousCoreSha256']):
                raise ValueError('Tilted DataBar cost binary identity differs')
            lanes = {r['Engine']: r for r in packet['Summary']}
            if set(lanes) != {'Baseline', 'Candidate'} or any(
                    r['Status'] != 'Succeeded' or r['FailureCount'] or r['SampleCount'] != 12
                    for r in lanes.values()):
                raise ValueError('Incomplete tilted DataBar cost evidence')
            before, now = lanes['Baseline'], lanes['Candidate']
            row = {'kind': kind.title(), 'requests': requests, 'domain': domain.upper(),
                   'before_ms': f'{before["MedianMs"]:.2f}', 'current_ms': f'{now["MedianMs"]:.2f}',
                   'ratio': f'{now["MedianMs"] / before["MedianMs"]:.3f}',
                   'before_mib': f'{before["Metrics"]["AllocatedBytes"] / 1048576:.2f}',
                   'current_mib': f'{now["Metrics"]["AllocatedBytes"] / 1048576:.2f}'}
            for engine, prefix in (('Baseline', 'before'), ('Candidate', 'current')):
                samples = [r for r in packet['Samples'] if r['Engine'] == engine and r['Iteration'] >= 0]
                if len(samples) != 12 or any(r['Metrics']['Requests'] != requests for r in samples):
                    raise ValueError('Incomplete tilted DataBar request samples')
                for metric, name in (('CorrectImages', 'correct'), ('InconclusiveImages', 'inconclusive')):
                    values = [r['Metrics'][metric] for r in samples]
                    row[f'{prefix}_{name}'] = f'{min(values):g}–{max(values):g}' if min(values) != max(values) else f'{min(values):g}'
            costs.append(row)
    return {'changes': proof['Changes'], 'scope': proof['Scope'], 'limits': proof['Limits'],
            'cost_scope': proof['CostScope'], 'costs': costs,
            'engines': [{'engine': name, 'before': r['PreviousCorrect'], 'correct': r['Correct'], 'unexpected': r['Unexpected']}
                        for name, r in proof['Engines'].items()]}
